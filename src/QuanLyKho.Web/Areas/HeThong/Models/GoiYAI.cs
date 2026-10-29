using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.NhapKho.Models;

namespace QuanLyKho.Web.Areas.HeThong.Models;

/// <summary>
/// Bảng GoiYAI (việc T4-8): mỗi gợi ý của AI và quyết định của người dùng. LoaiGoiY là G1 đến G4, TrangThai theo Bảng 2.16
/// (Chờ duyệt, Đã duyệt, Đã sửa và duyệt, Từ chối). MaPX, MaPN, MaPC cùng một lúc chỉ có tối đa một giá trị, theo loại (xem CHECK).
/// MaPX, MaPC chưa có khóa ngoại vì bảng PhieuXuat, PhieuChuyen chưa có trên main; thêm khi nhóm sở hữu có bảng thật.
/// </summary>
public class GoiYAI
{
    public int MaGoiY { get; set; }

    public string LoaiGoiY { get; set; } = "";

    public DateTime ThoiGianTao { get; set; }

    public string TrangThai { get; set; } = "";

    public int? MaNguoiDuyet { get; set; }

    public DateTime? ThoiGianDuyet { get; set; }

    public int? MaPX { get; set; }

    public int? MaPN { get; set; }

    public int? MaPC { get; set; }
}

/// <summary>
/// Bảng ChiTietGoiY (việc T4-8): các dòng đề xuất. <see cref="LaBanSua"/> = false là dòng gốc của AI (không bao giờ sửa, xóa);
/// true là dòng do người duyệt sửa lại khi "Sửa rồi duyệt". Gợi ý có dòng sửa thì dòng sửa là bản có hiệu lực, dòng gốc giữ để so sánh.
/// </summary>
public class ChiTietGoiY
{
    public int MaCTGY { get; set; }

    public int MaGoiY { get; set; }

    public int MaBT { get; set; }

    public int? MaLo { get; set; }

    public int? MaViTri { get; set; }

    public int? MaKhoNguon { get; set; }

    public int? MaKhoDich { get; set; }

    public int SoLuong { get; set; }

    public string LyDo { get; set; } = "";

    public bool LaBanSua { get; set; }
}

public sealed class GoiYAIConfiguration : IEntityTypeConfiguration<GoiYAI>
{
    public void Configure(EntityTypeBuilder<GoiYAI> b)
    {
        b.ToTable("GoiYAI", t =>
        {
            t.HasCheckConstraint("CK_GoiYAI_LoaiGoiY", "[LoaiGoiY] IN ('G1', 'G2', 'G3', 'G4')");
            t.HasCheckConstraint("CK_GoiYAI_TrangThai", "[TrangThai] IN (N'Chờ duyệt', N'Đã duyệt', N'Đã sửa và duyệt', N'Từ chối')");
            // Chưa xử lý thì chưa có người duyệt; đã xử lý thì phải có cả người duyệt lẫn thời điểm.
            t.HasCheckConstraint("CK_GoiYAI_NguoiDuyet",
                "([TrangThai] = N'Chờ duyệt' AND [MaNguoiDuyet] IS NULL AND [ThoiGianDuyet] IS NULL) " +
                "OR ([TrangThai] <> N'Chờ duyệt' AND [MaNguoiDuyet] IS NOT NULL AND [ThoiGianDuyet] IS NOT NULL)");
            // G1 gắn đúng một phiếu xuất, G2 đúng một phiếu nhập, G3 chỉ được gắn phiếu nhập, G4 chỉ phiếu chuyển (điền sau khi duyệt).
            t.HasCheckConstraint("CK_GoiYAI_ThamChieu",
                "([LoaiGoiY] = 'G1' AND [MaPX] IS NOT NULL AND [MaPN] IS NULL AND [MaPC] IS NULL) " +
                "OR ([LoaiGoiY] = 'G2' AND [MaPN] IS NOT NULL AND [MaPX] IS NULL AND [MaPC] IS NULL) " +
                "OR ([LoaiGoiY] = 'G3' AND [MaPX] IS NULL AND [MaPC] IS NULL) " +
                "OR ([LoaiGoiY] = 'G4' AND [MaPX] IS NULL AND [MaPN] IS NULL)");
        });
        b.HasKey(x => x.MaGoiY);
        b.Property(x => x.MaGoiY).ValueGeneratedOnAdd();
        b.Property(x => x.LoaiGoiY).HasColumnType("char(2)").IsRequired();
        b.Property(x => x.ThoiGianTao).HasColumnType("datetime2").IsRequired();
        b.Property(x => x.TrangThai).HasColumnType("nvarchar(20)").IsRequired();
        b.Property(x => x.ThoiGianDuyet).HasColumnType("datetime2");

        b.HasIndex(x => new { x.TrangThai, x.LoaiGoiY });
        b.HasIndex(x => x.MaPX);
        b.HasIndex(x => x.MaPN);

        b.HasOne<NguoiDung>().WithMany().HasForeignKey(x => x.MaNguoiDuyet).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<PhieuNhap>().WithMany().HasForeignKey(x => x.MaPN).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ChiTietGoiYConfiguration : IEntityTypeConfiguration<ChiTietGoiY>
{
    public void Configure(EntityTypeBuilder<ChiTietGoiY> b)
    {
        b.ToTable("ChiTietGoiY", t =>
        {
            t.HasCheckConstraint("CK_ChiTietGoiY_SoLuong", "[SoLuong] > 0");
            t.HasCheckConstraint("CK_ChiTietGoiY_LyDo", "LEN(LTRIM(RTRIM([LyDo]))) > 0");
            t.HasCheckConstraint("CK_ChiTietGoiY_KhoKhac", "[MaKhoNguon] IS NULL OR [MaKhoDich] IS NULL OR [MaKhoNguon] <> [MaKhoDich]");
        });
        b.HasKey(x => x.MaCTGY);
        b.Property(x => x.MaCTGY).ValueGeneratedOnAdd();
        b.Property(x => x.LyDo).HasColumnType("nvarchar(500)").IsRequired();
        b.Property(x => x.LaBanSua).HasDefaultValue(false);

        b.HasIndex(x => x.MaGoiY);

        // Gợi ý không bao giờ bị xóa nên không có xóa dây chuyền; dòng chi tiết giữ vết cùng gợi ý.
        b.HasOne<GoiYAI>().WithMany().HasForeignKey(x => x.MaGoiY).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<BienThe>().WithMany().HasForeignKey(x => x.MaBT).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<LoHang>().WithMany().HasForeignKey(x => x.MaLo).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<ViTri>().WithMany().HasForeignKey(x => x.MaViTri).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Kho>().WithMany().HasForeignKey(x => x.MaKhoNguon).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Kho>().WithMany().HasForeignKey(x => x.MaKhoDich).OnDelete(DeleteBehavior.Restrict);
    }
}
