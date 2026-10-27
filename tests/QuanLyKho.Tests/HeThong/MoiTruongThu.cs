using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Tests.HeThong;

/// <summary>Nhật ký giả ghi lại mọi lần gọi để kiểm tra việc nào được ghi nhật ký.</summary>
internal sealed class NhatKyGia : IAuditService
{
    public List<(int MaND, string HanhDong, string? ChiTiet)> Dong { get; } = new();

    public Task Ghi(int maND, string hanhDong, LoaiChungTu? loaiCT = null, int? maCT = null, string? chiTiet = null, CancellationToken ct = default)
    {
        lock (Dong) Dong.Add((maND, hanhDong, chiTiet));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NhatKyDto>> LayTheoChungTu(LoaiChungTu loaiCT, int maCT, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<NhatKyDto>>(Array.Empty<NhatKyDto>());
}

/// <summary>Đồng hồ giả để thử việc hết hạn chặn đăng nhập mà không phải chờ thật.</summary>
internal sealed class DongHoGia : TimeProvider
{
    public DateTimeOffset BayGio { get; private set; } = new(2026, 10, 26, 8, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => BayGio;

    public void Tang(TimeSpan t) => BayGio += t;
}

internal sealed class NguoiDungHienTaiGia : INguoiDungHienTai
{
    public NguoiDungHienTai? NguoiDung { get; set; }

    public NguoiDungHienTai? Lay() => NguoiDung;
}

/// <summary>
/// Dựng sẵn toàn bộ dịch vụ của T4-2 trên kho dữ liệu trong bộ nhớ (4 tài khoản mẫu, mật khẩu <see cref="MatKhauMau"/>) nên test chạy không cần SQL Server.
/// Mã mẫu: 1 Quản trị viên (quantri), 2 Quản lý kho (quanlykho), 3 Thủ kho (thukho), 4 Nhân viên bán hàng (nhanvienbh).
/// </summary>
internal sealed class MoiTruongThu
{
    public const string MatKhauMau = "Demo@12345";
    public const int QuanTriVien = 1, QuanLyKho = 2, ThuKho = 3, NhanVienBanHang = 4;

    public MatKhauService MatKhau { get; } = new();
    public DongHoGia DongHo { get; } = new();
    public NhatKyGia NhatKy { get; } = new();
    public NguoiDungHienTaiGia HienTai { get; } = new();
    public DangNhapGioiHan GioiHan { get; } = new();
    public MemoryCache NhoTam { get; } = new(new MemoryCacheOptions());
    public BoNhoHeThongStore Store { get; }
    public TaiKhoanService TaiKhoan { get; }
    public PhanQuyenService PhanQuyen { get; }
    public NguoiDungQuanLyService NguoiDung { get; }

    public MoiTruongThu(string? matKhauMau = MatKhauMau)
    {
        var cauHinh = new ConfigurationBuilder()
            .AddInMemoryCollection(matKhauMau is null
                ? new Dictionary<string, string?>()
                : new Dictionary<string, string?> { [BoNhoHeThongStore.KhoaCauHinhMatKhauMau] = matKhauMau })
            .Build();

        Store = new BoNhoHeThongStore(cauHinh, MatKhau, NullLogger<BoNhoHeThongStore>.Instance);
        TaiKhoan = new TaiKhoanService(Store, MatKhau, GioiHan, NhatKy, NhoTam, DongHo);
        PhanQuyen = new PhanQuyenService(Store, HienTai, NhatKy, NhoTam);
        NguoiDung = new NguoiDungQuanLyService(Store, MatKhau, TaiKhoan, NhatKy);
    }

    /// <summary>Đặt người đang đăng nhập (cho IPhanQuyenService.CoQuyenHienTai và KiemTra).</summary>
    public void DangNhapLa(int maND)
    {
        var n = Store.TimNguoiDung(maND).Result!;
        var vt = Store.LayVaiTro().Result.Single(v => v.MaVaiTro == n.MaVaiTro);
        HienTai.NguoiDung = new NguoiDungHienTai(n.MaND, n.TenDangNhap, n.HoTen, vt.MaVaiTro, vt.TenVaiTro);
    }
}
