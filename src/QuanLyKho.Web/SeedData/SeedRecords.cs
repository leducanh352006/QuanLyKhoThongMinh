namespace QuanLyKho.Web.SeedData;

// Tên bản ghi và thuộc tính bám đúng tên bảng, cột ở báo cáo mục 2.8 (Bảng 2.19 đến 2.26)
// để nhóm nào viết bảng thật cũng ánh xạ một-một được.

public sealed record VaiTroSeed(int MaVaiTro, string TenVaiTro);

/// <summary>Không có mật khẩu ở đây có chủ đích: mật khẩu mẫu do việc "Đăng nhập" tạo từ cấu hình, không commit lên git.</summary>
public sealed record NguoiDungSeed(int MaND, string TenDangNhap, string HoTen, string Email, int MaVaiTro);

public sealed record KhoSeed(int MaKho, string TenKho, string LoaiKho, string DiaChi, int MaQuanLy, int ThoiGianCho);

/// <summary>Chỉ ô (CapViTri = "Ô") mới có LoaiViTri, MaVachO và kích thước (cm), khối lượng tối đa (kg).</summary>
public sealed record ViTriSeed(
    int MaViTri, string TenViTri, int MaKho, int? MaViTriCha, string CapViTri,
    string? LoaiViTri = null, string? MaVachO = null,
    decimal? Dai = null, decimal? Rong = null, decimal? Cao = null, decimal? KhoiLuongToiDa = null);

public sealed record DanhMucSeed(int MaDM, string TenDM);

public sealed record NhaCungCapSeed(int MaNCC, string TenNCC, string? SDT, string? DiaChi);

public sealed record SanPhamSeed(int MaSP, string TenSP, int MaDM, string DonViTinh, bool CoHanDung);

/// <summary>Kích thước (cm) và khối lượng (kg) của một đơn vị.</summary>
public sealed record BienTheSeed(
    int MaBT, int MaSP, string MaVach, string ThuocTinh,
    decimal Dai, decimal Rong, decimal Cao, decimal KhoiLuong);

public sealed record LoHangSeed(int MaLo, string MaVachLo, int MaBT, DateOnly? HanDung);

public sealed record TonKhoSeed(int MaLo, int MaViTri, int SoLuongTon, int SoLuongGiu);
