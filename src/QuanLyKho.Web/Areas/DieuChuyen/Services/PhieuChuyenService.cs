using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using QuanLyKho.Web.Areas.DieuChuyen.Models;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Web.Areas.DieuChuyen.Services;

/// <summary>Mã đã quét cho một dòng chuyển khi xuất khỏi kho xuất.</summary>
public sealed record MaQuetDong(int MaCTC, string? MaVachO, string? MaVachLo, string? MaVachBT);

/// <summary>
/// Nghiệp vụ phiếu chuyển kho (việc T5-2 đến T5-6, báo cáo mục 2.1.5, Hình 2.6). Mọi thay đổi tồn đi qua <see cref="IStockService"/>
/// trong cùng giao dịch với việc đổi trạng thái phiếu; điều kiện trạng thái đặt ngay trong lệnh UPDATE nên bấm hai lần hoặc hai người
/// cùng thao tác chỉ ghi một lần.
/// <list type="bullet">
/// <item>Lập (T5-2): kho xuất khác kho nhập, có tuyến; ô xuất là ô lưu trữ của kho xuất, ô nhận là ô lưu trữ của kho nhập, cả hai đang
/// dùng và không khóa kiểm kê; không vượt tồn khả dụng tại (lô, ô xuất); ô nhận còn vừa kích thước, thể tích, khối lượng.</item>
/// <item>Duyệt (T5-3): người duyệt khác người lập; giữ chỗ tại ô xuất.</item>
/// <item>Xuất (T5-4): thủ kho kho đi quét đúng ô, lô, mã vạch của mọi dòng; trừ tồn kho đi (Chuyển đi, từ phần đã giữ). Phiếu Đang vận
/// chuyển: hàng không thuộc tồn của kho nào.</item>
/// <item>Nhận (T5-5): thủ kho kho đến quét ô xếp và lô từng dòng, nhập số thực nhận (không vượt số gửi; thiếu phải ghi lý do); kiểm lại
/// sức chứa rồi cộng tồn kho đến (Nhận chuyển). Đếm hết mọi dòng: đủ thì Đã nhận, có thiếu thì Nhận một phần.</item>
/// <item>Xử lý chênh lệch (báo cáo mục 2.1.5, Bảng 1.5): quản lý kho chọn nhập phần thiếu tại kho đến (ghi loại Nhận hàng) hoặc trả về
/// kho đi (ghi loại Hoàn tác), bắt buộc ghi nguyên nhân; không sinh phiếu nhập. Xử lý hết thì Đã nhận. Hai loại biến động này khác
/// "Nhận chuyển" để sổ cái không coi là ghi lặp khi số thiếu trùng số đã nhận.</item>
/// <item>Hủy (T5-6): chỉ khi Nháp hoặc Đã duyệt; Đã duyệt thì trả chỗ đã giữ.</item>
/// </list>
/// </summary>
public sealed class PhieuChuyenService(KhoDbContext db, IStockService ton, ICapacityService sucChua, IAuditService audit)
{
    // ---- Lập, sửa phiếu nháp (T5-2) ----

