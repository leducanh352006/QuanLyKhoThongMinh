using QuanLyKho.Web.Areas.HangHoa.Models;
using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HangHoa.Services;

/// <summary>
/// Ghi nhận phát sinh (việc T3-6, Bảng 2.4 dòng 5): mỗi dòng lệch phải được phân loại thành Thiếu hoặc thừa, Hư hỏng hoặc hết hạn,
/// Sai vị trí hoặc Sai mã hàng, kèm nguyên nhân. Chỉ ghi nhận, không đổi tồn: phiếu điều chỉnh và chuyển ô là việc duyệt (T3-7).
/// <para>Quy tắc theo loại: Hư hỏng hoặc hết hạn và Sai vị trí chỉ dành cho dòng thiếu (hàng có trong sổ ở ô này nhưng không còn đủ);
/// Sai vị trí bắt buộc chọn ô đúng là ô Lưu trữ đang hoạt động cùng kho, khác ô hiện tại, đặt vừa số hàng thiếu theo
/// <see cref="ICapacityService.KiemTra"/>; Sai mã hàng bắt buộc chọn mã hàng đúng khác mã của lô.</para>
/// <para>Điều kiện sang bước duyệt: không còn dòng lệch nào chưa phân loại, xem <see cref="KiemTraSanSangDuyet"/> và
/// <see cref="KiemTraDongDuyetDuoc"/> để T3-7 gọi trước khi duyệt.</para>
/// </summary>
public sealed class PhatSinhService(IKiemDemStore store, ICapacityService sucChua, IAuditService audit)
{
    public async Task<PhatSinhTrang?> LayTrang(int maPK, LocPhatSinh loc, PhanLoaiVm? form = null, CancellationToken ct = default)
    {
        var phieu = await store.LayPhieu(maPK, ct);
        if (phieu is null) return null;
        var dongLech = (await store.LayDong(maPK, ct)).Where(d => d.LaDongLech).ToList();
        var oLuuTru = await store.LayOLuuTruHoatDong(phieu.MaKho, ct);
        var bienThe = await store.LayBienTheChon(ct);
        return new PhatSinhTrang(phieu, dongLech, loc, form ?? new PhanLoaiVm(), oLuuTru, bienThe);
    }

