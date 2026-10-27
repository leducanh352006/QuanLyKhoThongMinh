using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>CRUD Kho (màn hình Danh sách kho). Thêm kho tự tạo kèm một ô Khu tiếp nhận để mọi kho luôn có khu tiếp nhận.</summary>
public interface IKhoQuanLy
{
    Task<IReadOnlyList<KhoDong>> LayDanhSach(CancellationToken ct = default);

    Task<KhoDong?> Lay(int maKho, CancellationToken ct = default);

    /// <summary>Danh sách người để chọn làm quản lý kho trên form.</summary>
    Task<IReadOnlyList<NguoiQuanLyDto>> LayNguoiCoTheQuanLy(CancellationToken ct = default);

    Task<KetQua> Them(KhoMoi moi, int maNguoiThucHien, CancellationToken ct = default);

    Task<KetQua> Sua(int maKho, KhoSuaDoi doi, int maNguoiThucHien, CancellationToken ct = default);

    Task<KetQua> DatTrangThai(int maKho, bool hoatDong, int maNguoiThucHien, CancellationToken ct = default);
}

public sealed class KhoService(IKhoViTriStore store, IHeThongStore nguoiDungStore, IAuditService audit) : IKhoQuanLy
{
    public async Task<IReadOnlyList<KhoDong>> LayDanhSach(CancellationToken ct = default)
    {
        var nguoiDung = await nguoiDungStore.LayNguoiDung(ct);
        return (await store.LayKho(ct)).Select(k => ChoHienThi(k, nguoiDung)).ToList();
    }

    public async Task<KhoDong?> Lay(int maKho, CancellationToken ct = default)
    {
        var kho = await store.TimKho(maKho, ct);
        return kho is null ? null : ChoHienThi(kho, await nguoiDungStore.LayNguoiDung(ct));
    }

    public async Task<IReadOnlyList<NguoiQuanLyDto>> LayNguoiCoTheQuanLy(CancellationToken ct = default)
        => (await nguoiDungStore.LayNguoiDung(ct)).Where(n => n.DangHoatDong).Select(n => new NguoiQuanLyDto(n.MaND, n.HoTen)).ToList();

    public async Task<KetQua> Them(KhoMoi moi, int maNguoiThucHien, CancellationToken ct = default)
    {
        var ten = moi.TenKho.Trim();
        var loi = KiemTraTen(ten) ?? KiemTraLoaiKho(moi.LoaiKho) ?? KiemTraDiaChi(moi.DiaChi) ?? KiemTraThoiGianCho(moi.ThoiGianCho);
        if (loi is not null) return loi;

        if (await nguoiDungStore.TimNguoiDung(moi.MaQuanLy, ct) is not { DangHoatDong: true })
            return KetQua.Loi("Người quản lý kho không tồn tại hoặc đã bị khóa.", MaLoiDichVu.DuLieuKhongHopLe);

        Kho kho;
        try
        {
            kho = await store.ThemKho(new Kho
            {
                TenKho = ten, LoaiKho = moi.LoaiKho, DiaChi = Chuan(moi.DiaChi),
                MaQuanLy = moi.MaQuanLy, ThoiGianCho = moi.ThoiGianCho, TrangThai = TrangThaiHoatDong.HoatDong,
            }, ct);
        }
        catch (XungDotDuLieuException)
        {
            return KetQua.Loi("Tên kho đã tồn tại.", MaLoiDichVu.DuLieuKhongHopLe);
        }

        // Mọi kho phải có khu tiếp nhận (tiêu chí hoàn thành việc T4-3); tạo kèm ngay khi thêm kho để không ai quên.
        try
        {
            await store.ThemViTri(new ViTri
            {
                TenViTri = "Khu tiếp nhận", MaKho = kho.MaKho, MaViTriCha = null, CapViTri = CapViTriGiaTri.O,
                LoaiViTri = LoaiViTriGiaTri.TiepNhan, MaVachO = $"O-TN-{kho.MaKho}", TrangThai = TrangThaiHoatDong.HoatDong,
            }, ct);
        }
        catch (XungDotDuLieuException ex)
        {
            // Cực hiếm (trùng mã vạch "O-TN-<mã>"); báo rõ để quản trị viên vào màn Vị trí tự thêm khu tiếp nhận.
            await audit.Ghi(maNguoiThucHien, "Thêm kho", chiTiet: ten, ct: ct);
            return KetQua.Ok($"Đã thêm kho {ten}, nhưng chưa tạo được khu tiếp nhận tự động ({ex.Message}). Vào Vị trí để thêm thủ công.");
        }

        await audit.Ghi(maNguoiThucHien, "Thêm kho", chiTiet: ten, ct: ct);
        return KetQua.Ok($"Đã thêm kho {ten} kèm khu tiếp nhận.");
    }

