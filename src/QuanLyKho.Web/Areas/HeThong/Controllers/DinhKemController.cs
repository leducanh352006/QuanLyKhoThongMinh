using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Controllers;

/// <summary>
/// Đính kèm tệp cho chứng từ (việc T4-7). Quyền kiểm ở phía máy chủ theo loại chứng từ trong dữ liệu gửi lên:
/// đính kèm cần quyền Thêm hoặc Sửa trên loại chứng từ đó, xem lại cần quyền Xem.
/// </summary>
[Area("HeThong")]
public class DinhKemController(
    ITepDinhKemQuanLy dinhKem, IPhanQuyenService phanQuyen, INguoiDungHienTai hienTai) : Controller
{
    public const long GioiHanYeuCauByte = QuyTacTep.ToiDaByteMacDinh + 1024 * 1024;

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(GioiHanYeuCauByte)]
    public async Task<IActionResult> Them(LoaiChungTu loai, int maCT, IFormFile? tep, string? quayLai, CancellationToken ct)
    {
        var nguoi = hienTai.Lay();
        if (nguoi is null) return Challenge();

        if (!await phanQuyen.CoQuyenHienTai(loai, QuyenThaoTac.Them, ct) && !await phanQuyen.CoQuyenHienTai(loai, QuyenThaoTac.Sua, ct))
            return Forbid();

        if (tep is null || tep.Length == 0)
            TempData["Loi"] = "Chưa chọn tệp để đính kèm.";
        else
        {
            await using var luong = tep.OpenReadStream();
            var kq = await dinhKem.Them(loai, maCT, tep.FileName, luong, nguoi.MaND, ct);
            TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        }

        return !string.IsNullOrEmpty(quayLai) && Url.IsLocalUrl(quayLai) ? LocalRedirect(quayLai) : RedirectToAction("Index", "TrangChu");
    }

    [HttpGet]
    public async Task<IActionResult> Tai(int id, CancellationToken ct)
    {
        var chungTu = await dinhKem.ChungTuCuaTep(id, ct);
        if (chungTu is null) return NotFound();
        if (!await phanQuyen.CoQuyenHienTai(chungTu.Value.Loai, QuyenThaoTac.Xem, ct)) return Forbid();

        var kq = await dinhKem.Mo(id, ct);
        if (!kq.ThanhCong || kq.GiaTri is null) return NotFound();

        Response.Headers.XContentTypeOptions = "nosniff";
        return File(kq.GiaTri.Luong, kq.GiaTri.LoaiNoiDung, kq.GiaTri.TenTep);   // luôn tải về, không để trình duyệt tự chạy nội dung tệp
    }
}

/// <summary>
/// Thành phần đính kèm dùng chung cho các phiếu. Nhóm khác nhúng vào view của mình:
/// <c>@await Component.InvokeAsync("DinhKem", new { loai = LoaiChungTu.NK, maCT = Model.MaPN })</c>.
/// Hiện danh sách tệp (nếu có quyền Xem) và ô tải tệp lên (nếu có quyền Thêm hoặc Sửa).
/// </summary>
public sealed class DinhKemViewComponent(ITepDinhKemQuanLy dinhKem, IPhanQuyenService phanQuyen) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(LoaiChungTu loai, int maCT)
    {
        var xem = await phanQuyen.CoQuyenHienTai(loai, QuyenThaoTac.Xem);
        var them = await phanQuyen.CoQuyenHienTai(loai, QuyenThaoTac.Them) || await phanQuyen.CoQuyenHienTai(loai, QuyenThaoTac.Sua);
        var tep = xem ? await dinhKem.LayTheoChungTu(loai, maCT) : Array.Empty<TepDto>();
        var quayLai = ViewContext.HttpContext.Request.Path + ViewContext.HttpContext.Request.QueryString;
        return View("~/Areas/HeThong/Views/Shared/Components/DinhKem/Default.cshtml", new DinhKemViewModel(loai, maCT, tep, xem, them, quayLai));
    }
}

public sealed record DinhKemViewModel(
    LoaiChungTu Loai, int MaCT, IReadOnlyList<TepDto> Tep, bool DuocXem, bool DuocThem, string QuayLai);
