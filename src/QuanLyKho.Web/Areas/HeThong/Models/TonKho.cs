namespace QuanLyKho.Web.Areas.HeThong.Models;

/// <summary>
/// Bảng TonKho (Bảng 2.21, việc T4-4): tồn hiện tại của một lô tại một ô. Khóa chính (MaLo, MaViTri).
/// SoLuongTon, SoLuongGiu không bao giờ âm; SoLuongGiu luôn nhỏ hơn hoặc bằng SoLuongTon (ràng buộc ở cấu hình bảng).
/// Chỉ <see cref="QuanLyKho.Web.Areas.HeThong.Services.StockService"/> (bản thật của IStockService) được sửa bảng này.
/// </summary>
public class TonKho
{
    public int MaLo { get; set; }

    public int MaViTri { get; set; }

    public int SoLuongTon { get; set; }

    public int SoLuongGiu { get; set; }

    /// <summary>Token khóa lạc quan (SQL Server rowversion): chống hai thao tác đổi tồn đồng thời ghi đè nhau (cập nhật có điều kiện).</summary>
    public byte[] PhienBan { get; set; } = Array.Empty<byte>();

    public int KhaDung => SoLuongTon - SoLuongGiu;
}
