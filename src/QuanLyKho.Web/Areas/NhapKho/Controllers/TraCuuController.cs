using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.NhapKho.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.NhapKho.Controllers;

/// <summary>
/// Tra cứu vị trí và tồn (việc T1-5): hàng nằm ở ô nào, còn bao nhiêu; và báo cáo tổng hợp tồn từng kho.
/// Chỉ đọc dữ liệu, nghiệp vụ nằm ở <see cref="TraCuuTonService"/>. Quyền: Xem phiếu nhập (NK).
/// </summary>
[Area("NhapKho")]
[CanQuyen(LoaiChungTu.NK, QuyenThaoTac.Xem)]
public class TraCuuController(KhoDbContext? db = null) : Controller
{
    // Chưa cấu hình chuỗi kết nối thì KhoDbContext không có, báo lỗi rõ ràng thay vì lỗi khó hiểu.
    private KhoDbContext Db => db ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:KhoDb nên chưa tra cứu được tồn kho.");

    private TraCuuTonService Ts => new(Db);

    /// <summary>Tra theo kho, biến thể, lô, ô. Không điền gì thì hiện toàn bộ hàng đã sắp xếp (tối đa 500 dòng).</summary>
    [HttpGet]
    public async Task<IActionResult> Index(int? maKho, string? bienThe, string? lo, string? o)
    {
        var loc = new BoLocTra(maKho, bienThe, lo, o);
        ViewBag.Loc = loc;
        ViewBag.Kho = await Db.Kho.AsNoTracking().OrderBy(k => k.MaKho).ToListAsync();
        return View(await Ts.Tra(loc));
    }

    /// <summary>Báo cáo tổng hợp tồn: đầu kỳ, nhập, xuất, cuối kỳ của từng biến thể, tính riêng từng kho. Mặc định từ đầu tháng đến hôm nay.</summary>
    [HttpGet]
    public async Task<IActionResult> BaoCao(DateOnly? tuNgay, DateOnly? denNgay, int? maKho)
    {
        var homNay = DateOnly.FromDateTime(DateTime.Now);
        var tu = tuNgay ?? new DateOnly(homNay.Year, homNay.Month, 1);
        var den = denNgay ?? homNay;
        ViewBag.Tu = tu;
        ViewBag.Den = den;
        ViewBag.MaKho = maKho;
        ViewBag.Kho = await Db.Kho.AsNoTracking().OrderBy(k => k.MaKho).ToListAsync();

        if (tu > den || tu.Year < 2000 || den.Year > 2100)
        {
            ViewBag.Loi = "Chọn khoảng ngày hợp lệ: từ ngày không sau đến ngày, trong khoảng năm 2000 đến 2100.";
            return View(new List<DongBaoCao>());
        }
        // Ngày người dùng chọn tính theo giờ máy chủ; sổ cái lưu giờ UTC. Kỳ báo cáo là cả ngày "từ" đến hết ngày "đến".
        return View(await Ts.BaoCao(DauNgayUtc(tu), DauNgayUtc(den.AddDays(1)), maKho));
    }

    private static DateTime DauNgayUtc(DateOnly ngay) =>
        DateTime.SpecifyKind(ngay.ToDateTime(TimeOnly.MinValue), DateTimeKind.Local).ToUniversalTime();
}
