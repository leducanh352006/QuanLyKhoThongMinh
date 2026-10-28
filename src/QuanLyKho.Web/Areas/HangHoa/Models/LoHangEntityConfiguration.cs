using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuanLyKho.Web.Areas.HangHoa.Models;

// Cấu hình bảng việc T3-3 (Lô hàng). Nạp tự động bởi KhoDbContext (ApplyConfigurationsFromAssembly).

public sealed class LoHangConfiguration : IEntityTypeConfiguration<LoHang>
{
    public void Configure(EntityTypeBuilder<LoHang> b)
    {
        b.ToTable("LoHang");
        b.HasKey(x => x.MaLo);
        b.Property(x => x.MaLo).ValueGeneratedOnAdd();
        b.Property(x => x.MaVachLo).HasColumnType("varchar(50)").IsRequired();
        b.HasIndex(x => x.MaVachLo).IsUnique();
        b.Property(x => x.MaBT).IsRequired();
        b.Property(x => x.HanDung);
        // Đích của khóa ngoại kép ChiTietPhieuNhap (MaLo, MaBT) → LoHang (Bảng 2.19): bảo đảm lô thuộc đúng biến thể.
        b.HasIndex(x => new { x.MaLo, x.MaBT }).IsUnique();
        b.HasOne<BienThe>().WithMany().HasForeignKey(x => x.MaBT).OnDelete(DeleteBehavior.Restrict);
    }
}
