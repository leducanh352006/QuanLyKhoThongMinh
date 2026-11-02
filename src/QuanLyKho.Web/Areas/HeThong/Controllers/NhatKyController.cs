using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Controllers;

/// <summary>Dữ liệu màn hình tra cứu nhật ký: giá trị bộ lọc đang chọn và một trang kết quả.</summary>
public sealed class NhatKyViewModel
{
    public int? MaND { get; set; }
    public string? HanhDong { get; set; }
    public string? LoaiCT { get; set; }
    public int? MaCT { get; set; }
    public DateOnly? TuNgay { get; set; }
    public DateOnly? DenNgay { get; set; }
    public int Trang { get; set; } = 1;

    public NhatKyTrang Ket { get; set; } = new(Array.Empty<NhatKyDong>(), 0, 1, NhatKyLoc.CoTrangMacDinh);
    public IReadOnlyList<SelectListItem> NguoiDung { get; set; } = Array.Empty<SelectListItem>();
}

/// <summary>
/// Tra cứu nhật ký thao tác (báo cáo F10): chỉ Quản trị viên, lọc theo người, loại thao tác, chứng từ và thời gian. Kiểm quyền ở phía máy chủ.
/// Ngày lọc hiểu theo giờ máy chủ, dữ liệu lưu giờ UTC.
/// </summary>
[Area("HeThong")]
[Authorize(Roles = VaiTroTen.QuanTriVien)]
public class NhatKyController(INhatKyQuanLy nhatKy, IHeThongStore nguoiDung) : Controller
{
    public async Task<IActionResult> Index([FromQuery] NhatKyViewModel loc, CancellationToken ct)
    {
        ViewData["Title"] = "Nhật ký thao tác";

        LoaiChungTu? loai = Enum.TryParse<LoaiChungTu>(loc.LoaiCT, out var l) && Enum.IsDefined(l) ? l : null;
        var boLoc = new NhatKyLoc(
            loc.MaND, loc.HanhDong, loai, loc.MaCT,
            loc.TuNgay is DateOnly tu ? DauNgayUtc(tu) : null,
            loc.DenNgay is DateOnly den ? DauNgayUtc(den.AddDays(1)).AddTicks(-1) : null,
            loc.Trang);

        loc.Ket = await nhatKy.Tim(boLoc, ct);
        loc.Trang = loc.Ket.Trang;
        loc.NguoiDung = (await nguoiDung.LayNguoiDung(ct)).OrderBy(n => n.HoTen)
            .Select(n => new SelectListItem($"{n.HoTen} ({n.TenDangNhap})", n.MaND.ToString(), loc.MaND == n.MaND)).ToList();
        return View(loc);
    }

    private static DateTime DauNgayUtc(DateOnly ngay) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(ngay.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), TimeZoneInfo.Local);
}
