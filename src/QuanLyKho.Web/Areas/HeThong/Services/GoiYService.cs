using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Đánh dấu handler mẫu (chỉ để thử, Development). Khi nhóm sở hữu đăng ký handler thật cho cùng loại, khung luôn dùng handler thật.</summary>
public interface IGoiYHandlerMau : IGoiYHandler
{
    /// <summary>false thì khung coi như không có handler này (ví dụ ngoài môi trường Development).</summary>
    bool DangBat { get; }
}

/// <summary>Phần tra cứu thêm cho màn hình của Area HeThong (ngoài <see cref="IGoiYService"/> dùng chung).</summary>
public interface IGoiYQuanLy
{
    /// <summary>Gợi ý theo trạng thái và loại; null là tất cả. Chờ duyệt thì cũ trước, còn lại mới trước.</summary>
    Task<IReadOnlyList<GoiYDto>> DanhSach(TrangThaiGoiY? trangThai, LoaiGoiY? loai, int toiDa = 100, CancellationToken ct = default);

    /// <summary>Dòng gốc của AI (để so với dòng người duyệt đã sửa).</summary>
    Task<IReadOnlyList<ChiTietGoiYDto>> LayChiTietGoc(int maGoiY, CancellationToken ct = default);

    /// <summary>Đã có handler cho loại gợi ý này chưa (để giao diện báo trước "chưa nối thao tác, hãy làm tay").</summary>
    bool CoXuLy(LoaiGoiY loai);
}

