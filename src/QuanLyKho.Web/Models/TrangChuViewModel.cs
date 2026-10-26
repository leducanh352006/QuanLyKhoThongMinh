using QuanLyKho.Web.Contracts;
using QuanLyKho.Web.Infrastructure.Database;
using QuanLyKho.Web.Infrastructure.Menu;

namespace QuanLyKho.Web.Models;

public sealed record TomTatDuLieuMau(int Kho, int ViTri, int O, int SanPham, int BienThe, int Lo, int NguoiDung);

public sealed record TrangChuViewModel(
    NguoiDungHienTai? NguoiDung,
    IReadOnlyList<MenuNhom> Nhom,
    DatabaseStatus Csdl,
    string MoiTruong,
    bool LaDevelopment,
    TomTatDuLieuMau DuLieuMau);

public sealed record LoiViewModel(string? MaYeuCau);