    public async Task<KetQua> Sua(int maKho, KhoSuaDoi doi, int maNguoiThucHien, CancellationToken ct = default)
    {
        var kho = await store.TimKho(maKho, ct);
        if (kho is null) return KetQua.Loi("Không tìm thấy kho.", MaLoiDichVu.KhongTimThay);

        var ten = doi.TenKho.Trim();
        var loi = KiemTraTen(ten) ?? KiemTraLoaiKho(doi.LoaiKho) ?? KiemTraDiaChi(doi.DiaChi) ?? KiemTraThoiGianCho(doi.ThoiGianCho);
        if (loi is not null) return loi;

        if (!string.Equals(ten, kho.TenKho, StringComparison.OrdinalIgnoreCase)
            && (await store.LayKho(ct)).Any(k => k.MaKho != maKho && string.Equals(k.TenKho, ten, StringComparison.OrdinalIgnoreCase)))
            return KetQua.Loi("Tên kho đã tồn tại.", MaLoiDichVu.DuLieuKhongHopLe);

        if (await nguoiDungStore.TimNguoiDung(doi.MaQuanLy, ct) is not { DangHoatDong: true })
            return KetQua.Loi("Người quản lý kho không tồn tại hoặc đã bị khóa.", MaLoiDichVu.DuLieuKhongHopLe);

        kho.TenKho = ten;
        kho.LoaiKho = doi.LoaiKho;
        kho.DiaChi = Chuan(doi.DiaChi);
        kho.MaQuanLy = doi.MaQuanLy;
        kho.ThoiGianCho = doi.ThoiGianCho;
        await store.LuuThayDoi(ct);

        await audit.Ghi(maNguoiThucHien, "Sửa kho", chiTiet: ten, ct: ct);
        return KetQua.Ok("Đã lưu thay đổi.");
    }

    public async Task<KetQua> DatTrangThai(int maKho, bool hoatDong, int maNguoiThucHien, CancellationToken ct = default)
    {
        var kho = await store.TimKho(maKho, ct);
        if (kho is null) return KetQua.Loi("Không tìm thấy kho.", MaLoiDichVu.KhongTimThay);
        if (kho.DangHoatDong == hoatDong) return KetQua.Ok(hoatDong ? "Kho đang hoạt động." : "Kho đã ngừng hoạt động từ trước.");

        kho.TrangThai = hoatDong ? TrangThaiHoatDong.HoatDong : TrangThaiHoatDong.NgungHoatDong;
        await store.LuuThayDoi(ct);

        await audit.Ghi(maNguoiThucHien, hoatDong ? "Mở lại kho" : "Ngừng hoạt động kho", chiTiet: kho.TenKho, ct: ct);
        return KetQua.Ok(hoatDong ? $"Đã mở lại kho {kho.TenKho}." : $"Đã ngừng hoạt động kho {kho.TenKho}.");
    }

    private static KetQua? KiemTraTen(string ten)
        => string.IsNullOrWhiteSpace(ten) || ten.Length > 100
            ? KetQua.Loi("Tên kho không được để trống và tối đa 100 ký tự.", MaLoiDichVu.DuLieuKhongHopLe) : null;

    private static KetQua? KiemTraLoaiKho(string loai)
        => LoaiKhoGiaTri.TatCa.Contains(loai) ? null
            : KetQua.Loi($"Loại kho phải là {string.Join(" hoặc ", LoaiKhoGiaTri.TatCa)}.", MaLoiDichVu.DuLieuKhongHopLe);

    private static KetQua? KiemTraDiaChi(string? diaChi)
        => diaChi is { Length: > 255 } ? KetQua.Loi("Địa chỉ tối đa 255 ký tự.", MaLoiDichVu.DuLieuKhongHopLe) : null;

    private static KetQua? KiemTraThoiGianCho(int thoiGianCho)
        => thoiGianCho < 0 ? KetQua.Loi("Thời gian chờ bổ sung hàng không được âm.", MaLoiDichVu.DuLieuKhongHopLe) : null;

    private static string? Chuan(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static KhoDong ChoHienThi(Kho k, IReadOnlyList<NguoiDung> nguoiDung) => new(
        k.MaKho, k.TenKho, k.LoaiKho, k.DiaChi, k.MaQuanLy,
        nguoiDung.FirstOrDefault(n => n.MaND == k.MaQuanLy)?.HoTen ?? "?", k.ThoiGianCho, k.TrangThai);
}
