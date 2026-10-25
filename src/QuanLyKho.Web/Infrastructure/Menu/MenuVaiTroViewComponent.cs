using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Infrastructure.Menu;

/// <summary>Menu bên trái, chỉ hiện các mục dành cho vai trò đang dùng. Dùng trong layout: &lt;vc:menu-vai-tro /&gt;.</summary>
public sealed class MenuVaiTroViewComponent(IMenuService menu, INguoiDungHienTai nguoiDung) : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var area = ViewContext.RouteData.Values["area"] as string;
        var dangODauTrang = area is null && (ViewContext.RouteData.Values["controller"] as string) == "Home";
        var nhom = menu.LayMenu(nguoiDung.Lay()?.TenVaiTro);
        return View(new MenuViewModel(nhom, area, dangODauTrang));
    }
}
