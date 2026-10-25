namespace QuanLyKho.Web.Data;

/// <summary>
/// Nạp dữ liệu mẫu (SeedData) vào các bảng. Nhóm nào có bảng thì viết seeder cho bảng của mình, đọc từ <c>SeedDataSet</c>,
/// và đăng ký trong module Area: <c>services.AddScoped&lt;IDbSeeder, ViTriSeeder&gt;()</c>.
/// Phải chạy lại được nhiều lần (chỉ thêm dòng chưa có). Chạy khi bật Database:SeedOnStartup, theo <see cref="ThuTu"/> tăng dần.
/// </summary>
public interface IDbSeeder
{
    /// <summary>Thứ tự chạy; bảng cha chạy trước bảng con (VaiTro, NguoiDung, Kho, ViTri, DanhMuc, SanPham, BienThe, LoHang, TonKho).</summary>
    int ThuTu { get; }

    Task SeedAsync(KhoDbContext db, CancellationToken ct = default);
}
