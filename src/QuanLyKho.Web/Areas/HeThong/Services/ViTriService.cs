using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>
/// Cây vị trí nhiều cấp trong một kho: thêm, sửa, khóa (xóa mềm) và xóa cứng khi chưa có dữ liệu tồn (màn hình Vị trí).
/// Ràng buộc theo Bảng 2.17 dòng 1 (hàng chỉ nằm ở ô, cấp con thấp hơn cấp cha) và mục 2.8.1 (vị trí nhiều cấp, kích thước, khối lượng).
/// </summary>
public interface IViTriQuanLy
{
    /// <summary>Cây vị trí của một kho (các nút gốc, mỗi nút kèm toàn bộ cây con), đường dẫn đầy đủ bắt đầu từ tên kho.</summary>
    Task<KetQua<IReadOnlyList<ViTriCayDto>>> LayCay(int maKho, CancellationToken ct = default);

    Task<ViTriCayDto?> LayMot(int maViTri, CancellationToken ct = default);

    /// <summary>Danh sách (MaViTri, Đường dẫn) các vị trí có thể làm cấp cha trong một kho (loại trừ chính ô, vì ô không thể có con).</summary>
    Task<IReadOnlyList<(int MaViTri, string DuongDan)>> LayUngVienLamCha(int maKho, CancellationToken ct = default);

    Task<KetQua> Them(ViTriMoi moi, int maNguoiThucHien, CancellationToken ct = default);

    Task<KetQua> Sua(int maViTri, ViTriSuaDoi doi, int maNguoiThucHien, CancellationToken ct = default);

    /// <summary>
    /// Khóa (xóa mềm) hoặc mở lại. Khóa bị từ chối nếu còn vị trí con đang hoạt động, đây là khu tiếp nhận cuối cùng của kho,
    /// hoặc ô còn tồn (hàng nằm trong ô ngừng hoạt động sẽ không xuất, chuyển được). Mở lại bị từ chối nếu cấp cha đang ngừng hoạt động.
    /// </summary>
    Task<KetQua> DatTrangThai(int maViTri, bool hoatDong, int maNguoiThucHien, CancellationToken ct = default);

    /// <summary>
    /// Xóa cứng. Chỉ cho phép khi không còn vị trí con (mọi trạng thái), nếu là ô thì không còn tồn hiện tại (qua IStockService),
    /// và không có dữ liệu nào tham chiếu tới (sổ cái BienDongTon, dòng tồn, phiếu, gợi ý: khóa ngoại của CSDL chặn và báo lỗi rõ).
    /// </summary>
    Task<KetQua> Xoa(int maViTri, int maNguoiThucHien, CancellationToken ct = default);
}

public sealed class ViTriService(IKhoViTriStore store, IStockService stock, IAuditService audit) : IViTriQuanLy
{
    private const string PhanCach = " ▷ ";

    public async Task<KetQua<IReadOnlyList<ViTriCayDto>>> LayCay(int maKho, CancellationToken ct = default)
    {
        var kho = await store.TimKho(maKho, ct);
        if (kho is null) return KetQua<IReadOnlyList<ViTriCayDto>>.Loi("Không tìm thấy kho.", MaLoiDichVu.KhongTimThay);

        var tatCa = await store.LayViTriTheoKho(maKho, ct);
        var goc = Dung(tatCa, null, kho.TenKho);
        return KetQua<IReadOnlyList<ViTriCayDto>>.Ok(goc);
    }

    public async Task<ViTriCayDto?> LayMot(int maViTri, CancellationToken ct = default)
    {
        var v = await store.TimViTri(maViTri, ct);
        if (v is null) return null;
        var kho = await store.TimKho(v.MaKho, ct);
        var tatCa = await store.LayViTriTheoKho(v.MaKho, ct);
        return DungMot(v, tatCa, DuongDanCuaCha(v.MaViTriCha, tatCa, kho?.TenKho ?? "?"));
    }

    public async Task<IReadOnlyList<(int MaViTri, string DuongDan)>> LayUngVienLamCha(int maKho, CancellationToken ct = default)
    {
        var kho = await store.TimKho(maKho, ct);
        var tatCa = await store.LayViTriTheoKho(maKho, ct);
        return Phang(tatCa, null, kho?.TenKho ?? "?")
            .Where(x => x.v.CapViTri != CapViTriGiaTri.O)   // ô là cấp thấp nhất, không thể làm cha
            .Select(x => (x.v.MaViTri, x.duongDan))
            .ToList();
    }

