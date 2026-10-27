using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuanLyKho.Web.Areas.HeThong.Models;

// Cấu hình bảng theo báo cáo mục 2.8.1 và Bảng 2.20. Nạp tự động bởi KhoDbContext (ApplyConfigurationsFromAssembly).

public sealed class KhoConfiguration : IEntityTypeConfiguration<Kho>
{
    public void Configure(EntityTypeBuilder<Kho> b)
    {
        b.ToTable("Kho", t =>
        {
            t.HasCheckConstraint("CK_Kho_LoaiKho", "[LoaiKho] IN (N'Kho bán hàng', N'Kho lưu trữ')");
            t.HasCheckConstraint("CK_Kho_TrangThai", "[TrangThai] IN (N'Hoạt động', N'Ngừng hoạt động')");
            t.HasCheckConstraint("CK_Kho_ThoiGianCho", "[ThoiGianCho] >= 0");
        });
        b.HasKey(x => x.MaKho);
        b.Property(x => x.MaKho).ValueGeneratedOnAdd();
        b.Property(x => x.TenKho).HasColumnType("nvarchar(100)").IsRequired();
        b.HasIndex(x => x.TenKho).IsUnique();
        b.Property(x => x.LoaiKho).HasColumnType("nvarchar(20)").IsRequired();
        b.Property(x => x.DiaChi).HasColumnType("nvarchar(255)");
        b.Property(x => x.MaQuanLy).IsRequired();
        b.Property(x => x.ThoiGianCho).HasDefaultValue(0);
        b.Property(x => x.TrangThai).HasColumnType("nvarchar(20)").IsRequired().HasDefaultValue(TrangThaiHoatDong.HoatDong);
        b.Ignore(x => x.DangHoatDong);
        // Không khai báo kiểu NguoiDung ở đây (thuộc Model khác trong cùng Area): FK bằng khóa ngoài không điều hướng.
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaQuanLy).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ViTriConfiguration : IEntityTypeConfiguration<ViTri>
{
    public void Configure(EntityTypeBuilder<ViTri> b)
    {
        b.ToTable("ViTri", t =>
        {
            t.HasCheckConstraint("CK_ViTri_CapViTri", "[CapViTri] IN (N'Tòa', N'Tầng', N'Phòng', N'Dãy', N'Hàng', N'Kệ', N'Ô')");
            t.HasCheckConstraint("CK_ViTri_LoaiViTri", "[LoaiViTri] IS NULL OR [LoaiViTri] IN (N'Lưu trữ', N'Tiếp nhận')");
            t.HasCheckConstraint("CK_ViTri_TrangThai", "[TrangThai] IN (N'Hoạt động', N'Ngừng hoạt động')");
            // Chỉ ô mới có LoaiViTri và MaVachO; cấp khác ô thì cả hai phải NULL.
            t.HasCheckConstraint("CK_ViTri_ChiOCoLoaiVaMaVach",
                "([CapViTri] = N'Ô' AND [LoaiViTri] IS NOT NULL AND [MaVachO] IS NOT NULL) " +
                "OR ([CapViTri] <> N'Ô' AND [LoaiViTri] IS NULL AND [MaVachO] IS NULL)");
            // Ô Lưu trữ bắt buộc có đủ 4 kích thước, mỗi kích thước > 0; ô Tiếp nhận và cấp khác ô thì cả 4 cột để NULL.
            t.HasCheckConstraint("CK_ViTri_KichThuoc",
                "([CapViTri] = N'Ô' AND [LoaiViTri] = N'Lưu trữ' AND [Dai] > 0 AND [Rong] > 0 AND [Cao] > 0 AND [KhoiLuongToiDa] > 0) " +
                "OR ([CapViTri] = N'Ô' AND [LoaiViTri] = N'Tiếp nhận' AND [Dai] IS NULL AND [Rong] IS NULL AND [Cao] IS NULL AND [KhoiLuongToiDa] IS NULL) " +
                "OR ([CapViTri] <> N'Ô' AND [Dai] IS NULL AND [Rong] IS NULL AND [Cao] IS NULL AND [KhoiLuongToiDa] IS NULL)");
        });

        b.HasKey(x => x.MaViTri);
        b.Property(x => x.MaViTri).ValueGeneratedOnAdd();
        // Khóa thay thế (MaViTri, MaKho): điểm đích của khóa ngoại kép bên dưới, bảo đảm cấp con cùng kho với cấp cha.
        b.HasAlternateKey(x => new { x.MaViTri, x.MaKho });

        b.Property(x => x.TenViTri).HasColumnType("nvarchar(50)").IsRequired();
        b.Property(x => x.MaKho).IsRequired();
        b.Property(x => x.MaViTriCha);
        b.Property(x => x.CapViTri).HasColumnType("nvarchar(10)").IsRequired();
        b.Property(x => x.LoaiViTri).HasColumnType("nvarchar(15)");
        b.Property(x => x.MaVachO).HasColumnType("varchar(50)");
        b.Property(x => x.Dai).HasColumnType("decimal(8,2)");
        b.Property(x => x.Rong).HasColumnType("decimal(8,2)");
        b.Property(x => x.Cao).HasColumnType("decimal(8,2)");
        b.Property(x => x.KhoiLuongToiDa).HasColumnType("decimal(10,3)");
        b.Property(x => x.DangKhoa).HasDefaultValue(false);
        b.Property(x => x.TrangThai).HasColumnType("nvarchar(20)").IsRequired().HasDefaultValue(TrangThaiHoatDong.HoatDong);
        b.Ignore(x => x.DangHoatDong);
        b.Ignore(x => x.LaO);
        b.Ignore(x => x.TheTichCm3);

        // MaVachO duy nhất, chỉ xét giá trị khác NULL (chỉ mục lọc).
        b.HasIndex(x => x.MaVachO).IsUnique().HasFilter("[MaVachO] IS NOT NULL");
        // Cùng một kho, một vị trí cha, không trùng tên con.
        b.HasIndex(x => new { x.MaKho, x.MaViTriCha, x.TenViTri }).IsUnique();

        b.HasOne<Kho>().WithMany().HasForeignKey(x => x.MaKho).OnDelete(DeleteBehavior.Restrict);
        // Khóa ngoại kép tự tham chiếu: (MaViTriCha, MaKho) -> ViTri(MaViTri, MaKho), bảo đảm cấp con cùng kho với cấp cha.
        // MaViTriCha cho phép NULL (gốc); SQL Server bỏ qua ràng buộc FK khi một cột trong khóa kép là NULL.
        b.HasOne<ViTri>().WithMany()
            .HasForeignKey(x => new { x.MaViTriCha, x.MaKho })
            .HasPrincipalKey(x => new { x.MaViTri, x.MaKho })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
