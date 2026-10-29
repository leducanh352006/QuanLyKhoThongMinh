using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Areas.HeThong.Controllers;

/// <summary>Tổng quan Area Hệ thống: các chức năng theo vai trò, số kho và kết quả đối chiếu sổ cái với tồn kho (việc T4-4).</summary>
public sealed record HeThongTongQuan(IReadOnlyList<MenuMuc> ChucNang, int SoKhoHoatDong, int SoKho, int? SoChenhLechSoCai, string? LoiDoiChieu);

[Area("HeThong")]
[Authorize(Roles = VaiTroTen.QuanTriVien + "," + VaiTroTen.QuanLyKho)]   // khớp menu: Quản trị viên và Quản lý kho
public class TrangChuController(IMenuService menu, INguoiDungHienTai hienTai, IKhoQuanLy kho, IStockService ton) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Hệ thống";
        var chucNang = menu.LayMenu(hienTai.Lay()?.TenVaiTro)
            .Where(n => n.Muc.Any(m => m.Area == "HeThong")).SelectMany(n => n.Muc)
            .Where(m => m.Area == "HeThong" && m.Controller != "TrangChu").ToList();
        var dsKho = await kho.LayDanhSach(ct);

        int? chenhLech = null;
        string? loi = null;
        try
        {
            chenhLech = (await ton.DoiChieuSoCai(ct)).Count;
        }
        catch (Exception)
        {
            loi = "Chưa đối chiếu được sổ cái (kiểm tra kết nối cơ sở dữ liệu).";
        }
        return View(new HeThongTongQuan(chucNang, dsKho.Count(k => k.DangHoatDong), dsKho.Count, chenhLech, loi));
    }
}
