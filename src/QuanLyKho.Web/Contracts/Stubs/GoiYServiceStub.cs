namespace QuanLyKho.Web.Contracts.Stubs;

/// <summary>Kho lưu gợi ý trong bộ nhớ, dùng chung giữa các request (đăng ký singleton).</summary>
public sealed class GoiYStubStore
{
    public object Khoa { get; } = new();
    public Dictionary<int, GoiYDto> GoiY { get; } = new();
    public int MaGoiYCuoi { get; set; }
}

/// <summary>
/// Bản giả của <see cref="IGoiYService"/>: đúng vòng đời Chờ duyệt, Đã duyệt, Đã sửa và duyệt, Từ chối.
/// Khi duyệt sẽ gọi <see cref="IGoiYHandler"/> của loại gợi ý; chưa có handler hoặc handler lỗi thì gợi ý vẫn ở Chờ duyệt.
/// </summary>
public sealed class GoiYServiceStub(GoiYStubStore store, IEnumerable<IGoiYHandler> handlers) : IGoiYService
{
    private readonly IReadOnlyList<IGoiYHandler> _handlers = handlers.ToList();

    public Task<KetQua<int>> Tao(LoaiGoiY loai, ThamChieuGoiY thamChieu, IReadOnlyList<ChiTietGoiYDto> chiTiet, CancellationToken ct = default)
    {
        var loi = KiemTraThamChieuKhiTao(loai, thamChieu) ?? KiemTraChiTiet(chiTiet);
        if (loi is not null) return Task.FromResult(KetQua<int>.Loi(loi, MaLoiDichVu.DuLieuKhongHopLe));

        lock (store.Khoa)
        {
            var ma = ++store.MaGoiYCuoi;
            store.GoiY[ma] = new GoiYDto(ma, loai, TrangThaiGoiY.ChoDuyet, DateTime.UtcNow, null, null, thamChieu, chiTiet.ToList());
            return Task.FromResult(KetQua<int>.Ok(ma));
        }
    }

    public Task<GoiYDto?> Lay(int maGoiY, CancellationToken ct = default)
    {
        lock (store.Khoa) { return Task.FromResult(store.GoiY.GetValueOrDefault(maGoiY)); }
    }

    public Task<IReadOnlyList<GoiYDto>> DanhSachChoDuyet(LoaiGoiY? loai = null, CancellationToken ct = default)
    {
        lock (store.Khoa)
        {
            IReadOnlyList<GoiYDto> kq = store.GoiY.Values
                .Where(g => g.TrangThai == TrangThaiGoiY.ChoDuyet && (loai is null || g.Loai == loai))
                .OrderBy(g => g.MaGoiY).ToList();
            return Task.FromResult(kq);
        }
    }

    public Task<KetQua> Duyet(int maGoiY, int maNguoiDuyet, CancellationToken ct = default) =>
        DuyetNoiBo(maGoiY, null, maNguoiDuyet, TrangThaiGoiY.DaDuyet, ct);

    public Task<KetQua> SuaVaDuyet(int maGoiY, IReadOnlyList<ChiTietGoiYDto> chiTietDaSua, int maNguoiDuyet, CancellationToken ct = default)
    {
        var loi = KiemTraChiTiet(chiTietDaSua);
        if (loi is not null) return Task.FromResult(KetQua.Loi(loi, MaLoiDichVu.DuLieuKhongHopLe));
        return DuyetNoiBo(maGoiY, chiTietDaSua, maNguoiDuyet, TrangThaiGoiY.DaSuaVaDuyet, ct);
    }

    public Task<KetQua> TuChoi(int maGoiY, int maNguoiDuyet, string? lyDo = null, CancellationToken ct = default)
    {
        lock (store.Khoa)
        {
            var g = store.GoiY.GetValueOrDefault(maGoiY);
            if (g is null) return Task.FromResult(KhongThay(maGoiY));
            if (g.TrangThai != TrangThaiGoiY.ChoDuyet) return Task.FromResult(DaXuLy(g));
            store.GoiY[maGoiY] = g with { TrangThai = TrangThaiGoiY.TuChoi, MaNguoiDuyet = maNguoiDuyet, ThoiGianDuyetUtc = DateTime.UtcNow };
            return Task.FromResult(KetQua.Ok());
        }
    }

