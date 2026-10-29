namespace QuanLyKho.Web.Contracts;

/// <summary>Mã lỗi dùng chung để nơi gọi xử lý theo mã, không phải so chuỗi thông báo.</summary>
public static class MaLoiDichVu
{
    public const string VuotTon = "VUOT_TON";
    public const string VuotSucChua = "VUOT_SUC_CHUA";
    public const string OBiKhoa = "O_BI_KHOA";
    public const string KhongTimThay = "KHONG_TIM_THAY";
    public const string DuLieuKhongHopLe = "DU_LIEU_KHONG_HOP_LE";
    public const string SaiTrangThai = "SAI_TRANG_THAI";
    public const string ChuaCoXuLy = "CHUA_CO_XU_LY";
    public const string AiKhongDung = "AI_KHONG_DUNG";
}

/// <summary>Kết quả của một thao tác nghiệp vụ không trả dữ liệu. Thông báo viết tiếng Việt, hiển thị thẳng cho người dùng.</summary>
public sealed record KetQua(bool ThanhCong, string? ThongBao = null, string? MaLoi = null)
{
    public static KetQua Ok(string? thongBao = null) => new(true, thongBao);
    public static KetQua Loi(string thongBao, string? maLoi = null) => new(false, thongBao, maLoi);
}

/// <summary>Kết quả của một thao tác nghiệp vụ có trả dữ liệu.</summary>
public sealed record KetQua<T>(bool ThanhCong, T? GiaTri, string? ThongBao = null, string? MaLoi = null)
{
    public static KetQua<T> Ok(T giaTri, string? thongBao = null) => new(true, giaTri, thongBao);
    public static KetQua<T> Loi(string thongBao, string? maLoi = null) => new(false, default, thongBao, maLoi);
}
