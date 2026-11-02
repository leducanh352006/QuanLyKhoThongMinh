using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.XuatKho.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.XuatKho.Services;

/// <summary>
/// Bộ xử lý gợi ý G1 "lấy hàng từ lô, ô nào" (việc T2-2, issue #22) cho khung gợi ý AI dùng chung (T4-8). Quản lý kho duyệt gợi ý
/// qua thành phần DuyetGoiY nhúng trong màn hình phiếu xuất; khi duyệt, khung gọi <see cref="ThucHien"/> ở đây để thật sự giữ chỗ
/// tồn (IStockService.GiuCho), ghi các dòng LayHang rồi khóa phiếu — đúng việc mà PhieuXuatController.Duyet làm tay trước đây (T2-8),
/// chỉ khác là lô/ô do gợi ý AI chọn thay cho PhanBoFifo tính lại. Không chạm hàng thật: thủ kho vẫn quét mã lấy hàng ở việc T2-4.
/// </summary>
public sealed class HandlerG1(IStockService ton, KhoDbContext? db = null) : IGoiYHandler
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
