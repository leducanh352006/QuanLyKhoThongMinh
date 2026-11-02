using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.NhapKho.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.NhapKho.Services;

/// <summary>Bộ lọc danh sách phiếu nhập. Null hoặc chuỗi rỗng là không lọc theo mục đó. Ngày tính theo giờ máy chủ, gồm cả ngày đến.</summary>
public sealed record BoLocPhieu(string? Tim = null, DateOnly? TuNgay = null, DateOnly? DenNgay = null, string? TrangThai = null);

/// <summary>Kết quả tìm danh sách; <see cref="BiCat"/> là true khi còn nhiều hơn <see cref="PhieuNhapService.ToiDa"/> phiếu nên chỉ trả phần mới nhất.</summary>
public sealed record KetQuaDanhSach(List<PhieuNhap> Phieu, bool BiCat);

/// <summary>
/// Quản lý phiếu nhập (việc T1-7): tìm danh sách và hủy phiếu. Mọi thay đổi tồn đi qua <see cref="IStockService"/>,
/// mọi thao tác hủy ghi nhật ký qua <see cref="IAuditService"/>.
/// </summary>
public sealed class PhieuNhapService(KhoDbContext db, IStockService ton, IAuditService audit)
{
    /// <summary>Số phiếu tối đa mỗi lần tìm, để trang không quá nặng.</summary>
    public const int ToiDa = 200;

    public const int LyDoToiDa = 200;

    private const string DiemLuu = "HuyPhieuNhap";

    // ---- Danh sách ----

    /// <summary>Tìm theo số phiếu hoặc tên nhà cung cấp (có chứa, không phân biệt hoa thường), lọc theo khoảng ngày và trạng thái; phiếu mới nhất trước.</summary>
    public async Task<KetQuaDanhSach> TimKiem(BoLocPhieu loc, CancellationToken ct = default)
    {
        var q = db.PhieuNhap.AsNoTracking().AsQueryable();
        if (loc.Tim?.Trim() is { Length: > 0 } tim)
            q = q.Where(p => p.SoPhieu.Contains(tim) || (p.NhaCungCap != null && p.NhaCungCap.TenNCC.Contains(tim)));
        if (loc.TuNgay is DateOnly tu) { var tuUtc = DauNgayUtc(tu); q = q.Where(p => p.ThoiGian >= tuUtc); }
        if (loc.DenNgay is DateOnly den) { var denUtc = DauNgayUtc(den.AddDays(1)); q = q.Where(p => p.ThoiGian < denUtc); }
        if (loc.TrangThai?.Trim() is { Length: > 0 } tt) q = q.Where(p => p.TrangThai == tt);

        var phieu = await q.Include(p => p.Kho).Include(p => p.NhaCungCap).Include(p => p.ChiTiet)
            .OrderByDescending(p => p.MaPN).Take(ToiDa + 1).ToListAsync(ct);
        var biCat = phieu.Count > ToiDa;
        if (biCat) phieu.RemoveAt(phieu.Count - 1);
        return new KetQuaDanhSach(phieu, biCat);
    }

    /// <summary>0 giờ của một ngày theo giờ máy chủ, đổi sang UTC để so với PhieuNhap.ThoiGian.</summary>
    public static DateTime DauNgayUtc(DateOnly ngay) =>
        DateTime.SpecifyKind(ngay.ToDateTime(TimeOnly.MinValue), DateTimeKind.Local).ToUniversalTime();

    // ---- Hủy phiếu ----

    /// <summary>
    /// Hủy phiếu (Bảng 2.3 dòng 7): chỉ khi chưa có dòng nào được sắp xếp vào ô. Phiếu Nháp, Đã xác nhận hoặc Đã kiểm tra (chưa có lệnh sắp xếp
    /// nào đã xác nhận) hủy được; phiếu Hoàn tất, đã hủy hoặc đã có hàng xếp vào ô thì không. Cần lý do, ghi vào nhật ký.
    /// Làm tất cả trong MỘT giao dịch: chuyển phiếu sang Đã hủy (lệnh có điều kiện nên bấm hai lần chỉ ghi một lần), hủy các lệnh sắp xếp còn chờ
    /// (để không còn chiếm sức chứa của ô), và nếu phiếu đã tiếp nhận hàng thì hoàn tác nhập khỏi ô Tiếp nhận bằng IStockService.GhiGiam loại HoanTac.
    /// Lỗi ở đâu thì hoàn tác hết. Lô hàng đã tạo vẫn giữ trong danh mục với tồn 0.
    /// </summary>
    public async Task<KetQua> Huy(int maPN, string? lyDo, int maND, CancellationToken ct = default)
    {
        lyDo = lyDo?.Trim();
        if (string.IsNullOrEmpty(lyDo)) return KetQua.Loi("Nhập lý do hủy phiếu.", MaLoiDichVu.DuLieuKhongHopLe);
        if (lyDo.Length > LyDoToiDa) return KetQua.Loi($"Lý do tối đa {LyDoToiDa} ký tự.", MaLoiDichVu.DuLieuKhongHopLe);

        var p = await db.PhieuNhap.AsNoTracking().Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPN == maPN, ct);
        if (p is null) return KetQua.Loi("Không tìm thấy phiếu nhập.", MaLoiDichVu.KhongTimThay);
        if (!p.HuyDuoc) return KetQua.Loi($"Phiếu {p.SoPhieu} đang {p.TrangThai} nên không hủy được.", MaLoiDichVu.SaiTrangThai);
        var maCTN = p.ChiTiet.Select(d => d.MaCTN).ToList();
        if (await db.SapXep.AnyAsync(s => s.TrangThai == SapXep.DaXacNhan && maCTN.Contains(s.MaCTN), ct))
            return KetQua.Loi($"Phiếu {p.SoPhieu} đã có hàng được sắp xếp vào ô nên không hủy được.", MaLoiDichVu.SaiTrangThai);

