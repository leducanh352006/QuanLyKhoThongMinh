namespace QuanLyKho.Web.SeedData;

/// <summary>
/// Dữ liệu mẫu CHUNG của cả nhóm (việc T4-1): 2 kho, cây vị trí và ô có sức chứa, sản phẩm, biến thể, lô, tồn mẫu và 4 người dùng ứng với 4 vai trò.
/// <para>Đây là nguồn duy nhất: bản giả trong Contracts đọc từ đây, và seeder nạp vào SQL Server (xem README, mục "Nạp dữ liệu mẫu") cũng đọc từ đây.
/// Giữ nguyên mã (MaKho, MaViTri...) để các nhóm cùng nói về một bộ dữ liệu khi demo ghép.</para>
/// <para>Số liệu được chọn để demo được các tình huống của Bảng 1.7: ghi vượt tồn bị từ chối, ô nhỏ hết chỗ,
/// hai lô cùng biến thể khác hạn dùng (chọn lô hết hạn trước), kho quầy sắp hết hàng (gợi ý G3, G4).</para>
/// </summary>
public static class SeedDataSet
{
    // Ô tiếp nhận và ô lưu trữ dùng đúng chuỗi ở Bảng 2.16.
    public const string CapO = "Ô";
    public const string OLuuTru = "Lưu trữ";
    public const string OTiepNhan = "Tiếp nhận";

    /// <summary>Thứ tự cấp vị trí từ cao xuống thấp (Bảng 2.16); cấp con phải thấp hơn cấp cha.</summary>
    public static readonly IReadOnlyList<string> ThuTuCapViTri = new[] { "Tòa", "Tầng", "Phòng", "Dãy", "Hàng", "Kệ", "Ô" };

    public static readonly IReadOnlyList<VaiTroSeed> VaiTro = new VaiTroSeed[]
    {
        new(1, "Quản trị viên"),
        new(2, "Quản lý kho"),
        new(3, "Thủ kho"),
        new(4, "Nhân viên bán hàng"),
    };

    public static readonly IReadOnlyList<NguoiDungSeed> NguoiDung = new NguoiDungSeed[]
    {
        new(1, "quantri", "Quản trị viên (mẫu)", "quantri@example.com", 1),
        new(2, "quanlykho", "Quản lý kho (mẫu)", "quanlykho@example.com", 2),
        new(3, "thukho", "Thủ kho (mẫu)", "thukho@example.com", 3),
        new(4, "nhanvienbh", "Nhân viên bán hàng (mẫu)", "nhanvienbh@example.com", 4),
    };

    public static readonly IReadOnlyList<KhoSeed> Kho = new KhoSeed[]
    {
        new(1, "Kho tổng", "Kho lưu trữ", "Hà Nội (dữ liệu mẫu)", MaQuanLy: 2, ThoiGianCho: 3),
        new(2, "Kho quầy cửa hàng", "Kho bán hàng", "Hà Nội (dữ liệu mẫu)", MaQuanLy: 2, ThoiGianCho: 1),
    };

    public static readonly IReadOnlyList<ViTriSeed> ViTri = new ViTriSeed[]
    {
        // ---- Kho 1: Dãy A > Kệ A1 > 2 ô; Kệ A2 > 1 ô nhỏ; khu tiếp nhận ----
        new(1, "Dãy A", 1, null, "Dãy"),
        new(2, "Kệ A1", 1, 1, "Kệ"),
        new(3, "Ô A1-01", 1, 2, CapO, OLuuTru, "O-K1-A1-01", 100, 60, 50, 120),
        new(4, "Ô A1-02", 1, 2, CapO, OLuuTru, "O-K1-A1-02", 100, 60, 50, 120),
        new(5, "Kệ A2", 1, 1, "Kệ"),
        new(6, "Ô A2-01", 1, 5, CapO, OLuuTru, "O-K1-A2-01", 60, 40, 40, 20),   // ô nhỏ, nhẹ: dễ demo vượt sức chứa
        new(7, "Khu tiếp nhận", 1, null, CapO, OTiepNhan, "O-K1-TN"),            // không giới hạn kích thước
        // ---- Kho 2 ----
        new(8, "Dãy A", 2, null, "Dãy"),
        new(9, "Kệ A1", 2, 8, "Kệ"),
        new(10, "Ô A1-01", 2, 9, CapO, OLuuTru, "O-K2-A1-01", 80, 50, 40, 50),
        new(11, "Ô A1-02", 2, 9, CapO, OLuuTru, "O-K2-A1-02", 80, 50, 40, 50),
        new(12, "Khu tiếp nhận", 2, null, CapO, OTiepNhan, "O-K2-TN"),
    };

