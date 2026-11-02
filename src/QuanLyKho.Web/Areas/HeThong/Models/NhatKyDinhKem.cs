using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace QuanLyKho.Web.Areas.HeThong.Models;

/// <summary>
/// Bảng NhatKyThaoTac (việc T4-7): ghi ai làm gì, lúc nào trên chứng từ. Chỉ thêm, không sửa, không xóa.
/// LoaiCT, MaCT trỏ tới chứng từ nhưng không là khóa ngoại (chứng từ do nhóm khác quản lý, nhật ký phải còn nguyên khi chứng từ đổi);
/// hai cột này cùng null (thao tác không gắn chứng từ, ví dụ đăng nhập) hoặc cùng có giá trị.
/// </summary>
public class NhatKyThaoTac
{
    public long MaNK { get; set; }

    public int MaND { get; set; }

    /// <summary>Giờ UTC.</summary>
    public DateTime ThoiGian { get; set; }

    public string HanhDong { get; set; } = "";

    /// <summary>NK, XK, CK hoặc KK; null nếu thao tác không gắn chứng từ.</summary>
    public string? LoaiCT { get; set; }

    public int? MaCT { get; set; }

    public string? ChiTiet { get; set; }
}

/// <summary>
/// Bảng TepDinhKem (việc T4-7): tệp đính kèm theo chứng từ (LoaiCT, MaCT). DuongDan là đường dẫn tương đối trong thư mục lưu tệp
/// (<c>Storage:TepDinhKem</c>), không bao giờ là đường dẫn tuyệt đối; nội dung tệp không nằm trong CSDL và không nằm trong git.
/// </summary>
public class TepDinhKem
{
    public int MaTep { get; set; }

    public string LoaiCT { get; set; } = "";

    public int MaCT { get; set; }

    public string TenTep { get; set; } = "";

    public string DuongDan { get; set; } = "";

    public int MaND { get; set; }

    /// <summary>Giờ UTC.</summary>
    public DateTime ThoiGian { get; set; }
}

public sealed class NhatKyThaoTacConfiguration : IEntityTypeConfiguration<NhatKyThaoTac>
{
    public void Configure(EntityTypeBuilder<NhatKyThaoTac> b)
    {
        b.ToTable("NhatKyThaoTac", t =>
        {
            t.HasCheckConstraint("CK_NhatKyThaoTac_LoaiCT", "[LoaiCT] IS NULL OR [LoaiCT] IN ('NK', 'XK', 'CK', 'KK')");
            t.HasCheckConstraint("CK_NhatKyThaoTac_ChungTu", "([LoaiCT] IS NULL AND [MaCT] IS NULL) OR ([LoaiCT] IS NOT NULL AND [MaCT] IS NOT NULL)");
        });
        b.HasKey(x => x.MaNK);
        b.Property(x => x.MaNK).ValueGeneratedOnAdd();
        b.Property(x => x.MaND).IsRequired();
        b.Property(x => x.ThoiGian).HasColumnType("datetime2").IsRequired();
        b.Property(x => x.HanhDong).HasColumnType("nvarchar(100)").IsRequired();
        b.Property(x => x.LoaiCT).HasColumnType("varchar(2)");
        b.Property(x => x.ChiTiet).HasColumnType("nvarchar(1000)");

        // Đọc nhiều theo chứng từ (lịch sử một phiếu), theo người và theo thời gian (màn hình tra cứu).
        b.HasIndex(x => new { x.LoaiCT, x.MaCT });
        b.HasIndex(x => x.MaND);
        b.HasIndex(x => x.ThoiGian);

        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaND).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TepDinhKemConfiguration : IEntityTypeConfiguration<TepDinhKem>
{
    public void Configure(EntityTypeBuilder<TepDinhKem> b)
    {
        b.ToTable("TepDinhKem", t =>
            t.HasCheckConstraint("CK_TepDinhKem_LoaiCT", "[LoaiCT] IN ('NK', 'XK', 'CK', 'KK')"));
        b.HasKey(x => x.MaTep);
        b.Property(x => x.MaTep).ValueGeneratedOnAdd();
        b.Property(x => x.LoaiCT).HasColumnType("varchar(2)").IsRequired();
        b.Property(x => x.MaCT).IsRequired();
        b.Property(x => x.TenTep).HasColumnType("nvarchar(255)").IsRequired();
        b.Property(x => x.DuongDan).HasColumnType("nvarchar(500)").IsRequired();
        b.Property(x => x.MaND).IsRequired();
        b.Property(x => x.ThoiGian).HasColumnType("datetime2").IsRequired();

        b.HasIndex(x => new { x.LoaiCT, x.MaCT });

        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaND).OnDelete(DeleteBehavior.Restrict);
    }
}
