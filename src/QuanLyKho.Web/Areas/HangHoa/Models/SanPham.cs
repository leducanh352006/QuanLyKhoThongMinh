namespace QuanLyKho.Web.Areas.HangHoa.Models;

/// <summary>Bảng SanPham (việc T3-2).</summary>
public class SanPham
{
    public int MaSP { get; set; }

    public string TenSP { get; set; } = "";

    public int MaDM { get; set; }

    public string DonViTinh { get; set; } = "";

    public bool CoHanDung { get; set; }
}
