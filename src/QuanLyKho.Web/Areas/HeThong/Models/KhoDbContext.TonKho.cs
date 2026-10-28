using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;

namespace QuanLyKho.Web.Data;

/// <summary>Bảng của Area HeThong cho việc T4-4: TonKho, BienDongTon (Bảng 2.21).</summary>
public partial class KhoDbContext
{
    public DbSet<TonKho> TonKho => Set<TonKho>();

    public DbSet<BienDongTon> BienDongTon => Set<BienDongTon>();
}
