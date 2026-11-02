using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Một thao tác do handler mẫu "tạo" sau khi duyệt (chỉ để demo; thao tác thật do nhóm sở hữu viết).</summary>
public sealed record ThaoTacMau(int MaGoiY, LoaiGoiY Loai, string MoTa, IReadOnlyList<ChiTietGoiYDto> Dong, int MaNguoiDuyet, DateTime ThoiGianUtc);

/// <summary>Nơi giữ thao tác mẫu trong bộ nhớ (singleton). Khóa theo mã gợi ý: gọi lại sau một lần hoàn tác thì ghi đè, không nhân đôi.</summary>
public sealed class ThaoTacMauStore
{
    private readonly object _khoa = new();
    private readonly Dictionary<int, ThaoTacMau> _thaoTac = new();

    public void Ghi(ThaoTacMau t) { lock (_khoa) _thaoTac[t.MaGoiY] = t; }

    public IReadOnlyList<ThaoTacMau> TatCa() { lock (_khoa) return _thaoTac.Values.OrderByDescending(t => t.MaGoiY).ToList(); }
}

/// <summary>
/// Handler mẫu cho một loại gợi ý: chỉ bật ở Development và chỉ dùng khi nhóm sở hữu chưa đăng ký handler thật (xem <see cref="GoiYService"/>).
/// Mô tả thao tác đúng như báo cáo (Bảng vòng đời gợi ý, bước 6) và dùng đúng các dòng đã duyệt (dòng sửa nếu có). Không chạm tồn kho.
/// </summary>
public sealed class GoiYHandlerMau(LoaiGoiY loai, ThaoTacMauStore noiGhi, IHostEnvironment moiTruong, TimeProvider gio) : IGoiYHandlerMau
{
    public LoaiGoiY Loai => loai;

    public bool DangBat => moiTruong.IsDevelopment();

    public Task<KetQua<ThamChieuGoiY>> ThucHien(GoiYDto goiY, int maNguoiDuyet, CancellationToken ct = default)
    {
        if (!DangBat) return Task.FromResult(KetQua<ThamChieuGoiY>.Loi("Handler mẫu chỉ dùng ở môi trường thử.", MaLoiDichVu.ChuaCoXuLy));
        var tong = goiY.ChiTiet.Sum(c => c.SoLuong);
        var moTa = goiY.Loai switch
        {
            LoaiGoiY.G1 => $"Điền lệnh lấy hàng cho phiếu xuất #{goiY.ThamChieu.MaPX}: {goiY.ChiTiet.Count} dòng (lô, ô), tổng {tong}; thủ kho quét mã khi lấy.",
            LoaiGoiY.G2 => $"Tạo lệnh xếp hàng cho phiếu nhập #{goiY.ThamChieu.MaPN}: {goiY.ChiTiet.Count} dòng, tổng {tong}; thủ kho quét mã ô khi xếp.",
            LoaiGoiY.G3 => $"Tạo đơn nhập bổ sung (mẫu): {goiY.ChiTiet.Count} dòng, tổng {tong}.",
            LoaiGoiY.G4 => $"Tạo phiếu chuyển và lệnh lấy hàng ở kho đi (mẫu): {goiY.ChiTiet.Count} dòng, tổng {tong}.",
            _ => "Thao tác mẫu.",
        };
        noiGhi.Ghi(new ThaoTacMau(goiY.MaGoiY, goiY.Loai, moTa, goiY.ChiTiet, maNguoiDuyet, gio.GetUtcNow().UtcDateTime));
        return Task.FromResult(KetQua<ThamChieuGoiY>.Ok(goiY.ThamChieu));   // mẫu không tạo phiếu thật nên giữ nguyên tham chiếu
    }
}

