namespace QuanLyKho.Web.Contracts.Stubs;

/// <summary>Bản giả của <see cref="ILockService"/>: lưu tập ô đang khóa trong bộ nhớ.</summary>
public sealed class LockServiceStub : ILockService
{
    private readonly object _khoa = new();
    private readonly HashSet<int> _odangKhoa = new();

    public Task<KetQua> KhoaO(int maViTri, int maND, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            if (!_odangKhoa.Add(maViTri))
                return Task.FromResult(KetQua.Loi($"Ô {maViTri} đang bị khóa để kiểm kê.", MaLoiDichVu.OBiKhoa));
        }
        return Task.FromResult(KetQua.Ok());
    }

    public Task<KetQua> MoO(int maViTri, int maND, CancellationToken ct = default)
    {
        lock (_khoa) { _odangKhoa.Remove(maViTri); }
        return Task.FromResult(KetQua.Ok());
    }

    public Task<bool> DangKhoa(int maViTri, CancellationToken ct = default)
    {
        lock (_khoa) { return Task.FromResult(_odangKhoa.Contains(maViTri)); }
    }
}
