using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuanLyKho.Web.Areas.HeThong.Models;

// Cấu hình bảng theo báo cáo mục 2.8 (Bảng 2.16 và 2.19). Được nạp tự động bởi KhoDbContext (ApplyConfigurationsFromAssembly).

public sealed class VaiTroConfiguration : IEntityTypeConfiguration<VaiTro>
{
    public void Configure(EntityTypeBuilder<VaiTro> b)
    {
        b.ToTable("VaiTro");
        b.HasKey(x => x.MaVaiTro);
        b.Property(x => x.MaVaiTro).ValueGeneratedOnAdd();
        b.Property(x => x.TenVaiTro).HasColumnType("nvarchar(50)").IsRequired();
        b.HasIndex(x => x.TenVaiTro).IsUnique();
    }
}

public sealed class PhanQuyenConfiguration : IEntityTypeConfiguration<PhanQuyen>
{
    public void Configure(EntityTypeBuilder<PhanQuyen> b)
    {
        b.ToTable("PhanQuyen", t => t.HasCheckConstraint("CK_PhanQuyen_LoaiChungTu", "[LoaiChungTu] IN ('NK', 'XK', 'CK', 'KK')"));
        b.HasKey(x => new { x.MaVaiTro, x.LoaiChungTu });
        b.Property(x => x.MaVaiTro).ValueGeneratedNever();
        b.Property(x => x.LoaiChungTu).HasColumnType("char(2)").IsRequired();
        b.Property(x => x.CoXem).HasDefaultValue(false);
        b.Property(x => x.CoThem).HasDefaultValue(false);
        b.Property(x => x.CoSua).HasDefaultValue(false);
        b.Property(x => x.CoXoa).HasDefaultValue(false);
        b.Property(x => x.CoDuyet).HasDefaultValue(false);
        b.HasOne<VaiTro>().WithMany().HasForeignKey(x => x.MaVaiTro).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class NguoiDungConfiguration : IEntityTypeConfiguration<NguoiDung>
{
    public void Configure(EntityTypeBuilder<NguoiDung> b)
    {
        b.ToTable("NguoiDung", t => t.HasCheckConstraint("CK_NguoiDung_TrangThai", "[TrangThai] IN (N'Hoạt động', N'Ngừng hoạt động')"));
        b.HasKey(x => x.MaND);
        b.Property(x => x.MaND).ValueGeneratedOnAdd();
        b.Property(x => x.TenDangNhap).HasColumnType("varchar(50)").IsRequired();
        b.HasIndex(x => x.TenDangNhap).IsUnique();
        b.Property(x => x.MatKhauHash).HasColumnType("nvarchar(255)").IsRequired();
        b.Property(x => x.HoTen).HasColumnType("nvarchar(100)").IsRequired();
        b.Property(x => x.Email).HasColumnType("varchar(100)");
        b.Property(x => x.SDT).HasColumnType("varchar(15)");
        b.Property(x => x.MaVaiTro).IsRequired();
        b.Property(x => x.TrangThai).HasColumnType("nvarchar(20)").IsRequired().HasDefaultValue(TrangThaiNguoiDung.HoatDong);
        b.Ignore(x => x.DangHoatDong);
        b.HasOne<VaiTro>().WithMany().HasForeignKey(x => x.MaVaiTro).OnDelete(DeleteBehavior.Restrict);
    }
}
