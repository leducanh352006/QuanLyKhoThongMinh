namespace QuanLyKho.Web.Contracts;

/// <summary>Hàm dùng chung do Nhóm 4 (TV3) sở hữu; nhóm 2 gọi khi tiếp nhận hàng (Bảng 1.6 dòng 2).</summary>
public interface IHangHoaService
{
    /// <summary>
    /// Tạo lô cho một biến thể. Hàng CoHanDung bắt buộc có hạn dùng. Mã vạch lô phải duy nhất; để null thì hệ thống tự sinh.
    /// </summary>
    Task<KetQua<LoHangDto>> TaoLo(int maBT, DateOnly? hanDung, string? maVachLo = null, CancellationToken ct = default);
}
