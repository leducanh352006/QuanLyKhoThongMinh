using QuanLyKho.Web.Areas.HangHoa.Services;

namespace QuanLyKho.Tests.HangHoa;

/// <summary>
/// Dựng sẵn SanPhamService và BienTheService trên kho dữ liệu trong bộ nhớ (3 sản phẩm, 4 biến thể mẫu từ SeedData)
/// và DanhMucService (3 danh mục mẫu) để kiểm tra MaDM hợp lệ.
/// </summary>
internal sealed class MoiTruongSanPhamThu
{
    public const int MaNguoiThucHien = 2;   // quanlykho (mẫu)

    public NhatKyGia NhatKy { get; } = new();
    public BoNhoSanPhamBienTheStore Store { get; } = new();
    public DanhMucService DanhMuc { get; }
    public SanPhamService SanPham { get; }
    public BienTheService BienThe { get; }

    public MoiTruongSanPhamThu()
    {
        DanhMuc = new DanhMucService(new BoNhoHangHoaStore(), NhatKy);
        SanPham = new SanPhamService(Store, DanhMuc, NhatKy);
        BienThe = new BienTheService(Store, NhatKy);
    }
}
