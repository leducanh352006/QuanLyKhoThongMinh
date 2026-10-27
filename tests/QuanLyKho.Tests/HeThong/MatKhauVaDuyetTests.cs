using QuanLyKho.Web.Areas.HeThong.Services;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Tests.HeThong;

public class MatKhauServiceTests
{
    private readonly MatKhauService _mk = new();

    [Fact]
    public void Bam_KhongChuaMatKhauGoc_VaVuaCot255()
    {
        var hash = _mk.Bam("Demo@12345");
        Assert.NotEqual("Demo@12345", hash);
        Assert.DoesNotContain("Demo@12345", hash);
        Assert.True(hash.Length <= 255, $"Bản băm dài {hash.Length} ký tự, vượt cột nvarchar(255)");
    }

    [Fact]
    public void Bam_HaiLanCungMatKhau_RaHaiBanKhacNhau_NhoMuoi()
    {
        Assert.NotEqual(_mk.Bam("Demo@12345"), _mk.Bam("Demo@12345"));
    }

    [Fact]
    public void XacThuc_DungThiTrueSaiThiFalse()
    {
        var hash = _mk.Bam("Demo@12345");
        Assert.True(_mk.XacThuc(hash, "Demo@12345", out _));
        Assert.False(_mk.XacThuc(hash, "demo@12345", out _));
        Assert.False(_mk.XacThuc(hash, "", out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("khong-phai-ban-bam!!!")]
    public void XacThuc_BanBamRongHoacHong_LaSaiVaKhongNem(string hash)
    {
        Assert.False(_mk.XacThuc(hash, "Demo@12345", out _));
        Assert.False(_mk.XacThuc(null, "Demo@12345", out _));
    }

    [Fact]
    public void BamGia_KhongBaoGioKhopMatKhauNaoDuocThu()
    {
        Assert.False(_mk.XacThuc(_mk.BamGia(), "Demo@12345", out _));
        Assert.Equal(_mk.BamGia(), _mk.BamGia());
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("abc12", false)]          // quá ngắn
    [InlineData("abcdefgh", false)]       // thiếu số
    [InlineData("12345678", false)]       // thiếu chữ
    [InlineData("Demo@12345", true)]
    [InlineData("matkhau99", true)]
    public void KiemTraDoManh_TheoQuyTac(string? matKhau, bool hopLe)
    {
        Assert.Equal(hopLe, _mk.KiemTraDoManh(matKhau).ThanhCong);
    }

    [Fact]
    public void KiemTraDoManh_KhongDuocTrungTenDangNhap()
    {
        Assert.False(_mk.KiemTraDoManh("quantri123", "quantri123").ThanhCong);
        Assert.True(_mk.KiemTraDoManh("quantri123", "quantri").ThanhCong);
    }

    [Fact]
    public void KiemTraDoManh_QuaDai_BiTuChoi()
    {
        Assert.False(_mk.KiemTraDoManh("a1" + new string('x', MatKhauService.DoDaiToiDa)).ThanhCong);
    }
}

public class QuyTacDuyetTests
{
    [Fact]
    public void NguoiLapTuDuyet_BiTuChoi_KemMaLoiVaThongBaoTiengViet()
    {
        var kq = QuyTacDuyet.KiemTra(maNguoiDuyet: 3, maNguoiLapHoacDem: 3, "phiếu xuất");
        Assert.False(kq.ThanhCong);
        Assert.Equal(MaLoiPhanQuyen.TuDuyet, kq.MaLoi);
        Assert.Contains("phiếu xuất", kq.ThongBao);
    }

    [Fact]
    public void NguoiKhacDuyet_DuocPhep()
    {
        Assert.True(QuyTacDuyet.KiemTra(2, 3).ThanhCong);
        Assert.True(QuyTacDuyet.DuocDuyet(2, 3));
    }

    [Fact]
    public void ChuaCoNguoiLap_KhongChan()
    {
        Assert.True(QuyTacDuyet.KiemTra(2, null).ThanhCong);
    }

    [Fact]
    public void NguoiDemTuDuyetDongKiem_BiTuChoi()
    {
        Assert.False(QuyTacDuyet.DuocDuyet(maNguoiDuyet: 3, maNguoiLapHoacDem: 3));
    }
}

public class ChinhSachQuyenTests
{
    [Fact]
    public void TenChinhSach_QuaLaiDuocVoiMoiToHop()
    {
        foreach (var loai in Enum.GetValues<LoaiChungTu>())
            foreach (var quyen in Enum.GetValues<QuyenThaoTac>())
            {
                var ten = ChinhSachQuyen.Ten(loai, quyen);
                Assert.True(ChinhSachQuyen.TryParse(ten, out var l, out var q), ten);
                Assert.Equal(loai, l);
                Assert.Equal(quyen, q);
            }
    }

    [Fact]
    public void TenNull_KhongParseDuoc()
    {
        Assert.False(ChinhSachQuyen.TryParse(null, out _, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Admin")]
    [InlineData("quyen:XK")]
    [InlineData("quyen:XK:Duyet:them")]
    [InlineData("quyen:ZZ:Duyet")]
    [InlineData("quyen:XK:Xoa-het")]
    [InlineData("quyen:xk:duyet")]       // phân biệt hoa thường để tên chính sách chỉ có một dạng
    [InlineData("quyen:9:0")]
    public void TenKhongHopLe_KhongParseDuoc(string ten)
    {
        Assert.False(ChinhSachQuyen.TryParse(ten, out _, out _));
    }

    [Fact]
    public void ThuocTinhCanQuyen_DatDungTenChinhSach()
    {
        var a = new CanQuyenAttribute(LoaiChungTu.KK, QuyenThaoTac.Duyet);
        Assert.Equal("quyen:KK:Duyet", a.Policy);
        Assert.Equal(LoaiChungTu.KK, a.Loai);
        Assert.Equal(QuyenThaoTac.Duyet, a.Quyen);
    }
}
