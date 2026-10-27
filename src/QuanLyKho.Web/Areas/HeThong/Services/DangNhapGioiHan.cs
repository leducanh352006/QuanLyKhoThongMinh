using System.Collections.Concurrent;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>
/// Chặn dò mật khẩu: sai <see cref="SoLanToiDa"/> lần trong <see cref="CuaSo"/> thì khóa việc đăng nhập của cặp (tên đăng nhập, địa chỉ IP)
/// trong <see cref="ThoiGianChan"/>. Khóa theo cặp để người lạ không khóa được tài khoản của người khác từ máy khác.
/// Giữ trong bộ nhớ (đủ cho một máy chủ); đổi sang kho dùng chung nếu sau này chạy nhiều máy chủ.
/// </summary>
public sealed class DangNhapGioiHan
{
    public const int SoLanToiDa = 5;
    public static readonly TimeSpan CuaSo = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan ThoiGianChan = TimeSpan.FromMinutes(15);
    private const int NguongDonRac = 1000;

    private sealed class Muc
    {
        public int Dem;
        public DateTimeOffset BatDau;
        public DateTimeOffset? ChanDen;
    }

    private readonly ConcurrentDictionary<string, Muc> _muc = new();

    public static string TaoKhoa(string tenDangNhap, string? diaChiIp) => $"{tenDangNhap.Trim().ToLowerInvariant()}|{diaChiIp}";

    /// <summary>Còn bị chặn bao lâu; null nếu được phép thử.</summary>
    public TimeSpan? ConBiChan(string khoa, DateTimeOffset bayGio)
    {
        if (!_muc.TryGetValue(khoa, out var m)) return null;
        lock (m)
        {
            if (m.ChanDen is DateTimeOffset den)
            {
                if (den > bayGio) return den - bayGio;
                m.ChanDen = null;   // hết thời gian chặn: cho thử lại từ đầu
                m.Dem = 0;
            }
            return null;
        }
    }

    public void GhiThatBai(string khoa, DateTimeOffset bayGio)
    {
        if (_muc.Count > NguongDonRac) DonRac(bayGio);
        var m = _muc.GetOrAdd(khoa, _ => new Muc { BatDau = bayGio });
        lock (m)
        {
            if (bayGio - m.BatDau > CuaSo) { m.Dem = 0; m.BatDau = bayGio; }
            m.Dem++;
            if (m.Dem >= SoLanToiDa) m.ChanDen = bayGio + ThoiGianChan;
        }
    }

    public void XoaThatBai(string khoa) => _muc.TryRemove(khoa, out _);

    private void DonRac(DateTimeOffset bayGio)
    {
        foreach (var (khoa, m) in _muc)
        {
            lock (m)
            {
                var hetChan = m.ChanDen is null || m.ChanDen <= bayGio;
                if (hetChan && bayGio - m.BatDau > CuaSo) _muc.TryRemove(khoa, out _);
            }
        }
    }
}
