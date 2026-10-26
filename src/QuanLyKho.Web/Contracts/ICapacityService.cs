namespace QuanLyKho.Web.Contracts;

/// <summary>
/// Kiểm tra sức chứa ô, một quy tắc dùng chung cho sắp xếp hàng nhập (G2) và nhận hàng chuyển kho (Bảng 1.6 dòng 4):
/// q × thể tích biến thể ≤ thể tích ô - thể tích đã dùng, và q × khối lượng biến thể ≤ khối lượng tối đa - đã dùng.
/// Ô Tiếp nhận không giới hạn; ô đang khóa kiểm kê bị từ chối.
/// </summary>
public interface ICapacityService
{
    Task<KetQuaSucChua> KiemTra(int maViTri, int maBT, int soLuong, CancellationToken ct = default);
}