    public async Task<KetQua> Them(ViTriMoi moi, int maNguoiThucHien, CancellationToken ct = default)
    {
        var ten = moi.TenViTri.Trim();
        var loi = KiemTraTen(ten) ?? KiemTraCapViTri(moi.CapViTri);
        if (loi is not null) return loi;

        var kho = await store.TimKho(moi.MaKho, ct);
        if (kho is null) return KetQua.Loi("Không tìm thấy kho.", MaLoiDichVu.KhongTimThay);
        if (!kho.DangHoatDong) return KetQua.Loi($"Kho {kho.TenKho} đang ngừng hoạt động; mở lại kho trước khi thêm vị trí.", MaLoiDichVu.DuLieuKhongHopLe);

        ViTri? cha = null;
        if (moi.MaViTriCha is int maCha)
        {
            cha = await store.TimViTri(maCha, ct);
            if (cha is null) return KetQua.Loi("Không tìm thấy vị trí cấp trên.", MaLoiDichVu.KhongTimThay);
            if (cha.MaKho != moi.MaKho) return KetQua.Loi("Vị trí cấp trên không thuộc kho này.", MaLoiDichVu.DuLieuKhongHopLe);
            if (cha.CapViTri == CapViTriGiaTri.O) return KetQua.Loi("Ô là cấp thấp nhất, không thể có vị trí con.", MaLoiDichVu.DuLieuKhongHopLe);
            if (!cha.DangHoatDong) return KetQua.Loi($"'{cha.TenViTri}' đang ngừng hoạt động; mở lại trước khi thêm vị trí con.", MaLoiDichVu.DuLieuKhongHopLe);
            if (CapViTriGiaTri.ChiSo(moi.CapViTri) <= CapViTriGiaTri.ChiSo(cha.CapViTri))
                return KetQua.Loi($"Cấp '{moi.CapViTri}' phải thấp hơn cấp cha '{cha.CapViTri}'.", MaLoiDichVu.DuLieuKhongHopLe);
        }

        var loiO = KiemTraTruongCuaO(moi.CapViTri, moi.LoaiViTri, moi.MaVachO, moi.Dai, moi.Rong, moi.Cao, moi.KhoiLuongToiDa);
        if (loiO is not null) return loiO;

        ViTri them;
        try
        {
            them = await store.ThemViTri(new ViTri
            {
                TenViTri = ten, MaKho = moi.MaKho, MaViTriCha = moi.MaViTriCha, CapViTri = moi.CapViTri,
                LoaiViTri = moi.LoaiViTri, MaVachO = Chuan(moi.MaVachO), Dai = moi.Dai, Rong = moi.Rong, Cao = moi.Cao,
                KhoiLuongToiDa = moi.KhoiLuongToiDa, DangKhoa = false, TrangThai = TrangThaiHoatDong.HoatDong,
            }, ct);
        }
        catch (XungDotDuLieuException ex)
        {
            return KetQua.Loi(ex.Message, MaLoiDichVu.DuLieuKhongHopLe);
        }

        await audit.Ghi(maNguoiThucHien, "Thêm vị trí", chiTiet: $"{ten} ({moi.CapViTri}), kho {kho.TenKho}", ct: ct);
        return KetQua.Ok($"Đã thêm {moi.CapViTri.ToLowerInvariant()} '{ten}'.");
    }

    public async Task<KetQua> Sua(int maViTri, ViTriSuaDoi doi, int maNguoiThucHien, CancellationToken ct = default)
    {
        var v = await store.TimViTri(maViTri, ct);
        if (v is null) return KetQua.Loi("Không tìm thấy vị trí.", MaLoiDichVu.KhongTimThay);

        var ten = doi.TenViTri.Trim();
        var loi = KiemTraTen(ten);
        if (loi is not null) return loi;

        var loiO = KiemTraTruongCuaO(v.CapViTri, doi.LoaiViTri, doi.MaVachO, doi.Dai, doi.Rong, doi.Cao, doi.KhoiLuongToiDa);
        if (loiO is not null) return loiO;

        // Đổi khu tiếp nhận hoạt động cuối cùng sang Lưu trữ thì kho mất khu tiếp nhận (Bảng 2.17, tiêu chí T4-3).
        if (v.LaO && v.DangHoatDong && v.LoaiViTri == LoaiViTriGiaTri.TiepNhan && doi.LoaiViTri != LoaiViTriGiaTri.TiepNhan
            && await store.DemOTiepNhanHoatDong(v.MaKho, maViTri, ct) == 0)
            return KetQua.Loi("Đây là khu tiếp nhận hoạt động cuối cùng của kho; mỗi kho phải có ít nhất một khu tiếp nhận.", MaLoiDichVu.DuLieuKhongHopLe);

        if (!string.Equals(ten, v.TenViTri, StringComparison.OrdinalIgnoreCase))
        {
            var anhEm = await store.LayViTriTheoKho(v.MaKho, ct);
            if (anhEm.Any(x => x.MaViTri != maViTri && x.MaViTriCha == v.MaViTriCha && string.Equals(x.TenViTri, ten, StringComparison.OrdinalIgnoreCase)))
                return KetQua.Loi("Tên vị trí đã tồn tại trong cùng cấp cha.", MaLoiDichVu.DuLieuKhongHopLe);
        }

        v.TenViTri = ten;
        v.LoaiViTri = doi.LoaiViTri;
        v.MaVachO = Chuan(doi.MaVachO);
        v.Dai = doi.Dai;
        v.Rong = doi.Rong;
        v.Cao = doi.Cao;
        v.KhoiLuongToiDa = doi.KhoiLuongToiDa;
        try
        {
            await store.LuuThayDoi(ct);
        }
        catch (XungDotDuLieuException ex)
        {
            return KetQua.Loi(ex.Message, MaLoiDichVu.DuLieuKhongHopLe);
        }

        await audit.Ghi(maNguoiThucHien, "Sửa vị trí", chiTiet: ten, ct: ct);
        return KetQua.Ok("Đã lưu thay đổi.");
    }

