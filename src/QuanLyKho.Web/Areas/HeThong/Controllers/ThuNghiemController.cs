using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Controllers;

/// <summary>
/// Trang thử các thành phần dùng chung. Chỉ mở ở môi trường Development.
/// Có hai hành động mẫu để xem <c>[CanQuyen]</c> hoạt động: đăng nhập bằng từng vai trò rồi mở từng địa chỉ.
/// </summary>
[Area("HeThong")]
public class ThuNghiemController(IWebHostEnvironment env, IPhanQuyenService phanQuyen) : Controller
{
    public IActionResult QuetMa()
    {
        if (!env.IsDevelopment()) return NotFound();
        ViewData["Title"] = "Thử thành phần quét mã";
        return View();
    }

    /// <summary>/HeThong/ThuNghiem/DinhKem?loai=NK&amp;maCT=1: thử thành phần đính kèm dùng chung (T4-7) trên một chứng từ bất kỳ.</summary>
    public IActionResult DinhKem(LoaiChungTu loai = LoaiChungTu.NK, int maCT = 1)
    {
        if (!env.IsDevelopment()) return NotFound();
        ViewData["Title"] = "Thử thành phần đính kèm";
        return View(new DinhKemThu(loai, maCT));
    }

    /// <summary>/HeThong/ThuNghiem/DuyetPhieuXuat: chỉ vai trò có quyền Duyệt trên phiếu xuất (mặc định Quản trị viên, Quản lý kho).</summary>
    [CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Duyet)]
    public IActionResult DuyetPhieuXuat()
        => env.IsDevelopment() ? Content("Được phép: bạn có quyền duyệt phiếu xuất.") : NotFound();

    /// <summary>/HeThong/ThuNghiem/TaoPhieuXuat: vai trò có quyền Thêm trên phiếu xuất (mặc định có cả Nhân viên bán hàng).</summary>
    [CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Them)]
    public IActionResult TaoPhieuXuat()
        => env.IsDevelopment() ? Content("Được phép: bạn có quyền tạo phiếu xuất.") : NotFound();

    /// <summary>/HeThong/ThuNghiem/KiemTraTrongCode: cách hỏi quyền bằng code trong Service (thay vì thuộc tính).</summary>
    public async Task<IActionResult> KiemTraTrongCode(CancellationToken ct)
    {
        if (!env.IsDevelopment()) return NotFound();
        var kq = await phanQuyen.KiemTra(LoaiChungTu.NK, QuyenThaoTac.Them, ct);
        return Content(kq.ThanhCong ? "Được phép tạo phiếu nhập." : kq.ThongBao ?? "Không đủ quyền.");
    }
}

public sealed record DinhKemThu(LoaiChungTu Loai, int MaCT);
