using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Web.Contracts.Stubs;

/// <summary>
/// Bản giả của <see cref="IStockService"/>: giữ tồn trong bộ nhớ, bắt đầu từ SeedData. Cư xử đúng quy tắc nghiệp vụ
/// (không tồn âm, vượt tồn bị từ chối kèm "cần bao nhiêu, còn bao nhiêu", mỗi lần đổi tồn ghi sổ cái) để các nhóm
/// làm trước khi bản thật xong. Mất dữ liệu khi khởi động lại ứng dụng. Tồn mẫu được coi là số dư đầu kỳ của sổ cái.
/// </summary>
public sealed class StockServiceStub : IStockService
{
    private readonly object _khoa = new();
    private readonly Dictionary<(int Lo, int ViTri), (int Ton, int Giu)> _ton = new();
    private readonly Dictionary<(int Lo, int ViTri), int> _dauKy = new();
    private readonly List<BienDongTonDto> _soCai = new();
    private long _maBD;

    public StockServiceStub()
    {
        foreach (var t in SeedDataSet.TonKho)
        {
            _ton[(t.MaLo, t.MaViTri)] = (t.SoLuongTon, t.SoLuongGiu);
            _dauKy[(t.MaLo, t.MaViTri)] = t.SoLuongTon;
        }
    }

    /// <summary>Sổ cái đã ghi (chỉ có ở bản giả, phục vụ kiểm thử và màn hình lịch sử tạm).</summary>
    public IReadOnlyList<BienDongTonDto> SoCai
    {
        get { lock (_khoa) { return _soCai.ToArray(); } }
    }

    public Task<KetQua> GiuCho(DongTon dong, CancellationToken ct = default)
    {
        var loi = KiemTraDong(dong);
        if (loi is not null) return Task.FromResult(loi);

        lock (_khoa)
        {
            var (ton, giu) = Lay(dong.MaLo, dong.MaViTri);
            var khaDung = ton - giu;
            if (dong.SoLuong > khaDung) return Task.FromResult(ThieuTon(dong.SoLuong, khaDung));
            _ton[(dong.MaLo, dong.MaViTri)] = (ton, giu + dong.SoLuong);
        }
        return Task.FromResult(KetQua.Ok());
    }

    public Task<KetQua> TraCho(DongTon dong, CancellationToken ct = default)
    {
        var loi = KiemTraDong(dong);
        if (loi is not null) return Task.FromResult(loi);

        lock (_khoa)
        {
            var (ton, giu) = Lay(dong.MaLo, dong.MaViTri);
            if (dong.SoLuong > giu)
                return Task.FromResult(KetQua.Loi($"Không thể trả chỗ: cần trả {dong.SoLuong}, đang giữ {giu}.", MaLoiDichVu.DuLieuKhongHopLe));
            _ton[(dong.MaLo, dong.MaViTri)] = (ton, giu - dong.SoLuong);
        }
        return Task.FromResult(KetQua.Ok());
    }

    public Task<KetQua> GhiTang(DongTon dong, LoaiBienDong loai, ThamChieuChungTu chungTu, int maND, CancellationToken ct = default)
    {
        var loi = KiemTraDong(dong);
        if (loi is not null) return Task.FromResult(loi);

        lock (_khoa)
        {
            var (ton, giu) = Lay(dong.MaLo, dong.MaViTri);
            _ton[(dong.MaLo, dong.MaViTri)] = (ton + dong.SoLuong, giu);
            GhiSoCai(dong.MaLo, dong.MaViTri, +dong.SoLuong, loai, chungTu, maND);
        }
        return Task.FromResult(KetQua.Ok());
    }

    public Task<KetQua> GhiGiam(DongTon dong, LoaiBienDong loai, ThamChieuChungTu chungTu, int maND, bool tuGiuCho = false, CancellationToken ct = default)
    {
        var loi = KiemTraDong(dong);
        if (loi is not null) return Task.FromResult(loi);

        lock (_khoa)
        {
            var (ton, giu) = Lay(dong.MaLo, dong.MaViTri);
            if (tuGiuCho)
            {
                if (dong.SoLuong > giu)
                    return Task.FromResult(KetQua.Loi($"Số lượng đang giữ chỗ không đủ: cần {dong.SoLuong}, đang giữ {giu}.", MaLoiDichVu.VuotTon));
                _ton[(dong.MaLo, dong.MaViTri)] = (ton - dong.SoLuong, giu - dong.SoLuong);
            }
            else
            {
                var khaDung = ton - giu;
                if (dong.SoLuong > khaDung) return Task.FromResult(ThieuTon(dong.SoLuong, khaDung));
                _ton[(dong.MaLo, dong.MaViTri)] = (ton - dong.SoLuong, giu);
            }
            GhiSoCai(dong.MaLo, dong.MaViTri, -dong.SoLuong, loai, chungTu, maND);
        }
        return Task.FromResult(KetQua.Ok());
    }

