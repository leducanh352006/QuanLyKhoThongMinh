using Microsoft.AspNetCore.Authorization;

namespace QuanLyKho.Web.Contracts;

// ---------------------------------------------------------------------------------------------------------------
// Hợp đồng PHÂN QUYỀN dùng chung (việc T4-2). Mọi nhóm dùng ba thứ trong file này, không tự viết kiểm quyền riêng:
//   1. [CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Duyet)]  đặt trên Controller hoặc Action  -> kiểm quyền ở phía máy chủ.
//   2. IPhanQuyenService                                dùng trong Service khi cần hỏi quyền bằng code.
//   3. QuyTacDuyet.KiemTra(...)                         hàm chung chặn người lập tự duyệt phiếu của mình (Bảng 2.17 dòng 8).
// ---------------------------------------------------------------------------------------------------------------

/// <summary>Năm quyền của bảng PhanQuyen (Bảng 2.19): CoXem, CoThem, CoSua, CoXoa, CoDuyet.</summary>
public enum QuyenThaoTac { Xem, Them, Sua, Xoa, Duyet }

/// <summary>Mã lỗi phân quyền để nơi gọi xử lý theo mã, không so chuỗi thông báo.</summary>
public static class MaLoiPhanQuyen
{
    public const string ChuaDangNhap = "CHUA_DANG_NHAP";
    public const string KhongCoQuyen = "KHONG_CO_QUYEN";
    public const string TuDuyet = "TU_DUYET";
}

/// <summary>Tên hiển thị tiếng Việt của quyền và chứng từ, dùng cho thông báo lỗi.</summary>
public static class TenHienThiPhanQuyen
{
    public static string Ten(this QuyenThaoTac q) => q switch
    {
        QuyenThaoTac.Xem => "xem",
        QuyenThaoTac.Them => "thêm",
        QuyenThaoTac.Sua => "sửa",
        QuyenThaoTac.Xoa => "xóa hoặc hủy",
        QuyenThaoTac.Duyet => "duyệt",
        _ => throw new ArgumentOutOfRangeException(nameof(q), q, null),
    };

    public static string Ten(this LoaiChungTu c) => c switch
    {
        LoaiChungTu.NK => "phiếu nhập",
        LoaiChungTu.XK => "phiếu xuất",
        LoaiChungTu.CK => "phiếu chuyển kho",
        LoaiChungTu.KK => "phiếu kiểm kê",
        _ => throw new ArgumentOutOfRangeException(nameof(c), c, null),
    };
}

/// <summary>
/// Hỏi quyền theo (vai trò, chứng từ, thao tác). Dữ liệu lấy từ bảng PhanQuyen, có nhớ tạm nên gọi nhiều lần không tốn truy vấn.
/// Bản thật do Area HeThong đăng ký; nhóm khác chỉ dùng interface này.
/// </summary>
public interface IPhanQuyenService
{
    /// <summary>Vai trò có quyền này trên chứng từ này không. Vai trò hoặc chứng từ chưa có dòng quyền thì trả false.</summary>
    Task<bool> CoQuyen(int maVaiTro, LoaiChungTu loai, QuyenThaoTac quyen, CancellationToken ct = default);

    /// <summary>Như <see cref="CoQuyen"/> nhưng cho người đang đăng nhập; chưa đăng nhập thì false.</summary>
    Task<bool> CoQuyenHienTai(LoaiChungTu loai, QuyenThaoTac quyen, CancellationToken ct = default);

    /// <summary>Như <see cref="CoQuyenHienTai"/> nhưng trả <see cref="KetQua"/> kèm thông báo tiếng Việt, tiện cho Service trả thẳng ra giao diện.</summary>
    Task<KetQua> KiemTra(LoaiChungTu loai, QuyenThaoTac quyen, CancellationToken ct = default);
}

/// <summary>
/// Quy tắc "người lập không tự duyệt phiếu của mình" (Bảng 2.17 dòng 8), viết thành hàm thuần để mọi nhóm gọi cùng một chỗ.
/// <list type="bullet">
/// <item>Phiếu xuất: truyền MaNguoiLap của phiếu.</item>
/// <item>Dòng kiểm kê: truyền MaNguoiDem của dòng kiểm.</item>
/// <item>Phiếu chuyển, phiếu nhập: báo cáo chưa bắt buộc; nhóm nào muốn áp dụng thì gọi cùng hàm này.</item>
/// </list>
/// Luôn gọi <b>ở phía máy chủ ngay trước khi ghi trạng thái duyệt</b>, không chỉ ẩn nút Duyệt trên giao diện.
/// </summary>
public static class QuyTacDuyet
{
    /// <param name="maNguoiDuyet">MaND của người đang duyệt (lấy từ <see cref="INguoiDungHienTai"/>).</param>
    /// <param name="maNguoiLapHoacDem">MaND người lập phiếu / người đếm; null nghĩa là chưa có người (không chặn).</param>
    /// <param name="doiTuong">Tên đối tượng để đưa vào thông báo, ví dụ "phiếu xuất", "dòng kiểm".</param>
    public static KetQua KiemTra(int maNguoiDuyet, int? maNguoiLapHoacDem, string doiTuong = "phiếu")
        => maNguoiLapHoacDem is int ma && ma == maNguoiDuyet
            ? KetQua.Loi($"Bạn không thể tự duyệt {doiTuong} do chính mình lập hoặc đếm.", MaLoiPhanQuyen.TuDuyet)
            : KetQua.Ok();

    public static bool DuocDuyet(int maNguoiDuyet, int? maNguoiLapHoacDem) => KiemTra(maNguoiDuyet, maNguoiLapHoacDem).ThanhCong;
}

/// <summary>Tên chính sách (policy) sinh từ (chứng từ, quyền), dạng <c>quyen:XK:Duyet</c>. Chính sách được dựng khi cần, không phải đăng ký từng cái.</summary>
public static class ChinhSachQuyen
{
    public const string TienTo = "quyen:";

    public static string Ten(LoaiChungTu loai, QuyenThaoTac quyen) => $"{TienTo}{loai}:{quyen}";

    public static bool TryParse(string? ten, out LoaiChungTu loai, out QuyenThaoTac quyen)
    {
        loai = default;
        quyen = default;
        if (ten is null || !ten.StartsWith(TienTo, StringComparison.Ordinal)) return false;
        var phan = ten[TienTo.Length..].Split(':');
        return phan.Length == 2
            && Enum.TryParse(phan[0], ignoreCase: false, out loai) && Enum.IsDefined(loai)
            && Enum.TryParse(phan[1], ignoreCase: false, out quyen) && Enum.IsDefined(quyen);
    }
}

/// <summary>
/// Đặt trên Controller hoặc Action để chỉ cho người có quyền (theo bảng PhanQuyen) gọi vào. Kiểm ở phía máy chủ:
/// chưa đăng nhập thì chuyển tới trang đăng nhập, đã đăng nhập mà thiếu quyền thì bị từ chối (403).
/// Đặt nhiều thuộc tính thì phải thỏa tất cả. Ví dụ: <c>[CanQuyen(LoaiChungTu.XK, QuyenThaoTac.Duyet)]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class CanQuyenAttribute : AuthorizeAttribute
{
    public CanQuyenAttribute(LoaiChungTu loai, QuyenThaoTac quyen)
    {
        Loai = loai;
        Quyen = quyen;
        Policy = ChinhSachQuyen.Ten(loai, quyen);
    }

    public LoaiChungTu Loai { get; }

    public QuyenThaoTac Quyen { get; }
}
