using Microsoft.Extensions.Caching.Memory;
using QuanLyKho.Web.Areas.HeThong.Models;
using QuanLyKho.Web.Contracts;

namespace QuanLyKho.Web.Areas.HeThong.Services;

/// <summary>Phần quản lý ma trận quyền, chỉ Area HeThong dùng (màn hình Phân quyền). Nhóm khác dùng <see cref="IPhanQuyenService"/>.</summary>
public interface IPhanQuyenQuanLy : IPhanQuyenService
{
    /// <summary>Quyền của một vai trò trên đủ bốn chứng từ theo thứ tự NK, XK, CK, KK; chứng từ chưa có dòng thì mọi quyền là false.</summary>
    Task<IReadOnlyList<PhanQuyen>> LayMaTran(int maVaiTro, CancellationToken ct = default);

    /// <summary>Lưu ma trận của một vai trò (phải đủ bốn chứng từ, mỗi chứng từ một dòng), kiểm ràng buộc, ghi nhật ký và làm mới nhớ tạm.</summary>
    Task<KetQua> Luu(int maVaiTro, IReadOnlyList<PhanQuyen> dong, int maNguoiThucHien, CancellationToken ct = default);
}

public sealed class PhanQuyenService(
    IHeThongStore store,
    INguoiDungHienTai nguoiDungHienTai,
    IAuditService audit,
    IMemoryCache nhoTam) : IPhanQuyenQuanLy
{
    private const string KhoaNhoTam = "hethong:matranquyen";
    public static readonly TimeSpan ThoiGianNhoTam = TimeSpan.FromMinutes(5);

    public async Task<bool> CoQuyen(int maVaiTro, LoaiChungTu loai, QuyenThaoTac quyen, CancellationToken ct = default)
    {
        var maTran = await LayMaTranNhoTam(ct);
        return maTran.TryGetValue((maVaiTro, loai.ToCsdl()), out var p) && p.Co(quyen);
    }

    public async Task<bool> CoQuyenHienTai(LoaiChungTu loai, QuyenThaoTac quyen, CancellationToken ct = default)
    {
        var nguoi = nguoiDungHienTai.Lay();
        return nguoi is not null && await CoQuyen(nguoi.MaVaiTro, loai, quyen, ct);
    }

    public async Task<KetQua> KiemTra(LoaiChungTu loai, QuyenThaoTac quyen, CancellationToken ct = default)
    {
        if (nguoiDungHienTai.Lay() is null)
            return KetQua.Loi("Bạn chưa đăng nhập.", MaLoiPhanQuyen.ChuaDangNhap);
        return await CoQuyenHienTai(loai, quyen, ct)
            ? KetQua.Ok()
            : KetQua.Loi($"Bạn không có quyền {quyen.Ten()} {loai.Ten()}.", MaLoiPhanQuyen.KhongCoQuyen);
    }

    public async Task<IReadOnlyList<PhanQuyen>> LayMaTran(int maVaiTro, CancellationToken ct = default)
    {
        var maTran = await LayMaTranNhoTam(ct);
        return QuyTacPhanQuyen.ThuTuChungTu.Select(c =>
            maTran.TryGetValue((maVaiTro, c.ToCsdl()), out var p)
                ? p.Sao()
                : new PhanQuyen { MaVaiTro = maVaiTro, LoaiChungTu = c.ToCsdl() }).ToList();
    }

    public async Task<KetQua> Luu(int maVaiTro, IReadOnlyList<PhanQuyen> dong, int maNguoiThucHien, CancellationToken ct = default)
    {
        var vaiTro = (await store.LayVaiTro(ct)).FirstOrDefault(v => v.MaVaiTro == maVaiTro);
        if (vaiTro is null)
            return KetQua.Loi("Vai trò không tồn tại.", MaLoiDichVu.KhongTimThay);

        var daCo = dong.Select(d => d.LoaiChungTu).ToList();
        if (dong.Count != QuyTacPhanQuyen.ThuTuChungTu.Count
            || QuyTacPhanQuyen.ThuTuChungTu.Any(c => daCo.Count(x => x == c.ToCsdl()) != 1))
            return KetQua.Loi("Cần gửi đủ bốn loại chứng từ (NK, XK, CK, KK), mỗi loại một dòng.", MaLoiDichVu.DuLieuKhongHopLe);

        foreach (var d in dong)
        {
            var kt = QuyTacPhanQuyen.KiemTra(vaiTro.TenVaiTro, d);
            if (!kt.ThanhCong) return kt;
        }

        var cu = await LayMaTran(maVaiTro, ct);
        await store.ThayPhanQuyenCuaVaiTro(maVaiTro, dong.Select(d => { var s = d.Sao(); s.MaVaiTro = maVaiTro; return s; }).ToList(), ct);
        nhoTam.Remove(KhoaNhoTam);

        var thayDoi = QuyTacPhanQuyen.ThuTuChungTu
            .Select(c => (c, truoc: cu.First(p => p.LoaiChungTu == c.ToCsdl()), sau: dong.First(p => p.LoaiChungTu == c.ToCsdl())))
            .Where(x => MoTa(x.truoc) != MoTa(x.sau))
            .Select(x => $"{x.c}: {MoTa(x.truoc)} -> {MoTa(x.sau)}");
        await audit.Ghi(maNguoiThucHien, "Đổi phân quyền", chiTiet: $"Vai trò {vaiTro.TenVaiTro}. {string.Join("; ", thayDoi)}", ct: ct);
        return KetQua.Ok("Đã lưu phân quyền.");
    }

    private async Task<Dictionary<(int, string), PhanQuyen>> LayMaTranNhoTam(CancellationToken ct)
    {
        if (nhoTam.TryGetValue(KhoaNhoTam, out Dictionary<(int, string), PhanQuyen>? coSan) && coSan is not null) return coSan;
        var tatCa = await store.LayPhanQuyen(ct);
        var moi = tatCa.ToDictionary(p => (p.MaVaiTro, p.LoaiChungTu));
        nhoTam.Set(KhoaNhoTam, moi, ThoiGianNhoTam);
        return moi;
    }

    private static string MoTa(PhanQuyen p)
    {
        var ds = new List<string>();
        if (p.CoXem) ds.Add("Xem");
        if (p.CoThem) ds.Add("Thêm");
        if (p.CoSua) ds.Add("Sửa");
        if (p.CoXoa) ds.Add("Xóa");
        if (p.CoDuyet) ds.Add("Duyệt");
        return ds.Count == 0 ? "(không)" : string.Join(",", ds);
    }
}
