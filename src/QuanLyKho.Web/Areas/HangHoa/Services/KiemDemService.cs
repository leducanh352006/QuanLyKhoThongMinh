using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>
/// Nghiệp vụ kiểm đếm quét mã (việc T3-5, Bảng 2.4 dòng 4). Mỗi dòng đếm gắn một ô và một lô: thủ kho quét mã ô, quét mã lô
/// (hoặc mã vạch hàng khi ô chỉ có một lô của mặt hàng đó) rồi nhập số lượng thực tế. Số hệ thống là số đã chụp lúc tạo phiếu,
/// không đọc lại tồn. Chênh lệch = thực tế - hệ thống; dòng tự rơi vào tab Khớp, Thừa, Thiếu, Chưa kiểm theo số đếm.
/// <para>Quét sai ô báo ngay: ô không thuộc phạm vi phiếu (kho khác, khu Tiếp nhận, hoặc ngoài các ô của phiếu Một phần) bị từ chối;
/// lô mà số hệ thống không có ở ô vừa quét cũng bị chặn kèm tên ô đang chứa lô đó, chỉ ghi khi người đếm xác nhận hàng ngoài sổ.</para>
/// <para>Kiểm đếm không làm đổi tồn: tồn chỉ đổi khi phát sinh được duyệt (T3-7) qua phiếu điều chỉnh.</para>
/// </summary>
public sealed class KiemDemService(IKiemDemStore store, IAuditService audit)
{
    /// <summary>Chặn số nhập nhầm quá lớn (ví dụ quét mã vạch vào ô số lượng).</summary>
    public const int SoLuongToiDa = 1_000_000;

    public async Task<KiemDemTrang?> LayTrang(int maPK, TabKiemDem tab, GhiNhanDemVm? form = null, CancellationToken ct = default)
    {
        var phieu = await store.LayPhieu(maPK, ct);
        if (phieu is null) return null;
        return new KiemDemTrang(phieu, await store.LayDong(maPK, ct), tab, form ?? new GhiNhanDemVm());
    }

    /// <summary>Ghi số đếm cho một ô và một lô. Thành công thì thông báo nêu số hệ thống, số thực tế và chênh lệch.</summary>
    public async Task<KetQua> GhiNhan(int maPK, GhiNhanDemVm vm, int maND, CancellationToken ct = default)
    {
        var maVachO = vm.MaVachO?.Trim();
        var maHang = vm.MaHang?.Trim();
        if (string.IsNullOrEmpty(maVachO)) return Loi("Quét hoặc nhập mã ô.");
        if (string.IsNullOrEmpty(maHang)) return Loi("Quét hoặc nhập mã lô (hoặc mã vạch hàng).");
        if (vm.SLThucTe is not int sl) return Loi("Nhập số lượng thực tế.");
        if (sl is < 0 or > SoLuongToiDa) return Loi($"Số lượng thực tế phải từ 0 đến {SoLuongToiDa:N0}.");

        var phieu = await store.LayPhieu(maPK, ct);
        if (phieu is null) return KetQua.Loi("Không tìm thấy phiếu kiểm.", MaLoiDichVu.KhongTimThay);
        if (!phieu.DangKiem)
            return KetQua.Loi($"Phiếu {phieu.SoPhieu} đang ở trạng thái {phieu.TrangThai} nên không kiểm đếm được nữa.", MaLoiDichVu.SaiTrangThai);

        var dong = await store.LayDong(maPK, ct);

        // ---- Ô: quét sai ô báo ngay ----
        var o = await store.TimO(maVachO, ct);
        if (o is null) return KetQua.Loi($"Không tìm thấy ô có mã '{maVachO}'.", MaLoiDichVu.KhongTimThay);
        if (o.MaKho != phieu.MaKho)
            return Loi($"Quét sai ô: ô '{o.TenViTri}' không thuộc kho {phieu.TenKho} của phiếu {phieu.SoPhieu}.");
        if (!o.LaOLuuTru)
            return Loi($"Quét sai ô: ô '{o.TenViTri}' là khu Tiếp nhận, không thuộc phạm vi kiểm kê.");
        var laMotPhan = phieu.LoaiKiemKe == PhieuKiem.MotPhan;
        if (laMotPhan && dong.All(d => d.MaViTri != o.MaViTri))
            return Loi($"Quét sai ô: ô '{o.TenViTri}' không nằm trong phạm vi của phiếu kiểm một phần {phieu.SoPhieu}.");

        // ---- Hàng: mã lô, hoặc mã vạch hàng khi ô chỉ có một lô của mặt hàng đó ----
        var dongTaiO = dong.Where(d => d.MaViTri == o.MaViTri).ToList();
        var lo = await store.TimLo(maHang, ct);
        if (lo is null)
        {
            if (await store.TimBienThe(maHang, ct) is not int maBT)
                return KetQua.Loi($"Không tìm thấy lô hoặc mặt hàng có mã '{maHang}'.", MaLoiDichVu.KhongTimThay);
            var ungVien = dongTaiO.Where(d => d.MaBT == maBT).ToList();
            if (ungVien.Count == 0)
                return Loi($"Theo số hệ thống, ô '{o.TenViTri}' không có mặt hàng mang mã vạch '{maHang}'. Nếu hàng thật sự nằm ở ô này, hãy quét mã lô để ghi nhận hàng ngoài sổ.");
            if (ungVien.Count > 1)
                return Loi($"Ô '{o.TenViTri}' có {ungVien.Count} lô của mặt hàng này ({string.Join(", ", ungVien.Select(d => d.MaVachLo))}). Hãy quét mã lô.");
            lo = new LoQuet(ungVien[0].MaLo, ungVien[0].MaVachLo, maBT);
        }

        var dongXem = dongTaiO.FirstOrDefault(d => d.MaLo == lo.MaLo);
        DongKiem dongKiem;
        var ngoaiSo = false;
        if (dongXem is not null)
        {
            dongKiem = await store.TimDong(maPK, dongXem.MaDK, ct)
                ?? throw new InvalidOperationException($"Dòng kiểm {dongXem.MaDK} vừa bị xóa.");
            if (dongKiem.DaChot)
                return KetQua.Loi($"Dòng này đã {dongKiem.TrangThaiXuLy.ToLowerInvariant()} nên không đếm lại được.", MaLoiDichVu.SaiTrangThai);
        }
        else
        {
            // Lô không có ở ô này theo số hệ thống: có thể quét nhầm ô, hoặc hàng thật sự nằm sai chỗ / ngoài sổ.
            if (laMotPhan && dong.All(d => d.MaBT != lo.MaBT))
                return Loi($"Lô {lo.MaVachLo} thuộc mặt hàng không nằm trong phạm vi của phiếu kiểm một phần {phieu.SoPhieu}.");
            if (!vm.NgoaiSo)
            {
                var oKhac = dong.Where(d => d.MaLo == lo.MaLo).Select(d => $"'{d.TenO}'").Distinct().ToList();
                var dangO = oKhac.Count > 0 ? $"lô này đang ở ô {string.Join(", ", oKhac)}" : "lô này không có trong phạm vi phiếu";
                return Loi($"Quét sai ô? Theo số hệ thống, ô '{o.TenViTri}' không có lô {lo.MaVachLo} ({dangO}). " +
                           "Nếu hàng thật sự nằm ở ô này, tích \"Hàng ngoài sổ tại ô này\" rồi ghi lại.");
            }
            if (sl == 0) return Loi("Hàng ngoài sổ phải có số lượng thực tế lớn hơn 0.");
            dongKiem = new DongKiem { MaPK = maPK, MaViTri = o.MaViTri, MaLo = lo.MaLo, SLHeThong = 0 };
            store.Them(dongKiem);
            ngoaiSo = true;
        }

        dongKiem.GhiSoDem(sl, maND);
        try
        {
            await store.LuuThayDoi(ct);
        }
        catch (InvalidOperationException ex)
        {
            return KetQua.Loi(ex.Message + " Hãy tải lại trang rồi ghi lại.");
        }

        var chenh = dongKiem.ChenhLech!.Value;
        var ketLuan = chenh == 0 ? "khớp" : chenh > 0 ? $"thừa {chenh}" : $"thiếu {-chenh}";
        await audit.Ghi(maND, "Kiểm đếm", LoaiChungTu.KK, maPK,
            $"{phieu.SoPhieu}: ô {o.TenViTri}, lô {lo.MaVachLo}, hệ thống {dongKiem.SLHeThong}, thực tế {sl}, {ketLuan}{(ngoaiSo ? " (hàng ngoài sổ)" : "")}", ct);
        return KetQua.Ok($"Đã ghi ô '{o.TenViTri}', lô {lo.MaVachLo}: hệ thống {dongKiem.SLHeThong}, thực tế {sl}, {ketLuan}.");
    }

