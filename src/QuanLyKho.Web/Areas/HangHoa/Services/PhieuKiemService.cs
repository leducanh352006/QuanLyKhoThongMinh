using Microsoft.Extensions.Logging.Abstractions;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>
/// Nghiệp vụ tạo phiếu kiểm (việc T3-4, Bảng 2.4 dòng 1 đến 3). Tạo phiếu là bắt đầu kiểm ngay, theo đúng thứ tự:
/// (a) xác định các ô thuộc phạm vi, (b) khóa hết các ô đó qua <see cref="ILockService"/>, (c) SAU KHI khóa xong mới chụp
/// số hệ thống qua <see cref="IStockService.LayTonTheoViTri"/>, (d) ghi phiếu. Bất kỳ bước nào lỗi sau khi đã khóa thì mở lại
/// toàn bộ ô vừa khóa, không để kho kẹt vì ô khóa mà không có phiếu.
/// <para>Phạm vi ô: Toàn bộ khóa mọi ô Lưu trữ của kho, kể cả ô trống (để trong lúc kiểm không ai xếp hàng mới vào);
/// Một phần khóa các ô Lưu trữ đang chứa mặt hàng đã chọn. Khu Tiếp nhận không thuộc phạm vi vì hàng ở đó chưa kiểm, chưa xếp.</para>
/// </summary>
public sealed class PhieuKiemService(
    IPhieuKiemStore store, ILockService khoa, IStockService ton, IAuditService audit, ILogger<PhieuKiemService>? log = null)
{
    /// <summary>Số phiếu tối đa hiện trên danh sách.</summary>
    public const int ToiDa = 200;

    private readonly ILogger _log = (ILogger?)log ?? NullLogger.Instance;

    public Task<IReadOnlyList<PhieuKiemDong>> DanhSach(CancellationToken ct = default) => store.DanhSach(ToiDa, ct);

    public Task<PhieuKiemXem?> Xem(int maPK, CancellationToken ct = default) => store.Xem(maPK, ct);

    public Task<DanhMucChonPhieuKiem> LayDanhMucChon(CancellationToken ct = default) => store.LayDanhMucChon(ct);

    /// <summary>Tạo phiếu và bắt đầu kiểm. Thành công thì trả phiếu đã có MaPK và số phiếu KK, trạng thái Đang kiểm.</summary>
    public async Task<KetQua<PhieuKiem>> Tao(TaoPhieuKiemVm vm, int maND, CancellationToken ct = default)
    {
        var loi = vm.KiemTra() ?? await KiemTraDanhMuc(vm, ct);
        if (loi is not null) return KetQua<PhieuKiem>.Loi(loi, MaLoiDichVu.DuLieuKhongHopLe);

        var laMotPhan = vm.LoaiKiemKe == PhieuKiem.MotPhan;
        var phamVi = vm.PhamViMaBT!.ToHashSet();

        // (a) Xác định các ô cần khóa.
        var oCanKhoa = await store.LayOLuuTru(vm.MaKho, ct);
        if (oCanKhoa.Count == 0)
            return KetQua<PhieuKiem>.Loi("Kho này chưa có ô lưu trữ nào để kiểm.", MaLoiDichVu.DuLieuKhongHopLe);
        if (laMotPhan)
        {
            // Dò trước để biết ô nào đang chứa mặt hàng đã chọn. Đây CHƯA phải số hệ thống của phiếu: số đó chụp lại sau khi khóa.
            var doTruoc = await DocDongTon(oCanKhoa, ct);
            oCanKhoa = doTruoc.Where(d => phamVi.Contains(d.MaBT)).Select(d => d.MaViTri).Distinct().ToList();
            if (oCanKhoa.Count == 0)
                return KetQua<PhieuKiem>.Loi("Không có tồn kho nào của mặt hàng đã chọn tại các ô lưu trữ của kho này.", MaLoiDichVu.DuLieuKhongHopLe);
        }

        var daKhoa = new List<int>();
        PhieuKiem phieu;
        try
        {
            // (b) Khóa từng ô; một ô không khóa được (đang khóa cho phiếu kiểm khác, còn hàng giữ chỗ...) thì hủy cả lượt.
            foreach (var maViTri in oCanKhoa)
            {
                var kq = await khoa.KhoaO(maViTri, maND, ct);
                if (!kq.ThanhCong)
                {
                    var chuaMo = await MoLai(daKhoa, maND);
                    return KetQua<PhieuKiem>.Loi((kq.ThongBao ?? "Không khóa được ô để kiểm.") + CanhBaoChuaMo(chuaMo), kq.MaLoi);
                }
                daKhoa.Add(maViTri);
            }

            // (c) Các ô đã khóa nên tồn không còn đổi: chụp số hệ thống tại thời điểm bắt đầu kiểm.
            var dong = await DocDongTon(daKhoa, ct);
            if (laMotPhan)
            {
                dong = dong.Where(d => phamVi.Contains(d.MaBT)).ToList();
                if (dong.Count == 0)
                {
                    var chuaMo = await MoLai(daKhoa, maND);
                    return KetQua<PhieuKiem>.Loi(
                        "Tồn của mặt hàng đã chọn vừa thay đổi, không còn dòng nào trong phạm vi kiểm. Hãy tạo lại phiếu." + CanhBaoChuaMo(chuaMo),
                        MaLoiDichVu.DuLieuKhongHopLe);
                }
            }

            // (d) Ghi phiếu. Kiểm Toàn bộ kho đang trống vẫn tạo được phiếu (không có dòng số hệ thống) để ghi nhận hàng thừa nếu có.
            phieu = new PhieuKiem
            {
                MaKho = vm.MaKho, LoaiKiemKe = vm.LoaiKiemKe!, ThamChieu = vm.ThamChieu, DienGiai = vm.DienGiai,
                ThoiGianBatDau = DateTime.UtcNow, MaNguoiLap = maND, TrangThai = PhieuKiem.DangKiem,
                Dong = dong.Select(d => new DongKiem { MaLo = d.MaLo, MaViTri = d.MaViTri, SLHeThong = d.SoLuong }).ToList(),
                ThamGia = vm.ThamGia!.Select(t => new ThamGiaKiem { MaND = t!.MaND, GhiChu = t.GhiChu }).ToList(),
            };
            await store.Luu(phieu, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Tạo phiếu kiểm cho kho {MaKho} lỗi sau khi đã khóa {SoO} ô; đang mở lại các ô.", vm.MaKho, daKhoa.Count);
            var chuaMo = await MoLai(daKhoa, maND);
            return KetQua<PhieuKiem>.Loi("Không lưu được phiếu kiểm, các ô vừa khóa đã được mở lại. Hãy thử lại." + CanhBaoChuaMo(chuaMo));
        }

        // Nhật ký ghi sau khi phiếu đã lưu: nhật ký lỗi thì phiếu và các ô khóa vẫn đúng trạng thái, không hoàn tác.
        try
        {
            await audit.Ghi(maND, "Tạo phiếu kiểm", LoaiChungTu.KK, phieu.MaPK,
                $"{phieu.SoPhieu}: {phieu.LoaiKiemKe}, khóa {daKhoa.Count} ô, {phieu.Dong.Count} dòng số hệ thống", ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Đã tạo phiếu kiểm {SoPhieu} nhưng không ghi được nhật ký.", phieu.SoPhieu);
        }
        return KetQua<PhieuKiem>.Ok(phieu);
    }

    /// <summary>Kho còn hoạt động; người kiểm là nhân viên có trong danh mục và còn hoạt động; mặt hàng trong phạm vi có thật.</summary>
    private async Task<string?> KiemTraDanhMuc(TaoPhieuKiemVm vm, CancellationToken ct)
    {
        if (!await store.KhoHoatDong(vm.MaKho, ct)) return "Kho kiểm kê không tồn tại hoặc đã ngừng hoạt động.";

        var maND = vm.ThamGia!.Select(t => t!.MaND).ToList();
        if (await store.DemNguoiHoatDong(maND, ct) != maND.Count) return "Người tham gia kiểm phải là nhân viên có trong danh mục và còn hoạt động.";

        if (vm.PhamViMaBT!.Count > 0 && await store.DemBienThe(vm.PhamViMaBT, ct) != vm.PhamViMaBT.Count)
            return "Có mặt hàng trong phạm vi kiểm không tồn tại.";
        return null;
    }

    private sealed record DongTonHeThong(int MaBT, int MaLo, int MaViTri, int SoLuong);

    /// <summary>Đọc tồn của từng ô qua IStockService (không đọc thẳng bảng TonKho), kèm biến thể của từng lô.</summary>
    private async Task<List<DongTonHeThong>> DocDongTon(IReadOnlyCollection<int> maViTri, CancellationToken ct)
    {
        var tonTheoO = new List<TonKhoDto>();
        foreach (var o in maViTri) tonTheoO.AddRange(await ton.LayTonTheoViTri(o, ct));
        tonTheoO.RemoveAll(t => t.SoLuongTon <= 0);
        if (tonTheoO.Count == 0) return new();

        var bienTheCuaLo = await store.LayBienTheCuaLo(tonTheoO.Select(t => t.MaLo).Distinct().ToList(), ct);
        return tonTheoO.Select(t => new DongTonHeThong(
            bienTheCuaLo.TryGetValue(t.MaLo, out var maBT) ? maBT : throw new InvalidOperationException($"Không tìm thấy lô {t.MaLo} trong danh mục lô hàng."),
            t.MaLo, t.MaViTri, t.SoLuongTon)).ToList();
    }

    /// <summary>Mở lại các ô đã khóa trong lượt này; trả về các ô chưa mở được (để báo người dùng mở tay).</summary>
    private async Task<List<int>> MoLai(IReadOnlyCollection<int> daKhoa, int maND)
    {
        var chuaMo = new List<int>();
        foreach (var maViTri in daKhoa)
        {
            try
            {
                // Không truyền CancellationToken của yêu cầu: người dùng đóng trình duyệt giữa chừng vẫn phải mở hết ô.
                var kq = await khoa.MoO(maViTri, maND, CancellationToken.None);
                if (kq.ThanhCong) continue;
                _log.LogError("Không mở lại được ô {MaViTri}: {ThongBao}", maViTri, kq.ThongBao);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Không mở lại được ô {MaViTri}.", maViTri);
            }
            chuaMo.Add(maViTri);
        }
        return chuaMo;
    }

    private static string CanhBaoChuaMo(IReadOnlyCollection<int> chuaMo) => chuaMo.Count == 0
        ? ""
        : $" Lưu ý: {chuaMo.Count} ô (mã {string.Join(", ", chuaMo)}) chưa mở khóa lại được, cần mở tay ở Sơ đồ vị trí kho.";
}
