using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>
/// Ma trận quyền MẶC ĐỊNH (nạp lúc seed, chỉ thêm dòng còn thiếu, không đè dữ liệu đã chỉnh), suy ra từ Bảng 2.2 của báo cáo.
/// Quản trị viên có thể gán lại trên màn hình Phân quyền (Bảng 2.2, ghi chú dưới bảng).
/// <para>GIẢ ĐỊNH (cần nhóm xác nhận): Bảng 2.2 chỉ mô tả nhiệm vụ bằng lời nên các ô dưới đây là cách quy đổi sang năm cờ.
/// Thủ kho có CoDuyet trên NK vì duyệt gợi ý G2 (xếp hàng) thuộc phiếu nhập; Quản lý kho có CoDuyet trên XK, CK, KK vì duyệt phiếu xuất cùng G1, G4 và phát sinh kiểm kê.
/// Nhân viên bán hàng chỉ xem và tạo phiếu xuất.</para>
/// </summary>
public static class QuyenMacDinh
{
    /// <summary>Mỗi chứng từ là chuỗi 5 ký tự theo thứ tự Xem, Thêm, Sửa, Xóa, Duyệt; chữ cái là có quyền, dấu '-' là không.</summary>
    public static IReadOnlyList<PhanQuyen> Cho(int maVaiTro, string tenVaiTro)
    {
        var ma = tenVaiTro switch
        {
            VaiTroTen.QuanTriVien => new[]
            {
                (LoaiChungTu.NK, "XTSXD"), (LoaiChungTu.XK, "XTSXD"), (LoaiChungTu.CK, "XTSXD"),
                (LoaiChungTu.KK, "XTSX-"),   // duyệt phát sinh kiểm kê chỉ gán cho Quản lý kho (Bảng 2.2)
            },
            VaiTroTen.QuanLyKho => new[]
            {
                (LoaiChungTu.NK, "XTSXD"), (LoaiChungTu.XK, "XTSXD"), (LoaiChungTu.CK, "XTSXD"), (LoaiChungTu.KK, "XTSXD"),
            },
            VaiTroTen.ThuKho => new[]
            {
                (LoaiChungTu.NK, "XTSXD"), (LoaiChungTu.XK, "XTS--"), (LoaiChungTu.CK, "X-S--"), (LoaiChungTu.KK, "XTS--"),
            },
            VaiTroTen.NhanVienBanHang => new[]
            {
                (LoaiChungTu.NK, "-----"), (LoaiChungTu.XK, "XT---"), (LoaiChungTu.CK, "-----"), (LoaiChungTu.KK, "-----"),
            },
            _ => Array.Empty<(LoaiChungTu, string)>(),
        };

        return ma.Select(m => new PhanQuyen
        {
            MaVaiTro = maVaiTro,
            LoaiChungTu = m.Item1.ToCsdl(),
            CoXem = m.Item2[0] == 'X',
            CoThem = m.Item2[1] == 'T',
            CoSua = m.Item2[2] == 'S',
            CoXoa = m.Item2[3] == 'X',
            CoDuyet = m.Item2[4] == 'D',
        }).ToList();
    }
}

/// <summary>Ràng buộc của ma trận quyền, kiểm cả ở giao diện lẫn máy chủ.</summary>
public static class QuyTacPhanQuyen
{
    /// <summary>Thứ tự bốn chứng từ trên mọi màn hình.</summary>
    public static readonly IReadOnlyList<LoaiChungTu> ThuTuChungTu = new[] { LoaiChungTu.NK, LoaiChungTu.XK, LoaiChungTu.CK, LoaiChungTu.KK };

    /// <summary>Quyền duyệt phát sinh của phiếu kiểm chỉ gán cho Quản lý kho (Bảng 2.2, ghi chú dưới bảng).</summary>
    public static bool ChoPhepDuyetKiemKe(string tenVaiTro) => tenVaiTro == VaiTroTen.QuanLyKho;

    public static KetQua KiemTra(string tenVaiTro, PhanQuyen p)
    {
        if (!Enum.TryParse<LoaiChungTu>(p.LoaiChungTu, out var loai) || !Enum.IsDefined(loai))
            return KetQua.Loi($"Loại chứng từ '{p.LoaiChungTu}' không hợp lệ.", MaLoiDichVu.DuLieuKhongHopLe);

        if ((p.CoThem || p.CoSua || p.CoXoa || p.CoDuyet) && !p.CoXem)
            return KetQua.Loi($"{Hoa(loai.Ten())}: phải có quyền Xem thì mới gán được Thêm, Sửa, Xóa hoặc Duyệt.", MaLoiDichVu.DuLieuKhongHopLe);

        if (loai == LoaiChungTu.KK && p.CoDuyet && !ChoPhepDuyetKiemKe(tenVaiTro))
            return KetQua.Loi($"Quyền duyệt phát sinh của phiếu kiểm chỉ gán cho {VaiTroTen.QuanLyKho}.", MaLoiDichVu.DuLieuKhongHopLe);

        return KetQua.Ok();
    }

    private static string Hoa(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
