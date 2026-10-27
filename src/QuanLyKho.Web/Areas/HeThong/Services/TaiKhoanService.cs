using Microsoft.Extensions.Caching.Memory;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Mã lỗi riêng của đăng nhập (ngoài <see cref="MaLoiDichVu"/>).</summary>
public static class MaLoiTaiKhoan
{
    public const string SaiThongTin = "SAI_THONG_TIN";
    public const string TaiKhoanBiKhoa = "TAI_KHOAN_BI_KHOA";
    public const string QuaNhieuLan = "QUA_NHIEU_LAN";
}

public interface ITaiKhoanService
{
    /// <summary>Kiểm tra tên đăng nhập và mật khẩu. Thành công trả thông tin người dùng để tạo phiên đăng nhập.</summary>
    Task<KetQua<NguoiDungHienTai>> DangNhap(string? tenDangNhap, string? matKhau, string? diaChiIp, CancellationToken ct = default);

    /// <summary>
    /// Kiểm tra phiên đang có còn hợp lệ không (gọi mỗi yêu cầu, có nhớ tạm ngắn). Trả thông tin MỚI NHẤT của người dùng,
    /// hoặc null nếu tài khoản bị khóa, bị xóa hoặc mất vai trò. Nhờ đó khóa tài khoản và đổi vai trò có hiệu lực ngay, không phải chờ hết hạn cookie.
    /// </summary>
    Task<NguoiDungHienTai?> KiemTraPhien(int maND, CancellationToken ct = default);

    /// <summary>Xóa nhớ tạm của phiên một người dùng (gọi sau khi khóa, đổi vai trò).</summary>
    void HuyNhoTamPhien(int maND);

    Task GhiDangXuat(int maND, CancellationToken ct = default);
}

public sealed class TaiKhoanService(
    IHeThongStore store,
    IMatKhauService matKhau,
    DangNhapGioiHan gioiHan,
    IAuditService audit,
    IMemoryCache nhoTam,
    TimeProvider thoiGian) : ITaiKhoanService
{
    public static readonly TimeSpan ThoiGianNhoTamPhien = TimeSpan.FromSeconds(30);

    private const string ThongBaoSaiThongTin = "Tên đăng nhập hoặc mật khẩu không đúng.";

    public async Task<KetQua<NguoiDungHienTai>> DangNhap(string? tenDangNhap, string? mk, string? diaChiIp, CancellationToken ct = default)
    {
        var ten = (tenDangNhap ?? "").Trim();
        if (ten.Length == 0 || string.IsNullOrEmpty(mk))
            return KetQua<NguoiDungHienTai>.Loi("Nhập tên đăng nhập và mật khẩu.", MaLoiDichVu.DuLieuKhongHopLe);

        var bayGio = thoiGian.GetUtcNow();
        var khoa = DangNhapGioiHan.TaoKhoa(ten, diaChiIp);
        if (gioiHan.ConBiChan(khoa, bayGio) is TimeSpan conLai)
            return KetQua<NguoiDungHienTai>.Loi(
                $"Đăng nhập sai quá nhiều lần. Thử lại sau {Math.Max(1, (int)Math.Ceiling(conLai.TotalMinutes))} phút.", MaLoiTaiKhoan.QuaNhieuLan);

        var nguoi = await store.TimNguoiDungTheoTen(ten, ct);

        // Tên không tồn tại vẫn băm và so một lần để thời gian phản hồi không lộ tên nào có thật.
        var dung = matKhau.XacThuc(nguoi?.MatKhauHash ?? matKhau.BamGia(), mk, out var canBamLai) && nguoi is not null;
        if (!dung)
        {
            gioiHan.GhiThatBai(khoa, bayGio);
            return KetQua<NguoiDungHienTai>.Loi(ThongBaoSaiThongTin, MaLoiTaiKhoan.SaiThongTin);
        }

        // Đã đúng mật khẩu mới báo tài khoản bị khóa, để người lạ không dò được tài khoản nào tồn tại hay bị khóa.
        if (!nguoi!.DangHoatDong)
            return KetQua<NguoiDungHienTai>.Loi("Tài khoản đã bị khóa. Liên hệ quản trị viên.", MaLoiTaiKhoan.TaiKhoanBiKhoa);

        var hienTai = await TaoThongTin(nguoi, ct);
        if (hienTai is null)
            return KetQua<NguoiDungHienTai>.Loi("Tài khoản chưa có vai trò hợp lệ. Liên hệ quản trị viên.", MaLoiDichVu.DuLieuKhongHopLe);

        if (canBamLai)
        {
            nguoi.MatKhauHash = matKhau.Bam(mk);
            await store.LuuThayDoi(ct);
        }

        gioiHan.XoaThatBai(khoa);
        await audit.Ghi(nguoi.MaND, "Đăng nhập", ct: ct);
        return KetQua<NguoiDungHienTai>.Ok(hienTai);
    }

    public async Task<NguoiDungHienTai?> KiemTraPhien(int maND, CancellationToken ct = default)
    {
        var khoa = KhoaNhoTam(maND);
        if (nhoTam.TryGetValue(khoa, out NguoiDungHienTai? coSan)) return coSan;

        var nguoi = await store.TimNguoiDung(maND, ct);
        var kq = nguoi is { DangHoatDong: true } ? await TaoThongTin(nguoi, ct) : null;
        nhoTam.Set(khoa, kq, ThoiGianNhoTamPhien);   // nhớ cả kết quả null để người bị khóa không gây truy vấn mỗi yêu cầu
        return kq;
    }

    public void HuyNhoTamPhien(int maND) => nhoTam.Remove(KhoaNhoTam(maND));

    public Task GhiDangXuat(int maND, CancellationToken ct = default) => audit.Ghi(maND, "Đăng xuất", ct: ct);

    private async Task<NguoiDungHienTai?> TaoThongTin(NguoiDung nguoi, CancellationToken ct)
    {
        var vaiTro = (await store.LayVaiTro(ct)).FirstOrDefault(v => v.MaVaiTro == nguoi.MaVaiTro);
        return vaiTro is null
            ? null
            : new NguoiDungHienTai(nguoi.MaND, nguoi.TenDangNhap, nguoi.HoTen, vaiTro.MaVaiTro, vaiTro.TenVaiTro);
    }

    private static string KhoaNhoTam(int maND) => $"hethong:phien:{maND}";
}
