using System.Net.Mail;
using System.Text.RegularExpressions;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Một người dùng để hiển thị; không bao giờ mang theo bản băm mật khẩu.</summary>
public sealed record NguoiDungDong(int MaND, string TenDangNhap, string HoTen, string? Email, string? SDT, int MaVaiTro, string TenVaiTro, string TrangThai)
{
    public bool DangHoatDong => TrangThai == TrangThaiNguoiDung.HoatDong;
}

public sealed record NguoiDungMoi(string TenDangNhap, string MatKhau, string HoTen, string? Email, string? SDT, int MaVaiTro);

public sealed record NguoiDungSuaDoi(string HoTen, string? Email, string? SDT, int MaVaiTro);

/// <summary>Thêm, sửa, khóa người dùng và đặt lại mật khẩu (màn hình Quản lý người dùng, chỉ Quản trị viên).</summary>
public interface INguoiDungQuanLy
{
    Task<IReadOnlyList<NguoiDungDong>> LayDanhSach(CancellationToken ct = default);

    Task<NguoiDungDong?> Lay(int maND, CancellationToken ct = default);

    Task<IReadOnlyList<VaiTro>> LayVaiTro(CancellationToken ct = default);

    Task<KetQua> Them(NguoiDungMoi moi, int maNguoiThucHien, CancellationToken ct = default);

    Task<KetQua> Sua(int maND, NguoiDungSuaDoi doi, int maNguoiThucHien, CancellationToken ct = default);

    /// <summary>Khóa (hoatDong = false) hoặc mở khóa. Có hiệu lực ngay với phiên đang đăng nhập.</summary>
    Task<KetQua> DatTrangThai(int maND, bool hoatDong, int maNguoiThucHien, CancellationToken ct = default);

    Task<KetQua> DatLaiMatKhau(int maND, string matKhauMoi, int maNguoiThucHien, CancellationToken ct = default);
}

