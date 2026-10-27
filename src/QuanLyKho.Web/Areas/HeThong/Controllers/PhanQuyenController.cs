using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Controllers;

/// <summary>
/// Gán quyền Xem, Thêm, Sửa, Xóa, Duyệt cho từng vai trò theo loại chứng từ (Bảng 2.2). Chỉ Quản trị viên và kiểm theo VAI TRÒ (không theo bảng PhanQuyen),
/// nên không ai tự gỡ quyền của chính màn hình này mà bị khóa ngoài.
/// </summary>
[Area("HeThong")]
[Authorize(Roles = VaiTroTen.QuanTriVien)]
public class PhanQuyenController(IPhanQuyenQuanLy phanQuyen, INguoiDungQuanLy nguoiDung, INguoiDungHienTai hienTai) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? maVaiTro, CancellationToken ct)
    {
        var vaiTro = await nguoiDung.LayVaiTro(ct);
        var chon = vaiTro.FirstOrDefault(v => v.MaVaiTro == maVaiTro) ?? vaiTro.FirstOrDefault();
        if (chon is null) return NotFound();

        var vm = await DungViewModel(chon, vaiTro, await phanQuyen.LayMaTran(chon.MaVaiTro, ct));
        ViewData["Title"] = "Phân quyền";
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Luu(PhanQuyenViewModel model, CancellationToken ct)
    {
        var dong = model.Dong.Select(d => new PhanQuyen
        {
            MaVaiTro = model.MaVaiTro, LoaiChungTu = d.LoaiChungTu,
            CoXem = d.CoXem, CoThem = d.CoThem, CoSua = d.CoSua, CoXoa = d.CoXoa, CoDuyet = d.CoDuyet,
        }).ToList();

        var kq = await phanQuyen.Luu(model.MaVaiTro, dong, hienTai.Lay()?.MaND ?? throw new InvalidOperationException("Chưa đăng nhập."), ct);
        TempData[kq.ThanhCong ? "ThongBao" : "Loi"] = kq.ThongBao;
        return RedirectToAction(nameof(Index), new { maVaiTro = model.MaVaiTro });
    }

    private Task<PhanQuyenViewModel> DungViewModel(VaiTro chon, IReadOnlyList<VaiTro> vaiTro, IReadOnlyList<PhanQuyen> maTran)
    {
        var vm = new PhanQuyenViewModel
        {
            MaVaiTro = chon.MaVaiTro,
            TenVaiTro = chon.TenVaiTro,
            DanhSachVaiTro = vaiTro,
            Dong = maTran.Select(p =>
            {
                Enum.TryParse<LoaiChungTu>(p.LoaiChungTu, out var loai);
                return new DongQuyenViewModel
                {
                    LoaiChungTu = p.LoaiChungTu,
                    TenChungTu = loai.Ten(),
                    CoXem = p.CoXem, CoThem = p.CoThem, CoSua = p.CoSua, CoXoa = p.CoXoa, CoDuyet = p.CoDuyet,
                    KhoaDuyet = loai == LoaiChungTu.KK && !QuyTacPhanQuyen.ChoPhepDuyetKiemKe(chon.TenVaiTro),
                };
            }).ToList(),
        };
        return Task.FromResult(vm);
    }
}
