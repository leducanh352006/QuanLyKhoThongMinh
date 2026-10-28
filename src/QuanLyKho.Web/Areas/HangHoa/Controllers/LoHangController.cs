using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QuanLyKho.Web.Areas.HangHoa.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HangHoa.Controllers;

/// <summary>Danh sách lô và hạn dùng, lọc theo biến thể (chọn sản phẩm trước, rồi chọn biến thể, việc T3-3). Chỉ xem: lô được tạo qua IHangHoaService.TaoLo.</summary>
[Area("HangHoa")]
[Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho + "," + VaiTroTen.ThuKho)]
public class LoHangController(ILoHangQuanLy loHang, IBienTheQuanLy bienThe, ISanPhamQuanLy sanPham) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? maSP, int? maBT, CancellationToken ct)
    {
        ViewData["Title"] = "Lô hàng";

        var dsSanPham = await sanPham.LayDanhSach(ct);
        var chonSanPham = dsSanPham.FirstOrDefault(s => s.MaSP == maSP) ?? dsSanPham.FirstOrDefault();
        if (chonSanPham is null)
            return View("ChuaCoSanPham");

        var dsBienThe = await bienThe.LayTheoSanPham(chonSanPham.MaSP, ct);
        var chonBienThe = dsBienThe.FirstOrDefault(b => b.MaBT == maBT) ?? dsBienThe.FirstOrDefault();

        ViewData["DanhSachSanPham"] = new SelectList(dsSanPham, "MaSP", "TenSP", chonSanPham.MaSP);
        ViewData["TenSP"] = chonSanPham.TenSP;
        ViewData["MaSP"] = chonSanPham.MaSP;

        if (chonBienThe is null)
        {
            ViewData["DanhSachBienThe"] = null;
            return View("ChuaCoBienThe");
        }

        ViewData["DanhSachBienThe"] = new SelectList(dsBienThe, "MaBT", "ThuocTinh", chonBienThe.MaBT);
        ViewData["MaBT"] = chonBienThe.MaBT;
        ViewData["ThuocTinh"] = chonBienThe.ThuocTinh;
        return View(await loHang.LayTheoBienThe(chonBienThe.MaBT, ct));
    }
}
