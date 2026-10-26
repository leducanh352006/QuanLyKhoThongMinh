using QuanLyKho.Web.SeedData;

namespace QuanLyKho.Tests;

/// <summary>SeedData phải tuân thủ ràng buộc của CSDL (Bảng 2.17, 2.20, 2.21) để nạp vào SQL Server không bị từ chối.</summary>
public class SeedDataTests
{
    [Fact]
    public void CoDuHaiKho_BonNguoiDung_BonVaiTro()
    {
        Assert.Equal(2, SeedDataSet.Kho.Count);
        Assert.Equal(4, SeedDataSet.NguoiDung.Count);
        Assert.Equal(4, SeedDataSet.VaiTro.Count);
        Assert.NotEmpty(SeedDataSet.SanPham);
        Assert.NotEmpty(SeedDataSet.BienThe);
        Assert.NotEmpty(SeedDataSet.LoHang);
    }

    [Fact]
    public void BonNguoiDung_MoiNguoiMotVaiTroKhacNhau()
    {
        var vaiTro = SeedDataSet.NguoiDung.Select(n => n.MaVaiTro).ToList();
        Assert.Equal(4, vaiTro.Distinct().Count());
        foreach (var n in SeedDataSet.NguoiDung)
            Assert.Contains(SeedDataSet.VaiTro, v => v.MaVaiTro == n.MaVaiTro);
    }

    [Fact]
    public void TenVaiTro_KhopHangSoTrongContracts()
    {
        var ten = SeedDataSet.VaiTro.Select(v => v.TenVaiTro).OrderBy(x => x).ToList();
        var mongDoi = QuanLyKho.Web.Contracts.VaiTroTen.TatCa.OrderBy(x => x).ToList();
        Assert.Equal(mongDoi, ten);
    }

    [Fact]
    public void QuanLyKho_LaNguoiDungCoVaiTroQuanLyKho()
    {
        var maVaiTroQl = SeedDataSet.VaiTro.Single(v => v.TenVaiTro == "Quản lý kho").MaVaiTro;
        foreach (var k in SeedDataSet.Kho)
        {
            var nd = SeedDataSet.NguoiDung.Single(n => n.MaND == k.MaQuanLy);
            Assert.Equal(maVaiTroQl, nd.MaVaiTro);
        }
    }

    [Fact]
    public void ViTri_TuanThuRangBuocCsdl()
    {
        var thuTu = SeedDataSet.ThuTuCapViTri.ToList();
        foreach (var v in SeedDataSet.ViTri)
        {
            Assert.Contains(SeedDataSet.Kho, k => k.MaKho == v.MaKho);
            Assert.Contains(v.CapViTri, thuTu);

            if (v.MaViTriCha is int cha)
            {
                var vCha = SeedDataSet.ViTri.Single(x => x.MaViTri == cha);
                Assert.Equal(vCha.MaKho, v.MaKho);                                           // cấp con cùng kho với cấp cha
                Assert.True(thuTu.IndexOf(v.CapViTri) > thuTu.IndexOf(vCha.CapViTri));       // cấp con thấp hơn cấp cha
            }

            if (v.CapViTri == SeedDataSet.CapO)
            {
                Assert.False(string.IsNullOrEmpty(v.LoaiViTri));
                Assert.False(string.IsNullOrEmpty(v.MaVachO));
                if (v.LoaiViTri == SeedDataSet.OLuuTru)
                {
                    Assert.True(v.Dai > 0 && v.Rong > 0 && v.Cao > 0 && v.KhoiLuongToiDa > 0);
                }
                else
                {
                    Assert.Equal(SeedDataSet.OTiepNhan, v.LoaiViTri);
                    Assert.Null(v.Dai); Assert.Null(v.Rong); Assert.Null(v.Cao); Assert.Null(v.KhoiLuongToiDa);
                }
            }
            else
            {
                Assert.Null(v.LoaiViTri); Assert.Null(v.MaVachO);
                Assert.Null(v.Dai); Assert.Null(v.KhoiLuongToiDa);
            }
        }
    }

    [Fact]
    public void ViTri_MaVachODuyNhat_VaTenKhongTrungCungCha()
    {
        var maVach = SeedDataSet.ViTri.Where(v => v.MaVachO != null).Select(v => v.MaVachO).ToList();
        Assert.Equal(maVach.Count, maVach.Distinct().Count());

        var khoaTen = SeedDataSet.ViTri.Select(v => (v.MaKho, v.MaViTriCha, v.TenViTri)).ToList();
        Assert.Equal(khoaTen.Count, khoaTen.Distinct().Count());
    }

