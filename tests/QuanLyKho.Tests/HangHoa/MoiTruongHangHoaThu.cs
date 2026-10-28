using QuanLyKho.Web.Areas.HangHoa.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Tests.HangHoa;

/// <summary>Nhật ký giả: ghi lại mọi lần gọi để kiểm tra việc nào được ghi nhật ký (giống NhatKyGia của T4-3).</summary>
internal sealed class NhatKyGia : IAuditService
{
    public List<(int MaND, string HanhDong, string? ChiTiet)> Dong { get; } = new();

    public Task Ghi(int maND, string hanhDong, LoaiChungTu? loaiCT = null, int? maCT = null, string? chiTiet = null, CancellationToken ct = default)
    {
        lock (Dong) Dong.Add((maND, hanhDong, chiTiet));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NhatKyDto>> LayTheoChungTu(LoaiChungTu loaiCT, int maCT, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<NhatKyDto>>(Array.Empty<NhatKyDto>());
}

/// <summary>
/// Dựng sẵn NhaCungCapService và DanhMucService trên kho dữ liệu trong bộ nhớ (3 nhà cung cấp, 3 danh mục mẫu từ SeedData).
/// </summary>
internal sealed class MoiTruongHangHoaThu
{
    public const int MaNguoiThucHien = 2;   // quanlykho (mẫu)

    public NhatKyGia NhatKy { get; } = new();
    public BoNhoHangHoaStore Store { get; } = new();
    public NhaCungCapService NhaCungCap { get; }
    public DanhMucService DanhMuc { get; }

    public MoiTruongHangHoaThu()
    {
        NhaCungCap = new NhaCungCapService(Store, NhatKy);
        DanhMuc = new DanhMucService(Store, NhatKy);
    }
}