    /// <summary>Kiểm toàn bộ quy tắc lập phiếu; trả về lỗi đầu tiên hoặc null nếu hợp lệ.</summary>
    public async Task<string?> KiemTra(PhieuChuyen p, CancellationToken ct = default)
    {
        if (p.KiemTra() is string loi) return loi;

        var kho = await db.Kho.AsNoTracking().Where(k => k.MaKho == p.MaKhoXuat || k.MaKho == p.MaKhoNhap).ToListAsync(ct);
        if (kho.Count != 2 || kho.Any(k => k.TrangThai != TrangThaiHoatDong.HoatDong)) return "Kho đi hoặc kho đến không tồn tại hoặc đã ngừng hoạt động.";
        if (!await db.TuyenVanChuyen.AnyAsync(t => t.MaKhoXuat == p.MaKhoXuat && t.MaKhoNhap == p.MaKhoNhap, ct))
            return "Chưa có tuyến vận chuyển giữa hai kho này. Khai báo tuyến ở mục Tuyến vận chuyển trước.";

        var maO = p.ChiTiet.SelectMany(d => new[] { d.MaViTriXuat, d.MaViTriNhap }).Distinct().ToList();
        var o = await db.ViTri.AsNoTracking().Where(v => maO.Contains(v.MaViTri)).ToDictionaryAsync(v => v.MaViTri, ct);
        var maLo = p.ChiTiet.Select(d => d.MaLo).Distinct().ToList();
        var lo = await db.LoHang.AsNoTracking().Where(l => maLo.Contains(l.MaLo)).ToDictionaryAsync(l => l.MaLo, ct);

        for (var i = 0; i < p.ChiTiet.Count; i++)
        {
            var d = p.ChiTiet[i];
            if (!lo.ContainsKey(d.MaLo)) return $"Dòng {i + 1}: lô không tồn tại.";
            if (LoiO(o.GetValueOrDefault(d.MaViTriXuat), p.MaKhoXuat, "lấy hàng", "kho đi") is string loiX) return $"Dòng {i + 1}: {loiX}";
            if (LoiO(o.GetValueOrDefault(d.MaViTriNhap), p.MaKhoNhap, "xếp hàng", "kho đến") is string loiN) return $"Dòng {i + 1}: {loiN}";

            // Phiếu Nháp chưa giữ chỗ nên so với phần khả dụng hiện tại (tồn trừ phần đang giữ cho phiếu khác).
            var t = await ton.LayTon(d.MaLo, d.MaViTriXuat, ct);
            var khaDung = t is null ? 0 : t.SoLuongTon - t.SoLuongGiu;
            if (d.SoLuong > khaDung)
                return $"Dòng {i + 1}: lô {lo[d.MaLo].MaVachLo} ở {o[d.MaViTriXuat].TenViTri} chỉ còn {khaDung} có thể chuyển (cần {d.SoLuong}).";
        }

        // Sức chứa ô nhận: gộp theo (ô, biến thể) để hai dòng cùng vào một ô không cùng lọt qua khi mỗi dòng riêng lẻ vừa đủ.
        var theoO = p.ChiTiet.GroupBy(d => (d.MaViTriNhap, lo[d.MaLo].MaBT)).Select(g => (g.Key.MaViTriNhap, g.Key.MaBT, SoLuong: g.Sum(d => d.SoLuong)));
        foreach (var (maViTri, maBT, soLuong) in theoO)
        {
            var kq = await sucChua.KiemTra(maViTri, maBT, soLuong, ct);
            if (!kq.Du) return $"Ô xếp hàng '{o[maViTri].TenViTri}': {kq.LyDo ?? "không đủ chỗ."}";
        }
        return null;
    }

    private static string? LoiO(ViTri? v, int maKho, string vaiTro, string tenKho)
    {
        if (v is null) return $"ô {vaiTro} không tồn tại.";
        if (v.MaKho != maKho) return $"ô {vaiTro} '{v.TenViTri}' không thuộc {tenKho}.";
        if (!v.LaO || v.LoaiViTri != LoaiViTriGiaTri.LuuTru) return $"ô {vaiTro} '{v.TenViTri}' phải là ô lưu trữ.";
        if (!v.DangHoatDong) return $"ô {vaiTro} '{v.TenViTri}' đã ngừng dùng.";
        if (v.DangKhoa) return $"ô {vaiTro} '{v.TenViTri}' đang khóa kiểm kê.";
        return null;
    }

