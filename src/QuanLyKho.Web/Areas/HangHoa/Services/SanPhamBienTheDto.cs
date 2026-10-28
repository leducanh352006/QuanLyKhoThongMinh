namespace QuanLyKho.Web.Areas.HangHoa.Services;

public sealed record SanPhamDong(int MaSP, string TenSP, int MaDM, string TenDM, string DonViTinh, bool CoHanDung);

public sealed record SanPhamMoi(string TenSP, int MaDM, string DonViTinh, bool CoHanDung);

public sealed record SanPhamSuaDoi(string TenSP, int MaDM, string DonViTinh, bool CoHanDung);

public sealed record BienTheDong(int MaBT, int MaSP, string MaVach, string ThuocTinh, decimal Dai, decimal Rong, decimal Cao, decimal KhoiLuong)
{
    public decimal TheTichCm3 => Dai * Rong * Cao;
}

public sealed record BienTheMoi(int MaSP, string MaVach, string ThuocTinh, decimal Dai, decimal Rong, decimal Cao, decimal KhoiLuong);

public sealed record BienTheSuaDoi(string MaVach, string ThuocTinh, decimal Dai, decimal Rong, decimal Cao, decimal KhoiLuong);
