namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Một nút trong cây vị trí, kèm đường dẫn đầy đủ từ kho và danh sách con (đã đệ quy).</summary>
public sealed record ViTriCayDto(
    int MaViTri, int MaKho, string TenViTri, string CapViTri, string? LoaiViTri, string? MaVachO,
    decimal? Dai, decimal? Rong, decimal? Cao, decimal? KhoiLuongToiDa,
    bool DangKhoa, string TrangThai, string DuongDanDayDu, IReadOnlyList<ViTriCayDto> Con)
{
    public bool DangHoatDong => TrangThai == Models.TrangThaiHoatDong.HoatDong;

    public bool LaO => CapViTri == Models.CapViTriGiaTri.O;
}

public sealed record ViTriMoi(
    string TenViTri, int MaKho, int? MaViTriCha, string CapViTri,
    string? LoaiViTri = null, string? MaVachO = null,
    decimal? Dai = null, decimal? Rong = null, decimal? Cao = null, decimal? KhoiLuongToiDa = null);

/// <summary>Sửa một vị trí. Không đổi được CapViTri và MaViTriCha (di chuyển cả nhánh ngoài phạm vi việc này, xem README).</summary>
public sealed record ViTriSuaDoi(
    string TenViTri, string? LoaiViTri = null, string? MaVachO = null,
    decimal? Dai = null, decimal? Rong = null, decimal? Cao = null, decimal? KhoiLuongToiDa = null);
