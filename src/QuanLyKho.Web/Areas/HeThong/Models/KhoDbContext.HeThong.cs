using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;

namespace QuanLyKho.Web.Data;

/// <summary>Bảng của Area HeThong: VaiTro, PhanQuyen, NguoiDung (Bảng 2.19).</summary>
public partial class KhoDbContext
{
    public DbSet<VaiTro> VaiTro => Set<VaiTro>();

    public DbSet<PhanQuyen> PhanQuyen => Set<PhanQuyen>();

    public DbSet<NguoiDung> NguoiDung => Set<NguoiDung>();
}
