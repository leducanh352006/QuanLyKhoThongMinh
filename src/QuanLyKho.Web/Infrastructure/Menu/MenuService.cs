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
