using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Infrastructure.NguoiDung;

/// <summary>
/// Người dùng hiện tại BẢN GIẢ cho giai đoạn chưa có đăng nhập: chỉ hoạt động ở môi trường Development,
/// lấy vai trò từ cookie (chọn ở góc phải thanh trên), mặc định là Quản trị viên. Ngoài Development luôn trả null (chưa đăng nhập).
/// Việc "Đăng nhập" của Nhóm 1 sẽ đăng ký bản thật của <see cref="INguoiDungHienTai"/> để thay lớp này.
/// </summary>
public sealed class DevNguoiDungHienTai(IHttpContextAccessor http, IWebHostEnvironment env) : INguoiDungHienTai
{
    public const string TenCookie = "kho_dev_vaitro";

    public NguoiDungHienTai? Lay()
    {
        if (!env.IsDevelopment()) return null;

        var maVaiTro = 1;
        if (http.HttpContext?.Request.Cookies.TryGetValue(TenCookie, out var raw) == true && int.TryParse(raw, out var ma))
            maVaiTro = ma;

        var nd = SeedDataSet.NguoiDung.FirstOrDefault(n => n.MaVaiTro == maVaiTro) ?? SeedDataSet.NguoiDung[0];
        var vaiTro = SeedDataSet.VaiTro.First(v => v.MaVaiTro == nd.MaVaiTro);
        return new NguoiDungHienTai(nd.MaND, nd.TenDangNhap, nd.HoTen, vaiTro.MaVaiTro, vaiTro.TenVaiTro);
    }
}
