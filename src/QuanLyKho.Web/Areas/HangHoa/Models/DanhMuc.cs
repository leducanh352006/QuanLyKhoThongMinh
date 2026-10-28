namespace QuanLyKho.Web.Areas.HangHoa.Models;

/// <summary>Bảng DanhMuc (việc T3-1). Không có TrangThai: danh mục còn sản phẩm tham chiếu thì không xóa được.</summary>
public class DanhMuc
{
    public int MaDM { get; set; }

    public string TenDM { get; set; } = "";
}
