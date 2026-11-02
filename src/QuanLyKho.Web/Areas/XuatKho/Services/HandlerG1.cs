using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.XuatKho.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.XuatKho.Services;

/// <summary>
/// Bộ xử lý gợi ý G1 "lấy hàng từ lô, ô nào" (việc T2-2, issue #22) cho khung gợi ý AI dùng chung (T4-8). Quản lý kho duyệt gợi ý
/// qua thành phần DuyetGoiY nhúng trong màn hình phiếu xuất; khi duyệt, khung gọi <see cref="ThucHien"/> ở đây để thật sự giữ chỗ
/// tồn (IStockService.GiuCho), ghi các dòng LayHang rồi khóa phiếu — đúng việc mà PhieuXuatController.Duyet làm tay trước đây (T2-8),
/// chỉ khác là lô/ô do gợi ý AI chọn thay cho IGoiYG1Service tính lại. Không chạm hàng thật: thủ kho vẫn quét mã lấy hàng ở T2-4.
/// Gợi ý có thể đã cũ (chờ duyệt nhiều ngày) hoặc bị người duyệt "sửa rồi duyệt" tự tay, nên <see cref="ThucHien"/> không tin hoàn
/// toàn vào dòng gợi ý: kiểm lại từng ràng buộc (kho, ô Lưu trữ đang hoạt động không khóa, lô đúng biến thể còn hạn dùng) trước khi
/// giữ chỗ, vì IStockService.GiuCho không kiểm những điều này (chỉ chặn ô khóa kiểm kê và vượt tồn khả dụng).
/// </summary>
public sealed class HandlerG1(IStockService ton, TimeProvider gio, KhoDbContext? db = null) : IGoiYHandler
{
    public LoaiGoiY Loai => LoaiGoiY.G1;

    public async Task<KetQua<ThamChieuGoiY>> ThucHien(GoiYDto goiY, int maNguoiDuyet, CancellationToken ct = default)
    {
        if (db is null)
            return KetQua<ThamChieuGoiY>.Loi("Chưa cấu hình ConnectionStrings:KhoDb nên chưa duyệt được gợi ý G1.", MaLoiDichVu.ChuaCoXuLy);
        if (goiY.ThamChieu.MaPX is not int maPX)
            return KetQua<ThamChieuGoiY>.Loi("Gợi ý G1 phải gắn phiếu xuất.", MaLoiDichVu.DuLieuKhongHopLe);

        var p = await db.PhieuXuat.Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPX == maPX, ct);
        if (p is null) return KetQua<ThamChieuGoiY>.Loi("Không còn thấy phiếu xuất của gợi ý.", MaLoiDichVu.KhongTimThay);
        if (p.TrangThai != PhieuXuat.Nhap)
            return KetQua<ThamChieuGoiY>.Loi("Phiếu xuất không còn ở trạng thái Nháp (đã được xử lý ở nơi khác).", MaLoiDichVu.SaiTrangThai);

        var guard = QuyTacDuyet.KiemTra(maNguoiDuyet, p.MaNguoiLap, "phiếu xuất");
        if (!guard.ThanhCong) return KetQua<ThamChieuGoiY>.Loi(guard.ThongBao ?? "Người lập phiếu không được tự duyệt.", guard.MaLoi);

        var (phan, loi) = KhopVoiDongPhieu(goiY.ChiTiet, p.ChiTiet);
        if (loi is not null) return KetQua<ThamChieuGoiY>.Loi(loi, MaLoiDichVu.DuLieuKhongHopLe);

        var loiRangBuoc = await KiemTraRangBuoc(p, phan, ct);
        if (loiRangBuoc is not null) return KetQua<ThamChieuGoiY>.Loi(loiRangBuoc, MaLoiDichVu.DuLieuKhongHopLe);

