using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuanLyKho.Web.Areas.HangHoa.Models;

// Cấu hình bảng việc T3-1 (Nhà cung cấp, danh mục sản phẩm). Nạp tự động bởi KhoDbContext (ApplyConfigurationsFromAssembly).

public sealed class NhaCungCapConfiguration : IEntityTypeConfiguration<NhaCungCap>
{
    public void Configure(EntityTypeBuilder<NhaCungCap> b)
    {
        b.ToTable("NhaCungCap", t => t.HasCheckConstraint("CK_NhaCungCap_TrangThai", "[TrangThai] IN (N'Hoạt động', N'Ngừng hoạt động')"));
        b.HasKey(x => x.MaNCC);
        b.Property(x => x.MaNCC).ValueGeneratedOnAdd();
        b.Property(x => x.TenNCC).HasColumnType("nvarchar(100)").IsRequired();
        b.HasIndex(x => x.TenNCC).IsUnique();
        b.Property(x => x.SDT).HasColumnType("varchar(15)");
        b.Property(x => x.DiaChi).HasColumnType("nvarchar(255)");
        b.Property(x => x.TrangThai).HasColumnType("nvarchar(20)").IsRequired().HasDefaultValue(TrangThaiNhaCungCap.HoatDong);
        b.Ignore(x => x.DangHoatDong);
    }
}

public sealed class DanhMucConfiguration : IEntityTypeConfiguration<DanhMuc>
{
    public void Configure(EntityTypeBuilder<DanhMuc> b)
    {
        b.ToTable("DanhMuc");
        b.HasKey(x => x.MaDM);
        b.Property(x => x.MaDM).ValueGeneratedOnAdd();
        b.Property(x => x.TenDM).HasColumnType("nvarchar(100)").IsRequired();
        b.HasIndex(x => x.TenDM).IsUnique();
    }
}
