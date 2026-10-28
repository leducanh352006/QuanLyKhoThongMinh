using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.HangHoa.Models;

namespace QuanLyKho.Web.Data;

/// <summary>Bảng của Area Hàng hóa (việc T3-1): NhaCungCap, DanhMuc.</summary>
public partial class KhoDbContext
{
    public DbSet<NhaCungCap> NhaCungCap => Set<NhaCungCap>();

    public DbSet<DanhMuc> DanhMuc => Set<DanhMuc>();
}