/// <summary>
/// Bản thật của <see cref="IGoiYService"/> (việc T4-8), thay <see cref="Contracts.Stubs.GoiYServiceStub"/> mà không đổi interface.
/// <para>Vòng đời (báo cáo mục 1.2.4): AI tạo gợi ý Chờ duyệt; người có thẩm quyền (<see cref="QuyTacGoiY.VaiTroDuocDuyet"/>) duyệt, sửa rồi duyệt
/// hoặc từ chối; khi duyệt, khung gọi <see cref="IGoiYHandler"/> của nhóm sở hữu để tạo thao tác thay người dùng.</para>
/// <para>Đổi trạng thái và handler chạy trong <b>một giao dịch</b>: lệnh đổi trạng thái có điều kiện "đang Chờ duyệt" nên hai người duyệt
/// cùng lúc thì chỉ một người thắng (người kia chờ khóa dòng rồi nhận "đã xử lý"), handler không bao giờ chạy hai lần.
/// Handler thất bại hoặc ném lỗi thì mọi thứ hoàn tác, gợi ý vẫn Chờ duyệt và người dùng làm tay được.</para>
/// </summary>
public sealed class GoiYService(
    IGoiYStore store, IEnumerable<IGoiYHandler> handlers, IHeThongStore nguoiDung, IAuditService audit, TimeProvider gio,
    ILogger<GoiYService>? log = null) : IGoiYService, IGoiYQuanLy
{
    private readonly IReadOnlyList<IGoiYHandler> _handlers = handlers.ToList();

    public async Task<KetQua<int>> Tao(LoaiGoiY loai, ThamChieuGoiY thamChieu, IReadOnlyList<ChiTietGoiYDto> chiTiet, CancellationToken ct = default)
    {
        var loi = QuyTacGoiY.KiemTraThamChieuKhiTao(loai, thamChieu) ?? QuyTacGoiY.KiemTraChiTiet(chiTiet);
        if (loi is not null) return KetQua<int>.Loi(loi, MaLoiDichVu.DuLieuKhongHopLe);
        return await store.Them(loai, thamChieu, chiTiet, Bay(), ct);
    }

    public Task<GoiYDto?> Lay(int maGoiY, CancellationToken ct = default) => store.Lay(maGoiY, ct);

    public Task<IReadOnlyList<GoiYDto>> DanhSachChoDuyet(LoaiGoiY? loai = null, CancellationToken ct = default) =>
        store.DanhSach(TrangThaiGoiY.ChoDuyet, loai, int.MaxValue, ct);

    public Task<IReadOnlyList<GoiYDto>> DanhSach(TrangThaiGoiY? trangThai, LoaiGoiY? loai, int toiDa = 100, CancellationToken ct = default) =>
        store.DanhSach(trangThai, loai, Math.Clamp(toiDa, 1, 1000), ct);

    public Task<IReadOnlyList<ChiTietGoiYDto>> LayChiTietGoc(int maGoiY, CancellationToken ct = default) => store.LayChiTietGoc(maGoiY, ct);

    public bool CoXuLy(LoaiGoiY loai) => ChonHandler(loai) is not null;

    public Task<KetQua> Duyet(int maGoiY, int maNguoiDuyet, CancellationToken ct = default) =>
        DuyetNoiBo(maGoiY, null, maNguoiDuyet, TrangThaiGoiY.DaDuyet, ct);

    public Task<KetQua> SuaVaDuyet(int maGoiY, IReadOnlyList<ChiTietGoiYDto> chiTietDaSua, int maNguoiDuyet, CancellationToken ct = default)
    {
        var loi = QuyTacGoiY.KiemTraChiTiet(chiTietDaSua);
        if (loi is not null) return Task.FromResult(KetQua.Loi(loi, MaLoiDichVu.DuLieuKhongHopLe));
        return DuyetNoiBo(maGoiY, chiTietDaSua, maNguoiDuyet, TrangThaiGoiY.DaSuaVaDuyet, ct);
    }

    public async Task<KetQua> TuChoi(int maGoiY, int maNguoiDuyet, string? lyDo = null, CancellationToken ct = default)
    {
        lyDo = string.IsNullOrWhiteSpace(lyDo) ? null : lyDo.Trim();
        if (lyDo is { Length: > QuyTacGoiY.DoDaiToiDaLyDo })
            return KetQua.Loi($"Lý do từ chối tối đa {QuyTacGoiY.DoDaiToiDaLyDo} ký tự.", MaLoiDichVu.DuLieuKhongHopLe);

        var g = await store.Lay(maGoiY, ct);
        if (g is null) return KhongThay(maGoiY);
        if (g.TrangThai != TrangThaiGoiY.ChoDuyet) return DaXuLy(g);
        var loiQuyen = await KiemTraNguoiDuyet(g.Loai, maNguoiDuyet, ct);
        if (loiQuyen is not null) return loiQuyen;

        var kq = await store.ChayTrongGiaoDich(async () =>
            await store.NhanXuLy(maGoiY, TrangThaiGoiY.TuChoi, maNguoiDuyet, Bay(), ct) ? KetQua.Ok() : await DaXuLyMoiNhat(maGoiY, ct), ct);
        if (!kq.ThanhCong) return kq;

        await audit.Ghi(maNguoiDuyet, "Từ chối gợi ý AI", chiTiet: $"{g.Loai} #{maGoiY}" + (lyDo is null ? "" : $": {lyDo}"), ct: ct);
        return KetQua.Ok($"Đã từ chối gợi ý {g.Loai} #{maGoiY}; không có thao tác nào được tạo.");
    }

    private async Task<KetQua> DuyetNoiBo(int maGoiY, IReadOnlyList<ChiTietGoiYDto>? chiTietMoi, int maNguoiDuyet, TrangThaiGoiY trangThaiMoi, CancellationToken ct)
    {
        var g = await store.Lay(maGoiY, ct);
        if (g is null) return KhongThay(maGoiY);
        if (g.TrangThai != TrangThaiGoiY.ChoDuyet) return DaXuLy(g);
        var loiQuyen = await KiemTraNguoiDuyet(g.Loai, maNguoiDuyet, ct);
        if (loiQuyen is not null) return loiQuyen;

        var handler = ChonHandler(g.Loai);
        if (handler is null)
            return KetQua.Loi($"Chưa có bộ xử lý cho gợi ý {g.Loai}: nhóm sở hữu chưa đăng ký IGoiYHandler. Gợi ý vẫn ở Chờ duyệt; hãy làm tay.", MaLoiDichVu.ChuaCoXuLy);

        var sauSua = chiTietMoi is null ? g : g with { ChiTiet = chiTietMoi.Select(c => c with { LyDo = c.LyDo.Trim() }).ToList() };
        ThamChieuGoiY? thamChieuMoi = null;

        var kq = await store.ChayTrongGiaoDich(async () =>
        {
            // Nhận quyền xử lý trước khi gọi handler: người thua dừng ở đây, handler không chạy hai lần.
            if (!await store.NhanXuLy(maGoiY, trangThaiMoi, maNguoiDuyet, Bay(), ct)) return await DaXuLyMoiNhat(maGoiY, ct);

            KetQua<ThamChieuGoiY> kqHandler;
            try
            {
                kqHandler = await handler.ThucHien(sauSua with { TrangThai = trangThaiMoi, MaNguoiDuyet = maNguoiDuyet }, maNguoiDuyet, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                log?.LogError(ex, "Handler gợi ý {Loai} lỗi khi duyệt gợi ý {MaGoiY}", g.Loai, maGoiY);
                return KetQua.Loi("Không tạo được thao tác sau khi duyệt (bộ xử lý gặp lỗi). Gợi ý vẫn ở Chờ duyệt; hãy làm tay.", MaLoiDichVu.ChuaCoXuLy);
            }
            if (!kqHandler.ThanhCong)
                return KetQua.Loi(kqHandler.ThongBao ?? "Không thực hiện được thao tác sau khi duyệt. Gợi ý vẫn ở Chờ duyệt; hãy làm tay.", kqHandler.MaLoi);

            // Handler chỉ cần trả phiếu nó tạo; ô nào để trống thì giữ tham chiếu cũ (ví dụ G1 giữ MaPX).
            var tra = kqHandler.GiaTri;
            var tc = new ThamChieuGoiY(tra?.MaPX ?? g.ThamChieu.MaPX, tra?.MaPN ?? g.ThamChieu.MaPN, tra?.MaPC ?? g.ThamChieu.MaPC);
            var loiThamChieu = QuyTacGoiY.KiemTraThamChieuSauDuyet(g.Loai, tc);
            if (loiThamChieu is not null) return KetQua.Loi(loiThamChieu + " Gợi ý vẫn ở Chờ duyệt.", MaLoiDichVu.DuLieuKhongHopLe);

            var luu = await store.CapNhatKetQua(maGoiY, tc, chiTietMoi is null ? null : sauSua.ChiTiet, ct);
            if (luu.ThanhCong) thamChieuMoi = tc;
            return luu;
        }, ct);
        if (!kq.ThanhCong) return kq;

        var hanhDong = trangThaiMoi == TrangThaiGoiY.DaSuaVaDuyet ? "Sửa và duyệt gợi ý AI" : "Duyệt gợi ý AI";
        await audit.Ghi(maNguoiDuyet, hanhDong, chiTiet: $"{g.Loai} #{maGoiY}{MoTaPhieu(thamChieuMoi!)}", ct: ct);
        return KetQua.Ok($"Đã {(trangThaiMoi == TrangThaiGoiY.DaSuaVaDuyet ? "sửa và duyệt" : "duyệt")} gợi ý {g.Loai} #{maGoiY}; hệ thống đã tạo thao tác{MoTaPhieu(thamChieuMoi!)}.");
    }

    /// <summary>Ưu tiên handler thật của nhóm sở hữu; nếu có nhiều thì lấy cái đăng ký sau cùng (quy ước "đăng ký sau cùng thắng"); handler mẫu chỉ dùng khi chưa có handler thật.</summary>
    private IGoiYHandler? ChonHandler(LoaiGoiY loai) =>
        _handlers.Where(h => h.Loai == loai && (h is not IGoiYHandlerMau mau || mau.DangBat))
            .OrderBy(h => h is IGoiYHandlerMau ? 0 : 1).LastOrDefault();

    private async Task<KetQua?> KiemTraNguoiDuyet(LoaiGoiY loai, int maND, CancellationToken ct)
    {
        var nd = await nguoiDung.TimNguoiDung(maND, ct);
        if (nd is null) return KetQua.Loi($"Không tìm thấy người dùng {maND}.", MaLoiDichVu.KhongTimThay);
        if (!nd.DangHoatDong) return KetQua.Loi("Tài khoản đã bị khóa, không xử lý được gợi ý.", MaLoiPhanQuyen.KhongCoQuyen);

        var tenVaiTro = (await nguoiDung.LayVaiTro(ct)).FirstOrDefault(v => v.MaVaiTro == nd.MaVaiTro)?.TenVaiTro;
        if (!QuyTacGoiY.DuocDuyet(loai, tenVaiTro))
            return KetQua.Loi($"Vai trò '{tenVaiTro ?? "không rõ"}' không có thẩm quyền xử lý gợi ý {loai}. Người có thẩm quyền: {string.Join(", ", QuyTacGoiY.VaiTroDuocDuyet(loai))}.",
                MaLoiPhanQuyen.KhongCoQuyen);
        return null;
    }

    private async Task<KetQua> DaXuLyMoiNhat(int maGoiY, CancellationToken ct)
    {
        var g = await store.Lay(maGoiY, ct);
        return g is null ? KhongThay(maGoiY) : DaXuLy(g);
    }

    private DateTime Bay() => gio.GetUtcNow().UtcDateTime;

    private static string MoTaPhieu(ThamChieuGoiY t) =>
        t.MaPX is int px ? $" (phiếu xuất #{px})" : t.MaPN is int pn ? $" (phiếu nhập #{pn})" : t.MaPC is int pc ? $" (phiếu chuyển #{pc})" : "";

    private static KetQua KhongThay(int ma) => KetQua.Loi($"Không tìm thấy gợi ý {ma}.", MaLoiDichVu.KhongTimThay);

    private static KetQua DaXuLy(GoiYDto g) =>
        KetQua.Loi($"Gợi ý {g.MaGoiY} đã ở trạng thái '{g.TrangThai.ToCsdl()}', không thể xử lý lại.", MaLoiDichVu.SaiTrangThai);
}
