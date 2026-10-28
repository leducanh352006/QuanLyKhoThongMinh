namespace QuanLyKho.Web.Areas.HangHoa.Models;

/// <summary>Bảng BienThe (việc T3-2): size, màu... của một sản phẩm, kèm kích thước và khối lượng để tính sức chứa.</summary>
public class BienThe
{
    public int MaBT { get; set; }

    public int MaSP { get; set; }

    public string MaVach { get; set; } = "";

    public string ThuocTinh { get; set; } = "";

    public decimal Dai { get; set; }

    public decimal Rong { get; set; }

    public decimal Cao { get; set; }

    public decimal KhoiLuong { get; set; }

    /// <summary>Thể tích tính từ ba cạnh (cm³); không lưu thành cột riêng.</summary>
    public decimal TheTichCm3 => Dai * Rong * Cao;
}
