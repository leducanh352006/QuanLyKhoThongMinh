using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Infrastructure.Database;
using QuanLyKho.Web.Infrastructure.Menu;
using QuanLyKho.Web.Models;
using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Controllers;

public class HomeController(
    INguoiDungHienTai nguoiDungHienTai,
    IMenuService menu,
    IDatabaseStatusService csdl,
    IWebHostEnvironment env) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var nguoiDung = nguoiDungHienTai.Lay();
        var mau = new TomTatDuLieuMau(
            SeedDataSet.Kho.Count,
            SeedDataSet.ViTri.Count,
            SeedDataSet.ViTri.Count(v => v.CapViTri == SeedDataSet.CapO),
            SeedDataSet.SanPham.Count,
            SeedDataSet.BienThe.Count,
            SeedDataSet.LoHang.Count,
            SeedDataSet.NguoiDung.Count);

        var vm = new TrangChuViewModel(
            nguoiDung,
            menu.LayMenu(nguoiDung?.TenVaiTro),
            await csdl.KiemTraAsync(ct),
            env.EnvironmentName,
            env.IsDevelopment(),
            mau);

        ViewData["Title"] = "Trang chủ";
        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewData["Title"] = "Có lỗi xảy ra";
        return View(new LoiViewModel(Activity.Current?.Id ?? HttpContext.TraceIdentifier));
    }
}