/// <summary>
/// Bản gợi ý giả để các nhóm thử khung (chỉ Development): tạo G1, G3, G4 (và G2 nếu có phiếu nhập) từ dữ liệu mẫu chung, đi qua đường lui
/// <see cref="GoiYAnToan"/> như AI thật. <see cref="TaoMauAiLoi"/> giả lập AI lỗi để thấy luồng làm tay vẫn chạy.
/// </summary>
public sealed class GoiYMauService(IGoiYService goiY, KhoDbContext? db = null)
{
    public async Task<KetQua> TaoMau(int? maPN, CancellationToken ct = default)
    {
        // Lô mẫu có thể chưa nạp vào CSDL (Database:SeedOnStartup tắt): khi đó để trống MaLo thay vì báo lỗi khóa ngoại.
        int? loKem = db is null || await db.LoHang.AnyAsync(l => l.MaLo == 3, ct) ? 3 : null;

        var viec = new List<(LoaiGoiY Loai, ThamChieuGoiY TC, ChiTietGoiYDto[] Dong)>
        {
            (LoaiGoiY.G1, new ThamChieuGoiY(MaPX: 1), new[]
            {
                new ChiTietGoiYDto(3, 10, "Hết hạn trước lấy trước: lô LO-2609-001 hạn 31/03/2027, sớm hơn lô LO-2610-003 (30/09/2027); ô A1-01 còn 24, lấy đủ một lần.", MaLo: loKem, MaViTri: 3),
            }),
            (LoaiGoiY.G3, new ThamChieuGoiY(), new[]
            {
                new ChiTietGoiYDto(4, 42, "Kho quầy còn A = 5; d = 1,5/ngày, L = 1, m = 10 nên R = 11,5 ≥ A. Nhập d × (L + 30) − A = 41,5, làm tròn 42.", MaKhoDich: 2),
            }),
            (LoaiGoiY.G4, new ThamChieuGoiY(), new[]
            {
                new ChiTietGoiYDto(4, 7, "d = 1,5/ngày. Kho tổng A = 60, đủ bán 40 ngày > T = 30, dư 60 − 1,5 × 30 = 15. Kho quầy A = 5, đủ bán 3,3 ngày < L + N = 8, cần bù 1,5 × 8 − 5 = 7. Chuyển min(15, 7) = 7.", MaKhoNguon: 1, MaKhoDich: 2),
            }),
        };
        if (maPN is int pn)
            viec.Add((LoaiGoiY.G2, new ThamChieuGoiY(MaPN: pn), new[]
            {
                new ChiTietGoiYDto(1, 10, "Điểm S = 0,82: gần cửa xuất (0,9), cạnh hàng cùng biến thể (1,0), độ lấp đầy phù hợp (0,4). Áo M thuộc nhóm A.", MaViTri: 4),
            }));

        var daTao = new List<string>();
        var loi = new List<string>();
        foreach (var (loai, tc, dong) in viec)
        {
            var kq = await GoiYAnToan.Chay(c => goiY.Tao(loai, tc, dong, c), ct: ct);
            if (kq.ThanhCong) daTao.Add($"{loai} #{kq.GiaTri}");
            else loi.Add($"{loai}: {kq.ThongBao}");
        }

        var thongBao = daTao.Count > 0 ? $"Đã tạo gợi ý mẫu: {string.Join(", ", daTao)}." : "Không tạo được gợi ý mẫu nào.";
        if (loi.Count > 0) thongBao += " Lỗi: " + string.Join("; ", loi);
        return daTao.Count > 0 ? KetQua.Ok(thongBao) : KetQua.Loi(thongBao, MaLoiDichVu.DuLieuKhongHopLe);
    }

    /// <summary>Giả lập thuật toán AI ném lỗi: không có gợi ý nào được tạo, nhận thông báo "làm tay"; mọi chức năng khác vẫn dùng bình thường.</summary>
    public async Task<KetQua> TaoMauAiLoi(CancellationToken ct = default)
    {
        var kq = await GoiYAnToan.Chay<int>(_ => throw new InvalidOperationException("Mô hình dự báo chưa sẵn sàng (giả lập)."), ct: ct);
        return KetQua.Loi(kq.ThongBao ?? GoiYAnToan.ThongBaoMacDinh, kq.MaLoi);
    }
}