        foreach (var (maCTX, maLo, maViTri, soLuong) in phan)
        {
            var kqGiu = await ton.GiuCho(new DongTon(maLo, maViTri, soLuong), ct);
            if (!kqGiu.ThanhCong) return KetQua<ThamChieuGoiY>.Loi(kqGiu.ThongBao ?? "Không giữ chỗ được tồn kho.", kqGiu.MaLoi);
            db.LayHang.Add(new LayHang { MaCTX = maCTX, MaLo = maLo, MaViTri = maViTri, SLGiu = soLuong });
        }
        p.TrangThai = PhieuXuat.DaDuyet;
        p.MaNguoiDuyet = maNguoiDuyet;
        await db.SaveChangesAsync(ct);
        return KetQua<ThamChieuGoiY>.Ok(new ThamChieuGoiY(MaPX: maPX));
    }

    /// <summary>
    /// Kiểm lại ràng buộc thật của từng dòng đã khớp trước khi giữ chỗ (gợi ý có thể đã cũ hoặc bị người duyệt sửa tay nên không
    /// còn đáng tin hoàn toàn — issue #22 "gợi ý không vi phạm ràng buộc"): kho còn hoạt động; mỗi ô thuộc đúng kho của phiếu, là
    /// ô Lưu trữ (không phải khu tiếp nhận hay cấp khác), đang hoạt động, không bị khóa kiểm kê; mỗi lô còn tồn tại, đúng biến thể
    /// của dòng phiếu, chưa hết hạn dùng. IStockService.GiuCho không kiểm những điều này (chỉ chặn ô khóa và vượt tồn khả dụng).
    /// </summary>
    private async Task<string?> KiemTraRangBuoc(PhieuXuat p, List<(int MaCTX, int MaLo, int MaViTri, int SoLuong)> phan, CancellationToken ct)
    {
        if (!await db!.Kho.AnyAsync(k => k.MaKho == p.MaKho && k.TrangThai == TrangThaiHoatDong.HoatDong, ct))
            return "Kho xuất không còn hoạt động; gợi ý không còn dùng được, hãy làm tay.";

        var homNay = DateOnly.FromDateTime(gio.GetUtcNow().UtcDateTime);
        var maBTTheoCTX = p.ChiTiet.ToDictionary(c => c.MaCTX, c => c.MaBT);
        var maViTriCanXet = phan.Select(x => x.MaViTri).Distinct().ToList();
        var maLoCanXet = phan.Select(x => x.MaLo).Distinct().ToList();
        var viTri = await db.ViTri.AsNoTracking().Where(v => maViTriCanXet.Contains(v.MaViTri)).ToDictionaryAsync(v => v.MaViTri, ct);
        var lo = await db.LoHang.AsNoTracking().Where(l => maLoCanXet.Contains(l.MaLo)).ToDictionaryAsync(l => l.MaLo, ct);

        foreach (var (maCTX, maLo, maViTri, _) in phan)
        {
            if (!viTri.TryGetValue(maViTri, out var v)) return $"Ô #{maViTri} không còn tồn tại.";
            if (v.MaKho != p.MaKho) return $"Ô '{v.TenViTri}' không thuộc kho xuất của phiếu.";
            if (v.LoaiViTri != LoaiViTriGiaTri.LuuTru) return $"Ô '{v.TenViTri}' không phải ô Lưu trữ (không được lấy từ khu tiếp nhận hoặc cấp khác).";
            if (!v.DangHoatDong) return $"Ô '{v.TenViTri}' đã ngừng hoạt động.";
            if (v.DangKhoa) return $"Ô '{v.TenViTri}' đang bị khóa để kiểm kê.";

            if (!lo.TryGetValue(maLo, out var l)) return $"Lô #{maLo} không còn tồn tại.";
            if (l.MaBT != maBTTheoCTX.GetValueOrDefault(maCTX)) return $"Lô '{l.MaVachLo}' không đúng biến thể của dòng phiếu.";
            if (l.HanDung is DateOnly h && h < homNay) return $"Lô '{l.MaVachLo}' đã hết hạn dùng ({h:dd/MM/yyyy}).";
        }
        return null;
    }

    /// <summary>
    /// Ghép các dòng gợi ý (theo biến thể, MaBT) với các dòng phiếu (theo MaCTX): một biến thể có thể nằm trên nhiều dòng phiếu
    /// (hiếm) và lấy từ nhiều lô, ô (thường). Trả lỗi rõ ràng nếu không khớp (ví dụ phiếu bị sửa sau khi tạo gợi ý) để khung hoàn tác
    /// và giữ gợi ý ở Chờ duyệt, không khóa nhầm phiếu với số liệu không còn đúng.
    /// </summary>
    private static (List<(int MaCTX, int MaLo, int MaViTri, int SoLuong)> Phan, string? Loi) KhopVoiDongPhieu(
        IReadOnlyList<ChiTietGoiYDto> chiTietGoiY, List<ChiTietPhieuXuat> dongPhieu)
    {
        var phan = new List<(int, int, int, int)>();
        var theoBienThe = dongPhieu.OrderBy(d => d.MaCTX).GroupBy(d => d.MaBT).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var nhom in chiTietGoiY.GroupBy(c => c.MaBT))
        {
            if (!theoBienThe.TryGetValue(nhom.Key, out var dongCho))
                return (phan, $"Gợi ý có biến thể #{nhom.Key} không khớp dòng nào của phiếu; phiếu có thể đã bị sửa sau khi tạo gợi ý.");

            var tongGoiY = nhom.Sum(c => c.SoLuong);
            var tongDong = dongCho.Sum(d => d.SoLuong);
            if (tongGoiY != tongDong)
                return (phan, $"Gợi ý ({tongGoiY}) không khớp số lượng dòng phiếu ({tongDong}) của biến thể #{nhom.Key}; phiếu có thể đã bị sửa sau khi tạo gợi ý.");

            var hang = nhom.ToList();
            var iHang = 0;
            var conLaiHang = hang[0].SoLuong;
            foreach (var dong in dongCho)
            {
                var conLaiDong = dong.SoLuong;
                while (conLaiDong > 0)
                {
                    if (iHang >= hang.Count)
                        return (phan, $"Không đủ dòng gợi ý cho biến thể #{nhom.Key}; phiếu có thể đã bị sửa sau khi tạo gợi ý.");
                    var c = hang[iHang];
                    if (c.MaLo is not int maLo || c.MaViTri is not int maViTri)
                        return (phan, $"Mỗi dòng gợi ý G1 phải có lô và ô (biến thể #{nhom.Key}).");

                    var lay = Math.Min(conLaiDong, conLaiHang);
                    phan.Add((dong.MaCTX, maLo, maViTri, lay));
                    conLaiDong -= lay;
                    conLaiHang -= lay;
                    if (conLaiHang == 0 && iHang + 1 < hang.Count) { iHang++; conLaiHang = hang[iHang].SoLuong; }
                }
            }
        }

        var thieu = theoBienThe.Keys.Except(chiTietGoiY.Select(c => c.MaBT)).ToList();
        if (thieu.Count > 0) return (phan, $"Gợi ý thiếu dòng cho biến thể #{thieu[0]} của phiếu.");
        return (phan, null);
    }
}
