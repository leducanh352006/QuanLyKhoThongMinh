using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Controllers;

/// <summary>Dữ liệu màn hình lịch sử biến động tồn: giá trị bộ lọc đang chọn và một trang kết quả.</summary>
public sealed class LichSuTonViewModel
{
    public int? MaLo { get; set; }
    public int? MaViTri { get; set; }
    public string? LoaiBienDong { get; set; }
    public string? LoaiCT { get; set; }
    public int? MaCT { get; set; }
    public DateOnly? TuNgay { get; set; }
    public DateOnly? DenNgay { get; set; }
    public int Trang { get; set; } = 1;

    public LichSuTonTrang Ket { get; set; } = new(Array.Empty<LichSuTonDong>(), 0, 1, LichSuTonLoc.CoTrangMacDinh);
}

/// <summary>
/// Lịch sử biến động tồn và đối chiếu sổ cái (việc T4-6): cho Quản trị viên và Quản lý kho xem mọi dòng sổ cái, lọc theo lô, ô,
/// loại biến động, loại chứng từ và thời gian. Chỉ đọc. Kiểm quyền ở phía máy chủ; ngày lọc hiểu theo giờ máy chủ, dữ liệu lưu giờ UTC.
/// </summary>
[Area("HeThong")]
[Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]
public class LichSuTonController(ILichSuTonQuanLy lichSu) : Controller
{
    public async Task<IActionResult> Index([FromQuery] LichSuTonViewModel loc, CancellationToken ct)
    {
        ViewData["Title"] = "Lịch sử biến động tồn";

        LoaiChungTu? loai = Enum.TryParse<LoaiChungTu>(loc.LoaiCT, out var l) && Enum.IsDefined(l) ? l : null;
        var boLoc = new LichSuTonLoc(
            loc.MaLo, loc.MaViTri, string.IsNullOrWhiteSpace(loc.LoaiBienDong) ? null : loc.LoaiBienDong.Trim(), loai, loc.MaCT,
            loc.TuNgay is DateOnly tu ? DauNgayUtc(tu) : null,
            loc.DenNgay is DateOnly den ? DauNgayUtc(den.AddDays(1)).AddTicks(-1) : null,
            loc.Trang);

        loc.Ket = await lichSu.Tim(boLoc, ct);
        loc.Trang = loc.Ket.Trang;
        return View(loc);
    }

    /// <summary>Đối chiếu tổng sổ cái với TonKho trên dữ liệu hiện có (dùng <see cref="IStockService.DoiChieuSoCai"/> của T4-4).</summary>
    public async Task<IActionResult> DoiChieu(CancellationToken ct)
    {
        ViewData["Title"] = "Đối chiếu sổ cái";
        return View(await lichSu.DoiChieu(ct));
    }

    private static DateTime DauNgayUtc(DateOnly ngay) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(ngay.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), TimeZoneInfo.Local);
}
