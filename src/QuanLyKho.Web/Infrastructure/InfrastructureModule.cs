using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Infrastructure.Menu;
using QuanLyKho.Web.Infrastructure.NguoiDung;

namespace QuanLyKho.Web.Infrastructure;

public static class InfrastructureModule
{
    public static IServiceCollection AddKhoInfrastructure(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<INguoiDungHienTai, DevNguoiDungHienTai>();   // việc Đăng nhập sẽ đăng ký bản thật đè lên
        return services;
    }
}