    [Fact]
    public void MoiKho_CoDungMotKhuTiepNhan_VaCoOLuuTru()
    {
        foreach (var k in SeedDataSet.Kho)
        {
            Assert.Equal(1, SeedDataSet.ViTri.Count(v => v.MaKho == k.MaKho && v.LoaiViTri == SeedDataSet.OTiepNhan));
            Assert.True(SeedDataSet.ViTri.Any(v => v.MaKho == k.MaKho && v.LoaiViTri == SeedDataSet.OLuuTru));
        }
    }

    [Fact]
    public void BienThe_LoHang_ThamChieuDungVaCoHanDungThiLoCoHanDung()
    {
        foreach (var sp in SeedDataSet.SanPham) Assert.Contains(SeedDataSet.DanhMuc, d => d.MaDM == sp.MaDM);
        foreach (var bt in SeedDataSet.BienThe)
        {
            Assert.Contains(SeedDataSet.SanPham, s => s.MaSP == bt.MaSP);
            Assert.True(bt.Dai > 0 && bt.Rong > 0 && bt.Cao > 0 && bt.KhoiLuong > 0);
        }

        var maVach = SeedDataSet.BienThe.Select(b => b.MaVach).Concat(SeedDataSet.LoHang.Select(l => l.MaVachLo)).ToList();
        Assert.Equal(maVach.Count, maVach.Distinct().Count());

        foreach (var lo in SeedDataSet.LoHang)
        {
            var bt = SeedDataSet.BienThe.Single(b => b.MaBT == lo.MaBT);
            var sp = SeedDataSet.SanPham.Single(s => s.MaSP == bt.MaSP);
            if (sp.CoHanDung) Assert.NotNull(lo.HanDung);                                     // Bảng 2.17 dòng 10
        }
    }

    [Fact]
    public void CoHaiLoCungBienTheKhacHanDung_DeDemoChonLoHetHanTruoc()
    {
        var hanDung = SeedDataSet.LoHang.Where(l => l.MaBT == 3).Select(l => l.HanDung).ToList();
        Assert.Equal(2, hanDung.Count);
        Assert.Equal(2, hanDung.Distinct().Count());
    }

    [Fact]
    public void TonKho_ThamChieuDung_KhongAm_GiuKhongVuotTon_ChiONo()
    {
        var khoa = SeedDataSet.TonKho.Select(t => (t.MaLo, t.MaViTri)).ToList();
        Assert.Equal(khoa.Count, khoa.Distinct().Count());                                    // PK (MaLo, MaViTri)

        foreach (var t in SeedDataSet.TonKho)
        {
            Assert.NotNull(SeedDataSet.TimLo(t.MaLo));
            var vt = SeedDataSet.TimViTri(t.MaViTri);
            Assert.NotNull(vt);
            Assert.Equal(SeedDataSet.CapO, vt!.CapViTri);                                     // hàng chỉ nằm ở ô
            Assert.True(t.SoLuongTon >= 0 && t.SoLuongGiu >= 0);
            Assert.True(t.SoLuongGiu <= t.SoLuongTon);                                        // CHECK Bảng 2.21
        }
    }

    [Fact]
    public void TonKhoMau_KhongVuotSucChuaCuaO()
    {
        foreach (var nhom in SeedDataSet.TonKho.GroupBy(t => t.MaViTri))
        {
            var o = SeedDataSet.TimViTri(nhom.Key)!;
            decimal theTich = 0, khoi = 0;
            foreach (var t in nhom)
            {
                var bt = SeedDataSet.TimBienThe(SeedDataSet.TimLo(t.MaLo)!.MaBT)!;
                theTich += t.SoLuongTon * SeedDataSet.TheTichBienThe(bt);
                khoi += t.SoLuongTon * bt.KhoiLuong;
            }
            Assert.True(theTich <= SeedDataSet.TheTichO(o), $"Ô {o.TenViTri} vượt thể tích");
            Assert.True(khoi <= o.KhoiLuongToiDa, $"Ô {o.TenViTri} vượt khối lượng");
        }
    }

    [Fact]
    public void TonKhoMau_KhongCoTonOKhuTiepNhan()
    {
        // Không có tồn ở ô Tiếp nhận: hàng chờ kiểm không được tính vào tồn mẫu khả dụng.
        foreach (var t in SeedDataSet.TonKho)
            Assert.NotEqual(SeedDataSet.OTiepNhan, SeedDataSet.TimViTri(t.MaViTri)!.LoaiViTri);
    }
}
