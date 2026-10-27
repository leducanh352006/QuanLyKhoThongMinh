using Microsoft.AspNetCore.Identity;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Băm và kiểm tra mật khẩu. Chỉ lưu bản băm (cột MatKhauHash, nvarchar(255)), không lưu mật khẩu gốc.</summary>
public interface IMatKhauService
{
    string Bam(string matKhau);

    /// <param name="canBamLai">true nếu mật khẩu đúng nhưng bản băm dùng tham số cũ, nên băm lại và lưu.</param>
    bool XacThuc(string? hash, string matKhau, out bool canBamLai);

    /// <summary>Bản băm của một mật khẩu ngẫu nhiên, dùng để so khớp giả khi tên đăng nhập không tồn tại (tránh lộ tên đăng nhập qua thời gian phản hồi).</summary>
    string BamGia();

    KetQua KiemTraDoManh(string? matKhau, string? tenDangNhap = null);
}

/// <summary>PBKDF2 của ASP.NET Core Identity (PasswordHasher), không cần cài thêm gói. Bản băm khoảng 100 ký tự nên vừa cột 255.</summary>
public sealed class MatKhauService : IMatKhauService
{
    public const int DoDaiToiThieu = 8;
    public const int DoDaiToiDa = 128;

    private readonly PasswordHasher<object> _hasher = new();
    private readonly object _nguoi = new();
    private readonly Lazy<string> _hashGia;

    public MatKhauService()
    {
        _hashGia = new Lazy<string>(() => _hasher.HashPassword(_nguoi, Guid.NewGuid().ToString("N")));
    }

    public string Bam(string matKhau) => _hasher.HashPassword(_nguoi, matKhau);

    public bool XacThuc(string? hash, string matKhau, out bool canBamLai)
    {
        canBamLai = false;
        if (string.IsNullOrEmpty(hash)) return false;
        try
        {
            var kq = _hasher.VerifyHashedPassword(_nguoi, hash, matKhau);
            canBamLai = kq == PasswordVerificationResult.SuccessRehashNeeded;
            return kq != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;   // bản băm hỏng: coi như sai mật khẩu
        }
    }

    public string BamGia() => _hashGia.Value;

    public KetQua KiemTraDoManh(string? matKhau, string? tenDangNhap = null)
    {
        if (string.IsNullOrEmpty(matKhau))
            return KetQua.Loi("Mật khẩu không được để trống.", MaLoiDichVu.DuLieuKhongHopLe);
        if (matKhau.Length < DoDaiToiThieu)
            return KetQua.Loi($"Mật khẩu cần ít nhất {DoDaiToiThieu} ký tự.", MaLoiDichVu.DuLieuKhongHopLe);
        if (matKhau.Length > DoDaiToiDa)
            return KetQua.Loi($"Mật khẩu tối đa {DoDaiToiDa} ký tự.", MaLoiDichVu.DuLieuKhongHopLe);
        if (!matKhau.Any(char.IsLetter) || !matKhau.Any(char.IsDigit))
            return KetQua.Loi("Mật khẩu cần có cả chữ và số.", MaLoiDichVu.DuLieuKhongHopLe);
        if (!string.IsNullOrWhiteSpace(tenDangNhap) && string.Equals(matKhau, tenDangNhap, StringComparison.OrdinalIgnoreCase))
            return KetQua.Loi("Mật khẩu không được trùng tên đăng nhập.", MaLoiDichVu.DuLieuKhongHopLe);
        return KetQua.Ok();
    }
}
