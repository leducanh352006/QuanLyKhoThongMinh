namespace QuanLyKho.Web.Contracts;

/// <summary>
/// Đường lui khi AI lỗi (báo cáo mục 1.2.4): nơi tính gợi ý bọc lời gọi AI bằng hàm này để một lỗi, một lần quá thời gian
/// hay thiếu dữ liệu không bao giờ làm hỏng màn hình hoặc chặn quy trình làm tay. Hàm không ném lỗi, luôn trả <see cref="KetQua{T}"/>.
/// <code>var kq = await GoiYAnToan.Chay(ct2 => tinhG3(ct2));
/// if (!kq.ThanhCong) { /* hiện kq.ThongBao, người dùng làm tay */ }</code>
/// </summary>
public static class GoiYAnToan
{
    public const string ThongBaoMacDinh = "Gợi ý AI tạm thời không dùng được. Bạn vẫn làm tay được toàn bộ quy trình.";

    public static readonly TimeSpan ThoiGianToiDaMacDinh = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Chạy <paramref name="tinhGoiY"/>. Ném lỗi, quá <paramref name="toiDa"/> (mặc định 10 giây) hoặc trả kết quả thất bại thì trả
    /// <see cref="KetQua{T}.Loi"/> mã <see cref="MaLoiDichVu.AiKhongDung"/>; thất bại do nghiệp vụ (có thông báo riêng) được giữ nguyên.
    /// Chỉ khi chính <paramref name="ct"/> bị hủy mới ném <see cref="OperationCanceledException"/> (người dùng bỏ yêu cầu).
    /// </summary>
    public static async Task<KetQua<T>> Chay<T>(Func<CancellationToken, Task<KetQua<T>>> tinhGoiY, TimeSpan? toiDa = null, CancellationToken ct = default)
    {
        using var het = CancellationTokenSource.CreateLinkedTokenSource(ct);
        het.CancelAfter(toiDa ?? ThoiGianToiDaMacDinh);
        try
        {
            var kq = await tinhGoiY(het.Token);
            return kq ?? KetQua<T>.Loi(ThongBaoMacDinh, MaLoiDichVu.AiKhongDung);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return KetQua<T>.Loi("Gợi ý AI quá thời gian chờ. Bạn vẫn làm tay được toàn bộ quy trình.", MaLoiDichVu.AiKhongDung);
        }
        catch (Exception)
        {
            return KetQua<T>.Loi(ThongBaoMacDinh, MaLoiDichVu.AiKhongDung);
        }
    }
}
