using System.Security.Claims;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Tên các claim trong cookie đăng nhập. Vai trò dùng claim chuẩn <see cref="ClaimTypes.Role"/> nên <c>[Authorize(Roles = ...)]</c> chạy được.</summary>
public static class ClaimKho
{
    public const string MaVaiTro = "kho:mavaitro";
    public const string HoTen = "kho:hoten";
}

/// <summary>
/// Bản THẬT của <see cref="INguoiDungHienTai"/>: đọc người đang đăng nhập từ cookie (đã được kiểm tra lại với CSDL mỗi yêu cầu, xem <see cref="XacThucCookie"/>).
/// Đăng ký trong HeThongModule nên thay bản giả Development của việc T4-1; không đổi interface.
/// </summary>
public sealed class NguoiDungHienTaiService(IHttpContextAccessor http) : INguoiDungHienTai
{
    public NguoiDungHienTai? Lay() => TuPrincipal(http.HttpContext?.User);

    public static NguoiDungHienTai? TuPrincipal(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true) return null;

        if (!int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var maND)) return null;
        if (!int.TryParse(user.FindFirstValue(ClaimKho.MaVaiTro), out var maVaiTro)) return null;
        var ten = user.FindFirstValue(ClaimTypes.Name);
        var vaiTro = user.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrEmpty(ten) || string.IsNullOrEmpty(vaiTro)) return null;

        return new NguoiDungHienTai(maND, ten, user.FindFirstValue(ClaimKho.HoTen) ?? ten, maVaiTro, vaiTro);
    }

    public static ClaimsPrincipal TaoPrincipal(NguoiDungHienTai nguoi, string scheme)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, nguoi.MaND.ToString()),
            new Claim(ClaimTypes.Name, nguoi.TenDangNhap),
            new Claim(ClaimKho.HoTen, nguoi.HoTen),
            new Claim(ClaimKho.MaVaiTro, nguoi.MaVaiTro.ToString()),
            new Claim(ClaimTypes.Role, nguoi.TenVaiTro),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, scheme));
    }
}
