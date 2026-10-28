namespace QuanLyKho.Web.Areas.HangHoa.Models;

/// <summary>Bảng LoHang (việc T3-3): lô hàng nhập, gắn với một biến thể. Mã vạch lô duy nhất toàn hệ thống.</summary>
public class LoHang
{
    public int MaLo { get; set; }

    public string MaVachLo { get; set; } = "";

    public int MaBT { get; set; }

    /// <summary>Hạn dùng; bắt buộc khi sản phẩm của biến thể quản lý theo hạn dùng (SanPham.CoHanDung).</summary>
    public DateOnly? HanDung { get; set; }
}