        // Một giao dịch: nơi gọi chưa mở thì mở ở đây. Điểm lưu (savepoint) giúp hoàn tác riêng phần này kể cả khi nơi gọi đã mở giao dịch.
        await using var giaoDichMoi = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(ct) : null;
        var giaoDich = db.Database.CurrentTransaction!;
        await giaoDich.CreateSavepointAsync(DiemLuu, ct);

        var ghi = await GhiHuy(p, maCTN, maND, ct);
        if (!ghi.ThanhCong)
        {
            await giaoDich.RollbackToSavepointAsync(DiemLuu, ct);
            return ghi;
        }
        if (giaoDichMoi is not null) await giaoDichMoi.CommitAsync(ct);

        await audit.Ghi(maND, "Hủy phiếu nhập", LoaiChungTu.NK, p.MaPN, $"{p.SoPhieu} (từ trạng thái {p.TrangThai}): {lyDo}", ct);
        return KetQua.Ok($"Đã hủy phiếu {p.SoPhieu}." + (p.TrangThai == PhieuNhap.DaKiemTra ? " Hàng đã tiếp nhận được hoàn tác khỏi khu tiếp nhận." : ""));
    }

    /// <summary>Các bước ghi của Huy (đã nằm trong giao dịch). Trả lỗi thì nơi gọi hoàn tác.</summary>
    private async Task<KetQua> GhiHuy(PhieuNhap p, List<int> maCTN, int maND, CancellationToken ct)
    {
        // 1. Chuyển phiếu sang Đã hủy bằng lệnh có điều kiện: trạng thái chưa đổi và chưa có lệnh sắp xếp nào được xác nhận.
        var truoc = p.TrangThai;
        var da = await db.PhieuNhap.Where(x => x.MaPN == p.MaPN && x.TrangThai == truoc
                                               && !db.SapXep.Any(s => s.TrangThai == SapXep.DaXacNhan && maCTN.Contains(s.MaCTN)))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, PhieuNhap.DaHuy), ct);
        if (da == 0) return KetQua.Loi("Phiếu vừa đổi trạng thái hoặc đã có hàng được sắp xếp vào ô. Hãy tải lại trang rồi thử lại.", MaLoiDichVu.SaiTrangThai);

        // 2. Lệnh sắp xếp còn chờ xác nhận thì hủy theo, để ô không còn bị tính là sắp nhận hàng (INguonHangChoXep).
        await db.SapXep.Where(s => s.TrangThai == SapXep.ChoXacNhan && maCTN.Contains(s.MaCTN))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, SapXep.DaHuy), ct);

        // 3. Phiếu đã tiếp nhận: trả hàng khỏi ô Tiếp nhận (hoàn tác nhận hàng), cả hàng đạt và không đạt. Dòng thực nhận 0 không có lô nên bỏ qua.
        if (truoc != PhieuNhap.DaKiemTra) return KetQua.Ok();
        foreach (var d in p.ChiTiet.Where(x => x.MaLo != null && x.SLThucNhan > 0).OrderBy(x => x.MaCTN))
        {
            var soLuong = d.SLThucNhan!.Value;
            var nguon = await (
                from t in db.TonKho.AsNoTracking()
                join v in db.ViTri.AsNoTracking() on t.MaViTri equals v.MaViTri
                where t.MaLo == d.MaLo && v.MaKho == p.MaKho && v.LoaiViTri == LoaiViTriGiaTri.TiepNhan && t.SoLuongTon - t.SoLuongGiu >= soLuong
                orderby v.MaViTri
                select (int?)v.MaViTri).FirstOrDefaultAsync(ct);
            if (nguon is null) return KetQua.Loi($"Khu tiếp nhận không còn đủ {soLuong} hàng của dòng {d.MaCTN} để hoàn tác. Chưa hủy gì.", MaLoiDichVu.VuotTon);

            var giam = await ton.GhiGiam(new DongTon(d.MaLo!.Value, nguon.Value, soLuong), LoaiBienDong.HoanTac, new ThamChieuChungTu(LoaiChungTu.NK, p.MaPN), maND, ct: ct);
            if (!giam.ThanhCong) return KetQua.Loi(giam.ThongBao ?? "Không hoàn tác được tồn.", giam.MaLoi);
        }
        return KetQua.Ok();
    }
}
