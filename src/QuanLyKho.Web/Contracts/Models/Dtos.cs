namespace QuanLyKho.Web.Contracts;

// ---- Tồn kho và sổ cái -------------------------------------------------------------------

/// <summary>Một dòng thao tác tồn: lô, ô và số lượng (luôn dương; hướng tăng/giảm do tên phương thức quyết định).</summary>
public sealed record DongTon(int MaLo, int MaViTri, int SoLuong);

/// <summary>Chứng từ gây biến động (khớp LoaiCT + MaCT ở BienDongTon; không có khóa ngoại).</summary>
public sealed record ThamChieuChungTu(LoaiChungTu Loai, int MaCT);

/// <summary>Tồn của một lô tại một ô. Tồn khả dụng = SoLuongTon - SoLuongGiu (không lưu, tính ra).</summary>
public sealed record TonKhoDto(int MaLo, int MaViTri, int SoLuongTon, int SoLuongGiu)
{
    public int KhaDung => SoLuongTon - SoLuongGiu;
}

/// <summary>Một dòng sổ cái đã ghi (BienDongTon). SoLuong dương là tăng, âm là giảm.</summary>
public sealed record BienDongTonDto(
    long MaBD, int MaLo, int MaViTri, int SoLuong, LoaiBienDong LoaiBienDong,
    LoaiChungTu LoaiCT, int MaCT, int MaND, DateTime ThoiGianUtc);

/// <summary>Chênh lệch giữa tồn hiện tại (TonKho) và tồn tính lại từ sổ cái.</summary>
public sealed record ChenhLechSoCai(int MaLo, int MaViTri, int TonHienTai, int TonTheoSoCai);

// ---- Sức chứa ----------------------------------------------------------------------------

/// <summary>Kết quả kiểm tra một ô còn nhận thêm được hàng không (thể tích và khối lượng, Bảng 2.3 dòng 3 và 4).</summary>
/// <param name="Du">true nếu nhận thêm đủ số lượng yêu cầu.</param>
/// <param name="SoLuongToiDaThem">Số đơn vị tối đa còn nhận thêm; null nếu ô không giới hạn (ô Tiếp nhận).</param>
public sealed record KetQuaSucChua(
    bool Du, int? SoLuongToiDaThem, decimal TheTichConLaiCm3, decimal KhoiLuongConLaiKg,
    string? LyDo = null, string? MaLoi = null);

// ---- Chứng từ nhập, xuất -----------------------------------------------------------------

/// <summary>Dòng hàng khi nhóm khác nhờ nhóm nhập/xuất tạo phiếu. MaLo, MaViTri bắt buộc với phiếu Thừa/Thiếu do kiểm kê.</summary>
public sealed record DongPhieu(int MaBT, int SoLuong, int? MaLo = null, int? MaViTri = null);

/// <summary>Tham chiếu tới phiếu vừa tạo.</summary>
public sealed record PhieuRef(int MaPhieu, string SoPhieu);

// ---- Hàng hóa ----------------------------------------------------------------------------

public sealed record LoHangDto(int MaLo, string MaVachLo, int MaBT, DateOnly? HanDung);

// ---- Nhật ký -----------------------------------------------------------------------------

public sealed record NhatKyDto(
    long MaNK, int MaND, DateTime ThoiGianUtc, string HanhDong,
    LoaiChungTu? LoaiCT, int? MaCT, string? ChiTiet);

// ---- Gợi ý AI ----------------------------------------------------------------------------

/// <summary>Phiếu gắn với gợi ý: G1 có MaPX; G2 có MaPN; G3 có MaPN và G4 có MaPC (chỉ điền sau khi duyệt).</summary>
public sealed record ThamChieuGoiY(int? MaPX = null, int? MaPN = null, int? MaPC = null);

/// <summary>Một dòng đề xuất ban đầu của AI (ChiTietGoiY). LyDo là lý do kèm số liệu căn cứ, bắt buộc.</summary>
public sealed record ChiTietGoiYDto(
    int MaBT, int SoLuong, string LyDo,
    int? MaLo = null, int? MaViTri = null, int? MaKhoNguon = null, int? MaKhoDich = null);

public sealed record GoiYDto(
    int MaGoiY, LoaiGoiY Loai, TrangThaiGoiY TrangThai, DateTime ThoiGianTaoUtc,
    int? MaNguoiDuyet, DateTime? ThoiGianDuyetUtc, ThamChieuGoiY ThamChieu,
    IReadOnlyList<ChiTietGoiYDto> ChiTiet);

// ---- Người dùng hiện tại -----------------------------------------------------------------

public sealed record NguoiDungHienTai(int MaND, string TenDangNhap, string HoTen, int MaVaiTro, string TenVaiTro);
