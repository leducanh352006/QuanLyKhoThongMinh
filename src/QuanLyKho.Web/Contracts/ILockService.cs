namespace QuanLyKho.Web.Contracts;

/// <summary>Khóa và mở ô (cột ViTri.DangKhoa). Nhóm 4 khóa ô khi bắt đầu kiểm và mở khi hoàn tất; ô khóa bị loại khỏi nhập, xuất, chuyển và gợi ý G1, G2.</summary>
public interface ILockService
{
    /// <summary>Khóa ô. Từ chối (MaLoiDichVu.OBiKhoa) nếu ô đã bị khóa, để hai phiếu kiểm không khóa chung một ô.</summary>
    Task<KetQua> KhoaO(int maViTri, int maND, CancellationToken ct = default);

    /// <summary>Mở ô. An toàn khi gọi lại: ô đang mở thì vẫn trả thành công.</summary>
    Task<KetQua> MoO(int maViTri, int maND, CancellationToken ct = default);

    Task<bool> DangKhoa(int maViTri, CancellationToken ct = default);
}
