using System.ComponentModel.DataAnnotations;

namespace QuanLyKho.Web.Areas.HangHoa.Models;

// ---- Nhà cung cấp, danh mục sản phẩm (việc T3-1) ----

public class NhaCungCapThemViewModel
{
    [Required(ErrorMessage = "Nhập tên nhà cung cấp."), StringLength(100)]
    [Display(Name = "Tên nhà cung cấp")]
    public string TenNCC { get; set; } = "";

    [StringLength(15)]
    [Display(Name = "Số điện thoại")]
    public string? SDT { get; set; }

    [StringLength(255)]
    [Display(Name = "Địa chỉ")]
    public string? DiaChi { get; set; }
}

public sealed class NhaCungCapSuaViewModel : NhaCungCapThemViewModel
{
    public int MaNCC { get; set; }
}

public class DanhMucThemViewModel
{
    [Required(ErrorMessage = "Nhập tên danh mục."), StringLength(100)]
    [Display(Name = "Tên danh mục")]
    public string TenDM { get; set; } = "";
}

public sealed class DanhMucSuaViewModel : DanhMucThemViewModel
{
    public int MaDM { get; set; }
}