    /// <summary>Bỏ số đếm của một dòng (nhập nhầm): dòng về Chưa kiểm; dòng hàng ngoài sổ (số hệ thống 0) thì xóa hẳn.</summary>
    public async Task<KetQua> BoDem(int maPK, int maDK, int maND, CancellationToken ct = default)
    {
        var phieu = await store.LayPhieu(maPK, ct);
        if (phieu is null) return KetQua.Loi("Không tìm thấy phiếu kiểm.", MaLoiDichVu.KhongTimThay);
        if (!phieu.DangKiem)
            return KetQua.Loi($"Phiếu {phieu.SoPhieu} đang ở trạng thái {phieu.TrangThai} nên không sửa số đếm được nữa.", MaLoiDichVu.SaiTrangThai);

        var dong = await store.TimDong(maPK, maDK, ct);
        if (dong is null) return KetQua.Loi("Không tìm thấy dòng kiểm.", MaLoiDichVu.KhongTimThay);
        if (dong.SLThucTe is null) return KetQua.Ok("Dòng này chưa có số đếm.");
        if (dong.DaChot)
            return KetQua.Loi($"Dòng này đã {dong.TrangThaiXuLy.ToLowerInvariant()} nên không bỏ số đếm được.", MaLoiDichVu.SaiTrangThai);

        var soCu = dong.SLThucTe;
        if (dong.SLHeThong == 0) store.Xoa(dong);
        else dong.BoSoDem();
        try
        {
            await store.LuuThayDoi(ct);
        }
        catch (InvalidOperationException ex)
        {
            return KetQua.Loi(ex.Message);
        }
        await audit.Ghi(maND, "Bỏ số đếm", LoaiChungTu.KK, maPK, $"{phieu.SoPhieu}: dòng {maDK}, số đã đếm {soCu}", ct);
        return KetQua.Ok("Đã bỏ số đếm của dòng.");
    }

    private static KetQua Loi(string thongBao) => KetQua.Loi(thongBao, MaLoiDichVu.DuLieuKhongHopLe);
}
