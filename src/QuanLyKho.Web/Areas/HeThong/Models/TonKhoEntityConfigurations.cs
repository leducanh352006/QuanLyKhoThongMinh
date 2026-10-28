using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuanLyKho.Web.Areas.HangHoa.Models;

namespace QuanLyKho.Web.Areas.HeThong.Models;

// Cấu hình bảng theo báo cáo mục 2.8.1 và Bảng 2.21 (việc T4-4). Nạp tự động bởi KhoDbContext (ApplyConfigurationsFromAssembly).

public sealed class TonKhoConfiguration : IEntityTypeConfiguration<TonKho>
{
    public void Configure(EntityTypeBuilder<TonKho> b)
    {
        b.ToTable("TonKho", t =>
        {
            t.HasCheckConstraint("CK_TonKho_KhongAm", "[SoLuongTon] >= 0 AND [SoLuongGiu] >= 0");
            t.HasCheckConstraint("CK_TonKho_GiuKhongVuotTon", "[SoLuongGiu] <= [SoLuongTon]");
        });
        b.HasKey(x => new { x.MaLo, x.MaViTri });
        b.Property(x => x.SoLuongTon).IsRequired().HasDefaultValue(0);
        b.Property(x => x.SoLuongGiu).IsRequired().HasDefaultValue(0);
        b.Property(x => x.PhienBan).IsRowVersion();
        b.Ignore(x => x.KhaDung);

        // Đọc theo ô (màn hình vị trí, sắp xếp) và theo lô (lịch sử một lô).
        b.HasIndex(x => x.MaViTri);

        b.HasOne<LoHang>().WithMany().HasForeignKey(x => x.MaLo).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTri).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class BienDongTonConfiguration : IEntityTypeConfiguration<BienDongTon>
{
    public void Configure(EntityTypeBuilder<BienDongTon> b)
    {
        b.ToTable("BienDongTon", t =>
        {
            t.HasCheckConstraint("CK_BienDongTon_SoLuongKhac0", "[SoLuong] <> 0");
            t.HasCheckConstraint("CK_BienDongTon_LoaiBienDong",
                "[LoaiBienDong] IN (N'Nhận hàng', N'Sắp xếp', N'Xuất kho', N'Chuyển đi', N'Nhận chuyển', N'Đổi ô', N'Hoàn tác')");
            t.HasCheckConstraint("CK_BienDongTon_LoaiCT", "[LoaiCT] IN ('NK', 'XK', 'CK', 'KK')");
        });
        b.HasKey(x => x.MaBD);
        b.Property(x => x.MaBD).ValueGeneratedOnAdd();
        b.Property(x => x.MaLo).IsRequired();
        b.Property(x => x.MaViTri).IsRequired();
        b.Property(x => x.SoLuong).IsRequired();
        b.Property(x => x.LoaiBienDong).HasColumnType("nvarchar(20)").IsRequired();
        b.Property(x => x.LoaiCT).HasColumnType("varchar(2)").IsRequired();
        b.Property(x => x.MaCT).IsRequired();
        b.Property(x => x.MaND).IsRequired();
        b.Property(x => x.ThoiGianUtc).HasColumnType("datetime2").IsRequired();

        // Sổ cái: đọc nhiều theo lô+ô (đối chiếu TonKho, lịch sử của một ô) và theo chứng từ (lịch sử biến động của một phiếu).
        b.HasIndex(x => new { x.MaLo, x.MaViTri });
        b.HasIndex(x => new { x.LoaiCT, x.MaCT });

        // LoHang, ViTri có thể bị xóa mềm hoặc còn tồn tại; sổ cái không bao giờ xóa nên vẫn khóa ngoại Restrict để không mất vết.
        b.HasOne<LoHang>().WithMany().HasForeignKey(x => x.MaLo).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTri).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaND).OnDelete(DeleteBehavior.Restrict);
    }
}
