namespace QuanLyKho.Web.Infrastructure.Menu;

/// <summary>Một mục menu trỏ tới một màn hình. <paramref name="VaiTro"/> là các vai trò được thấy mục này.</summary>
public sealed record MenuMuc(
    string Ten, string MoTa, string Area, string Controller, string Action, IReadOnlyList<string> VaiTro);

public sealed record MenuNhom(string Ten, IReadOnlyList<MenuMuc> Muc);

/// <summary>
/// Mỗi Area đóng góp menu của chính mình bằng một lớp implement interface này (đăng ký trong module của Area),
/// nên không ai phải sửa file menu chung và không xung đột khi gộp code.
/// </summary>
public interface IMenuNguon
{
    /// <summary>Thứ tự nhóm trong menu (nhỏ lên trước).</summary>
    int ThuTu { get; }

    MenuNhom LayNhom();
}

/// <summary>Menu đã lọc theo vai trò, kèm Area đang mở để tô sáng.</summary>
public sealed record MenuViewModel(IReadOnlyList<MenuNhom> Nhom, string? AreaHienTai, bool DangODauTrang);
