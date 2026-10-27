using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>
/// Kho dữ liệu trong bộ nhớ của Area HeThong, nạp sẵn từ <see cref="SeedDataSet"/> (4 vai trò, 4 người dùng) và <see cref="QuyenMacDinh"/>.
/// Dùng khi chưa cấu hình SQL Server ở môi trường Development (để mọi thành viên đăng nhập và làm việc ngay) và trong kiểm thử.
/// Dữ liệu mất khi tắt ứng dụng. Mật khẩu 4 người dùng mẫu lấy từ cấu hình <c>Auth:SeedPassword</c> (User Secrets hoặc biến môi trường), không có trong git.
/// </summary>
public sealed class BoNhoHeThongStore : IHeThongStore
{
    public const string KhoaCauHinhMatKhauMau = "Auth:SeedPassword";

    private readonly object _khoa = new();
    private readonly List<VaiTro> _vaiTro = new();
    private readonly List<NguoiDung> _nguoiDung = new();
    private readonly List<PhanQuyen> _phanQuyen = new();
    private int _maND;

    public BoNhoHeThongStore(IConfiguration cauHinh, IMatKhauService matKhau, ILogger<BoNhoHeThongStore> log)
    {
        foreach (var v in SeedDataSet.VaiTro)
        {
            _vaiTro.Add(new VaiTro { MaVaiTro = v.MaVaiTro, TenVaiTro = v.TenVaiTro });
            _phanQuyen.AddRange(QuyenMacDinh.Cho(v.MaVaiTro, v.TenVaiTro));
        }

        var matKhauMau = cauHinh[KhoaCauHinhMatKhauMau];
        var kiemTra = matKhau.KiemTraDoManh(matKhauMau);
        if (!kiemTra.ThanhCong)
        {
            log.LogWarning(
                "Chưa có tài khoản mẫu nào vì cấu hình {Khoa} {LyDo}. Đặt bằng: dotnet user-secrets set \"Auth:SeedPassword\" \"<mật khẩu>\" (xem Areas/HeThong/README.md).",
                KhoaCauHinhMatKhauMau, string.IsNullOrEmpty(matKhauMau) ? "chưa được đặt" : "không đủ mạnh: " + kiemTra.ThongBao);
            return;
        }

        var hash = matKhau.Bam(matKhauMau!);   // cùng một mật khẩu cho cả 4 tài khoản mẫu: chỉ băm một lần cho nhanh
        foreach (var n in SeedDataSet.NguoiDung)
        {
            _nguoiDung.Add(new NguoiDung
            {
                MaND = n.MaND, TenDangNhap = n.TenDangNhap, MatKhauHash = hash, HoTen = n.HoTen,
                Email = n.Email, MaVaiTro = n.MaVaiTro, TrangThai = TrangThaiNguoiDung.HoatDong,
            });
            _maND = Math.Max(_maND, n.MaND);
        }
    }

    public Task<IReadOnlyList<VaiTro>> LayVaiTro(CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult<IReadOnlyList<VaiTro>>(_vaiTro.OrderBy(v => v.MaVaiTro).ToList());
    }

    public Task<IReadOnlyList<NguoiDung>> LayNguoiDung(CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult<IReadOnlyList<NguoiDung>>(_nguoiDung.OrderBy(n => n.MaND).ToList());
    }

    public Task<NguoiDung?> TimNguoiDung(int maND, CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult(_nguoiDung.FirstOrDefault(n => n.MaND == maND));
    }

    public Task<NguoiDung?> TimNguoiDungTheoTen(string tenDangNhap, CancellationToken ct = default)
    {
        lock (_khoa)
            return Task.FromResult(_nguoiDung.FirstOrDefault(n => string.Equals(n.TenDangNhap, tenDangNhap, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<NguoiDung> ThemNguoiDung(NguoiDung nguoiDung, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            if (_nguoiDung.Any(n => string.Equals(n.TenDangNhap, nguoiDung.TenDangNhap, StringComparison.OrdinalIgnoreCase)))
                throw new XungDotDuLieuException("Tên đăng nhập đã tồn tại.");
            nguoiDung.MaND = ++_maND;
            _nguoiDung.Add(nguoiDung);
            return Task.FromResult(nguoiDung);
        }
    }

    public Task LuuThayDoi(CancellationToken ct = default) => Task.CompletedTask;   // đối tượng trả ra là bản gốc nên sửa là đã lưu

    public Task<int> DemQuanTriVienHoatDong(int? botMaND, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            var maQtv = _vaiTro.Single(v => v.TenVaiTro == VaiTroTen.QuanTriVien).MaVaiTro;
            return Task.FromResult(_nguoiDung.Count(n => n.MaVaiTro == maQtv && n.DangHoatDong && n.MaND != botMaND));
        }
    }

    public Task<IReadOnlyList<PhanQuyen>> LayPhanQuyen(CancellationToken ct = default)
    {
        lock (_khoa) return Task.FromResult<IReadOnlyList<PhanQuyen>>(_phanQuyen.Select(p => p.Sao()).ToList());
    }

    public Task ThayPhanQuyenCuaVaiTro(int maVaiTro, IReadOnlyList<PhanQuyen> dong, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            foreach (var d in dong)
            {
                var cu = _phanQuyen.FirstOrDefault(p => p.MaVaiTro == maVaiTro && p.LoaiChungTu == d.LoaiChungTu);
                if (cu is null)
                {
                    var moi = d.Sao();
                    moi.MaVaiTro = maVaiTro;
                    _phanQuyen.Add(moi);
                }
                else
                {
                    cu.CoXem = d.CoXem; cu.CoThem = d.CoThem; cu.CoSua = d.CoSua; cu.CoXoa = d.CoXoa; cu.CoDuyet = d.CoDuyet;
                }
            }
        }
        return Task.CompletedTask;
    }
}
