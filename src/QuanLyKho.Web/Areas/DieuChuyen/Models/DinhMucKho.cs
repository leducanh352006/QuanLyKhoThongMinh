using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Models;

namespace QuanLyKho.Web.Areas.DieuChuyen.Models
{
    /// <summary>
    /// Bảng DinhMucKho (việc T5-1, Bảng 1.2, Bảng 2.20): định mức tồn tối thiểu và số ngày an toàn của một biến thể tại một kho,
    /// dùng làm đầu vào cho gợi ý G3 (nhập bổ sung) — m = TonToiThieu, N = NgayAnToan. Khóa chính (MaKho, MaBT).
    /// </summary>
    public class DinhMucKho
    {
        public int MaKho { get; set; }

        public int MaBT { get; set; }

        /// <summary>Tồn tối thiểu — m.</summary>
        public int TonToiThieu { get; set; }

        /// <summary>Số ngày an toàn — N.</summary>
        public int NgayAnToan { get; set; }

        public Kho? Kho { get; set; }

        public BienThe? BienThe { get; set; }

        /// <summary>Quy tắc không cần CSDL. Trả về lỗi, hoặc null nếu hợp lệ.</summary>
        public string? KiemTra()
        {
            if (MaKho <= 0) return "Chọn kho.";
            if (MaBT <= 0) return "Chọn biến thể.";
            if (TonToiThieu < 0) return "Tồn tối thiểu không được âm.";
            if (NgayAnToan < 0) return "Số ngày an toàn không được âm.";
            return null;
        }
    }

    public sealed class DinhMucKhoConfig : IEntityTypeConfiguration<DinhMucKho>
    {
        public void Configure(EntityTypeBuilder<DinhMucKho> b)
        {
            b.ToTable("DinhMucKho", t => t.HasCheckConstraint("CK_DinhMucKho_KhongAm", "[TonToiThieu] >= 0 AND [NgayAnToan] >= 0"));
            b.HasKey(x => new { x.MaKho, x.MaBT });
            b.HasOne(x => x.Kho).WithMany().HasForeignKey(x => x.MaKho).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.BienThe).WithMany().HasForeignKey(x => x.MaBT).OnDelete(DeleteBehavior.Restrict);
        }
    }
}

namespace QuanLyKho.Web.Data
{
    /// <summary>Gắn bảng DinhMucKho của Điều chuyển kho vào DbContext dùng chung (không sửa KhoDbContext.cs).</summary>
    public partial class KhoDbContext
    {
        public DbSet<QuanLyKho.Web.Areas.DieuChuyen.Models.DinhMucKho> DinhMucKho => Set<QuanLyKho.Web.Areas.DieuChuyen.Models.DinhMucKho>();
    }
}
