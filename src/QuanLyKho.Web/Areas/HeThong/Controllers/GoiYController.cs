using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Controllers;

/// <summary>Một dòng trên form "Sửa rồi duyệt". Bỏ chọn <see cref="Giu"/> để loại dòng khỏi đề xuất.</summary>
public sealed class DongSuaGoiY
{
    public bool Giu { get; set; } = true;
    public int MaBT { get; set; }
    public int? MaLo { get; set; }
    public int? MaViTri { get; set; }
    public int? MaKhoNguon { get; set; }
    public int? MaKhoDich { get; set; }
    public int SoLuong { get; set; }
    public string LyDo { get; set; } = "";
}

/// <summary>Dữ liệu cho khung duyệt một gợi ý (dùng ở danh sách, trang chi tiết và thành phần nhúng vào phiếu).</summary>
public sealed record GoiYKhungViewModel(
    GoiYDto GoiY, bool DuocXuLy, bool CoXuLy, string QuayLai, IReadOnlyList<ChiTietGoiYDto>? ChiTietGoc = null, bool MoRong = false);

public sealed class GoiYDanhSachViewModel
{
    public string? Loai { get; set; }
    public string TrangThai { get; set; } = "cho";
    public IReadOnlyList<GoiYKhungViewModel> GoiY { get; set; } = Array.Empty<GoiYKhungViewModel>();
    public IReadOnlyList<LoaiGoiY> LoaiDuocXuLy { get; set; } = Array.Empty<LoaiGoiY>();
    public bool LaPhatTrien { get; set; }
    public IReadOnlyList<ThaoTacMau> ThaoTacMau { get; set; } = Array.Empty<ThaoTacMau>();
}

