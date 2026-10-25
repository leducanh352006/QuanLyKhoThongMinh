namespace QuanLyKho.Web.Contracts;

/// <summary>
/// Dịch vụ tồn kho và sổ cái do Nhóm 1 (TV4) sở hữu. MỌI thay đổi tồn đi qua đây; không nhóm nào ghi thẳng vào TonKho.
/// <para>Quy ước chung cho bản thật:
/// mỗi lời gọi là một giao dịch nguyên tử (cập nhật TonKho và ghi BienDongTon cùng lúc); nếu nơi gọi đã mở giao dịch
/// trên cùng KhoDbContext thì tham gia giao dịch đó. Không bao giờ để SoLuongTon hoặc SoLuongGiu âm,
/// và luôn giữ SoLuongGiu nhỏ hơn hoặc bằng SoLuongTon (Bảng 2.21).</para>
/// </summary>
public interface IStockService
{
    /// <summary>Giữ chỗ khi duyệt phiếu: tăng SoLuongGiu. Từ chối nếu vượt tồn khả dụng (MaLoiDichVu.VuotTon). Không ghi sổ cái vì tồn thực không đổi.</summary>
    Task<KetQua> GiuCho(DongTon dong, CancellationToken ct = default);

    /// <summary>Trả chỗ khi hủy phiếu: giảm SoLuongGiu. Từ chối nếu trả nhiều hơn đang giữ.</summary>
    Task<KetQua> TraCho(DongTon dong, CancellationToken ct = default);

    /// <summary>Ghi tăng tồn (nhận hàng, sắp xếp, nhận chuyển, hoàn tác...): tăng SoLuongTon và ghi một dòng sổ cái dương.</summary>
    Task<KetQua> GhiTang(DongTon dong, LoaiBienDong loai, ThamChieuChungTu chungTu, int maND, CancellationToken ct = default);

    /// <summary>
    /// Ghi giảm tồn (xuất kho, chuyển đi...): giảm SoLuongTon và ghi một dòng sổ cái âm.
    /// <paramref name="tuGiuCho"/> = true khi hàng đã được giữ chỗ cho chính phiếu này: giảm cả SoLuongGiu.
    /// Nếu false, chỉ được giảm phần khả dụng.
    /// </summary>
    Task<KetQua> GhiGiam(DongTon dong, LoaiBienDong loai, ThamChieuChungTu chungTu, int maND, bool tuGiuCho = false, CancellationToken ct = default);

    /// <summary>Chuyển một lô từ ô này sang ô khác: tồn tổng không đổi, ghi hai dòng sổ cái (âm ở ô nguồn, dương ở ô đích) loại DoiO.</summary>
    Task<KetQua> ChuyenO(int maLo, int maViTriNguon, int maViTriDich, int soLuong, ThamChieuChungTu chungTu, int maND, CancellationToken ct = default);

    /// <summary>Tồn của một lô tại một ô; null nếu chưa từng có.</summary>
    Task<TonKhoDto?> LayTon(int maLo, int maViTri, CancellationToken ct = default);

    /// <summary>Mọi lô đang có tại một ô.</summary>
    Task<IReadOnlyList<TonKhoDto>> LayTonTheoViTri(int maViTri, CancellationToken ct = default);

    /// <summary>Đối chiếu TonKho với tồn tính lại từ sổ cái; danh sách rỗng nghĩa là khớp.</summary>
    Task<IReadOnlyList<ChenhLechSoCai>> DoiChieuSoCai(CancellationToken ct = default);
}
