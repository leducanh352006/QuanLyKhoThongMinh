using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Web.Areas.NhapKho.Models;
using QuanLyKho.Web.Data;

namespace QuanLyKho.Tests;

/// <summary>Kiểm thử việc T1-2 (Tiếp nhận và kiểm tra hàng nhập): luật số liệu từng dòng và cấu hình bảng. Không cần SQL Server.</summary>
public class KiemTraHangNhapTests
{
    // Dòng dự kiến 10 cái, thực nhận 10, đạt 8, nên 2 cái không đạt.
    private static ChiTietPhieuNhap Dong(int? thucNhan = 10, int? dat = 8, string? lyDo = null)
        => new() { SLDuKien = 10, SLThucNhan = thucNhan, SLDat = dat, LyDoLech = lyDo };

    [Fact]
    public void DuDungSoLuong_KhongCanLyDo() => Assert.Null(Dong().KiemTraKetQua());

    [Fact]
    public void ChuaNhapThucNhan_BiTuChoi() => Assert.NotNull(Dong(thucNhan: null).KiemTraKetQua());

    [Fact]
    public void ThucNhanAm_BiTuChoi() => Assert.NotNull(Dong(thucNhan: -1, dat: 0, lyDo: "x").KiemTraKetQua());

    [Fact]
    public void ChuaNhapSoDat_BiTuChoi() => Assert.NotNull(Dong(dat: null).KiemTraKetQua());

    [Fact]
    public void SoDatLonHonThucNhan_BiTuChoi() => Assert.Equal("Số đạt phải từ 0 đến số thực nhận.", Dong(dat: 11).KiemTraKetQua());

    [Fact]
    public void SoDatAm_BiTuChoi() => Assert.NotNull(Dong(dat: -1).KiemTraKetQua());

    [Fact]
    public void ThucNhanKhacDuKien_PhaiCoLyDo()
    {
        Assert.Equal("Số thực nhận khác phiếu phải ghi lý do.", Dong(thucNhan: 9, dat: 9).KiemTraKetQua());
        Assert.Equal("Số thực nhận khác phiếu phải ghi lý do.", Dong(thucNhan: 9, dat: 9, lyDo: "   ").KiemTraKetQua());
        Assert.Null(Dong(thucNhan: 9, dat: 9, lyDo: "Thiếu 1 thùng").KiemTraKetQua());
    }

    [Fact]
    public void HangKhongVe_ThucNhanBang0_ChiCanLyDo()
    {
        Assert.Null(Dong(thucNhan: 0, dat: 0, lyDo: "Nhà cung cấp chưa giao").KiemTraKetQua());
        Assert.NotNull(Dong(thucNhan: 0, dat: 0).KiemTraKetQua());
    }

    [Fact]
    public void LyDoQua255KyTu_BiTuChoi() => Assert.Equal("Lý do tối đa 255 ký tự.", Dong(lyDo: new string('a', 256)).KiemTraKetQua());

    [Fact]
    public void TrangThaiDaKiemTra_DaKhoa()
    {
        Assert.Equal("Đã kiểm tra", PhieuNhap.DaKiemTra);
        Assert.True(new PhieuNhap { TrangThai = PhieuNhap.DaKiemTra }.DaKhoa);
    }

    [Fact]
    public void CauHinhBang_CoCotKetQuaVaRangBuoc()
    {
        using var db = new KhoDbContext(new DbContextOptionsBuilder<KhoDbContext>()
            .UseSqlServer("Server=.;Database=KhongKetNoi;Trusted_Connection=True").Options);
        var dong = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ChiTietPhieuNhap))!;

        // MaLo, SLThucNhan, SLDat, LyDoLech cho phép NULL: dòng chưa tiếp nhận thì chưa có kết quả.
        foreach (var cot in new[] { "MaLo", "SLThucNhan", "SLDat", "LyDoLech" })
            Assert.True(dong.FindProperty(cot)!.IsNullable, cot);
        Assert.Equal(255, dong.FindProperty("LyDoLech")!.GetMaxLength());
        Assert.Contains(dong.GetIndexes(), i => i.Properties.Any(c => c.Name == "MaLo"));

        var rangBuoc = dong.GetCheckConstraints().Select(c => c.Name).ToHashSet();
        foreach (var ten in new[] { "CK_ChiTietPhieuNhap_SLThucNhan", "CK_ChiTietPhieuNhap_SLDat", "CK_ChiTietPhieuNhap_LyDoLech" })
            Assert.Contains(ten, rangBuoc);
    }
}
