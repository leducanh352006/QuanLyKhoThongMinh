using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using QuanLyKho.Web.Areas.HeThong.Services;

namespace QuanLyKho.Web.Areas.HeThong.Models;

public sealed class DangNhapViewModel
{
    [Required(ErrorMessage = "Nhập tên đăng nhập.")]
    [Display(Name = "Tên đăng nhập")]
    public string TenDangNhap { get; set; } = "";

    [Required(ErrorMessage = "Nhập mật khẩu.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string MatKhau { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public sealed class NguoiDungThemViewModel
{
    [Required(ErrorMessage = "Nhập tên đăng nhập."), StringLength(50)]
    [Display(Name = "Tên đăng nhập")]
    public string TenDangNhap { get; set; } = "";

    [Required(ErrorMessage = "Nhập mật khẩu."), DataType(DataType.Password), StringLength(128)]
    [Display(Name = "Mật khẩu")]
    public string MatKhau { get; set; } = "";

    [Required(ErrorMessage = "Nhập họ và tên."), StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = "";

    [StringLength(100)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(15)]
    [Display(Name = "Số điện thoại")]
    public string? SDT { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Chọn vai trò.")]
    [Display(Name = "Vai trò")]
    public int MaVaiTro { get; set; }
}

public sealed class NguoiDungSuaViewModel
{
    public int MaND { get; set; }

    [BindNever, ValidateNever]
    public string TenDangNhap { get; set; } = "";

    [Required(ErrorMessage = "Nhập họ và tên."), StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string HoTen { get; set; } = "";

    [StringLength(100)]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [StringLength(15)]
    [Display(Name = "Số điện thoại")]
    public string? SDT { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Chọn vai trò.")]
    [Display(Name = "Vai trò")]
    public int MaVaiTro { get; set; }
}

public sealed class DatLaiMatKhauViewModel
{
    public int MaND { get; set; }

    [BindNever, ValidateNever]
    public string TenDangNhap { get; set; } = "";

    [Required(ErrorMessage = "Nhập mật khẩu mới."), DataType(DataType.Password), StringLength(128)]
    [Display(Name = "Mật khẩu mới")]
    public string MatKhauMoi { get; set; } = "";
}

/// <summary>Một hàng của ma trận quyền (một loại chứng từ).</summary>
public sealed class DongQuyenViewModel
{
    public string LoaiChungTu { get; set; } = "";

    [BindNever, ValidateNever]
    public string TenChungTu { get; set; } = "";

    public bool CoXem { get; set; }

    public bool CoThem { get; set; }

    public bool CoSua { get; set; }

    public bool CoXoa { get; set; }

    public bool CoDuyet { get; set; }

    /// <summary>true khi vai trò này không được gán quyền Duyệt trên chứng từ này (Duyệt phát sinh kiểm kê chỉ cho Quản lý kho).</summary>
    [BindNever, ValidateNever]
    public bool KhoaDuyet { get; set; }
}

public sealed class PhanQuyenViewModel
{
    public int MaVaiTro { get; set; }

    [BindNever, ValidateNever]
    public string TenVaiTro { get; set; } = "";

    public List<DongQuyenViewModel> Dong { get; set; } = new();

    [BindNever, ValidateNever]
    public IReadOnlyList<VaiTro> DanhSachVaiTro { get; set; } = Array.Empty<VaiTro>();
}