    public Task<KetQua> ChuyenO(int maLo, int maViTriNguon, int maViTriDich, int soLuong, ThamChieuChungTu chungTu, int maND, CancellationToken ct = default)
    {
        if (maViTriNguon == maViTriDich)
            return Task.FromResult(KetQua.Loi("Ô nguồn và ô đích phải khác nhau.", MaLoiDichVu.DuLieuKhongHopLe));

        var loi = KiemTraDong(new DongTon(maLo, maViTriNguon, soLuong)) ?? KiemTraDong(new DongTon(maLo, maViTriDich, soLuong));
        if (loi is not null) return Task.FromResult(loi);

        lock (_khoa)
        {
            var (tonN, giuN) = Lay(maLo, maViTriNguon);
            var khaDung = tonN - giuN;
            if (soLuong > khaDung) return Task.FromResult(ThieuTon(soLuong, khaDung));

            var (tonD, giuD) = Lay(maLo, maViTriDich);
            _ton[(maLo, maViTriNguon)] = (tonN - soLuong, giuN);
            _ton[(maLo, maViTriDich)] = (tonD + soLuong, giuD);
            GhiSoCai(maLo, maViTriNguon, -soLuong, LoaiBienDong.DoiO, chungTu, maND);
            GhiSoCai(maLo, maViTriDich, +soLuong, LoaiBienDong.DoiO, chungTu, maND);
        }
        return Task.FromResult(KetQua.Ok());
    }

    public Task<TonKhoDto?> LayTon(int maLo, int maViTri, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            TonKhoDto? kq = _ton.TryGetValue((maLo, maViTri), out var t) ? new TonKhoDto(maLo, maViTri, t.Ton, t.Giu) : null;
            return Task.FromResult(kq);
        }
    }

    public Task<IReadOnlyList<TonKhoDto>> LayTonTheoViTri(int maViTri, CancellationToken ct = default)
    {
        lock (_khoa)
        {
            IReadOnlyList<TonKhoDto> kq = _ton
                .Where(x => x.Key.ViTri == maViTri && x.Value.Ton > 0)
                .Select(x => new TonKhoDto(x.Key.Lo, x.Key.ViTri, x.Value.Ton, x.Value.Giu))
                .OrderBy(x => x.MaLo)
                .ToList();
            return Task.FromResult(kq);
        }
    }

    public Task<IReadOnlyList<ChenhLechSoCai>> DoiChieuSoCai(CancellationToken ct = default)
    {
        lock (_khoa)
        {
            var khoa = _ton.Keys.Concat(_dauKy.Keys).Distinct();
            var kq = new List<ChenhLechSoCai>();
            foreach (var k in khoa)
            {
                var theoSoCai = _dauKy.GetValueOrDefault(k) + _soCai.Where(b => b.MaLo == k.Lo && b.MaViTri == k.ViTri).Sum(b => b.SoLuong);
                var hienTai = _ton.TryGetValue(k, out var t) ? t.Ton : 0;
                if (hienTai != theoSoCai) kq.Add(new ChenhLechSoCai(k.Lo, k.ViTri, hienTai, theoSoCai));
            }
            return Task.FromResult<IReadOnlyList<ChenhLechSoCai>>(kq);
        }
    }

    // ---- nội bộ (gọi trong lock) ----

    private (int Ton, int Giu) Lay(int maLo, int maViTri) => _ton.TryGetValue((maLo, maViTri), out var t) ? t : (0, 0);

    private void GhiSoCai(int maLo, int maViTri, int soLuong, LoaiBienDong loai, ThamChieuChungTu ct, int maND) =>
        _soCai.Add(new BienDongTonDto(++_maBD, maLo, maViTri, soLuong, loai, ct.Loai, ct.MaCT, maND, DateTime.UtcNow));

    private static KetQua ThieuTon(int can, int con) =>
        KetQua.Loi($"Không đủ tồn khả dụng: cần {can}, còn {con}.", MaLoiDichVu.VuotTon);

    private static KetQua? KiemTraDong(DongTon d)
    {
        if (d.SoLuong <= 0)
            return KetQua.Loi("Số lượng phải lớn hơn 0.", MaLoiDichVu.DuLieuKhongHopLe);

        var vt = SeedDataSet.TimViTri(d.MaViTri);
        if (vt is null)
            return KetQua.Loi($"Không tìm thấy vị trí {d.MaViTri}.", MaLoiDichVu.KhongTimThay);
        if (vt.CapViTri != SeedDataSet.CapO)
            return KetQua.Loi($"Hàng chỉ được để ở ô, '{vt.TenViTri}' là cấp {vt.CapViTri}.", MaLoiDichVu.DuLieuKhongHopLe);

        return null;
    }
}
