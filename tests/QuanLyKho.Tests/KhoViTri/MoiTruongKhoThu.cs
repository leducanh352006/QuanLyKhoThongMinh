using Microsoft.Extensions.Configuration;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Contracts.Stubs;

namespace QuanLyKho.Tests.KhoViTri;

/// <summary>Nhật ký giả: ghi lại mọi lần gọi để kiểm tra việc nào được ghi nhật ký (giống MoiTruongThu của T4-2).</summary>
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
/// Dựng sẵn KhoService và ViTriService trên kho dữ liệu trong bộ nhớ (2 kho, cây vị trí mẫu từ SeedData) và bản giả IStockService
/// (đọc tồn trực tiếp từ SeedData, không cần SQL Server). Mã mẫu: Kho 1 = "Kho tổng", Kho 2 = "Kho quầy cửa hàng".
/// </summary>
internal sealed class MoiTruongKhoThu
{
    public const int MaNguoiThucHien = 2;   // quanlykho (mẫu)
    public const int MaKho1 = 1, MaKho2 = 2;

    public NhatKyGia NhatKy { get; } = new();
    public BoNhoKhoViTriStore Store { get; } = new();
    public StockServiceStub Stock { get; } = new();
    public KhoService Kho { get; }
    public ViTriService ViTri { get; }

    public MoiTruongKhoThu()
    {
        var heThongCauHinh = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:SeedPassword"] = "Demo@12345" }).Build();
        var matKhau = new MatKhauService();
        var nguoiDungStore = new BoNhoHeThongStore(heThongCauHinh, matKhau, Microsoft.Extensions.Logging.Abstractions.NullLogger<BoNhoHeThongStore>.Instance);

        Kho = new KhoService(Store, nguoiDungStore, NhatKy);
        ViTri = new ViTriService(Store, Stock, NhatKy);
    }
}