    /// <summary>Phân loại một dòng lệch. Thành công thì thông báo nêu loại và nguyên nhân đã ghi.</summary>
    public async Task<KetQua> PhanLoai(int maPK, PhanLoaiVm vm, int maND, CancellationToken ct = default)
    {
        var loai = vm.LoaiPhatSinh?.Trim();
        var nguyenNhan = vm.NguyenNhan?.Trim();
        if (string.IsNullOrEmpty(loai) || !DongKiem.TatCaLoaiPhatSinh.Contains(loai))
            return Loi("Chọn loại phát sinh: Thiếu hoặc thừa, Hư hỏng hoặc hết hạn, Sai vị trí, hoặc Sai mã hàng.");
        if (string.IsNullOrEmpty(nguyenNhan)) return Loi("Nhập nguyên nhân chênh lệch.");
        if (nguyenNhan.Length > DongKiem.NguyenNhanToiDa) return Loi($"Nguyên nhân tối đa {DongKiem.NguyenNhanToiDa} ký tự.");

        var phieu = await store.LayPhieu(maPK, ct);
        if (phieu is null) return KetQua.Loi("Không tìm thấy phiếu kiểm.", MaLoiDichVu.KhongTimThay);
        if (!phieu.DangKiem)
            return KetQua.Loi($"Phiếu {phieu.SoPhieu} đang ở trạng thái {phieu.TrangThai} nên không ghi nhận phát sinh được nữa.", MaLoiDichVu.SaiTrangThai);

        var tatCa = await store.LayDong(maPK, ct);
        var dong = tatCa.FirstOrDefault(d => d.MaDK == vm.MaDK);
        if (dong is null) return KetQua.Loi("Không tìm thấy dòng kiểm trong phiếu này.", MaLoiDichVu.KhongTimThay);
        if (dong.SLThucTe is null) return Loi("Dòng này chưa đếm nên chưa có chênh lệch để ghi nhận. Hãy đếm trước.");
        if (!dong.LaDongLech) return Loi("Dòng này khớp số hệ thống nên không cần ghi nhận phát sinh.");
        if (dong.DaChot)
            return KetQua.Loi($"Dòng này đã {dong.TrangThaiXuLy.ToLowerInvariant()} nên không phân loại lại được.", MaLoiDichVu.SaiTrangThai);

        var chenhLech = dong.ChenhLech!.Value;
        int? maViTriDung = null, maBTDung = null;
        switch (loai)
        {
            case DongKiem.HuHongHoacHetHan:
                if (chenhLech > 0) return Loi("Hư hỏng hoặc hết hạn chỉ dành cho dòng thiếu. Dòng này đang thừa, hãy chọn Thiếu hoặc thừa.");
                break;

            case DongKiem.SaiViTri:
            {
                if (chenhLech > 0) return Loi("Sai vị trí chỉ dành cho dòng thiếu (hàng có trong sổ ở ô này nhưng thực tế nằm ở ô khác). Dòng này đang thừa.");
                var loiO = await KiemTraODung(phieu, tatCa, dong, vm.MaViTriDung, -chenhLech, ct);
                if (loiO is not null) return loiO;
                maViTriDung = vm.MaViTriDung;
                break;
            }

            case DongKiem.SaiMaHang:
            {
                if (vm.MaBTDung is not int maBT || maBT <= 0) return Loi("Chọn mã hàng đúng cho dòng sai mã hàng.");
                if (maBT == dong.MaBT) return Loi("Mã hàng đúng phải khác mã hàng của lô đang kiểm.");
                if (await store.TenBienThe(maBT, ct) is null) return Loi("Mã hàng đúng không tồn tại.");
                maBTDung = maBT;
                break;
            }
        }

        var entity = await store.TimDong(maPK, dong.MaDK, ct) ?? throw new InvalidOperationException($"Dòng kiểm {dong.MaDK} vừa bị xóa.");
        entity.PhanLoai(loai, nguyenNhan, maViTriDung, maBTDung);
        try
        {
            await store.LuuThayDoi(ct);
        }
        catch (InvalidOperationException ex)
        {
            return KetQua.Loi(ex.Message + " Hãy tải lại trang rồi ghi lại.");
        }

        await audit.Ghi(maND, "Ghi nhận phát sinh", LoaiChungTu.KK, maPK,
            $"{phieu.SoPhieu}: ô {dong.TenO}, lô {dong.MaVachLo}, chênh lệch {chenhLech:+#;-#}, {loai}: {nguyenNhan}", ct);
        return KetQua.Ok($"Đã ghi nhận phát sinh '{loai}' cho ô '{dong.TenO}', lô {dong.MaVachLo}.");
    }

    /// <summary>
    /// Phiếu đủ điều kiện sang bước duyệt (T3-7) khi không còn dòng lệch nào chưa phân loại. Trả lỗi nêu số dòng còn thiếu.
    /// </summary>
    public async Task<KetQua> KiemTraSanSangDuyet(int maPK, CancellationToken ct = default)
    {
        var phieu = await store.LayPhieu(maPK, ct);
        if (phieu is null) return KetQua.Loi("Không tìm thấy phiếu kiểm.", MaLoiDichVu.KhongTimThay);
        var chua = (await store.LayDong(maPK, ct)).Where(d => d.LaDongLech && !d.DaPhanLoai).ToList();
        return chua.Count == 0
            ? KetQua.Ok()
            : KetQua.Loi($"Còn {chua.Count} dòng lệch chưa phân loại phát sinh (ô {string.Join(", ", chua.Select(d => d.TenO).Distinct())}), chưa duyệt được.",
                MaLoiDichVu.DuLieuKhongHopLe);
    }