    /// <summary>Tạo mới (MaPC = 0) hoặc sửa phiếu Nháp. Trả về MaPC.</summary>
    public async Task<KetQua<int>> Luu(PhieuChuyen p, int maND, CancellationToken ct = default)
    {
        var dong = p.ChiTiet.Select(d => new ChiTietChuyen { MaLo = d.MaLo, MaViTriXuat = d.MaViTriXuat, MaViTriNhap = d.MaViTriNhap, SoLuong = d.SoLuong }).ToList();
        p.ChiTiet = dong;
        if (await KiemTra(p, ct) is string loi) return KetQua<int>.Loi(loi, MaLoiDichVu.DuLieuKhongHopLe);
        static string? Gon(string? x) => string.IsNullOrWhiteSpace(x) ? null : x.Trim();
        var (dienGiai, nguoi, phuongTien) = (Gon(p.DienGiai), Gon(p.NguoiVanChuyen), Gon(p.PhuongTien));

        if (p.MaPC == 0)
        {
            var moi = new PhieuChuyen
            {
                SoPhieu = "TAM" + Guid.NewGuid().ToString("N")[..12],   // số tạm, đổi thành CK + mã ngay bên dưới
                ThoiGian = DateTime.UtcNow, MaKhoXuat = p.MaKhoXuat, MaKhoNhap = p.MaKhoNhap, NguoiVanChuyen = nguoi, PhuongTien = phuongTien,
                MaNguoiLap = maND, DienGiai = dienGiai, ChiTiet = dong,
            };
            await using var gd = await GiaoDich.Mo(db, ct);
            db.PhieuChuyen.Add(moi);
            await db.SaveChangesAsync(ct);
            moi.SoPhieu = $"CK{moi.MaPC:D6}";
            await db.SaveChangesAsync(ct);
            await gd.Xong(ct);
            await audit.Ghi(maND, "Tạo phiếu chuyển", LoaiChungTu.CK, moi.MaPC, moi.SoPhieu, ct);
            return KetQua<int>.Ok(moi.MaPC, $"Đã lưu phiếu nháp {moi.SoPhieu}.");
        }

        // Đọc trạng thái thẳng từ CSDL (phép chiếu không qua bộ nhớ đệm của EF): phiếu có thể vừa được duyệt bằng lệnh UPDATE trực tiếp.
        var trangThai = await db.PhieuChuyen.Where(x => x.MaPC == p.MaPC).Select(x => x.TrangThai).FirstOrDefaultAsync(ct);
        if (trangThai is null) return KetQua<int>.Loi("Không tìm thấy phiếu.", MaLoiDichVu.KhongTimThay);
        if (trangThai != PhieuChuyen.Nhap) return KetQua<int>.Loi($"Phiếu đang {trangThai} nên bị khóa, không sửa được.", MaLoiDichVu.SaiTrangThai);
        var cu = await db.PhieuChuyen.Include(x => x.ChiTiet).FirstAsync(x => x.MaPC == p.MaPC, ct);
        cu.MaKhoXuat = p.MaKhoXuat;
        cu.MaKhoNhap = p.MaKhoNhap;
        cu.NguoiVanChuyen = nguoi;
        cu.PhuongTien = phuongTien;
        cu.DienGiai = dienGiai;
        db.ChiTietChuyen.RemoveRange(cu.ChiTiet);
        await db.SaveChangesAsync(ct);   // xóa dòng cũ trước để chỉ mục duy nhất (lô, ô) không vướng dòng mới
        cu.ChiTiet = dong;
        await db.SaveChangesAsync(ct);
        await audit.Ghi(maND, "Sửa phiếu chuyển", LoaiChungTu.CK, cu.MaPC, cu.SoPhieu, ct);
        return KetQua<int>.Ok(cu.MaPC, "Đã lưu phiếu nháp.");
    }

    // ---- Duyệt, giữ chỗ (T5-3) ----

