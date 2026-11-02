using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HeThong.Models;

namespace QuanLyKho.Web.Data;

/// <summary>Bảng của Area HeThong cho việc T4-7: NhatKyThaoTac, TepDinhKem.</summary>
public partial class KhoDbContext
{
    public DbSet<NhatKyThaoTac> NhatKyThaoTac => Set<NhatKyThaoTac>();

    public DbSet<TepDinhKem> TepDinhKem => Set<TepDinhKem>();
}
