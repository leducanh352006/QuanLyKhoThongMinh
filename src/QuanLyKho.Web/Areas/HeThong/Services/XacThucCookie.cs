using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Authorization;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Đăng nhập bằng cookie (không dùng bộ ASP.NET Identity đầy đủ vì bảng NguoiDung theo thiết kế của báo cáo).</summary>
public static class XacThucCookie
{
    public const string Scheme = CookieAuthenticationDefaults.AuthenticationScheme;
    public const string DuongDanDangNhap = "/HeThong/TaiKhoan/DangNhap";
    public const string DuongDanTuChoi = "/HeThong/TaiKhoan/TuChoi";

    public static IServiceCollection DangKy(IServiceCollection services)
    {
        services.AddAuthentication(Scheme).AddCookie(Scheme, o =>
        {
            o.Cookie.Name = "kho.auth";
            o.Cookie.HttpOnly = true;
            o.Cookie.SameSite = SameSiteMode.Lax;
            o.Cookie.IsEssential = true;
            o.LoginPath = DuongDanDangNhap;
            o.AccessDeniedPath = DuongDanTuChoi;
            o.ExpireTimeSpan = TimeSpan.FromHours(8);
            o.SlidingExpiration = true;
            o.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = ctx => ChuyenHoacMa(ctx.Request, ctx.Response, StatusCodes.Status401Unauthorized, ctx.RedirectUri),
                OnRedirectToAccessDenied = ctx => ChuyenHoacMa(ctx.Request, ctx.Response, StatusCodes.Status403Forbidden, ctx.RedirectUri),
                OnValidatePrincipal = KiemTraLaiPhien,
            };
        });

        // Ngoài Development cookie chỉ gửi qua HTTPS.
        services.AddOptions<CookieAuthenticationOptions>(Scheme).Configure<IHostEnvironment>((o, env) =>
            o.Cookie.SecurePolicy = env.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always);

        return services;
    }

    /// <summary>
    /// Mỗi yêu cầu: hỏi lại tài khoản (có nhớ tạm 30 giây, xóa ngay khi khóa hoặc đổi vai trò). Bị khóa hoặc bị xóa thì hủy phiên;
    /// đổi vai trò thì làm mới thông tin trong cookie nên quyền mới có hiệu lực ngay.
    /// </summary>
    private static async Task KiemTraLaiPhien(CookieValidatePrincipalContext ctx)
    {
        var trongCookie = NguoiDungHienTaiService.TuPrincipal(ctx.Principal);
        if (trongCookie is null)
        {
            await HuyPhien(ctx);
            return;
        }

        var taiKhoan = ctx.HttpContext.RequestServices.GetRequiredService<ITaiKhoanService>();
        var moiNhat = await taiKhoan.KiemTraPhien(trongCookie.MaND, ctx.HttpContext.RequestAborted);
        if (moiNhat is null)
        {
            await HuyPhien(ctx);
            return;
        }

        if (moiNhat != trongCookie)
        {
            ctx.ReplacePrincipal(NguoiDungHienTaiService.TaoPrincipal(moiNhat, Scheme));
            ctx.ShouldRenew = true;
        }
    }

    private static async Task HuyPhien(CookieValidatePrincipalContext ctx)
    {
        ctx.RejectPrincipal();
        await ctx.HttpContext.SignOutAsync(Scheme);
    }

    // Gọi bằng AJAX/fetch thì trả mã 401/403 thay vì chuyển hướng tới trang HTML.
    private static Task ChuyenHoacMa(HttpRequest req, HttpResponse res, int maTrangThai, string uri)
    {
        if (LaYeuCauAjax(req)) res.StatusCode = maTrangThai;
        else res.Redirect(uri);
        return Task.CompletedTask;
    }

    public static bool LaYeuCauAjax(HttpRequest req)
        => string.Equals(req.Headers.XRequestedWith, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
           || (req.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase)
               && !req.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Cả ứng dụng yêu cầu đăng nhập theo mặc định (FallbackPolicy), nên Controller mới của mọi nhóm tự được bảo vệ, không phải nhớ gắn [Authorize].
/// Chỉ trang đăng nhập và trang lỗi chung được mở cho người chưa đăng nhập. Trang lỗi của HomeController thuộc việc T4-1 nên mở bằng convention này,
/// không phải sửa file của việc khác.
/// </summary>
public sealed class MoTrangLoiChoMoiNguoi : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        if (controller.ControllerName != "Home" || controller.ControllerType.Namespace != "QuanLyKho.Web.Controllers") return;
        foreach (var action in controller.Actions.Where(a => a.ActionName == "Error"))
            foreach (var selector in action.Selectors)
                selector.EndpointMetadata.Add(new AllowAnonymousAttribute());
    }
}
