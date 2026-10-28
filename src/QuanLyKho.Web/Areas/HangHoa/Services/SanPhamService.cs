using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>CRUD sản phẩm (màn hình Sản phẩm, việc T3-2).</summary>
public interface ISanPhamQuanLy
{
    Task<IReadOnlyList<SanPhamDong>> LayDanhSach(CancellationToken ct = default);

    Task<SanPhamDong?> Lay(int maSP, CancellationToken ct = default);

    Task<KetQua> Them(SanPhamMoi moi, int maNguoiThucHien, CancellationToken ct = default);

    Task<KetQua> Sua(int maSP, SanPhamSuaDoi doi, int maNguoiThucHien, CancellationToken ct = default);
}

public sealed class SanPhamService(ISanPhamBienTheStore store, IDanhMucQuanLy danhMuc, IAuditService audit) : ISanPhamQuanLy
{
    public async Task<IReadOnlyList<SanPhamDong>> LayDanhSach(CancellationToken ct = default)
    {
        var dm = await danhMuc.LayDanhSach(ct);
        return (await store.LaySanPham(ct)).Select(s => ChoHienThi(s, dm)).ToList();
    }

    public async Task<SanPhamDong?> Lay(int maSP, CancellationToken ct = default)
    {
        var sp = await store.TimSanPham(maSP, ct);
        return sp is null ? null : ChoHienThi(sp, await danhMuc.LayDanhSach(ct));
    }

    public async Task<KetQua> Them(SanPhamMoi moi, int maNguoiThucHien, CancellationToken ct = default)
    {
        var ten = moi.TenSP.Trim();
        var donVi = moi.DonViTinh.Trim();
        var loi = KiemTraTen(ten) ?? KiemTraDonVi(donVi);
        if (loi is not null) return loi;

        if (await danhMuc.Lay(moi.MaDM, ct) is null)
            return KetQua.Loi("Không tìm thấy danh mục.", MaLoiDichVu.DuLieuKhongHopLe);

        var sp = await store.ThemSanPham(new SanPham { TenSP = ten, MaDM = moi.MaDM, DonViTinh = donVi, CoHanDung = moi.CoHanDung }, ct);

        await audit.Ghi(maNguoiThucHien, "Thêm sản phẩm", chiTiet: ten, ct: ct);
        return KetQua.Ok($"Đã thêm sản phẩm {ten}.");
    }

    public async Task<KetQua> Sua(int maSP, SanPhamSuaDoi doi, int maNguoiThucHien, CancellationToken ct = default)
    {
        var sp = await store.TimSanPham(maSP, ct);
        if (sp is null) return KetQua.Loi("Không tìm thấy sản phẩm.", MaLoiDichVu.KhongTimThay);

        var ten = doi.TenSP.Trim();
        var donVi = doi.DonViTinh.Trim();
        var loi = KiemTraTen(ten) ?? KiemTraDonVi(donVi);
        if (loi is not null) return loi;

        if (await danhMuc.Lay(doi.MaDM, ct) is null)
            return KetQua.Loi("Không tìm thấy danh mục.", MaLoiDichVu.DuLieuKhongHopLe);

        sp.TenSP = ten;
        sp.MaDM = doi.MaDM;
        sp.DonViTinh = donVi;
        sp.CoHanDung = doi.CoHanDung;
        await store.LuuThayDoi(ct);

        await audit.Ghi(maNguoiThucHien, "Sửa sản phẩm", chiTiet: ten, ct: ct);
        return KetQua.Ok("Đã lưu thay đổi.");
    }

    private static KetQua? KiemTraTen(string ten)
        => string.IsNullOrWhiteSpace(ten) || ten.Length > 100
            ? KetQua.Loi("Tên sản phẩm không được để trống và tối đa 100 ký tự.", MaLoiDichVu.DuLieuKhongHopLe) : null;

    private static KetQua? KiemTraDonVi(string donVi)
        => string.IsNullOrWhiteSpace(donVi) || donVi.Length > 20
            ? KetQua.Loi("Đơn vị tính không được để trống và tối đa 20 ký tự.", MaLoiDichVu.DuLieuKhongHopLe) : null;

    private static SanPhamDong ChoHienThi(SanPham s, IReadOnlyList<DanhMucDong> dm) => new(
        s.MaSP, s.TenSP, s.MaDM, dm.FirstOrDefault(d => d.MaDM == s.MaDM)?.TenDM ?? "?", s.DonViTinh, s.CoHanDung);
}
