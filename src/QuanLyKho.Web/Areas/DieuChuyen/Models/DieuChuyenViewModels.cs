using System.ComponentModel.DataAnnotations;

namespace QuanLyKho.Web.Areas.DieuChuyen.Models;

// ---- Tuyến vận chuyển, định mức kho (việc T5-1) ----

public class TuyenVanChuyenThemViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Chọn kho xuất.")]
    [Display(Name = "Kho xuất")]
    public int MaKhoXuat { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Chọn kho nhập.")]
    [Display(Name = "Kho nhập")]
    public int MaKhoNhap { get; set; }

    [Range(0, (double)decimal.MaxValue, ErrorMessage = "Đơn giá vận chuyển không được âm.")]
    [Display(Name = "Đơn giá vận chuyển (đ/đơn vị)")]
    public decimal DonGiaVC { get; set; }

    [Range(0, (double)decimal.MaxValue, ErrorMessage = "Phí mỗi chuyến không được âm.")]
    [Display(Name = "Phí cố định mỗi chuyến (đ)")]
    public decimal PhiChuyen { get; set; }
}

public sealed class TuyenVanChuyenSuaViewModel
{
    public int MaKhoXuat { get; set; }
    public int MaKhoNhap { get; set; }
    public string TenKhoXuat { get; set; } = "";
    public string TenKhoNhap { get; set; } = "";

    [Range(0, (double)decimal.MaxValue, ErrorMessage = "Đơn giá vận chuyển không được âm.")]
    [Display(Name = "Đơn giá vận chuyển (đ/đơn vị)")]
    public decimal DonGiaVC { get; set; }

    [Range(0, (double)decimal.MaxValue, ErrorMessage = "Phí mỗi chuyến không được âm.")]
    [Display(Name = "Phí cố định mỗi chuyến (đ)")]
    public decimal PhiChuyen { get; set; }
}

public class DinhMucKhoThemViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Chọn kho.")]
    [Display(Name = "Kho")]
    public int MaKho { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Chọn biến thể.")]
    [Display(Name = "Biến thể")]
    public int MaBT { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Tồn tối thiểu không được âm.")]
    [Display(Name = "Tồn tối thiểu (m)")]
    public int TonToiThieu { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số ngày an toàn không được âm.")]
    [Display(Name = "Số ngày an toàn (N)")]
    public int NgayAnToan { get; set; }
}

public sealed class DinhMucKhoSuaViewModel
{
    public int MaKho { get; set; }
    public int MaBT { get; set; }
    public string TenKho { get; set; } = "";
    public string TenBienThe { get; set; } = "";

    [Range(0, int.MaxValue, ErrorMessage = "Tồn tối thiểu không được âm.")]
    [Display(Name = "Tồn tối thiểu (m)")]
    public int TonToiThieu { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số ngày an toàn không được âm.")]
    [Display(Name = "Số ngày an toàn (N)")]
    public int NgayAnToan { get; set; }
}
