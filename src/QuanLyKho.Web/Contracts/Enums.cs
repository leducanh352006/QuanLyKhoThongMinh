namespace QuanLyKho.Web.Contracts;

/// <summary>Loại chứng từ; khớp CHECK ở Bảng 2.16 (NK nhập, XK xuất, CK chuyển, KK kiểm).</summary>
public enum LoaiChungTu { NK, XK, CK, KK }

/// <summary>Loại biến động ghi vào sổ cái BienDongTon (Bảng 2.16).</summary>
public enum LoaiBienDong { NhanHang, SapXep, XuatKho, ChuyenDi, NhanChuyen, DoiO, HoanTac }

/// <summary>Bốn bài toán gợi ý AI (Bảng 1.3): G1 lấy hàng, G2 xếp hàng, G3 nhập bổ sung, G4 luân chuyển kho.</summary>
public enum LoaiGoiY { G1, G2, G3, G4 }

/// <summary>Trạng thái duyệt của một gợi ý (mục 1.2.4).</summary>
public enum TrangThaiGoiY { ChoDuyet, DaDuyet, DaSuaVaDuyet, TuChoi }

/// <summary>Đổi enum sang đúng chuỗi lưu trong cơ sở dữ liệu (Bảng 2.16) để mọi nhóm dùng một chỗ.</summary>
public static class GiaTriCsdl
{
    public static string ToCsdl(this LoaiChungTu v) => v.ToString();

    public static string ToCsdl(this LoaiGoiY v) => v.ToString();

    public static string ToCsdl(this LoaiBienDong v) => v switch
    {
        LoaiBienDong.NhanHang => "Nhận hàng",
        LoaiBienDong.SapXep => "Sắp xếp",
        LoaiBienDong.XuatKho => "Xuất kho",
        LoaiBienDong.ChuyenDi => "Chuyển đi",
        LoaiBienDong.NhanChuyen => "Nhận chuyển",
        LoaiBienDong.DoiO => "Đổi ô",
        LoaiBienDong.HoanTac => "Hoàn tác",
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null),
    };

    /// <summary>Chiều ngược của ToCsdl(LoaiBienDong): đọc cột BienDongTon.LoaiBienDong (chuỗi) ra lại enum.</summary>
    public static LoaiBienDong ParseLoaiBienDong(string v) => v switch
    {
        "Nhận hàng" => LoaiBienDong.NhanHang,
        "Sắp xếp" => LoaiBienDong.SapXep,
        "Xuất kho" => LoaiBienDong.XuatKho,
        "Chuyển đi" => LoaiBienDong.ChuyenDi,
        "Nhận chuyển" => LoaiBienDong.NhanChuyen,
        "Đổi ô" => LoaiBienDong.DoiO,
        "Hoàn tác" => LoaiBienDong.HoanTac,
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null),
    };

    public static string ToCsdl(this TrangThaiGoiY v) => v switch
    {
        TrangThaiGoiY.ChoDuyet => "Chờ duyệt",
        TrangThaiGoiY.DaDuyet => "Đã duyệt",
        TrangThaiGoiY.DaSuaVaDuyet => "Đã sửa và duyệt",
        TrangThaiGoiY.TuChoi => "Từ chối",
        _ => throw new ArgumentOutOfRangeException(nameof(v), v, null),
    };
}

/// <summary>Tên bốn vai trò, khớp VaiTro.TenVaiTro (Bảng 2.2 và 2.19).</summary>
public static class VaiTroTen
{
    public const string QuanTriVien = "Quản trị viên";
    public const string QuanLyKho = "Quản lý kho";
    public const string ThuKho = "Thủ kho";
    public const string NhanVienBanHang = "Nhân viên bán hàng";

    public static readonly IReadOnlyList<string> TatCa = new[] { QuanTriVien, QuanLyKho, ThuKho, NhanVienBanHang };
}
