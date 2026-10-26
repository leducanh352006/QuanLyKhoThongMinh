namespace QuanLyKho.Web.Contracts.Stubs;

/// <summary>Bản giả của <see cref="IAuditService"/>: nhật ký giữ trong bộ nhớ, chỉ thêm.</summary>
public sealed class AuditServiceStub : IAuditService
{
    private readonly object _khoa = new();
    private readonly List<NhatKyDto> _nhatKy = new();
    private long _maNK;

    public Task Ghi(int maND, string hanhDong, LoaiChungTu? loaiCT = null, int? maCT = null, string? chiTiet = null, CancellationToken ct = default)
    {
        if (loaiCT.HasValue != maCT.HasValue)
            throw new ArgumentException("LoaiCT và MaCT phải cùng có giá trị hoặc cùng null (Bảng 2.26).");
        if (string.IsNullOrWhiteSpace(hanhDong))
            throw new ArgumentException("Hành động không được để trống.", nameof(hanhDong));

        lock (_khoa)
        {
            _nhatKy.Add(new NhatKyDto(++_maNK, maND, DateTime.UtcNow, hanhDong, loaiCT, maCT, chiTiet));
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NhatKyDto>> LayTheoChungTu(LoaiChungTu loaiCT, int maCT, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            IReadOnlyList<NhatKyDto> kq = _nhatKy.Where(n => n.LoaiCT == loaiCT && n.MaCT == maCT).OrderBy(n => n.MaNK).ToList();
            return Task.FromResult(kq);
        }
    }
}
