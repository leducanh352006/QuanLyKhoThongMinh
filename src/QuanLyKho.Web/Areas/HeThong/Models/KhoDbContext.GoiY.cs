using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;

namespace QuanLyKho.Web.Data;

/// <summary>Bảng của Area HeThong cho việc T4-8: GoiYAI, ChiTietGoiY.</summary>
public partial class KhoDbContext
{
    public DbSet<GoiYAI> GoiYAI => Set<GoiYAI>();

    public DbSet<ChiTietGoiY> ChiTietGoiY => Set<ChiTietGoiY>();
}
