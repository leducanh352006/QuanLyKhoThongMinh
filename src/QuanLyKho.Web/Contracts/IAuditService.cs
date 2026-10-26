namespace QuanLyKho.Web.Contracts;

/// <summary>Nhật ký thao tác (NhatKyThaoTac): chỉ thêm, không sửa, không xóa. Mọi chứng từ ghi nhật ký qua dịch vụ này (Bảng 1.6 dòng 6).</summary>
public interface IAuditService
{
    /// <summary>Ghi một thao tác. <paramref name="loaiCT"/> và <paramref name="maCT"/> phải cùng null hoặc cùng có giá trị.</summary>
    Task Ghi(int maND, string hanhDong, LoaiChungTu? loaiCT = null, int? maCT = null, string? chiTiet = null, CancellationToken ct = default);

    Task<IReadOnlyList<NhatKyDto>> LayTheoChungTu(LoaiChungTu loaiCT, int maCT, CancellationToken ct = default);
}
