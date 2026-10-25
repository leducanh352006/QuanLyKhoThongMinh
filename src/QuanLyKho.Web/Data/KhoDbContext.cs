using Microsoft.EntityFrameworkCore;

namespace QuanLyKho.Web.Data;

/// <summary>
/// DbContext dùng chung của cả nhóm, khai báo <c>partial</c> để mỗi nhóm thêm bảng của mình mà không sửa file này:
/// tạo <c>Areas/&lt;Area&gt;/Models/KhoDbContext.&lt;Area&gt;.cs</c> chứa <c>public partial class KhoDbContext</c> với các DbSet của nhóm,
/// và các lớp <c>IEntityTypeConfiguration&lt;T&gt;</c> (tự được nạp bên dưới). Mỗi nhóm chỉ cấu hình bảng của mình (mục 1.3 báo cáo).
/// </summary>
public partial class KhoDbContext(DbContextOptions<KhoDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KhoDbContext).Assembly);
    }
}