    public static readonly IReadOnlyList<DanhMucSeed> DanhMuc = new DanhMucSeed[]
    {
        new(1, "Thời trang và phụ kiện"),
        new(2, "Mỹ phẩm"),
        new(3, "Điện tử, điện máy"),
    };

    public static readonly IReadOnlyList<NhaCungCapSeed> NhaCungCap = new NhaCungCapSeed[]
    {
        new(1, "Công ty TNHH Dệt may Phương Nam", "0901234567", "TP. Hồ Chí Minh (dữ liệu mẫu)"),
        new(2, "Công ty CP Mỹ phẩm Hương Việt", "0912345678", "Hà Nội (dữ liệu mẫu)"),
        new(3, "Công ty TNHH Điện tử Thành Đạt", "0923456789", "Đà Nẵng (dữ liệu mẫu)"),
    };

    public static readonly IReadOnlyList<SanPhamSeed> SanPham = new SanPhamSeed[]
    {
        new(1, "Áo thun cổ tròn", 1, "cái", CoHanDung: false),
        new(2, "Kem chống nắng SPF50", 2, "tuýp", CoHanDung: true),
        new(3, "Tai nghe Bluetooth", 3, "cái", CoHanDung: false),
    };

    // Mã vạch là dữ liệu mẫu, không phải mã GS1 thật.
    public static readonly IReadOnlyList<BienTheSeed> BienThe = new BienTheSeed[]
    {
        new(1, 1, "8930000000011", "Size M, trắng", 30, 25, 3, 0.20m),
        new(2, 1, "8930000000028", "Size L, đen", 30, 25, 3, 0.22m),
        new(3, 2, "8930000000035", "50 ml", 12, 4, 4, 0.08m),
        new(4, 3, "8930000000042", "Màu đen", 15, 12, 6, 0.25m),
    };

    public static readonly IReadOnlyList<LoHangSeed> LoHang = new LoHangSeed[]
    {
        new(1, "LO-2610-001", 1, null),
        new(2, "LO-2610-002", 2, null),
        new(3, "LO-2609-001", 3, new DateOnly(2027, 3, 31)),   // hết hạn sớm hơn: G1 phải lấy lô này trước
        new(4, "LO-2610-003", 3, new DateOnly(2027, 9, 30)),
        new(5, "LO-2610-004", 4, null),
    };

    public static readonly IReadOnlyList<TonKhoSeed> TonKho = new TonKhoSeed[]
    {
        new(1, 3, 50, 0),     // áo M, Kho 1 ô A1-01
        new(3, 3, 24, 0),     // kem lô hết hạn sớm, Kho 1 ô A1-01
        new(2, 4, 40, 10),    // áo L, Kho 1 ô A1-02 (10 đang giữ chỗ cho phiếu xuất đã duyệt)
        new(4, 4, 36, 0),     // kem lô hết hạn muộn, Kho 1 ô A1-02
        new(5, 6, 60, 0),     // tai nghe, Kho 1 ô A2-01 (ô nhỏ: còn nhận thêm tối đa 20 cái vì giới hạn khối lượng)
        new(1, 10, 15, 0),    // áo M, Kho 2 ô A1-01
        new(5, 11, 5, 0),     // tai nghe, Kho 2 ô A1-02 (sắp hết: dùng demo G3, G4)
    };

    public static ViTriSeed? TimViTri(int maViTri) => ViTri.FirstOrDefault(v => v.MaViTri == maViTri);

    public static BienTheSeed? TimBienThe(int maBT) => BienThe.FirstOrDefault(b => b.MaBT == maBT);

    public static LoHangSeed? TimLo(int maLo) => LoHang.FirstOrDefault(l => l.MaLo == maLo);

    /// <summary>Thể tích một ô lưu trữ (cm³); null nếu không phải ô lưu trữ.</summary>
    public static decimal? TheTichO(ViTriSeed v) => v.Dai * v.Rong * v.Cao;

    /// <summary>Thể tích một đơn vị biến thể (cm³).</summary>
    public static decimal TheTichBienThe(BienTheSeed b) => b.Dai * b.Rong * b.Cao;
}
