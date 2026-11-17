namespace QuanLyKho.Web.Infrastructure.Menu;

public interface IMenuService
{
    /// <summary>Menu của một vai trò; vai trò null (chưa đăng nhập) thì rỗng. Nhóm không còn mục nào bị bỏ.</summary>
    IReadOnlyList<MenuNhom> LayMenu(string? tenVaiTro);
}

public sealed class MenuService(IEnumerable<IMenuNguon> nguon) : IMenuService
{
    public IReadOnlyList<MenuNhom> LayMenu(string? tenVaiTro)
    {
        if (string.IsNullOrEmpty(tenVaiTro)) return Array.Empty<MenuNhom>();

        return nguon
            .OrderBy(n => n.ThuTu)
            .Select(n => n.LayNhom())
            .Select(g => new MenuNhom(g.Ten, g.Muc.Where(m => m.VaiTro.Contains(tenVaiTro)).ToList()))
            .Where(g => g.Muc.Count > 0)
            .ToList();
    }
}

/// <summary>Trang tổng quan của một Area: các mục menu của Area đó mà vai trò được thấy, bỏ chính mục Tổng quan.</summary>
public sealed record TongQuanArea(string TieuDe, string MoTa, IReadOnlyList<MenuMuc> ChucNang);

public static class MenuServiceExtensions
{
    public static IReadOnlyList<MenuMuc> LayChucNangArea(this IMenuService menu, string? tenVaiTro, string area) =>
        menu.LayMenu(tenVaiTro).SelectMany(n => n.Muc).Where(m => m.Area == area && m.Controller != "TrangChu").ToList();
}