    /// <summary>Một dòng có duyệt được không: dòng khớp hoặc dòng lệch đã phân loại đủ thì được; dòng chưa đếm và dòng lệch chưa phân loại thì không.</summary>
    public static KetQua KiemTraDongDuyetDuoc(DongKiem dong)
    {
        if (dong.SLThucTe is null) return KetQua.Loi("Dòng chưa đếm nên chưa duyệt được.", MaLoiDichVu.SaiTrangThai);
        if (dong.LaDongLech && !dong.DaPhanLoai)
            return KetQua.Loi("Dòng lệch chưa phân loại phát sinh (loại và nguyên nhân) nên chưa duyệt được.", MaLoiDichVu.DuLieuKhongHopLe);
        return KetQua.Ok();
    }

    /// <summary>
    /// Ô đúng phải là ô Lưu trữ đang hoạt động cùng kho, khác ô hiện tại, và đặt vừa <paramref name="soLuong"/> đơn vị của lô (lọt lòng 3 chiều,
    /// thể tích, khối lượng theo <see cref="ICapacityService.KiemTra"/>). Trả lỗi, hoặc null nếu hợp lệ.
    /// <para>Ô đang khóa chính vì phiếu kiểm này (kiểm Toàn bộ khóa mọi ô Lưu trữ của kho; kiểm Một phần khóa các ô trong phiếu) bị
    /// <see cref="ICapacityService.KiemTra"/> từ chối với mã O_BI_KHOA nên không đo được thể tích, khối lượng còn lại. Trường hợp đó vẫn
    /// kiểm lọt lòng 3 chiều ngay ở đây, còn thể tích và khối lượng sẽ được kiểm lúc xử lý chuyển ô (T3-7), khi đã có thể mở ô.</para>
    /// </summary>
    private async Task<KetQua?> KiemTraODung(
        PhieuKiemTomTat phieu, IReadOnlyList<DongKiemXem> tatCa, DongKiemXem dong, int? maViTriDung, int soLuong, CancellationToken ct)
    {
        if (maViTriDung is not int maO || maO <= 0) return Loi("Chọn ô đúng cho dòng sai vị trí.");
        if (maO == dong.MaViTri) return Loi("Ô đúng phải khác ô đang kiểm.");

        var o = await store.TimViTri(maO, ct);
        if (o is null) return Loi("Ô đúng không tồn tại.");
        if (o.MaKho != phieu.MaKho) return Loi($"Ô '{o.TenViTri}' không thuộc kho {phieu.TenKho} của phiếu {phieu.SoPhieu}.");
        if (!o.LaOLuuTru) return Loi($"Ô '{o.TenViTri}' không phải ô Lưu trữ nên không dùng làm ô đúng được.");
        if (!o.DangHoatDong) return Loi($"Ô '{o.TenViTri}' đã ngừng hoạt động.");

        var kq = await sucChua.KiemTra(maO, dong.MaBT, soLuong, ct);
        if (kq.Du) return null;

        var khoaBoiPhieuNay = o.DangKhoa && (phieu.LoaiKiemKe == PhieuKiem.ToanBo || tatCa.Any(d => d.MaViTri == maO));
        if (kq.MaLoi != MaLoiDichVu.OBiKhoa || !khoaBoiPhieuNay)
            return KetQua.Loi(kq.LyDo ?? $"Ô '{o.TenViTri}' không đủ chỗ cho {soLuong} đơn vị.", kq.MaLoi ?? MaLoiDichVu.VuotSucChua);

        var kt = await store.KichThuoc(dong.MaBT, ct);
        if (kt is not null && !QuyTacSucChua.VuaKichThuoc3Chieu(kt.Dai, kt.Rong, kt.Cao, o.Dai, o.Rong, o.Cao))
            return KetQua.Loi($"Kích thước hàng ({Canh(kt.Dai)}×{Canh(kt.Rong)}×{Canh(kt.Cao)} cm) vượt lòng ô '{o.TenViTri}' " +
                              $"({Canh(o.Dai ?? 0)}×{Canh(o.Rong ?? 0)}×{Canh(o.Cao ?? 0)} cm), không đặt vừa.", MaLoiDichVu.VuotSucChua);
        return null;
    }

    private static string Canh(decimal v) => v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');

    private static KetQua Loi(string thongBao) => KetQua.Loi(thongBao, MaLoiDichVu.DuLieuKhongHopLe);
}
