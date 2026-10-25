using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using QuanLyKho.Web.Areas.DieuChuyen;
using QuanLyKho.Web.Areas.HangHoa;
using QuanLyKho.Web.Areas.HeThong;
using QuanLyKho.Web.Areas.NhapKho;
using QuanLyKho.Web.Areas.XuatKho;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Infrastructure;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Tests;

public class MenuVaAreaTests
{
    private static readonly string[] NamArea = { "HeThong", "NhapKho", "XuatKho", "HangHoa", "DieuChuyen" };

    private static IMenuService TaoMenu()
    {
        var services = new ServiceCollection()
            .AddKhoInfrastructure()
            .AddHeThong().AddNhapKho().AddXuatKho().AddHangHoa().AddDieuChuyen();
        return services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IMenuService>();
    }

    private static string[] AreaThay(IMenuService menu, string vaiTro) =>
        menu.LayMenu(vaiTro).SelectMany(g => g.Muc).Select(m => m.Area).Distinct().OrderBy(x => x).ToArray();

    [Fact]
    public void NamAreaTonTai_MoiAreaCoItNhatMotController()
    {
        var kieu = typeof(IStockService).Assembly.GetTypes().Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract).ToList();
        foreach (var area in NamArea)
        {
            var coController = kieu.Any(t => t.GetCustomAttributes(typeof(AreaAttribute), false)
                .Cast<AreaAttribute>().Any(a => a.RouteValue == area));
            Assert.True(coController, $"Area {area} chưa có controller nào");
        }
    }

    [Fact]
    public void MoiAreaTrenDia_CoDuBaThuMuc_ControllersViewsServicesModels()
    {
        // Chạy từ thư mục bin của dự án kiểm thử: đi ngược lên tới thư mục chứa file .sln rồi vào src/QuanLyKho.Web/Areas.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.GetFiles("*.sln").Any()) dir = dir.Parent;
        Assert.NotNull(dir);

        foreach (var area in NamArea)
            foreach (var con in new[] { "Controllers", "Views", "Services", "Models" })
                Assert.True(Directory.Exists(Path.Combine(dir!.FullName, "src", "QuanLyKho.Web", "Areas", area, con)), $"Thiếu Areas/{area}/{con}");
    }

    [Fact]
    public void QuanTriVien_ThayHeThongVaHangHoa()
    {
        Assert.Equal(new[] { "HangHoa", "HeThong" }, AreaThay(TaoMenu(), VaiTroTen.QuanTriVien));
    }

    [Fact]
    public void QuanLyKho_ThayCaNamKhuVuc()
    {
        Assert.Equal(NamArea.OrderBy(x => x).ToArray(), AreaThay(TaoMenu(), VaiTroTen.QuanLyKho));
    }

    [Fact]
    public void ThuKho_ThayNhapXuatHangHoaDieuChuyen_KhongThayHeThong()
    {
        Assert.Equal(new[] { "DieuChuyen", "HangHoa", "NhapKho", "XuatKho" }, AreaThay(TaoMenu(), VaiTroTen.ThuKho));
    }

    [Fact]
    public void NhanVienBanHang_ChiThayXuatKho()
    {
        Assert.Equal(new[] { "XuatKho" }, AreaThay(TaoMenu(), VaiTroTen.NhanVienBanHang));
    }

    [Fact]
    public void ChuaDangNhapHoacVaiTroLa_MenuRong()
    {
        var menu = TaoMenu();
        Assert.Empty(menu.LayMenu(null));
        Assert.Empty(menu.LayMenu(""));
        Assert.Empty(menu.LayMenu("Vai trò không có"));
    }

    [Fact]
    public void ThuTuNhomTheoNhom_HeThongTruocNhapTruocXuat()
    {
        var ten = TaoMenu().LayMenu(VaiTroTen.QuanLyKho).Select(g => g.Ten).ToArray();
        Assert.Equal(new[] { "Hệ thống", "Nhập kho", "Xuất kho", "Hàng hóa", "Điều chuyển kho" }, ten);
    }
}