/// <summary>
/// Màn hình gợi ý AI (việc T4-8): danh sách theo loại và trạng thái, chi tiết (so dòng gốc với dòng đã sửa), và ba cách xử lý.
/// Thẩm quyền kiểm ở <see cref="GoiYService"/> (phía máy chủ) theo <see cref="QuyTacGoiY.VaiTroDuocDuyet"/>; giao diện chỉ ẩn nút cho dễ dùng.
/// </summary>
[Area("HeThong")]
[Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho + "," + VaiTroTen.ThuKho)]
public class GoiYController(
    IGoiYService goiY, IGoiYQuanLy quanLy, INguoiDungHienTai hienTai, IWebHostEnvironment env,
    ThaoTacMauStore thaoTacMau, GoiYMauService mau) : Controller
{
    public async Task<IActionResult> Index(string? loai, string? trangThai, CancellationToken ct)
    {
        ViewData["Title"] = "Gợi ý AI";
        LoaiGoiY? loc = Enum.TryParse<LoaiGoiY>(loai, out var l) && Enum.IsDefined(l) ? l : null;
        TrangThaiGoiY? tt = trangThai switch { "daxuly" => null, _ => TrangThaiGoiY.ChoDuyet };

        var vaiTro = hienTai.Lay()?.TenVaiTro;
        var ds = await quanLy.DanhSach(tt, loc, 100, ct);
        if (tt is null) ds = ds.Where(g => g.TrangThai != TrangThaiGoiY.ChoDuyet).ToList();
        var quayLai = Request.Path + Request.QueryString;

        var model = new GoiYDanhSachViewModel
        {
            Loai = loc?.ToString(),
            TrangThai = tt is null ? "daxuly" : "cho",
            LoaiDuocXuLy = QuyTacGoiY.LoaiDuocDuyet(vaiTro),
            GoiY = ds.Select(g => new GoiYKhungViewModel(g, QuyTacGoiY.DuocDuyet(g.Loai, vaiTro), quanLy.CoXuLy(g.Loai), quayLai)).ToList(),
            LaPhatTrien = env.IsDevelopment(),
        };
        if (model.LaPhatTrien)
        {
            // Chỉ hiện thao tác của gợi ý đã duyệt thật (thao tác của lần duyệt bị hoàn tác không tính).
            var daDuyet = (await quanLy.DanhSach(null, null, 1000, ct))
                .Where(g => g.TrangThai is TrangThaiGoiY.DaDuyet or TrangThaiGoiY.DaSuaVaDuyet).Select(g => g.MaGoiY).ToHashSet();
            model.ThaoTacMau = thaoTacMau.TatCa().Where(t => daDuyet.Contains(t.MaGoiY)).ToList();
        }
        return View(model);
    }

    public async Task<IActionResult> ChiTiet(int id, CancellationToken ct)
    {
        var g = await goiY.Lay(id, ct);
        if (g is null) return NotFound();
        ViewData["Title"] = $"Gợi ý {g.Loai} #{g.MaGoiY}";
        var goc = await quanLy.LayChiTietGoc(id, ct);
        return View(new GoiYKhungViewModel(g, QuyTacGoiY.DuocDuyet(g.Loai, hienTai.Lay()?.TenVaiTro), quanLy.CoXuLy(g.Loai),
            Request.Path + Request.QueryString, goc, MoRong: true));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Duyet(int id, string? quayLai, CancellationToken ct)
    {
        var nguoi = hienTai.Lay();
        if (nguoi is null) return Challenge();
        return Ve(await goiY.Duyet(id, nguoi.MaND, ct), quayLai);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SuaVaDuyet(int id, List<DongSuaGoiY>? dong, string? quayLai, CancellationToken ct)
    {
        var nguoi = hienTai.Lay();
        if (nguoi is null) return Challenge();
        var chiTiet = (dong ?? new()).Where(d => d.Giu)
            .Select(d => new ChiTietGoiYDto(d.MaBT, d.SoLuong, d.LyDo ?? "", d.MaLo, d.MaViTri, d.MaKhoNguon, d.MaKhoDich)).ToList();
        return Ve(await goiY.SuaVaDuyet(id, chiTiet, nguoi.MaND, ct), quayLai);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TuChoi(int id, string? lyDo, string? quayLai, CancellationToken ct)
    {
        var nguoi = hienTai.Lay();
        if (nguoi is null) return Challenge();
        return Ve(await goiY.TuChoi(id, nguoi.MaND, lyDo, ct), quayLai);
    }

    /// <summary>Tạo gợi ý mẫu để thử khung (chỉ Development).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]
    public async Task<IActionResult> TaoMau(int? maPN, bool aiLoi, CancellationToken ct)
    {
        if (!env.IsDevelopment()) return NotFound();
        return Ve(aiLoi ? await mau.TaoMauAiLoi(ct) : await mau.TaoMau(maPN is > 0 ? maPN : null, ct), null);
    }

    private IActionResult Ve(KetQua kq, string? quayLai)
    {
        TempData[kq.ThanhCong ? "GoiYThongBao" : "GoiYLoi"] = kq.ThongBao ?? (kq.ThanhCong ? "Đã xử lý." : "Không xử lý được.");
        return !string.IsNullOrEmpty(quayLai) && Url.IsLocalUrl(quayLai) ? LocalRedirect(quayLai) : RedirectToAction(nameof(Index));
    }
}

/// <summary>
/// Thành phần duyệt gợi ý dùng chung. Nhóm khác nhúng vào màn hình của mình:
/// <c>@await Component.InvokeAsync("DuyetGoiY", new { loai = LoaiGoiY.G1, maPX = Model.MaPX })</c>
/// (G2 truyền maPN; G3, G4 không cần phiếu). Hiện các gợi ý Chờ duyệt kèm lý do và ba nút; không có gợi ý thì báo người dùng làm tay.
/// </summary>
public sealed class DuyetGoiYViewComponent(IGoiYService goiY, IGoiYQuanLy quanLy, INguoiDungHienTai hienTai) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(LoaiGoiY loai, int? maPX = null, int? maPN = null, int? maPC = null)
    {
        IReadOnlyList<GoiYDto> ds;
        try
        {
            ds = (await goiY.DanhSachChoDuyet(loai)).Where(g =>
                (maPX is null || g.ThamChieu.MaPX == maPX) && (maPN is null || g.ThamChieu.MaPN == maPN) && (maPC is null || g.ThamChieu.MaPC == maPC)).ToList();
        }
        catch (Exception)
        {
            ds = Array.Empty<GoiYDto>();   // đường lui: khung gợi ý lỗi không được làm hỏng màn hình phiếu
        }
        var vaiTro = hienTai.Lay()?.TenVaiTro;
        var quayLai = ViewContext.HttpContext.Request.Path + ViewContext.HttpContext.Request.QueryString;
        var model = ds.Select(g => new GoiYKhungViewModel(g, QuyTacGoiY.DuocDuyet(g.Loai, vaiTro), quanLy.CoXuLy(g.Loai), quayLai)).ToList();
        return View("~/Areas/HeThong/Views/Shared/Components/DuyetGoiY/Default.cshtml", (loai, (IReadOnlyList<GoiYKhungViewModel>)model));
    }
}