    public async Task<KetQua> DatTrangThai(int maViTri, bool hoatDong, int maNguoiThucHien, CancellationToken ct = default)
    {
        var v = await store.TimViTri(maViTri, ct);
        if (v is null) return KetQua.Loi("Không tìm thấy vị trí.", MaLoiDichVu.KhongTimThay);
        if (v.DangHoatDong == hoatDong) return KetQua.Ok(hoatDong ? "Vị trí đang hoạt động." : "Vị trí đã ngừng hoạt động từ trước.");

        if (!hoatDong)
        {
            if (await store.DemConHoatDong(maViTri, ct) > 0)
                return KetQua.Loi("Còn vị trí con đang hoạt động; hãy khóa các vị trí con trước.", MaLoiDichVu.DuLieuKhongHopLe);

            if (v.CapViTri == CapViTriGiaTri.O && v.LoaiViTri == LoaiViTriGiaTri.TiepNhan
                && await store.DemOTiepNhanHoatDong(v.MaKho, maViTri, ct) == 0)
                return KetQua.Loi("Đây là khu tiếp nhận hoạt động cuối cùng của kho; mỗi kho phải có ít nhất một khu tiếp nhận.", MaLoiDichVu.DuLieuKhongHopLe);

            if (v.LaO && (await stock.LayTonTheoViTri(maViTri, ct)).Sum(t => t.SoLuongTon) is var con and > 0)
                return KetQua.Loi($"Ô '{v.TenViTri}' còn {con} hàng; chuyển hết hàng sang ô khác trước khi khóa.", MaLoiDichVu.DuLieuKhongHopLe);
        }
        else if (v.MaViTriCha is int maCha && await store.TimViTri(maCha, ct) is { DangHoatDong: false } cha)
        {
            return KetQua.Loi($"Cấp cha '{cha.TenViTri}' đang ngừng hoạt động; mở lại cấp cha trước.", MaLoiDichVu.DuLieuKhongHopLe);
        }

        v.TrangThai = hoatDong ? TrangThaiHoatDong.HoatDong : TrangThaiHoatDong.NgungHoatDong;
        await store.LuuThayDoi(ct);

        await audit.Ghi(maNguoiThucHien, hoatDong ? "Mở lại vị trí" : "Khóa vị trí", chiTiet: v.TenViTri, ct: ct);
        return KetQua.Ok(hoatDong ? $"Đã mở lại '{v.TenViTri}'." : $"Đã khóa '{v.TenViTri}'.");
    }

    public async Task<KetQua> Xoa(int maViTri, int maNguoiThucHien, CancellationToken ct = default)
    {
        var v = await store.TimViTri(maViTri, ct);
        if (v is null) return KetQua.Loi("Không tìm thấy vị trí.", MaLoiDichVu.KhongTimThay);

        var conTatCa = (await store.LayViTriTheoKho(v.MaKho, ct)).Count(x => x.MaViTriCha == maViTri);
        if (conTatCa > 0)
            return KetQua.Loi("Còn vị trí con bên dưới; hãy xóa hoặc khóa các vị trí con trước.", MaLoiDichVu.DuLieuKhongHopLe);

        if (v.LaO && (await stock.LayTonTheoViTri(maViTri, ct)).Count > 0)
            return KetQua.Loi("Ô còn tồn kho ghi nhận, không thể xóa cứng; hãy khóa (xóa mềm) thay vào đó.", MaLoiDichVu.DuLieuKhongHopLe);

        var ten = v.TenViTri;
        try
        {
            await store.XoaViTri(v, ct);
        }
        catch (XungDotDuLieuException ex)
        {
            return KetQua.Loi(ex.Message, MaLoiDichVu.DuLieuKhongHopLe);
        }
        await audit.Ghi(maNguoiThucHien, "Xóa vị trí", chiTiet: ten, ct: ct);
        return KetQua.Ok($"Đã xóa '{ten}'.");
    }