public sealed partial class NguoiDungQuanLyService(
    IHeThongStore store,
    IMatKhauService matKhau,
    ITaiKhoanService taiKhoan,
    IAuditService audit) : INguoiDungQuanLy
{
    [GeneratedRegex("^[A-Za-z0-9._-]{3,50}$")]
    private static partial Regex MauTenDangNhap();

    [GeneratedRegex(@"^[0-9+ ().-]{8,15}$")]
    private static partial Regex MauSdt();

    public async Task<IReadOnlyList<NguoiDungDong>> LayDanhSach(CancellationToken ct = default)
    {
        var vaiTro = await store.LayVaiTro(ct);
        return (await store.LayNguoiDung(ct)).Select(n => ChoHienThi(n, vaiTro)).ToList();
    }

    public async Task<NguoiDungDong?> Lay(int maND, CancellationToken ct = default)
    {
        var nguoi = await store.TimNguoiDung(maND, ct);
        return nguoi is null ? null : ChoHienThi(nguoi, await store.LayVaiTro(ct));
    }

    public Task<IReadOnlyList<VaiTro>> LayVaiTro(CancellationToken ct = default) => store.LayVaiTro(ct);

    public async Task<KetQua> Them(NguoiDungMoi moi, int maNguoiThucHien, CancellationToken ct = default)
    {
        var ten = moi.TenDangNhap.Trim();
        var vaiTro = await store.LayVaiTro(ct);

        var loi = KiemTraTenDangNhap(ten) ?? KiemTraHoTen(moi.HoTen) ?? KiemTraEmail(moi.Email) ?? KiemTraSdt(moi.SDT) ?? KiemTraVaiTro(moi.MaVaiTro, vaiTro);
        if (loi is not null) return loi;

        var doManh = matKhau.KiemTraDoManh(moi.MatKhau, ten);
        if (!doManh.ThanhCong) return doManh;

        if (await store.TimNguoiDungTheoTen(ten, ct) is not null)
            return KetQua.Loi("Tên đăng nhập đã tồn tại.", MaLoiDichVu.DuLieuKhongHopLe);

        NguoiDung them;
        try
        {
            them = await store.ThemNguoiDung(new NguoiDung
            {
                TenDangNhap = ten, MatKhauHash = matKhau.Bam(moi.MatKhau), HoTen = moi.HoTen.Trim(),
                Email = Chuan(moi.Email), SDT = Chuan(moi.SDT), MaVaiTro = moi.MaVaiTro, TrangThai = TrangThaiNguoiDung.HoatDong,
            }, ct);
        }
        catch (XungDotDuLieuException)
        {
            return KetQua.Loi("Tên đăng nhập đã tồn tại.", MaLoiDichVu.DuLieuKhongHopLe);
        }

        await audit.Ghi(maNguoiThucHien, "Thêm người dùng", chiTiet: $"{ten}, vai trò {TenVaiTro(moi.MaVaiTro, vaiTro)}", ct: ct);
        return KetQua.Ok($"Đã thêm người dùng {ten}.");
    }

    public async Task<KetQua> Sua(int maND, NguoiDungSuaDoi doi, int maNguoiThucHien, CancellationToken ct = default)
    {
        var nguoi = await store.TimNguoiDung(maND, ct);
        if (nguoi is null) return KetQua.Loi("Không tìm thấy người dùng.", MaLoiDichVu.KhongTimThay);

        var vaiTro = await store.LayVaiTro(ct);
        var loi = KiemTraHoTen(doi.HoTen) ?? KiemTraEmail(doi.Email) ?? KiemTraSdt(doi.SDT) ?? KiemTraVaiTro(doi.MaVaiTro, vaiTro);
        if (loi is not null) return loi;

        var doiVaiTro = nguoi.MaVaiTro != doi.MaVaiTro;
        if (doiVaiTro)
        {
            if (maND == maNguoiThucHien)
                return KetQua.Loi("Không thể tự đổi vai trò của chính mình.", MaLoiDichVu.DuLieuKhongHopLe);
            if (nguoi.DangHoatDong && LaQuanTriVien(nguoi.MaVaiTro, vaiTro) && await store.DemQuanTriVienHoatDong(maND, ct) == 0)
                return KetQua.Loi("Đây là Quản trị viên hoạt động cuối cùng nên không thể đổi vai trò.", MaLoiDichVu.DuLieuKhongHopLe);
        }

        var vaiTroCu = TenVaiTro(nguoi.MaVaiTro, vaiTro);
        nguoi.HoTen = doi.HoTen.Trim();
        nguoi.Email = Chuan(doi.Email);
        nguoi.SDT = Chuan(doi.SDT);
        nguoi.MaVaiTro = doi.MaVaiTro;
        await store.LuuThayDoi(ct);
        taiKhoan.HuyNhoTamPhien(maND);

        await audit.Ghi(maNguoiThucHien, "Sửa người dùng", chiTiet: doiVaiTro
            ? $"{nguoi.TenDangNhap}, vai trò {vaiTroCu} -> {TenVaiTro(doi.MaVaiTro, vaiTro)}"
            : nguoi.TenDangNhap, ct: ct);
        return KetQua.Ok("Đã lưu thay đổi.");
    }

    public async Task<KetQua> DatTrangThai(int maND, bool hoatDong, int maNguoiThucHien, CancellationToken ct = default)
    {
        var nguoi = await store.TimNguoiDung(maND, ct);
        if (nguoi is null) return KetQua.Loi("Không tìm thấy người dùng.", MaLoiDichVu.KhongTimThay);
        if (nguoi.DangHoatDong == hoatDong) return KetQua.Ok(hoatDong ? "Tài khoản đang hoạt động." : "Tài khoản đã bị khóa từ trước.");

        if (!hoatDong)
        {
            if (maND == maNguoiThucHien)
                return KetQua.Loi("Không thể tự khóa tài khoản của chính mình.", MaLoiDichVu.DuLieuKhongHopLe);
            if (LaQuanTriVien(nguoi.MaVaiTro, await store.LayVaiTro(ct)) && await store.DemQuanTriVienHoatDong(maND, ct) == 0)
                return KetQua.Loi("Đây là Quản trị viên hoạt động cuối cùng nên không thể khóa.", MaLoiDichVu.DuLieuKhongHopLe);
        }

        nguoi.TrangThai = hoatDong ? TrangThaiNguoiDung.HoatDong : TrangThaiNguoiDung.NgungHoatDong;
        await store.LuuThayDoi(ct);
        taiKhoan.HuyNhoTamPhien(maND);

        await audit.Ghi(maNguoiThucHien, hoatDong ? "Mở khóa người dùng" : "Khóa người dùng", chiTiet: nguoi.TenDangNhap, ct: ct);
        return KetQua.Ok(hoatDong ? $"Đã mở khóa {nguoi.TenDangNhap}." : $"Đã khóa {nguoi.TenDangNhap}.");
    }

    public async Task<KetQua> DatLaiMatKhau(int maND, string matKhauMoi, int maNguoiThucHien, CancellationToken ct = default)
    {
        var nguoi = await store.TimNguoiDung(maND, ct);
        if (nguoi is null) return KetQua.Loi("Không tìm thấy người dùng.", MaLoiDichVu.KhongTimThay);

        var doManh = matKhau.KiemTraDoManh(matKhauMoi, nguoi.TenDangNhap);
        if (!doManh.ThanhCong) return doManh;

        nguoi.MatKhauHash = matKhau.Bam(matKhauMoi);
        await store.LuuThayDoi(ct);

        await audit.Ghi(maNguoiThucHien, "Đặt lại mật khẩu", chiTiet: nguoi.TenDangNhap, ct: ct);   // không ghi mật khẩu vào nhật ký
        return KetQua.Ok($"Đã đặt lại mật khẩu cho {nguoi.TenDangNhap}.");
    }

    // ---- kiểm tra đầu vào ----

    private static KetQua? KiemTraTenDangNhap(string ten)
        => MauTenDangNhap().IsMatch(ten) ? null
            : KetQua.Loi("Tên đăng nhập gồm 3 đến 50 ký tự: chữ cái không dấu, số, dấu chấm, gạch dưới hoặc gạch ngang.", MaLoiDichVu.DuLieuKhongHopLe);

    private static KetQua? KiemTraHoTen(string? hoTen)
        => string.IsNullOrWhiteSpace(hoTen) || hoTen.Trim().Length > 100
            ? KetQua.Loi("Họ và tên không được để trống và tối đa 100 ký tự.", MaLoiDichVu.DuLieuKhongHopLe) : null;

    private static KetQua? KiemTraEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var e = email.Trim();
        var hopLe = e.Length <= 100 && MailAddress.TryCreate(e, out var m) && m.Address == e;
        return hopLe ? null : KetQua.Loi("Email không hợp lệ (tối đa 100 ký tự).", MaLoiDichVu.DuLieuKhongHopLe);
    }

    private static KetQua? KiemTraSdt(string? sdt)
        => string.IsNullOrWhiteSpace(sdt) || MauSdt().IsMatch(sdt.Trim()) ? null
            : KetQua.Loi("Số điện thoại gồm 8 đến 15 ký tự (số, dấu cách, +, -, ., ngoặc).", MaLoiDichVu.DuLieuKhongHopLe);

    private static KetQua? KiemTraVaiTro(int maVaiTro, IReadOnlyList<VaiTro> vaiTro)
        => vaiTro.Any(v => v.MaVaiTro == maVaiTro) ? null : KetQua.Loi("Vai trò không tồn tại.", MaLoiDichVu.DuLieuKhongHopLe);

    private static string? Chuan(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static bool LaQuanTriVien(int maVaiTro, IReadOnlyList<VaiTro> vaiTro)
        => vaiTro.Any(v => v.MaVaiTro == maVaiTro && v.TenVaiTro == VaiTroTen.QuanTriVien);

    private static string TenVaiTro(int maVaiTro, IReadOnlyList<VaiTro> vaiTro)
        => vaiTro.FirstOrDefault(v => v.MaVaiTro == maVaiTro)?.TenVaiTro ?? "?";

    private static NguoiDungDong ChoHienThi(NguoiDung n, IReadOnlyList<VaiTro> vaiTro)
        => new(n.MaND, n.TenDangNhap, n.HoTen, n.Email, n.SDT, n.MaVaiTro, TenVaiTro(n.MaVaiTro, vaiTro), n.TrangThai);
}
