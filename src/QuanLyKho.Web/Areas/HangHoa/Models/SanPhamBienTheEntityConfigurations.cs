using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuanLyKho.Web.Areas.HangHoa.Models;

// Cấu hình bảng việc T3-2 (Sản phẩm, biến thể). Nạp tự động bởi KhoDbContext (ApplyConfigurationsFromAssembly).

public sealed class SanPhamConfiguration : IEntityTypeConfiguration<SanPham>
{
    public void Configure(EntityTypeBuilder<SanPham> b)
    {
        b.ToTable("SanPham");
        b.HasKey(x => x.MaSP);
        b.Property(x => x.MaSP).ValueGeneratedOnAdd();
        b.Property(x => x.TenSP).HasColumnType("nvarchar(100)").IsRequired();
        b.Property(x => x.MaDM).IsRequired();
        b.Property(x => x.DonViTinh).HasColumnType("nvarchar(20)").IsRequired();
        b.Property(x => x.CoHanDung).HasDefaultValue(false);
        b.HasOne<DanhMuc>().WithMany().HasForeignKey(x => x.MaDM).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class BienTheConfiguration : IEntityTypeConfiguration<BienThe>
{
    public void Configure(EntityTypeBuilder<BienThe> b)
    {
        b.ToTable("BienThe", t =>
        {
            t.HasCheckConstraint("CK_BienThe_KichThuoc", "[Dai] > 0 AND [Rong] > 0 AND [Cao] > 0 AND [KhoiLuong] > 0");
        });
        b.HasKey(x => x.MaBT);
        b.Property(x => x.MaBT).ValueGeneratedOnAdd();
        b.Property(x => x.MaSP).IsRequired();
        b.Property(x => x.MaVach).HasColumnType("varchar(50)").IsRequired();
        b.HasIndex(x => x.MaVach).IsUnique();
        b.Property(x => x.ThuocTinh).HasColumnType("nvarchar(100)").IsRequired();
        b.Property(x => x.Dai).HasColumnType("decimal(8,2)");
        b.Property(x => x.Rong).HasColumnType("decimal(8,2)");
        b.Property(x => x.Cao).HasColumnType("decimal(8,2)");
        b.Property(x => x.KhoiLuong).HasColumnType("decimal(10,3)");
        b.Ignore(x => x.TheTichCm3);
        b.HasOne<SanPham>().WithMany().HasForeignKey(x => x.MaSP).OnDelete(DeleteBehavior.Restrict);
    }
}