    // ---- kiểm tra đầu vào ----

    private static KetQua? KiemTraTen(string ten)
        => string.IsNullOrWhiteSpace(ten) || ten.Length > 50
            ? KetQua.Loi("Tên vị trí không được để trống và tối đa 50 ký tự.", MaLoiDichVu.DuLieuKhongHopLe) : null;

    private static KetQua? KiemTraCapViTri(string cap)
        => CapViTriGiaTri.ChiSo(cap) < 0
            ? KetQua.Loi($"Cấp vị trí phải là một trong: {string.Join(", ", CapViTriGiaTri.ThuTu)}.", MaLoiDichVu.DuLieuKhongHopLe) : null;

    private static KetQua? KiemTraTruongCuaO(
        string capViTri, string? loaiViTri, string? maVachO, decimal? dai, decimal? rong, decimal? cao, decimal? khoiLuongToiDa)
    {
        if (capViTri != CapViTriGiaTri.O)
        {
            return loaiViTri is not null || maVachO is not null || dai is not null || rong is not null || cao is not null || khoiLuongToiDa is not null
                ? KetQua.Loi("Chỉ ô mới có loại, mã vạch, kích thước và khối lượng tối đa; các cấp khác để trống.", MaLoiDichVu.DuLieuKhongHopLe)
                : null;
        }

        if (loaiViTri is null || !LoaiViTriGiaTri.TatCa.Contains(loaiViTri))
            return KetQua.Loi($"Ô phải chọn loại {string.Join(" hoặc ", LoaiViTriGiaTri.TatCa)}.", MaLoiDichVu.DuLieuKhongHopLe);
        if (string.IsNullOrWhiteSpace(maVachO) || maVachO.Trim().Length > 50)
            return KetQua.Loi("Ô phải có mã vạch, tối đa 50 ký tự.", MaLoiDichVu.DuLieuKhongHopLe);

        if (loaiViTri == LoaiViTriGiaTri.TiepNhan)
            return dai is not null || rong is not null || cao is not null || khoiLuongToiDa is not null
                ? KetQua.Loi("Ô Tiếp nhận không giới hạn kích thước; để trống dài, rộng, cao, khối lượng tối đa.", MaLoiDichVu.DuLieuKhongHopLe)
                : null;

        // Lưu trữ: bắt buộc đủ bốn kích thước, mỗi kích thước dương.
        if (dai is not > 0 || rong is not > 0 || cao is not > 0 || khoiLuongToiDa is not > 0)
            return KetQua.Loi("Ô Lưu trữ phải có đủ dài, rộng, cao và khối lượng tối đa, mỗi giá trị lớn hơn 0.", MaLoiDichVu.DuLieuKhongHopLe);
        return null;
    }

    private static string? Chuan(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    // ---- dựng cây và đường dẫn ----

    private static IReadOnlyList<ViTriCayDto> Dung(IReadOnlyList<ViTri> tatCa, int? maCha, string duongDanCha)
        => tatCa.Where(v => v.MaViTriCha == maCha).OrderBy(v => v.TenViTri)
            .Select(v => DungMot(v, tatCa, duongDanCha)).ToList();

    private static ViTriCayDto DungMot(ViTri v, IReadOnlyList<ViTri> tatCa, string duongDanCha)
    {
        var duongDan = duongDanCha + PhanCach + v.TenViTri;
        var con = Dung(tatCa, v.MaViTri, duongDan);
        return new ViTriCayDto(v.MaViTri, v.MaKho, v.TenViTri, v.CapViTri, v.LoaiViTri, v.MaVachO, v.Dai, v.Rong, v.Cao,
            v.KhoiLuongToiDa, v.DangKhoa, v.TrangThai, duongDan, con);
    }

    private static string DuongDanCuaCha(int? maCha, IReadOnlyList<ViTri> tatCa, string tenKho)
    {
        if (maCha is null) return tenKho;
        var cha = tatCa.FirstOrDefault(x => x.MaViTri == maCha);
        return cha is null ? tenKho : DuongDanCuaCha(cha.MaViTriCha, tatCa, tenKho) + PhanCach + cha.TenViTri;
    }

    private static IEnumerable<(ViTri v, string duongDan)> Phang(IReadOnlyList<ViTri> tatCa, int? maCha, string duongDanCha)
    {
        foreach (var v in tatCa.Where(x => x.MaViTriCha == maCha).OrderBy(x => x.TenViTri))
        {
            var duongDan = duongDanCha + PhanCach + v.TenViTri;
            yield return (v, duongDan);
            foreach (var con in Phang(tatCa, v.MaViTri, duongDan)) yield return con;
        }
    }
}
