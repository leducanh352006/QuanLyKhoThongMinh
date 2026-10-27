using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Models;

/// <summary>
/// Bảng PhanQuyen (Bảng 2.19): quyền của một vai trò trên một loại chứng từ. Khóa chính (MaVaiTro, LoaiChungTu).
/// <see cref="LoaiChungTu"/> lưu chuỗi 2 ký tự NK, XK, CK, KK (Bảng 2.16).
/// </summary>
public class PhanQuyen
{
    public int MaVaiTro { get; set; }

    public string LoaiChungTu { get; set; } = "";

    public bool CoXem { get; set; }

    public bool CoThem { get; set; }

    public bool CoSua { get; set; }

    public bool CoXoa { get; set; }

    public bool CoDuyet { get; set; }

    public bool Co(QuyenThaoTac quyen) => quyen switch
    {
        QuyenThaoTac.Xem => CoXem,
        QuyenThaoTac.Them => CoThem,
        QuyenThaoTac.Sua => CoSua,
        QuyenThaoTac.Xoa => CoXoa,
        QuyenThaoTac.Duyet => CoDuyet,
        _ => false,
    };

    public PhanQuyen Sao() => (PhanQuyen)MemberwiseClone();
}
