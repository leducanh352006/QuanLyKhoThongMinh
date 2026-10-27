using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuanLyKho.Web.Areas.HeThong;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Contracts.Stubs;
using QuanLyKho.Web.Controllers;
using QuanLyKho.Web.Infrastructure;

namespace QuanLyKho.Tests.HeThong;

public class NguoiDungHienTaiServiceTests
{
    private static readonly NguoiDungHienTai Mau = new(7, "thukho", "Thủ kho (mẫu)", 3, "Thủ kho");

    [Fact]
    public void QuaLaiGiuaPrincipalVaNguoiDungHienTai_KhongMatThongTin()
    {
        var principal = NguoiDungHienTaiService.TaoPrincipal(Mau, "Cookies");
        Assert.Equal(Mau, NguoiDungHienTaiService.TuPrincipal(principal));
        Assert.True(principal.IsInRole("Thủ kho"));   // cho [Authorize(Roles = ...)]
    }

    [Fact]
    public void ChuaDangNhapHoacThieuClaim_LaNull()
    {
        Assert.Null(NguoiDungHienTaiService.TuPrincipal(null));
        Assert.Null(NguoiDungHienTaiService.TuPrincipal(new ClaimsPrincipal(new ClaimsIdentity())));   // chưa xác thực
        Assert.Null(NguoiDungHienTaiService.TuPrincipal(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "a") }, "Cookies"))));
    }

    [Fact]
    public void LayTuHttpContext()
    {
        var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = NguoiDungHienTaiService.TaoPrincipal(Mau, "Cookies") } };
        var dichVu = new NguoiDungHienTaiService(http);
        Assert.Equal(Mau, dichVu.Lay());

        http.HttpContext = null;   // ngoài yêu cầu HTTP (ví dụ tác vụ nền) thì không có người dùng
        Assert.Null(dichVu.Lay());
    }
}

