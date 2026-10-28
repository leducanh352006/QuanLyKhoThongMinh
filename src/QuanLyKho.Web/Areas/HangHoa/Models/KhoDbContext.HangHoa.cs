using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;

namespace QuanLyKho.Web.Data;

/// <summary>Bảng của Area Hàng hóa: NhaCungCap, DanhMuc (T3-1); SanPham, BienThe (T3-2).</summary>
public partial class KhoDbContext
{
    public DbSet<NhaCungCap> NhaCungCap => Set<NhaCungCap>();

    public DbSet<DanhMuc> DanhMuc => Set<DanhMuc>();

    public DbSet<SanPham> SanPham => Set<SanPham>();

    public DbSet<BienThe> BienThe => Set<BienThe>();
}
