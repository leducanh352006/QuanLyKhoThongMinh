using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;

namespace QuanLyKho.Web.Data;

/// <summary>Bảng của Area HeThong cho việc T4-3: Kho, ViTri (Bảng 2.20).</summary>
public partial class KhoDbContext
{
    public DbSet<Kho> Kho => Set<Kho>();

    public DbSet<ViTri> ViTri => Set<ViTri>();
}
