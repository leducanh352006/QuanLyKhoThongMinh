using Microsoft.Extensions.Configuration;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Areas.HeThong;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Contracts.Stubs;
using QuanLyKho.Web.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace QuanLyKho.Tests.KhoViTri;

/// <summary>Quy tắc cấu trúc để Controller Kho, ViTri không vô tình mở cửa hoặc quên chống giả mạo yêu cầu (cùng kiểu kiểm với T4-2).</summary>
public class BaoVeControllerKhoViTriTests
{
    private static readonly Type[] KieuController =
    {
        typeof(QuanLyKho.Web.Areas.HeThong.Controllers.KhoController),
        typeof(QuanLyKho.Web.Areas.HeThong.Controllers.ViTriController),
    };

    [Fact]
    public void MoiController_CoAuthorizeTheoVaiTro_KhongAnDanh()
    {
        foreach (var t in KieuController)
        {
            var a = Assert.Single(t.GetCustomAttributes<AuthorizeAttribute>());
            Assert.NotNull(a.Roles);
            Assert.DoesNotContain(VaiTroTen.NhanVienBanHang, a.Roles!.Split(','));
            Assert.False(t.GetCustomAttributes<AllowAnonymousAttribute>().Any(), $"{t.Name} không được cho phép ẩn danh");
        }
    }

    [Fact]
    public void KhoController_ChiQuanTriVienThemVaDoiTrangThai()
    {
        var t = typeof(QuanLyKho.Web.Areas.HeThong.Controllers.KhoController);
        foreach (var ten in new[] { "Them", "Ngung", "MoLai" })
        {
            var m = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).First(x => x.Name == ten);
            var a = m.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(a);
            Assert.Equal(VaiTroTen.QuanTriVien, a!.Roles);
        }
    }

    [Fact]
    public void ViTriController_SuaDoiChiDanhChoQuanTriVaQuanLyKho()
    {
        var t = typeof(QuanLyKho.Web.Areas.HeThong.Controllers.ViTriController);
        foreach (var ten in new[] { "Them", "Sua" })
        {
            var a = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .First(x => x.Name == ten).GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(a);
            Assert.False(a!.Roles!.Split(',').Contains(VaiTroTen.ThuKho), $"{ten} không nên cho Thủ kho sửa cấu trúc vị trí");
        }
    }

    [Fact]
    public void MoiActionPost_CoChongGiaMaoYeuCau()
    {
        var ds = KieuController.SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes<HttpPostAttribute>().Any())).ToList();
        Assert.NotEmpty(ds);
        foreach (var m in ds)
            Assert.True(m.GetCustomAttributes<ValidateAntiForgeryTokenAttribute>().Any(), $"{m.DeclaringType!.Name}.{m.Name} thiếu [ValidateAntiForgeryToken]");
    }

    [Fact]
    public void HeThongModule_DangKyDuDichVuKhoViTri()
    {
        var cauHinh = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:SeedPassword"] = "Demo@12345" }).Build();
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(cauHinh);
        services.AddKhoInfrastructure().AddContractStubs().AddHeThong();
        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        Assert.IsType<QuanLyKho.Web.Areas.HeThong.Services.KhoService>(scope.ServiceProvider.GetRequiredService<QuanLyKho.Web.Areas.HeThong.Services.IKhoQuanLy>());
        Assert.IsType<QuanLyKho.Web.Areas.HeThong.Services.ViTriService>(scope.ServiceProvider.GetRequiredService<QuanLyKho.Web.Areas.HeThong.Services.IViTriQuanLy>());
        Assert.IsType<QuanLyKho.Web.Areas.HeThong.Services.BoNhoKhoViTriStore>(scope.ServiceProvider.GetRequiredService<QuanLyKho.Web.Areas.HeThong.Services.IKhoViTriStore>());
    }
}
