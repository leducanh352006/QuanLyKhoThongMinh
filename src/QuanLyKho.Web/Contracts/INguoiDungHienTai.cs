namespace QuanLyKho.Web.Contracts;

/// <summary>
/// Người đang thao tác (nguồn của MaND ghi sổ cái và nhật ký). Hiện dùng bản giả theo vai trò chọn ở môi trường Development;
/// việc "Đăng nhập" của Nhóm 1 sẽ thay bằng bản thật mà không đổi giao diện này.
/// </summary>
public interface INguoiDungHienTai
{
    /// <summary>null nếu chưa đăng nhập.</summary>
    NguoiDungHienTai? Lay();
}