/// <summary>Kiểm tra cả dây chuyền: thuộc tính [CanQuyen] → chính sách → bộ xử lý → PhanQuyenService → kho dữ liệu, qua DI giống Program.cs.</summary>
public class KiemQuyenQuaDiTests
{
    private static ServiceProvider Dung()
    {
        var cauHinh = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [BoNhoHeThongStore.KhoaCauHinhMatKhauMau] = MoiTruongThu.MatKhauMau })
            .Build();
        var s = new ServiceCollection();
        s.AddLogging();
        s.AddSingleton<IConfiguration>(cauHinh);
        s.AddKhoInfrastructure();     // như Program.cs: hạ tầng (có bản giả Development)
        s.AddContractStubs();         // rồi bản giả của Contracts
        s.AddHeThong();               // rồi module Area, đăng ký sau nên bản thật thay bản giả
        return s.BuildServiceProvider();
    }

    private static ClaimsPrincipal La(int maND, int maVaiTro, string tenVaiTro)
        => NguoiDungHienTaiService.TaoPrincipal(new NguoiDungHienTai(maND, "u" + maND, "U " + maND, maVaiTro, tenVaiTro), "Cookies");

    [Fact]
    public void BanThatThayBanGia_NguoiDungHienTaiVaPhanQuyen()
    {
        using var sp = Dung();
        using var scope = sp.CreateScope();
        Assert.IsType<NguoiDungHienTaiService>(scope.ServiceProvider.GetRequiredService<INguoiDungHienTai>());
        Assert.IsType<PhanQuyenService>(scope.ServiceProvider.GetRequiredService<IPhanQuyenService>());
        Assert.IsType<CanQuyenPolicyProvider>(scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>());
    }

    [Fact]
    public void KhongCoSqlServer_DungKhoTrongBoNho_VaCoBayNguoiDungMau()
    {
        using var sp = Dung();
        using var scope = sp.CreateScope();
        Assert.IsType<BoNhoHeThongStore>(scope.ServiceProvider.GetRequiredService<IHeThongStore>());
    }

    [Theory]
    [InlineData(2, "Quản lý kho", "quyen:XK:Duyet", true)]
    [InlineData(3, "Thủ kho", "quyen:XK:Duyet", false)]
    [InlineData(4, "Nhân viên bán hàng", "quyen:XK:Duyet", false)]
    [InlineData(4, "Nhân viên bán hàng", "quyen:XK:Them", true)]
    [InlineData(4, "Nhân viên bán hàng", "quyen:NK:Xem", false)]
    [InlineData(3, "Thủ kho", "quyen:NK:Them", true)]
    [InlineData(3, "Thủ kho", "quyen:KK:Duyet", false)]
    [InlineData(2, "Quản lý kho", "quyen:KK:Duyet", true)]
    [InlineData(1, "Quản trị viên", "quyen:KK:Duyet", false)]
    public async Task ChinhSachQuyen_ChoKetQuaDungTheoVaiTro(int maVaiTro, string tenVaiTro, string chinhSach, bool mongDoi)
    {
        using var sp = Dung();
        using var scope = sp.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        var kq = await auth.AuthorizeAsync(La(maVaiTro, maVaiTro, tenVaiTro), null, chinhSach);

        Assert.Equal(mongDoi, kq.Succeeded);
    }

    [Fact]
    public async Task NguoiChuaDangNhap_BiTuChoiMoiChinhSachQuyen()
    {
        using var sp = Dung();
        using var scope = sp.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        Assert.False((await auth.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, "quyen:XK:Xem")).Succeeded);
    }

    [Fact]
    public async Task ClaimMaVaiTroHong_BiTuChoi()
    {
        using var sp = Dung();
        using var scope = sp.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var hong = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimKho.MaVaiTro, "khong-phai-so") }, "Cookies"));
        Assert.False((await auth.AuthorizeAsync(hong, null, "quyen:XK:Xem")).Succeeded);
    }

    [Fact]
    public async Task MacDinhMoiEndpointPhaiDangNhap_FallbackPolicy()
    {
        using var sp = Dung();
        var provider = sp.GetRequiredService<IAuthorizationPolicyProvider>();
        var fallback = await provider.GetFallbackPolicyAsync();
        Assert.NotNull(fallback);
        Assert.Contains(fallback!.Requirements, r => r is Microsoft.AspNetCore.Authorization.Infrastructure.DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public async Task ChinhSachKhongPhaiQuyen_VanDungCachMacDinh()
    {
        using var sp = Dung();
        var provider = sp.GetRequiredService<IAuthorizationPolicyProvider>();
        Assert.Null(await provider.GetPolicyAsync("ten-chinh-sach-khong-co"));
        var quyen = await provider.GetPolicyAsync("quyen:CK:Duyet");
        Assert.NotNull(quyen);
        Assert.Contains(quyen!.Requirements, r => r is CanQuyenRequirement { Loai: LoaiChungTu.CK, Quyen: QuyenThaoTac.Duyet });
    }

    [Fact]
    public void TrangLoiChungCuaHome_DuocMoChoNguoiChuaDangNhap_ChiAction_Error()
    {
        var cm = new ControllerModel(typeof(HomeController).GetTypeInfo(), Array.Empty<object>()) { ControllerName = "Home" };
        var loi = new ActionModel(typeof(HomeController).GetMethod(nameof(HomeController.Error))!, Array.Empty<object>()) { ActionName = "Error" };
        var index = new ActionModel(typeof(HomeController).GetMethod(nameof(HomeController.Index))!, Array.Empty<object>()) { ActionName = "Index" };
        loi.Selectors.Add(new SelectorModel());
        index.Selectors.Add(new SelectorModel());
        cm.Actions.Add(loi);
        cm.Actions.Add(index);

        new MoTrangLoiChoMoiNguoi().Apply(cm);

        Assert.Contains(loi.Selectors[0].EndpointMetadata, m => m is IAllowAnonymous);
        Assert.DoesNotContain(index.Selectors[0].EndpointMetadata, m => m is IAllowAnonymous);
    }
}

/// <summary>Quy tắc cấu trúc để Controller sau này của HeThong không vô tình mở cửa hoặc quên chống giả mạo yêu cầu.</summary>
public class BaoVeControllerHeThongTests
{
    private static IEnumerable<Type> ControllerHeThong() =>
        typeof(HeThongModule).Assembly.GetTypes()
            .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract && t.Namespace == "QuanLyKho.Web.Areas.HeThong.Controllers");

    [Fact]
    public void QuanLyNguoiDungVaPhanQuyen_ChiQuanTriVien()
    {
        foreach (var t in new[] { typeof(QuanLyKho.Web.Areas.HeThong.Controllers.NguoiDungController), typeof(QuanLyKho.Web.Areas.HeThong.Controllers.PhanQuyenController) })
        {
            var a = Assert.Single(t.GetCustomAttributes<AuthorizeAttribute>());
            Assert.Equal(VaiTroTen.QuanTriVien, a.Roles);
        }
    }

    [Fact]
    public void ChiTaiKhoanController_CoActionChoPhepAnDanh()
    {
        foreach (var t in ControllerHeThong())
        {
            var coAnDanh = t.GetCustomAttributes<AllowAnonymousAttribute>().Any()
                           || t.GetMethods().Any(m => m.GetCustomAttributes<AllowAnonymousAttribute>().Any());
            Assert.Equal(t.Name == "TaiKhoanController", coAnDanh);
        }
    }

    [Fact]
    public void MoiActionPost_CoChongGiaMaoYeuCau()
    {
        var ds = ControllerHeThong().SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes<HttpPostAttribute>().Any())).ToList();
        Assert.NotEmpty(ds);
        foreach (var m in ds)
            Assert.True(m.GetCustomAttributes<ValidateAntiForgeryTokenAttribute>().Any(), $"{m.DeclaringType!.Name}.{m.Name} thiếu [ValidateAntiForgeryToken]");
    }

    [Fact]
    public void DangXuat_LaPost_VaKhongCoTrangThaiGiaoDienGiaLap()
    {
        var m = typeof(QuanLyKho.Web.Areas.HeThong.Controllers.TaiKhoanController).GetMethod("DangXuat")!;
        Assert.True(m.GetCustomAttributes<HttpPostAttribute>().Any());
        Assert.Null(typeof(HeThongModule).Assembly.GetType("QuanLyKho.Web.Areas.HeThong.Controllers.DevVaiTroController"));
    }
}
