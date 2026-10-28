using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

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

// ---- Sản phẩm, biến thể (việc T3-2) ----

public class SanPhamThemViewModel
{
    [Required(ErrorMessage = "Nhập tên sản phẩm."), StringLength(100)]
    [Display(Name = "Tên sản phẩm")]
    public string TenSP { get; set; } = "";

    [Range(1, int.MaxValue, ErrorMessage = "Chọn danh mục.")]
    [Display(Name = "Danh mục")]
    public int MaDM { get; set; }

    [Required(ErrorMessage = "Nhập đơn vị tính."), StringLength(20)]
    [Display(Name = "Đơn vị tính")]
    public string DonViTinh { get; set; } = "";

    [Display(Name = "Có hạn dùng")]
    public bool CoHanDung { get; set; }
}

public sealed class SanPhamSuaViewModel : SanPhamThemViewModel
{
    public int MaSP { get; set; }
}

public class BienTheThemViewModel
{
    public int MaSP { get; set; }

    [BindNever, ValidateNever]
    public string TenSP { get; set; } = "";

    [Required(ErrorMessage = "Nhập mã vạch."), StringLength(50)]
    [Display(Name = "Mã vạch")]
    public string MaVach { get; set; } = "";

    [Required(ErrorMessage = "Nhập thuộc tính (ví dụ: Size M, trắng)."), StringLength(100)]
    [Display(Name = "Thuộc tính")]
    public string ThuocTinh { get; set; } = "";

    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Dài phải lớn hơn 0.")]
    [Display(Name = "Dài (cm)")]
    public decimal Dai { get; set; }

    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Rộng phải lớn hơn 0.")]
    [Display(Name = "Rộng (cm)")]
    public decimal Rong { get; set; }

    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Cao phải lớn hơn 0.")]
    [Display(Name = "Cao (cm)")]
    public decimal Cao { get; set; }

    [Range(0.001, (double)decimal.MaxValue, ErrorMessage = "Khối lượng phải lớn hơn 0.")]
    [Display(Name = "Khối lượng (kg)")]
    public decimal KhoiLuong { get; set; }
}

public sealed class BienTheSuaViewModel : BienTheThemViewModel
{
    public int MaBT { get; set; }
}