    private async Task<KetQua> DuyetNoiBo(int maGoiY, IReadOnlyList<ChiTietGoiYDto>? chiTietMoi, int maNguoiDuyet, TrangThaiGoiY trangThaiMoi, CancellationToken ct)
    {
        GoiYDto? g;
        lock (store.Khoa) { g = store.GoiY.GetValueOrDefault(maGoiY); }
        if (g is null) return KhongThay(maGoiY);
        if (g.TrangThai != TrangThaiGoiY.ChoDuyet) return DaXuLy(g);

        var handler = _handlers.FirstOrDefault(h => h.Loai == g.Loai);
        if (handler is null)
            return KetQua.Loi($"Chưa có bộ xử lý cho gợi ý {g.Loai}: nhóm sở hữu chưa đăng ký IGoiYHandler. Gợi ý vẫn ở Chờ duyệt.", MaLoiDichVu.ChuaCoXuLy);

        var sauSua = chiTietMoi is null ? g : g with { ChiTiet = chiTietMoi.ToList() };
        var kq = await handler.ThucHien(sauSua, maNguoiDuyet, ct);
        if (!kq.ThanhCong)
            return KetQua.Loi(kq.ThongBao ?? "Không thực hiện được thao tác sau khi duyệt; hãy làm tay.", kq.MaLoi);

        lock (store.Khoa)
        {
            var hienTai = store.GoiY[maGoiY];
            if (hienTai.TrangThai != TrangThaiGoiY.ChoDuyet) return DaXuLy(hienTai);   // người khác vừa xử lý
            store.GoiY[maGoiY] = sauSua with
            {
                TrangThai = trangThaiMoi,
                MaNguoiDuyet = maNguoiDuyet,
                ThoiGianDuyetUtc = DateTime.UtcNow,
                ThamChieu = kq.GiaTri ?? sauSua.ThamChieu,
            };
        }
        return KetQua.Ok();
    }

    private static string? KiemTraThamChieuKhiTao(LoaiGoiY loai, ThamChieuGoiY t) => loai switch
    {
        LoaiGoiY.G1 when t.MaPX is null || t.MaPN is not null || t.MaPC is not null => "G1 phải gắn đúng một phiếu xuất (MaPX).",
        LoaiGoiY.G2 when t.MaPN is null || t.MaPX is not null || t.MaPC is not null => "G2 phải gắn đúng một phiếu nhập (MaPN).",
        LoaiGoiY.G3 or LoaiGoiY.G4 when t.MaPX is not null || t.MaPN is not null || t.MaPC is not null => $"{loai} chưa có phiếu khi tạo; phiếu được điền sau khi duyệt.",
        _ => null,
    };

    private static string? KiemTraChiTiet(IReadOnlyList<ChiTietGoiYDto> chiTiet)
    {
        if (chiTiet.Count == 0) return "Gợi ý phải có ít nhất một dòng đề xuất.";
        foreach (var c in chiTiet)
        {
            if (c.SoLuong <= 0) return "Số lượng đề xuất phải lớn hơn 0.";
            if (string.IsNullOrWhiteSpace(c.LyDo)) return "Mỗi dòng đề xuất phải kèm lý do và số liệu căn cứ.";
            if (c.MaKhoNguon.HasValue && c.MaKhoDich.HasValue && c.MaKhoNguon == c.MaKhoDich) return "Kho nguồn và kho đích phải khác nhau.";
        }
        return null;
    }

    private static KetQua KhongThay(int ma) => KetQua.Loi($"Không tìm thấy gợi ý {ma}.", MaLoiDichVu.KhongTimThay);

    private static KetQua DaXuLy(GoiYDto g) =>
        KetQua.Loi($"Gợi ý {g.MaGoiY} đã ở trạng thái '{g.TrangThai.ToCsdl()}', không thể xử lý lại.", MaLoiDichVu.SaiTrangThai);
}