    public async Task<KetQua> Duyet(int maPC, int maND, CancellationToken ct = default)
    {
        var p = await db.PhieuChuyen.AsNoTracking().Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPC == maPC, ct);
        if (p is null) return KetQua.Loi("Không tìm thấy phiếu.", MaLoiDichVu.KhongTimThay);
        if (p.TrangThai != PhieuChuyen.Nhap) return KetQua.Loi($"Phiếu đang {p.TrangThai}, chỉ duyệt được phiếu Nháp.", MaLoiDichVu.SaiTrangThai);
        if (QuyTacDuyet.KiemTra(maND, p.MaNguoiLap, "phiếu chuyển") is { ThanhCong: false } tuDuyet) return tuDuyet;
        if (await KiemTra(p, ct) is string loi) return KetQua.Loi(loi, MaLoiDichVu.DuLieuKhongHopLe);

        await using var gd = await GiaoDich.Mo(db, ct);
        var n = await db.PhieuChuyen.Where(x => x.MaPC == maPC && x.TrangThai == PhieuChuyen.Nhap)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, PhieuChuyen.DaDuyet).SetProperty(x => x.MaNguoiDuyet, maND), ct);
        if (n != 1) return KetQua.Loi("Phiếu vừa bị đổi trạng thái ở nơi khác.", MaLoiDichVu.SaiTrangThai);

        foreach (var d in p.ChiTiet)
        {
            var kq = await ton.GiuCho(new DongTon(d.MaLo, d.MaViTriXuat, d.SoLuong), ct);
            if (!kq.ThanhCong) return kq;   // không commit: hoàn tác cả đổi trạng thái lẫn các dòng đã giữ
        }
        await gd.Xong(ct);
        await audit.Ghi(maND, "Duyệt phiếu chuyển", LoaiChungTu.CK, maPC, p.SoPhieu, ct);
        return KetQua.Ok("Đã duyệt phiếu và giữ hàng ở kho đi. Thủ kho kho đi có thể quét mã xuất hàng.");
    }

    // ---- Hủy (T5-6) ----

    public async Task<KetQua> Huy(int maPC, int maND, CancellationToken ct = default)
    {
        var p = await db.PhieuChuyen.AsNoTracking().Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPC == maPC, ct);
        if (p is null) return KetQua.Loi("Không tìm thấy phiếu.", MaLoiDichVu.KhongTimThay);
        if (!p.HuyDuoc) return KetQua.Loi($"Phiếu đang {p.TrangThai}: hàng đã rời kho đi nên không hủy được.", MaLoiDichVu.SaiTrangThai);

        await using var gd = await GiaoDich.Mo(db, ct);
        var n = await db.PhieuChuyen.Where(x => x.MaPC == maPC && x.TrangThai == p.TrangThai)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, PhieuChuyen.DaHuy), ct);
        if (n != 1) return KetQua.Loi("Phiếu vừa bị đổi trạng thái ở nơi khác.", MaLoiDichVu.SaiTrangThai);

        if (p.TrangThai == PhieuChuyen.DaDuyet)
            foreach (var d in p.ChiTiet)
            {
                var kq = await ton.TraCho(new DongTon(d.MaLo, d.MaViTriXuat, d.SoLuong), ct);
                if (!kq.ThanhCong) return kq;
            }
        await gd.Xong(ct);
        await audit.Ghi(maND, "Hủy phiếu chuyển", LoaiChungTu.CK, maPC, p.SoPhieu, ct);
        return KetQua.Ok(p.TrangThai == PhieuChuyen.DaDuyet ? "Đã hủy phiếu và trả lại hàng đang giữ ở kho đi." : "Đã hủy phiếu.");
    }

    // ---- Xuất khỏi kho xuất (T5-4) ----

    /// <summary>
    /// Kiểm mã quét của MỌI dòng (đúng ô xuất, lô, mã vạch biến thể); đúng hết mới trừ tồn và chuyển phiếu sang Đã xuất.
    /// Sai bất kỳ mã nào thì từ chối, không ghi gì.
    /// </summary>
    public async Task<KetQua> Xuat(int maPC, IReadOnlyList<MaQuetDong> maQuet, int maND, CancellationToken ct = default)
    {
        var p = await db.PhieuChuyen.AsNoTracking().Include(x => x.ChiTiet).FirstOrDefaultAsync(x => x.MaPC == maPC, ct);
        if (p is null) return KetQua.Loi("Không tìm thấy phiếu.", MaLoiDichVu.KhongTimThay);
        if (p.TrangThai != PhieuChuyen.DaDuyet) return KetQua.Loi($"Phiếu đang {p.TrangThai}, chỉ xuất được phiếu Đã duyệt.", MaLoiDichVu.SaiTrangThai);

        var theoDong = maQuet.GroupBy(m => m.MaCTC).ToDictionary(g => g.Key, g => g.First());
        foreach (var d in p.ChiTiet)
        {
            var (o, lo, bt) = await MaVach(d, d.MaViTriXuat, ct);
            if (!theoDong.TryGetValue(d.MaCTC, out var m)) return KetQua.Loi($"Chưa quét dòng lô {lo.MaVachLo}. Chưa xuất gì.", MaLoiDichVu.DuLieuKhongHopLe);
            if (!Khop(m.MaVachO, o.MaVachO)) return KetQua.Loi($"Quét sai ô: lô {lo.MaVachLo} phải lấy ở {o.TenViTri}. Chưa xuất gì.", MaLoiDichVu.DuLieuKhongHopLe);
            if (!Khop(m.MaVachLo, lo.MaVachLo)) return KetQua.Loi($"Quét sai lô ở {o.TenViTri}. Chưa xuất gì.", MaLoiDichVu.DuLieuKhongHopLe);
            if (!Khop(m.MaVachBT, bt)) return KetQua.Loi($"Quét sai mã vạch hàng của lô {lo.MaVachLo}. Chưa xuất gì.", MaLoiDichVu.DuLieuKhongHopLe);
        }

        await using var gd = await GiaoDich.Mo(db, ct);
        var n = await db.PhieuChuyen.Where(x => x.MaPC == maPC && x.TrangThai == PhieuChuyen.DaDuyet)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, PhieuChuyen.DangVanChuyen), ct);
        if (n != 1) return KetQua.Loi("Phiếu vừa được xuất hoặc đổi trạng thái ở nơi khác.", MaLoiDichVu.SaiTrangThai);

        var chungTu = new ThamChieuChungTu(LoaiChungTu.CK, maPC);
        foreach (var d in p.ChiTiet)
        {
            var kq = await ton.GhiGiam(new DongTon(d.MaLo, d.MaViTriXuat, d.SoLuong), LoaiBienDong.ChuyenDi, chungTu, maND, tuGiuCho: true, ct);
            if (!kq.ThanhCong) return kq;
        }
        await gd.Xong(ct);
        await audit.Ghi(maND, "Xuất hàng chuyển kho", LoaiChungTu.CK, maPC, $"{p.SoPhieu}: {p.ChiTiet.Sum(d => d.SoLuong)} đơn vị", ct);
        return KetQua.Ok($"Đã xuất {p.ChiTiet.Sum(d => d.SoLuong)} đơn vị khỏi kho. Phiếu đang vận chuyển tới kho đến.");
    }

    // ---- Nhận tại kho nhập (T5-5) ----

    public async Task<KetQua> Nhan(int maCTC, string? maVachO, string? maVachLo, int slNhan, string? lyDoLech, int maND, CancellationToken ct = default)
    {
        var d = await db.ChiTietChuyen.AsNoTracking().FirstOrDefaultAsync(x => x.MaCTC == maCTC, ct);
        if (d is null) return KetQua.Loi("Không tìm thấy dòng hàng.", MaLoiDichVu.KhongTimThay);
        if (d.SLNhan is not null) return KetQua.Loi("Dòng này đã nhận rồi.", MaLoiDichVu.SaiTrangThai);
        var p = await db.PhieuChuyen.AsNoTracking().FirstAsync(x => x.MaPC == d.MaPC, ct);
        if (p.TrangThai != PhieuChuyen.DangVanChuyen) return KetQua.Loi($"Phiếu {p.SoPhieu} đang {p.TrangThai}, chỉ nhận được phiếu Đang vận chuyển.", MaLoiDichVu.SaiTrangThai);

        var (o, lo, _) = await MaVach(d, d.MaViTriNhap, ct);
        if (!Khop(maVachO, o.MaVachO)) return KetQua.Loi($"Quét sai ô: lô {lo.MaVachLo} phải xếp vào {o.TenViTri}. Chưa ghi nhận gì.", MaLoiDichVu.DuLieuKhongHopLe);
        if (!Khop(maVachLo, lo.MaVachLo)) return KetQua.Loi("Quét sai lô: mã lô không phải của dòng này. Chưa ghi nhận gì.", MaLoiDichVu.DuLieuKhongHopLe);
        if (slNhan < 0 || slNhan > d.SoLuong) return KetQua.Loi($"Số thực nhận phải từ 0 đến {d.SoLuong} (số đã gửi). Chưa ghi nhận gì.", MaLoiDichVu.DuLieuKhongHopLe);
        var lyDo = slNhan == d.SoLuong ? null : string.IsNullOrWhiteSpace(lyDoLech) ? null : lyDoLech.Trim();
        if (slNhan != d.SoLuong && lyDo is null) return KetQua.Loi("Nhận thiếu so với số gửi thì phải ghi lý do lệch. Chưa ghi nhận gì.", MaLoiDichVu.DuLieuKhongHopLe);
        if (lyDo?.Length > 255) return KetQua.Loi("Lý do lệch tối đa 255 ký tự.", MaLoiDichVu.DuLieuKhongHopLe);

        if (slNhan > 0)
        {
            var sc = await sucChua.KiemTra(d.MaViTriNhap, lo.MaBT, slNhan, ct);
            if (!sc.Du) return KetQua.Loi($"{o.TenViTri} không nhận được: {sc.LyDo ?? "không đủ chỗ."} Chưa ghi nhận gì.", sc.MaLoi ?? MaLoiDichVu.DuLieuKhongHopLe);
        }

        await using var gd = await GiaoDich.Mo(db, ct);
        var n = await db.ChiTietChuyen.Where(x => x.MaCTC == maCTC && x.SLNhan == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.SLNhan, slNhan).SetProperty(x => x.LyDoLech, lyDo), ct);
        if (n != 1) return KetQua.Loi("Dòng này vừa được nhận ở nơi khác.", MaLoiDichVu.SaiTrangThai);

        if (slNhan > 0)
        {
            var kq = await ton.GhiTang(new DongTon(d.MaLo, d.MaViTriNhap, slNhan), LoaiBienDong.NhanChuyen, new ThamChieuChungTu(LoaiChungTu.CK, d.MaPC), maND, ct);
            if (!kq.ThanhCong) return kq;
        }
        // Đếm xong dòng cuối: đủ hết thì Đã nhận, có dòng thiếu thì Nhận một phần (chờ quản lý kho xử lý chênh lệch).
        if (!await db.ChiTietChuyen.AnyAsync(x => x.MaPC == d.MaPC && x.SLNhan == null, ct))
        {
            var coThieu = await db.ChiTietChuyen.AnyAsync(x => x.MaPC == d.MaPC && x.SLNhan < x.SoLuong, ct);
            await db.PhieuChuyen.Where(x => x.MaPC == d.MaPC && x.TrangThai == PhieuChuyen.DangVanChuyen)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, coThieu ? PhieuChuyen.NhanMotPhan : PhieuChuyen.DaNhan), ct);
        }
        await gd.Xong(ct);

        var ketQua = slNhan == d.SoLuong ? $"Đã nhận đủ {slNhan} vào {o.TenViTri}." : $"Đã ghi nhận thiếu: nhận {slNhan}/{d.SoLuong} ({lyDo}).";
        await audit.Ghi(maND, "Nhận hàng chuyển kho", LoaiChungTu.CK, d.MaPC, $"{p.SoPhieu}: lô {lo.MaVachLo} — {ketQua}", ct);
        return KetQua.Ok(ketQua);
    }

    // ---- Xử lý chênh lệch khi nhận thiếu (báo cáo mục 2.1.5, Bảng 1.5) ----

    /// <summary>
    /// Quản lý kho chọn nhập phần thiếu tại kho đến (đã tìm thấy hàng) hoặc trả về kho đi (hàng chưa rời kho, xuất nhầm số), bắt buộc
    /// ghi nguyên nhân. Ghi sổ cái phần thiếu vào đúng ô của dòng (ô xếp hoặc ô lấy), kiểm sức chứa trước; không sinh phiếu nhập.
    /// </summary>
    public async Task<KetQua> XuLyLech(int maCTC, string? cach, string? nguyenNhan, int maND, CancellationToken ct = default)
    {
        var d = await db.ChiTietChuyen.AsNoTracking().FirstOrDefaultAsync(x => x.MaCTC == maCTC, ct);
        if (d is null) return KetQua.Loi("Không tìm thấy dòng hàng.", MaLoiDichVu.KhongTimThay);
        var p = await db.PhieuChuyen.AsNoTracking().FirstAsync(x => x.MaPC == d.MaPC, ct);
        if (p.TrangThai != PhieuChuyen.NhanMotPhan) return KetQua.Loi($"Phiếu {p.SoPhieu} đang {p.TrangThai}, không có chênh lệch chờ xử lý.", MaLoiDichVu.SaiTrangThai);
        if (!d.ChoXuLyLech) return KetQua.Loi("Dòng này không có phần thiếu chờ xử lý.", MaLoiDichVu.SaiTrangThai);
        if (cach is not (ChiTietChuyen.NhapTaiKhoDen or ChiTietChuyen.TraVeKhoDi)) return KetQua.Loi("Chọn cách xử lý: nhập tại kho đến hoặc trả về kho đi.", MaLoiDichVu.DuLieuKhongHopLe);
        var lyDo = string.IsNullOrWhiteSpace(nguyenNhan) ? null : nguyenNhan.Trim();
        if (lyDo is null) return KetQua.Loi("Ghi nguyên nhân chênh lệch.", MaLoiDichVu.DuLieuKhongHopLe);
        if (lyDo.Length > 255) return KetQua.Loi("Nguyên nhân tối đa 255 ký tự.", MaLoiDichVu.DuLieuKhongHopLe);

        var thieu = d.SoThieu;
        var veKhoDen = cach == ChiTietChuyen.NhapTaiKhoDen;
        var maViTri = veKhoDen ? d.MaViTriNhap : d.MaViTriXuat;
        var (o, lo, _) = await MaVach(d, maViTri, ct);
        var sc = await sucChua.KiemTra(maViTri, lo.MaBT, thieu, ct);
        if (!sc.Du) return KetQua.Loi($"{o.TenViTri} không nhận thêm được {thieu}: {sc.LyDo ?? "không đủ chỗ."}", sc.MaLoi ?? MaLoiDichVu.DuLieuKhongHopLe);

        await using var gd = await GiaoDich.Mo(db, ct);
        var n = await db.ChiTietChuyen.Where(x => x.MaCTC == maCTC && x.XuLyLech == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.XuLyLech, cach).SetProperty(x => x.NguyenNhanLech, lyDo), ct);
        if (n != 1) return KetQua.Loi("Dòng này vừa được xử lý ở nơi khác.", MaLoiDichVu.SaiTrangThai);

        var kq = await ton.GhiTang(new DongTon(d.MaLo, maViTri, thieu), veKhoDen ? LoaiBienDong.NhanHang : LoaiBienDong.HoanTac,
            new ThamChieuChungTu(LoaiChungTu.CK, d.MaPC), maND, ct);
        if (!kq.ThanhCong) return kq;

        if (!await db.ChiTietChuyen.AnyAsync(x => x.MaPC == d.MaPC && x.SLNhan < x.SoLuong && x.XuLyLech == null, ct))
            await db.PhieuChuyen.Where(x => x.MaPC == d.MaPC && x.TrangThai == PhieuChuyen.NhanMotPhan)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, PhieuChuyen.DaNhan), ct);
        await gd.Xong(ct);

        var ketQua = veKhoDen ? $"Đã nhập {thieu} còn thiếu của lô {lo.MaVachLo} vào {o.TenViTri} (kho đến)." : $"Đã trả {thieu} của lô {lo.MaVachLo} về {o.TenViTri} (kho đi).";
        await audit.Ghi(maND, "Xử lý chênh lệch chuyển kho", LoaiChungTu.CK, d.MaPC, $"{p.SoPhieu}: {ketQua} Nguyên nhân: {lyDo}", ct);
        return KetQua.Ok(ketQua);
    }

    // ---- Dùng chung ----

    private async Task<(ViTri O, HangHoa.Models.LoHang Lo, string MaVachBT)> MaVach(ChiTietChuyen d, int maViTri, CancellationToken ct)
    {
        var o = await db.ViTri.AsNoTracking().FirstAsync(v => v.MaViTri == maViTri, ct);
        var lo = await db.LoHang.AsNoTracking().FirstAsync(l => l.MaLo == d.MaLo, ct);
        var bt = await db.BienThe.AsNoTracking().Where(b => b.MaBT == lo.MaBT).Select(b => b.MaVach).FirstAsync(ct);
        return (o, lo, bt);
    }

    /// <summary>Mã quét khớp mã vạch (bỏ khoảng trắng hai đầu, không phân biệt hoa thường). Chưa có mã vạch thì không bao giờ khớp.</summary>
    public static bool Khop(string? maQuet, string? maVach) =>
        !string.IsNullOrWhiteSpace(maVach) && string.Equals(maQuet?.Trim(), maVach, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Giao dịch cho một thao tác: chưa có giao dịch thì mở mới (thoát mà chưa <see cref="Xong"/> là hoàn tác);
    /// nơi gọi đã mở sẵn (ví dụ kiểm thử) thì đặt savepoint và quay về đó khi lỗi, không đụng giao dịch bên ngoài.
    /// </summary>
    private sealed class GiaoDich : IAsyncDisposable
    {
        private IDbContextTransaction? _rieng, _ngoai;
        private string? _diemLuu;
        private bool _xong;

        public static async Task<GiaoDich> Mo(KhoDbContext db, CancellationToken ct)
        {
            var g = new GiaoDich();
            if (db.Database.CurrentTransaction is { } ngoai)
            {
                g._ngoai = ngoai;
                g._diemLuu = "PhieuChuyen" + Guid.NewGuid().ToString("N")[..8];
                await ngoai.CreateSavepointAsync(g._diemLuu, ct);
            }
            else g._rieng = await db.Database.BeginTransactionAsync(ct);
            return g;
        }

        public async Task Xong(CancellationToken ct)
        {
            _xong = true;
            if (_rieng is not null) await _rieng.CommitAsync(ct);
        }

        public async ValueTask DisposeAsync()
        {
            if (!_xong && _ngoai is not null && _diemLuu is not null) await _ngoai.RollbackToSavepointAsync(_diemLuu);
            if (_rieng is not null) await _rieng.DisposeAsync();   // chưa commit thì tự hoàn tác
        }
    }
}
