-- Script tạo CSDL QuanLyKho từ các migration (chạy được nhiều lần, chỉ áp phần còn thiếu).
-- Tạo database rỗng trước: CREATE DATABASE QuanLyKho;  rồi chạy file này trên database đó.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026113217_HeThongNguoiDungPhanQuyen'
)
BEGIN
    CREATE TABLE [VaiTro] (
        [MaVaiTro] int NOT NULL IDENTITY,
        [TenVaiTro] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_VaiTro] PRIMARY KEY ([MaVaiTro])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026113217_HeThongNguoiDungPhanQuyen'
)
BEGIN
    CREATE TABLE [NguoiDung] (
        [MaND] int NOT NULL IDENTITY,
        [TenDangNhap] varchar(50) NOT NULL,
        [MatKhauHash] nvarchar(255) NOT NULL,
        [HoTen] nvarchar(100) NOT NULL,
        [Email] varchar(100) NULL,
        [SDT] varchar(15) NULL,
        [MaVaiTro] int NOT NULL,
        [TrangThai] nvarchar(20) NOT NULL DEFAULT N'Hoạt động',
        CONSTRAINT [PK_NguoiDung] PRIMARY KEY ([MaND]),
        CONSTRAINT [CK_NguoiDung_TrangThai] CHECK ([TrangThai] IN (N'Hoạt động', N'Ngừng hoạt động')),
        CONSTRAINT [FK_NguoiDung_VaiTro_MaVaiTro] FOREIGN KEY ([MaVaiTro]) REFERENCES [VaiTro] ([MaVaiTro]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026113217_HeThongNguoiDungPhanQuyen'
)
BEGIN
    CREATE TABLE [PhanQuyen] (
        [MaVaiTro] int NOT NULL,
        [LoaiChungTu] char(2) NOT NULL,
        [CoXem] bit NOT NULL DEFAULT CAST(0 AS bit),
        [CoThem] bit NOT NULL DEFAULT CAST(0 AS bit),
        [CoSua] bit NOT NULL DEFAULT CAST(0 AS bit),
        [CoXoa] bit NOT NULL DEFAULT CAST(0 AS bit),
        [CoDuyet] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_PhanQuyen] PRIMARY KEY ([MaVaiTro], [LoaiChungTu]),
        CONSTRAINT [CK_PhanQuyen_LoaiChungTu] CHECK ([LoaiChungTu] IN ('NK', 'XK', 'CK', 'KK')),
        CONSTRAINT [FK_PhanQuyen_VaiTro_MaVaiTro] FOREIGN KEY ([MaVaiTro]) REFERENCES [VaiTro] ([MaVaiTro]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026113217_HeThongNguoiDungPhanQuyen'
)
BEGIN
    CREATE INDEX [IX_NguoiDung_MaVaiTro] ON [NguoiDung] ([MaVaiTro]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026113217_HeThongNguoiDungPhanQuyen'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NguoiDung_TenDangNhap] ON [NguoiDung] ([TenDangNhap]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026113217_HeThongNguoiDungPhanQuyen'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VaiTro_TenVaiTro] ON [VaiTro] ([TenVaiTro]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261026113217_HeThongNguoiDungPhanQuyen'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261026113217_HeThongNguoiDungPhanQuyen', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027153902_KhoViTri'
)
BEGIN
    CREATE TABLE [Kho] (
        [MaKho] int NOT NULL IDENTITY,
        [TenKho] nvarchar(100) NOT NULL,
        [LoaiKho] nvarchar(20) NOT NULL,
        [DiaChi] nvarchar(255) NULL,
        [MaQuanLy] int NOT NULL,
        [ThoiGianCho] int NOT NULL DEFAULT 0,
        [TrangThai] nvarchar(20) NOT NULL DEFAULT N'Hoạt động',
        CONSTRAINT [PK_Kho] PRIMARY KEY ([MaKho]),
        CONSTRAINT [CK_Kho_LoaiKho] CHECK ([LoaiKho] IN (N'Kho bán hàng', N'Kho lưu trữ')),
        CONSTRAINT [CK_Kho_ThoiGianCho] CHECK ([ThoiGianCho] >= 0),
        CONSTRAINT [CK_Kho_TrangThai] CHECK ([TrangThai] IN (N'Hoạt động', N'Ngừng hoạt động')),
        CONSTRAINT [FK_Kho_NguoiDung_MaQuanLy] FOREIGN KEY ([MaQuanLy]) REFERENCES [NguoiDung] ([MaND]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027153902_KhoViTri'
)
BEGIN
    CREATE TABLE [ViTri] (
        [MaViTri] int NOT NULL IDENTITY,
        [TenViTri] nvarchar(50) NOT NULL,
        [MaKho] int NOT NULL,
        [MaViTriCha] int NULL,
        [CapViTri] nvarchar(10) NOT NULL,
        [LoaiViTri] nvarchar(15) NULL,
        [MaVachO] varchar(50) NULL,
        [Dai] decimal(8,2) NULL,
        [Rong] decimal(8,2) NULL,
        [Cao] decimal(8,2) NULL,
        [KhoiLuongToiDa] decimal(10,3) NULL,
        [DangKhoa] bit NOT NULL DEFAULT CAST(0 AS bit),
        [TrangThai] nvarchar(20) NOT NULL DEFAULT N'Hoạt động',
        CONSTRAINT [PK_ViTri] PRIMARY KEY ([MaViTri]),
        CONSTRAINT [AK_ViTri_MaViTri_MaKho] UNIQUE ([MaViTri], [MaKho]),
        CONSTRAINT [CK_ViTri_CapViTri] CHECK ([CapViTri] IN (N'Tòa', N'Tầng', N'Phòng', N'Dãy', N'Hàng', N'Kệ', N'Ô')),
        CONSTRAINT [CK_ViTri_ChiOCoLoaiVaMaVach] CHECK (([CapViTri] = N'Ô' AND [LoaiViTri] IS NOT NULL AND [MaVachO] IS NOT NULL) OR ([CapViTri] <> N'Ô' AND [LoaiViTri] IS NULL AND [MaVachO] IS NULL)),
        CONSTRAINT [CK_ViTri_KichThuoc] CHECK (([CapViTri] = N'Ô' AND [LoaiViTri] = N'Lưu trữ' AND [Dai] > 0 AND [Rong] > 0 AND [Cao] > 0 AND [KhoiLuongToiDa] > 0) OR ([CapViTri] = N'Ô' AND [LoaiViTri] = N'Tiếp nhận' AND [Dai] IS NULL AND [Rong] IS NULL AND [Cao] IS NULL AND [KhoiLuongToiDa] IS NULL) OR ([CapViTri] <> N'Ô' AND [Dai] IS NULL AND [Rong] IS NULL AND [Cao] IS NULL AND [KhoiLuongToiDa] IS NULL)),
        CONSTRAINT [CK_ViTri_LoaiViTri] CHECK ([LoaiViTri] IS NULL OR [LoaiViTri] IN (N'Lưu trữ', N'Tiếp nhận')),
        CONSTRAINT [CK_ViTri_TrangThai] CHECK ([TrangThai] IN (N'Hoạt động', N'Ngừng hoạt động')),
        CONSTRAINT [FK_ViTri_Kho_MaKho] FOREIGN KEY ([MaKho]) REFERENCES [Kho] ([MaKho]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ViTri_ViTri_MaViTriCha_MaKho] FOREIGN KEY ([MaViTriCha], [MaKho]) REFERENCES [ViTri] ([MaViTri], [MaKho]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027153902_KhoViTri'
)
BEGIN
    CREATE INDEX [IX_Kho_MaQuanLy] ON [Kho] ([MaQuanLy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027153902_KhoViTri'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Kho_TenKho] ON [Kho] ([TenKho]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027153902_KhoViTri'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ViTri_MaKho_MaViTriCha_TenViTri] ON [ViTri] ([MaKho], [MaViTriCha], [TenViTri]) WHERE [MaViTriCha] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027153902_KhoViTri'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ViTri_MaVachO] ON [ViTri] ([MaVachO]) WHERE [MaVachO] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027153902_KhoViTri'
)
BEGIN
    CREATE INDEX [IX_ViTri_MaViTriCha_MaKho] ON [ViTri] ([MaViTriCha], [MaKho]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261027153902_KhoViTri'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261027153902_KhoViTri', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028005852_NhaCungCapDanhMuc'
)
BEGIN
    CREATE TABLE [DanhMuc] (
        [MaDM] int NOT NULL IDENTITY,
        [TenDM] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_DanhMuc] PRIMARY KEY ([MaDM])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028005852_NhaCungCapDanhMuc'
)
BEGIN
    CREATE TABLE [NhaCungCap] (
        [MaNCC] int NOT NULL IDENTITY,
        [TenNCC] nvarchar(100) NOT NULL,
        [SDT] varchar(15) NULL,
        [DiaChi] nvarchar(255) NULL,
        [TrangThai] nvarchar(20) NOT NULL DEFAULT N'Hoạt động',
        CONSTRAINT [PK_NhaCungCap] PRIMARY KEY ([MaNCC]),
        CONSTRAINT [CK_NhaCungCap_TrangThai] CHECK ([TrangThai] IN (N'Hoạt động', N'Ngừng hoạt động'))
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028005852_NhaCungCapDanhMuc'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DanhMuc_TenDM] ON [DanhMuc] ([TenDM]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028005852_NhaCungCapDanhMuc'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NhaCungCap_TenNCC] ON [NhaCungCap] ([TenNCC]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028005852_NhaCungCapDanhMuc'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028005852_NhaCungCapDanhMuc', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028013224_SanPhamBienThe'
)
BEGIN
    CREATE TABLE [SanPham] (
        [MaSP] int NOT NULL IDENTITY,
        [TenSP] nvarchar(100) NOT NULL,
        [MaDM] int NOT NULL,
        [DonViTinh] nvarchar(20) NOT NULL,
        [CoHanDung] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_SanPham] PRIMARY KEY ([MaSP]),
        CONSTRAINT [FK_SanPham_DanhMuc_MaDM] FOREIGN KEY ([MaDM]) REFERENCES [DanhMuc] ([MaDM]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028013224_SanPhamBienThe'
)
BEGIN
    CREATE TABLE [BienThe] (
        [MaBT] int NOT NULL IDENTITY,
        [MaSP] int NOT NULL,
        [MaVach] varchar(50) NOT NULL,
        [ThuocTinh] nvarchar(100) NOT NULL,
        [Dai] decimal(8,2) NOT NULL,
        [Rong] decimal(8,2) NOT NULL,
        [Cao] decimal(8,2) NOT NULL,
        [KhoiLuong] decimal(10,3) NOT NULL,
        CONSTRAINT [PK_BienThe] PRIMARY KEY ([MaBT]),
        CONSTRAINT [CK_BienThe_KichThuoc] CHECK ([Dai] > 0 AND [Rong] > 0 AND [Cao] > 0 AND [KhoiLuong] > 0),
        CONSTRAINT [FK_BienThe_SanPham_MaSP] FOREIGN KEY ([MaSP]) REFERENCES [SanPham] ([MaSP]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028013224_SanPhamBienThe'
)
BEGIN
    CREATE INDEX [IX_BienThe_MaSP] ON [BienThe] ([MaSP]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028013224_SanPhamBienThe'
)
BEGIN
    CREATE UNIQUE INDEX [IX_BienThe_MaVach] ON [BienThe] ([MaVach]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028013224_SanPhamBienThe'
)
BEGIN
    CREATE INDEX [IX_SanPham_MaDM] ON [SanPham] ([MaDM]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028013224_SanPhamBienThe'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028013224_SanPhamBienThe', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028022157_TaoPhieuNhap'
)
BEGIN
    CREATE TABLE [PhieuNhap] (
        [MaPN] int NOT NULL IDENTITY,
        [SoPhieu] varchar(20) NOT NULL,
        [LoaiPhieu] nvarchar(20) NOT NULL,
        [ThoiGian] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [MaKho] int NOT NULL,
        [MaNCC] int NULL,
        [MaNguoiLap] int NOT NULL,
        [DienGiai] nvarchar(500) NULL,
        [TrangThai] nvarchar(20) NOT NULL DEFAULT N'Nháp',
        CONSTRAINT [PK_PhieuNhap] PRIMARY KEY ([MaPN]),
        CONSTRAINT [CK_PhieuNhap_LoaiPhieu] CHECK ([LoaiPhieu] IN (N'Mua hàng', N'Khác', N'Thừa do kiểm kê')),
        CONSTRAINT [CK_PhieuNhap_TrangThai] CHECK ([TrangThai] IN (N'Nháp', N'Đã xác nhận', N'Đã kiểm tra', N'Hoàn tất', N'Đã hủy')),
        CONSTRAINT [FK_PhieuNhap_Kho_MaKho] FOREIGN KEY ([MaKho]) REFERENCES [Kho] ([MaKho]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhieuNhap_NguoiDung_MaNguoiLap] FOREIGN KEY ([MaNguoiLap]) REFERENCES [NguoiDung] ([MaND]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhieuNhap_NhaCungCap_MaNCC] FOREIGN KEY ([MaNCC]) REFERENCES [NhaCungCap] ([MaNCC]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028022157_TaoPhieuNhap'
)
BEGIN
    CREATE TABLE [ChiTietPhieuNhap] (
        [MaCTN] int NOT NULL IDENTITY,
        [MaPN] int NOT NULL,
        [MaBT] int NOT NULL,
        [SLDuKien] int NOT NULL,
        CONSTRAINT [PK_ChiTietPhieuNhap] PRIMARY KEY ([MaCTN]),
        CONSTRAINT [CK_ChiTietPhieuNhap_SLDuKien] CHECK ([SLDuKien] > 0),
        CONSTRAINT [FK_ChiTietPhieuNhap_BienThe_MaBT] FOREIGN KEY ([MaBT]) REFERENCES [BienThe] ([MaBT]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietPhieuNhap_PhieuNhap_MaPN] FOREIGN KEY ([MaPN]) REFERENCES [PhieuNhap] ([MaPN]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028022157_TaoPhieuNhap'
)
BEGIN
    CREATE INDEX [IX_ChiTietPhieuNhap_MaBT] ON [ChiTietPhieuNhap] ([MaBT]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028022157_TaoPhieuNhap'
)
BEGIN
    CREATE INDEX [IX_ChiTietPhieuNhap_MaPN] ON [ChiTietPhieuNhap] ([MaPN]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028022157_TaoPhieuNhap'
)
BEGIN
    CREATE INDEX [IX_PhieuNhap_MaKho_ThoiGian] ON [PhieuNhap] ([MaKho], [ThoiGian]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028022157_TaoPhieuNhap'
)
BEGIN
    CREATE INDEX [IX_PhieuNhap_MaNCC] ON [PhieuNhap] ([MaNCC]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028022157_TaoPhieuNhap'
)
BEGIN
    CREATE INDEX [IX_PhieuNhap_MaNguoiLap] ON [PhieuNhap] ([MaNguoiLap]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028022157_TaoPhieuNhap'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PhieuNhap_SoPhieu] ON [PhieuNhap] ([SoPhieu]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028022157_TaoPhieuNhap'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028022157_TaoPhieuNhap', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028024130_LoHangBangThat'
)
BEGIN
    CREATE TABLE [LoHang] (
        [MaLo] int NOT NULL IDENTITY,
        [MaVachLo] varchar(50) NOT NULL,
        [MaBT] int NOT NULL,
        [HanDung] date NULL,
        CONSTRAINT [PK_LoHang] PRIMARY KEY ([MaLo]),
        CONSTRAINT [FK_LoHang_BienThe_MaBT] FOREIGN KEY ([MaBT]) REFERENCES [BienThe] ([MaBT]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028024130_LoHangBangThat'
)
BEGIN
    CREATE INDEX [IX_LoHang_MaBT] ON [LoHang] ([MaBT]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028024130_LoHangBangThat'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LoHang_MaLo_MaBT] ON [LoHang] ([MaLo], [MaBT]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028024130_LoHangBangThat'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LoHang_MaVachLo] ON [LoHang] ([MaVachLo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028024130_LoHangBangThat'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028024130_LoHangBangThat', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028024148_TonKhoBienDongTon'
)
BEGIN
    CREATE TABLE [BienDongTon] (
        [MaBD] bigint NOT NULL IDENTITY,
        [MaLo] int NOT NULL,
        [MaViTri] int NOT NULL,
        [SoLuong] int NOT NULL,
        [LoaiBienDong] nvarchar(20) NOT NULL,
        [LoaiCT] varchar(2) NOT NULL,
        [MaCT] int NOT NULL,
        [MaND] int NOT NULL,
        [ThoiGianUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_BienDongTon] PRIMARY KEY ([MaBD]),
        CONSTRAINT [CK_BienDongTon_LoaiBienDong] CHECK ([LoaiBienDong] IN (N'Nhận hàng', N'Sắp xếp', N'Xuất kho', N'Chuyển đi', N'Nhận chuyển', N'Đổi ô', N'Hoàn tác')),
        CONSTRAINT [CK_BienDongTon_LoaiCT] CHECK ([LoaiCT] IN ('NK', 'XK', 'CK', 'KK')),
        CONSTRAINT [CK_BienDongTon_SoLuongKhac0] CHECK ([SoLuong] <> 0),
        CONSTRAINT [FK_BienDongTon_LoHang_MaLo] FOREIGN KEY ([MaLo]) REFERENCES [LoHang] ([MaLo]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BienDongTon_NguoiDung_MaND] FOREIGN KEY ([MaND]) REFERENCES [NguoiDung] ([MaND]) ON DELETE NO ACTION,
        CONSTRAINT [FK_BienDongTon_ViTri_MaViTri] FOREIGN KEY ([MaViTri]) REFERENCES [ViTri] ([MaViTri]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028024148_TonKhoBienDongTon'
)
BEGIN
    CREATE TABLE [TonKho] (
        [MaLo] int NOT NULL,
        [MaViTri] int NOT NULL,
        [SoLuongTon] int NOT NULL DEFAULT 0,
        [SoLuongGiu] int NOT NULL DEFAULT 0,
        [PhienBan] rowversion NOT NULL,
        CONSTRAINT [PK_TonKho] PRIMARY KEY ([MaLo], [MaViTri]),
        CONSTRAINT [CK_TonKho_GiuKhongVuotTon] CHECK ([SoLuongGiu] <= [SoLuongTon]),
        CONSTRAINT [CK_TonKho_KhongAm] CHECK ([SoLuongTon] >= 0 AND [SoLuongGiu] >= 0),
        CONSTRAINT [FK_TonKho_LoHang_MaLo] FOREIGN KEY ([MaLo]) REFERENCES [LoHang] ([MaLo]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TonKho_ViTri_MaViTri] FOREIGN KEY ([MaViTri]) REFERENCES [ViTri] ([MaViTri]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028024148_TonKhoBienDongTon'
)
BEGIN
    CREATE INDEX [IX_BienDongTon_LoaiCT_MaCT] ON [BienDongTon] ([LoaiCT], [MaCT]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028024148_TonKhoBienDongTon'
)
BEGIN
    CREATE INDEX [IX_BienDongTon_MaLo_MaViTri] ON [BienDongTon] ([MaLo], [MaViTri]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028024148_TonKhoBienDongTon'
)
BEGIN
    CREATE INDEX [IX_BienDongTon_MaND] ON [BienDongTon] ([MaND]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028024148_TonKhoBienDongTon'
)
BEGIN
    CREATE INDEX [IX_BienDongTon_MaViTri] ON [BienDongTon] ([MaViTri]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028024148_TonKhoBienDongTon'
)
BEGIN
    CREATE INDEX [IX_TonKho_MaViTri] ON [TonKho] ([MaViTri]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028024148_TonKhoBienDongTon'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028024148_TonKhoBienDongTon', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028040000_KiemTraHangNhap'
)
BEGIN
    ALTER TABLE [ChiTietPhieuNhap] ADD [LyDoLech] nvarchar(255) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028040000_KiemTraHangNhap'
)
BEGIN
    ALTER TABLE [ChiTietPhieuNhap] ADD [MaLo] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028040000_KiemTraHangNhap'
)
BEGIN
    ALTER TABLE [ChiTietPhieuNhap] ADD [SLDat] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028040000_KiemTraHangNhap'
)
BEGIN
    ALTER TABLE [ChiTietPhieuNhap] ADD [SLThucNhan] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028040000_KiemTraHangNhap'
)
BEGIN
    CREATE INDEX [IX_ChiTietPhieuNhap_MaLo] ON [ChiTietPhieuNhap] ([MaLo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028040000_KiemTraHangNhap'
)
BEGIN
    EXEC(N'ALTER TABLE [ChiTietPhieuNhap] ADD CONSTRAINT [CK_ChiTietPhieuNhap_LyDoLech] CHECK ([SLThucNhan] IS NULL OR [SLThucNhan] = [SLDuKien] OR [LyDoLech] IS NOT NULL)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028040000_KiemTraHangNhap'
)
BEGIN
    EXEC(N'ALTER TABLE [ChiTietPhieuNhap] ADD CONSTRAINT [CK_ChiTietPhieuNhap_SLDat] CHECK ([SLDat] >= 0 AND [SLDat] <= [SLThucNhan])');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028040000_KiemTraHangNhap'
)
BEGIN
    EXEC(N'ALTER TABLE [ChiTietPhieuNhap] ADD CONSTRAINT [CK_ChiTietPhieuNhap_SLThucNhan] CHECK ([SLThucNhan] >= 0)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028040000_KiemTraHangNhap'
)
BEGIN
    ALTER TABLE [ChiTietPhieuNhap] ADD CONSTRAINT [FK_ChiTietPhieuNhap_LoHang_MaLo_MaBT] FOREIGN KEY ([MaLo], [MaBT]) REFERENCES [LoHang] ([MaLo], [MaBT])
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028040000_KiemTraHangNhap'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028040000_KiemTraHangNhap', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028045313_NhatKyDinhKem'
)
BEGIN
    CREATE TABLE [NhatKyThaoTac] (
        [MaNK] bigint NOT NULL IDENTITY,
        [MaND] int NOT NULL,
        [ThoiGian] datetime2 NOT NULL,
        [HanhDong] nvarchar(100) NOT NULL,
        [LoaiCT] varchar(2) NULL,
        [MaCT] int NULL,
        [ChiTiet] nvarchar(1000) NULL,
        CONSTRAINT [PK_NhatKyThaoTac] PRIMARY KEY ([MaNK]),
        CONSTRAINT [CK_NhatKyThaoTac_ChungTu] CHECK (([LoaiCT] IS NULL AND [MaCT] IS NULL) OR ([LoaiCT] IS NOT NULL AND [MaCT] IS NOT NULL)),
        CONSTRAINT [CK_NhatKyThaoTac_LoaiCT] CHECK ([LoaiCT] IS NULL OR [LoaiCT] IN ('NK', 'XK', 'CK', 'KK')),
        CONSTRAINT [FK_NhatKyThaoTac_NguoiDung_MaND] FOREIGN KEY ([MaND]) REFERENCES [NguoiDung] ([MaND]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028045313_NhatKyDinhKem'
)
BEGIN
    CREATE TABLE [TepDinhKem] (
        [MaTep] int NOT NULL IDENTITY,
        [LoaiCT] varchar(2) NOT NULL,
        [MaCT] int NOT NULL,
        [TenTep] nvarchar(255) NOT NULL,
        [DuongDan] nvarchar(500) NOT NULL,
        [MaND] int NOT NULL,
        [ThoiGian] datetime2 NOT NULL,
        CONSTRAINT [PK_TepDinhKem] PRIMARY KEY ([MaTep]),
        CONSTRAINT [CK_TepDinhKem_LoaiCT] CHECK ([LoaiCT] IN ('NK', 'XK', 'CK', 'KK')),
        CONSTRAINT [FK_TepDinhKem_NguoiDung_MaND] FOREIGN KEY ([MaND]) REFERENCES [NguoiDung] ([MaND]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028045313_NhatKyDinhKem'
)
BEGIN
    CREATE INDEX [IX_NhatKyThaoTac_LoaiCT_MaCT] ON [NhatKyThaoTac] ([LoaiCT], [MaCT]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028045313_NhatKyDinhKem'
)
BEGIN
    CREATE INDEX [IX_NhatKyThaoTac_MaND] ON [NhatKyThaoTac] ([MaND]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028045313_NhatKyDinhKem'
)
BEGIN
    CREATE INDEX [IX_NhatKyThaoTac_ThoiGian] ON [NhatKyThaoTac] ([ThoiGian]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028045313_NhatKyDinhKem'
)
BEGIN
    CREATE INDEX [IX_TepDinhKem_LoaiCT_MaCT] ON [TepDinhKem] ([LoaiCT], [MaCT]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028045313_NhatKyDinhKem'
)
BEGIN
    CREATE INDEX [IX_TepDinhKem_MaND] ON [TepDinhKem] ([MaND]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028045313_NhatKyDinhKem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028045313_NhatKyDinhKem', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028050000_PhieuNhapThua'
)
BEGIN
    ALTER TABLE [PhieuNhap] ADD [MaPK] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028050000_PhieuNhapThua'
)
BEGIN
    CREATE INDEX [IX_PhieuNhap_MaPK] ON [PhieuNhap] ([MaPK]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028050000_PhieuNhapThua'
)
BEGIN
    EXEC(N'ALTER TABLE [PhieuNhap] ADD CONSTRAINT [CK_PhieuNhap_ThuaCoPhieuKiem] CHECK (([LoaiPhieu] = N''Thừa do kiểm kê'' AND [MaPK] IS NOT NULL) OR ([LoaiPhieu] <> N''Thừa do kiểm kê'' AND [MaPK] IS NULL))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028050000_PhieuNhapThua'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028050000_PhieuNhapThua', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028050812_TaoPhieuXuat'
)
BEGIN
    CREATE TABLE [PhieuXuat] (
        [MaPX] int NOT NULL IDENTITY,
        [SoPhieu] varchar(20) NOT NULL,
        [LoaiPhieu] nvarchar(20) NOT NULL,
        [ThoiGian] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [MaKho] int NOT NULL,
        [DoiTuong] nvarchar(150) NULL,
        [ThamChieu] nvarchar(100) NULL,
        [MaPNGoc] int NULL,
        [MaNguoiLap] int NOT NULL,
        [MaNguoiDuyet] int NULL,
        [LyDo] nvarchar(255) NULL,
        [DienGiai] nvarchar(500) NULL,
        [TrangThai] nvarchar(20) NOT NULL DEFAULT N'Nháp',
        CONSTRAINT [PK_PhieuXuat] PRIMARY KEY ([MaPX]),
        CONSTRAINT [CK_PhieuXuat_HuyHang] CHECK ([LoaiPhieu] <> N'Hủy hàng' OR [LyDo] IS NOT NULL),
        CONSTRAINT [CK_PhieuXuat_LoaiPhieu] CHECK ([LoaiPhieu] IN (N'Khác', N'Trả lại hàng mua', N'Hủy hàng', N'Thiếu do kiểm kê')),
        CONSTRAINT [CK_PhieuXuat_NguoiDuyet] CHECK ([TrangThai] = N'Nháp' OR [TrangThai] = N'Đã hủy' OR [MaNguoiDuyet] IS NOT NULL),
        CONSTRAINT [CK_PhieuXuat_TraHangMua] CHECK (([LoaiPhieu] <> N'Trả lại hàng mua' OR [MaPNGoc] IS NOT NULL) AND ([LoaiPhieu] = N'Trả lại hàng mua' OR [MaPNGoc] IS NULL)),
        CONSTRAINT [CK_PhieuXuat_TrangThai] CHECK ([TrangThai] IN (N'Nháp', N'Đã duyệt', N'Đang lấy hàng', N'Đã đối chiếu', N'Hoàn tất', N'Đã hủy')),
        CONSTRAINT [FK_PhieuXuat_Kho_MaKho] FOREIGN KEY ([MaKho]) REFERENCES [Kho] ([MaKho]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhieuXuat_NguoiDung_MaNguoiDuyet] FOREIGN KEY ([MaNguoiDuyet]) REFERENCES [NguoiDung] ([MaND]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhieuXuat_NguoiDung_MaNguoiLap] FOREIGN KEY ([MaNguoiLap]) REFERENCES [NguoiDung] ([MaND]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhieuXuat_PhieuNhap_MaPNGoc] FOREIGN KEY ([MaPNGoc]) REFERENCES [PhieuNhap] ([MaPN]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028050812_TaoPhieuXuat'
)
BEGIN
    CREATE TABLE [ChiTietPhieuXuat] (
        [MaCTX] int NOT NULL IDENTITY,
        [MaPX] int NOT NULL,
        [MaBT] int NOT NULL,
        [SoLuong] int NOT NULL,
        CONSTRAINT [PK_ChiTietPhieuXuat] PRIMARY KEY ([MaCTX]),
        CONSTRAINT [CK_ChiTietPhieuXuat_SoLuong] CHECK ([SoLuong] > 0),
        CONSTRAINT [FK_ChiTietPhieuXuat_BienThe_MaBT] FOREIGN KEY ([MaBT]) REFERENCES [BienThe] ([MaBT]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietPhieuXuat_PhieuXuat_MaPX] FOREIGN KEY ([MaPX]) REFERENCES [PhieuXuat] ([MaPX]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028050812_TaoPhieuXuat'
)
BEGIN
    CREATE INDEX [IX_ChiTietPhieuXuat_MaBT] ON [ChiTietPhieuXuat] ([MaBT]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028050812_TaoPhieuXuat'
)
BEGIN
    CREATE INDEX [IX_ChiTietPhieuXuat_MaPX] ON [ChiTietPhieuXuat] ([MaPX]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028050812_TaoPhieuXuat'
)
BEGIN
    CREATE INDEX [IX_PhieuXuat_MaKho_ThoiGian] ON [PhieuXuat] ([MaKho], [ThoiGian]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028050812_TaoPhieuXuat'
)
BEGIN
    CREATE INDEX [IX_PhieuXuat_MaNguoiDuyet] ON [PhieuXuat] ([MaNguoiDuyet]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028050812_TaoPhieuXuat'
)
BEGIN
    CREATE INDEX [IX_PhieuXuat_MaNguoiLap] ON [PhieuXuat] ([MaNguoiLap]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028050812_TaoPhieuXuat'
)
BEGIN
    CREATE INDEX [IX_PhieuXuat_MaPNGoc] ON [PhieuXuat] ([MaPNGoc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028050812_TaoPhieuXuat'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PhieuXuat_SoPhieu] ON [PhieuXuat] ([SoPhieu]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261028050812_TaoPhieuXuat'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261028050812_TaoPhieuXuat', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029000000_TaoSapXep'
)
BEGIN
    CREATE TABLE [SapXep] (
        [MaSX] int NOT NULL IDENTITY,
        [MaCTN] int NOT NULL,
        [MaViTri] int NOT NULL,
        [SoLuong] int NOT NULL,
        [TrangThai] nvarchar(20) NOT NULL DEFAULT N'Chờ xác nhận',
        [MaNguoiXN] int NULL,
        [ThoiGianXN] datetime2(0) NULL,
        CONSTRAINT [PK_SapXep] PRIMARY KEY ([MaSX]),
        CONSTRAINT [CK_SapXep_SoLuong] CHECK ([SoLuong] > 0),
        CONSTRAINT [CK_SapXep_TrangThai] CHECK ([TrangThai] IN (N'Chờ xác nhận', N'Đã xác nhận', N'Đã hủy')),
        CONSTRAINT [CK_SapXep_XacNhan] CHECK (([TrangThai] = N'Đã xác nhận' AND [MaNguoiXN] IS NOT NULL AND [ThoiGianXN] IS NOT NULL) OR ([TrangThai] <> N'Đã xác nhận' AND [MaNguoiXN] IS NULL AND [ThoiGianXN] IS NULL)),
        CONSTRAINT [FK_SapXep_ChiTietPhieuNhap_MaCTN] FOREIGN KEY ([MaCTN]) REFERENCES [ChiTietPhieuNhap] ([MaCTN]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SapXep_NguoiDung_MaNguoiXN] FOREIGN KEY ([MaNguoiXN]) REFERENCES [NguoiDung] ([MaND]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SapXep_ViTri_MaViTri] FOREIGN KEY ([MaViTri]) REFERENCES [ViTri] ([MaViTri]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029000000_TaoSapXep'
)
BEGIN
    CREATE INDEX [IX_SapXep_MaCTN] ON [SapXep] ([MaCTN]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029000000_TaoSapXep'
)
BEGIN
    CREATE INDEX [IX_SapXep_MaNguoiXN] ON [SapXep] ([MaNguoiXN]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029000000_TaoSapXep'
)
BEGIN
    CREATE INDEX [IX_SapXep_MaViTri_TrangThai] ON [SapXep] ([MaViTri], [TrangThai]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029000000_TaoSapXep'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261029000000_TaoSapXep', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029145406_GoiYAI'
)
BEGIN
    CREATE TABLE [GoiYAI] (
        [MaGoiY] int NOT NULL IDENTITY,
        [LoaiGoiY] char(2) NOT NULL,
        [ThoiGianTao] datetime2 NOT NULL,
        [TrangThai] nvarchar(20) NOT NULL,
        [MaNguoiDuyet] int NULL,
        [ThoiGianDuyet] datetime2 NULL,
        [MaPX] int NULL,
        [MaPN] int NULL,
        [MaPC] int NULL,
        CONSTRAINT [PK_GoiYAI] PRIMARY KEY ([MaGoiY]),
        CONSTRAINT [CK_GoiYAI_LoaiGoiY] CHECK ([LoaiGoiY] IN ('G1', 'G2', 'G3', 'G4')),
        CONSTRAINT [CK_GoiYAI_NguoiDuyet] CHECK (([TrangThai] = N'Chờ duyệt' AND [MaNguoiDuyet] IS NULL AND [ThoiGianDuyet] IS NULL) OR ([TrangThai] <> N'Chờ duyệt' AND [MaNguoiDuyet] IS NOT NULL AND [ThoiGianDuyet] IS NOT NULL)),
        CONSTRAINT [CK_GoiYAI_ThamChieu] CHECK (([LoaiGoiY] = 'G1' AND [MaPX] IS NOT NULL AND [MaPN] IS NULL AND [MaPC] IS NULL) OR ([LoaiGoiY] = 'G2' AND [MaPN] IS NOT NULL AND [MaPX] IS NULL AND [MaPC] IS NULL) OR ([LoaiGoiY] = 'G3' AND [MaPX] IS NULL AND [MaPC] IS NULL) OR ([LoaiGoiY] = 'G4' AND [MaPX] IS NULL AND [MaPN] IS NULL)),
        CONSTRAINT [CK_GoiYAI_TrangThai] CHECK ([TrangThai] IN (N'Chờ duyệt', N'Đã duyệt', N'Đã sửa và duyệt', N'Từ chối')),
        CONSTRAINT [FK_GoiYAI_NguoiDung_MaNguoiDuyet] FOREIGN KEY ([MaNguoiDuyet]) REFERENCES [NguoiDung] ([MaND]) ON DELETE NO ACTION,
        CONSTRAINT [FK_GoiYAI_PhieuNhap_MaPN] FOREIGN KEY ([MaPN]) REFERENCES [PhieuNhap] ([MaPN]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029145406_GoiYAI'
)
BEGIN
    CREATE TABLE [ChiTietGoiY] (
        [MaCTGY] int NOT NULL IDENTITY,
        [MaGoiY] int NOT NULL,
        [MaBT] int NOT NULL,
        [MaLo] int NULL,
        [MaViTri] int NULL,
        [MaKhoNguon] int NULL,
        [MaKhoDich] int NULL,
        [SoLuong] int NOT NULL,
        [LyDo] nvarchar(500) NOT NULL,
        [LaBanSua] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_ChiTietGoiY] PRIMARY KEY ([MaCTGY]),
        CONSTRAINT [CK_ChiTietGoiY_KhoKhac] CHECK ([MaKhoNguon] IS NULL OR [MaKhoDich] IS NULL OR [MaKhoNguon] <> [MaKhoDich]),
        CONSTRAINT [CK_ChiTietGoiY_LyDo] CHECK (LEN(LTRIM(RTRIM([LyDo]))) > 0),
        CONSTRAINT [CK_ChiTietGoiY_SoLuong] CHECK ([SoLuong] > 0),
        CONSTRAINT [FK_ChiTietGoiY_BienThe_MaBT] FOREIGN KEY ([MaBT]) REFERENCES [BienThe] ([MaBT]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietGoiY_GoiYAI_MaGoiY] FOREIGN KEY ([MaGoiY]) REFERENCES [GoiYAI] ([MaGoiY]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietGoiY_Kho_MaKhoDich] FOREIGN KEY ([MaKhoDich]) REFERENCES [Kho] ([MaKho]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietGoiY_Kho_MaKhoNguon] FOREIGN KEY ([MaKhoNguon]) REFERENCES [Kho] ([MaKho]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietGoiY_LoHang_MaLo] FOREIGN KEY ([MaLo]) REFERENCES [LoHang] ([MaLo]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietGoiY_ViTri_MaViTri] FOREIGN KEY ([MaViTri]) REFERENCES [ViTri] ([MaViTri]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029145406_GoiYAI'
)
BEGIN
    CREATE INDEX [IX_ChiTietGoiY_MaBT] ON [ChiTietGoiY] ([MaBT]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029145406_GoiYAI'
)
BEGIN
    CREATE INDEX [IX_ChiTietGoiY_MaGoiY] ON [ChiTietGoiY] ([MaGoiY]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029145406_GoiYAI'
)
BEGIN
    CREATE INDEX [IX_ChiTietGoiY_MaKhoDich] ON [ChiTietGoiY] ([MaKhoDich]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029145406_GoiYAI'
)
BEGIN
    CREATE INDEX [IX_ChiTietGoiY_MaKhoNguon] ON [ChiTietGoiY] ([MaKhoNguon]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029145406_GoiYAI'
)
BEGIN
    CREATE INDEX [IX_ChiTietGoiY_MaLo] ON [ChiTietGoiY] ([MaLo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029145406_GoiYAI'
)
BEGIN
    CREATE INDEX [IX_ChiTietGoiY_MaViTri] ON [ChiTietGoiY] ([MaViTri]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029145406_GoiYAI'
)
BEGIN
    CREATE INDEX [IX_GoiYAI_MaNguoiDuyet] ON [GoiYAI] ([MaNguoiDuyet]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029145406_GoiYAI'
)
BEGIN
    CREATE INDEX [IX_GoiYAI_MaPN] ON [GoiYAI] ([MaPN]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029145406_GoiYAI'
)
BEGIN
    CREATE INDEX [IX_GoiYAI_MaPX] ON [GoiYAI] ([MaPX]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029145406_GoiYAI'
)
BEGIN
    CREATE INDEX [IX_GoiYAI_TrangThai_LoaiGoiY] ON [GoiYAI] ([TrangThai], [LoaiGoiY]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029145406_GoiYAI'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261029145406_GoiYAI', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030011853_ThemMaPKPhieuXuat'
)
BEGIN
    ALTER TABLE [PhieuXuat] ADD [MaPK] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030011853_ThemMaPKPhieuXuat'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PhieuXuat_MaPK] ON [PhieuXuat] ([MaPK]) WHERE [LoaiPhieu] = N''Thiếu do kiểm kê''');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030011853_ThemMaPKPhieuXuat'
)
BEGIN
    EXEC(N'ALTER TABLE [PhieuXuat] ADD CONSTRAINT [CK_PhieuXuat_ThieuCoPhieuKiem] CHECK (([LoaiPhieu] = N''Thiếu do kiểm kê'' AND [MaPK] IS NOT NULL) OR ([LoaiPhieu] <> N''Thiếu do kiểm kê'' AND [MaPK] IS NULL))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030011853_ThemMaPKPhieuXuat'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030011853_ThemMaPKPhieuXuat', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031130020_ThemLayHang'
)
BEGIN
    CREATE TABLE [LayHang] (
        [MaLH] int NOT NULL IDENTITY,
        [MaCTX] int NOT NULL,
        [MaLo] int NOT NULL,
        [MaViTri] int NOT NULL,
        [SLGiu] int NOT NULL,
        [SLThucLay] int NULL,
        [LyDoLech] nvarchar(255) NULL,
        CONSTRAINT [PK_LayHang] PRIMARY KEY ([MaLH]),
        CONSTRAINT [CK_LayHang_LyDoLech] CHECK ([SLThucLay] IS NULL OR [SLThucLay] = [SLGiu] OR [LyDoLech] IS NOT NULL),
        CONSTRAINT [CK_LayHang_SLGiu] CHECK ([SLGiu] > 0),
        CONSTRAINT [CK_LayHang_SLThucLay] CHECK ([SLThucLay] >= 0 AND [SLThucLay] <= [SLGiu]),
        CONSTRAINT [FK_LayHang_ChiTietPhieuXuat_MaCTX] FOREIGN KEY ([MaCTX]) REFERENCES [ChiTietPhieuXuat] ([MaCTX]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LayHang_LoHang_MaLo] FOREIGN KEY ([MaLo]) REFERENCES [LoHang] ([MaLo]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LayHang_ViTri_MaViTri] FOREIGN KEY ([MaViTri]) REFERENCES [ViTri] ([MaViTri]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031130020_ThemLayHang'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LayHang_MaCTX_MaLo_MaViTri] ON [LayHang] ([MaCTX], [MaLo], [MaViTri]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031130020_ThemLayHang'
)
BEGIN
    CREATE INDEX [IX_LayHang_MaLo] ON [LayHang] ([MaLo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031130020_ThemLayHang'
)
BEGIN
    CREATE INDEX [IX_LayHang_MaViTri] ON [LayHang] ([MaViTri]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031130020_ThemLayHang'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261031130020_ThemLayHang', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101042425_GoiYAIKhoaNgoaiMaPX'
)
BEGIN
    ALTER TABLE [GoiYAI] ADD CONSTRAINT [FK_GoiYAI_PhieuXuat_MaPX] FOREIGN KEY ([MaPX]) REFERENCES [PhieuXuat] ([MaPX]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261101042425_GoiYAIKhoaNgoaiMaPX'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261101042425_GoiYAIKhoaNgoaiMaPX', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104021013_BoGoiYG3G4'
)
BEGIN
    DELETE FROM ChiTietGoiY WHERE MaGoiY IN (SELECT MaGoiY FROM GoiYAI WHERE LoaiGoiY IN ('G3', 'G4'))
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104021013_BoGoiYG3G4'
)
BEGIN
    DELETE FROM GoiYAI WHERE LoaiGoiY IN ('G3', 'G4')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104021013_BoGoiYG3G4'
)
BEGIN
    ALTER TABLE [GoiYAI] DROP CONSTRAINT [CK_GoiYAI_LoaiGoiY];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104021013_BoGoiYG3G4'
)
BEGIN
    ALTER TABLE [GoiYAI] DROP CONSTRAINT [CK_GoiYAI_ThamChieu];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104021013_BoGoiYG3G4'
)
BEGIN
    EXEC(N'ALTER TABLE [GoiYAI] ADD CONSTRAINT [CK_GoiYAI_LoaiGoiY] CHECK ([LoaiGoiY] IN (''G1'', ''G2''))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104021013_BoGoiYG3G4'
)
BEGIN
    EXEC(N'ALTER TABLE [GoiYAI] ADD CONSTRAINT [CK_GoiYAI_ThamChieu] CHECK (([LoaiGoiY] = ''G1'' AND [MaPX] IS NOT NULL AND [MaPN] IS NULL AND [MaPC] IS NULL) OR ([LoaiGoiY] = ''G2'' AND [MaPN] IS NOT NULL AND [MaPX] IS NULL AND [MaPC] IS NULL))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104021013_BoGoiYG3G4'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261104021013_BoGoiYG3G4', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112141328_TaoPhieuKiem'
)
BEGIN
    CREATE TABLE [PhieuKiem] (
        [MaPK] int NOT NULL IDENTITY,
        [SoPhieu] varchar(20) NOT NULL,
        [MaKho] int NOT NULL,
        [LoaiKiemKe] nvarchar(10) NOT NULL,
        [DienGiai] nvarchar(500) NULL,
        [ThamChieu] nvarchar(100) NULL,
        [ThoiGianBatDau] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [MaNguoiLap] int NOT NULL,
        [TrangThai] nvarchar(20) NOT NULL DEFAULT N'Đang kiểm',
        CONSTRAINT [PK_PhieuKiem] PRIMARY KEY ([MaPK]),
        CONSTRAINT [CK_PhieuKiem_LoaiKiemKe] CHECK ([LoaiKiemKe] IN (N'Toàn bộ', N'Một phần')),
        CONSTRAINT [CK_PhieuKiem_TrangThai] CHECK ([TrangThai] IN (N'Đang kiểm', N'Chờ duyệt', N'Đã xử lý')),
        CONSTRAINT [FK_PhieuKiem_Kho_MaKho] FOREIGN KEY ([MaKho]) REFERENCES [Kho] ([MaKho]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhieuKiem_NguoiDung_MaNguoiLap] FOREIGN KEY ([MaNguoiLap]) REFERENCES [NguoiDung] ([MaND]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112141328_TaoPhieuKiem'
)
BEGIN
    CREATE TABLE [ChiTietPhieuKiem] (
        [MaCTK] int NOT NULL IDENTITY,
        [MaPK] int NOT NULL,
        [MaBT] int NOT NULL,
        [MaLo] int NOT NULL,
        [MaViTri] int NOT NULL,
        [SoLuongHeThong] int NOT NULL,
        CONSTRAINT [PK_ChiTietPhieuKiem] PRIMARY KEY ([MaCTK]),
        CONSTRAINT [CK_ChiTietPhieuKiem_SoLuongHeThong] CHECK ([SoLuongHeThong] >= 0),
        CONSTRAINT [FK_ChiTietPhieuKiem_BienThe_MaBT] FOREIGN KEY ([MaBT]) REFERENCES [BienThe] ([MaBT]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietPhieuKiem_LoHang_MaLo] FOREIGN KEY ([MaLo]) REFERENCES [LoHang] ([MaLo]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietPhieuKiem_PhieuKiem_MaPK] FOREIGN KEY ([MaPK]) REFERENCES [PhieuKiem] ([MaPK]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietPhieuKiem_ViTri_MaViTri] FOREIGN KEY ([MaViTri]) REFERENCES [ViTri] ([MaViTri]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112141328_TaoPhieuKiem'
)
BEGIN
    CREATE TABLE [ThamGiaKiem] (
        [MaPK] int NOT NULL,
        [MaND] int NOT NULL,
        [GhiChu] nvarchar(255) NULL,
        CONSTRAINT [PK_ThamGiaKiem] PRIMARY KEY ([MaPK], [MaND]),
        CONSTRAINT [FK_ThamGiaKiem_NguoiDung_MaND] FOREIGN KEY ([MaND]) REFERENCES [NguoiDung] ([MaND]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ThamGiaKiem_PhieuKiem_MaPK] FOREIGN KEY ([MaPK]) REFERENCES [PhieuKiem] ([MaPK]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112141328_TaoPhieuKiem'
)
BEGIN
    CREATE INDEX [IX_ChiTietPhieuKiem_MaBT] ON [ChiTietPhieuKiem] ([MaBT]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112141328_TaoPhieuKiem'
)
BEGIN
    CREATE INDEX [IX_ChiTietPhieuKiem_MaLo] ON [ChiTietPhieuKiem] ([MaLo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112141328_TaoPhieuKiem'
)
BEGIN
    CREATE INDEX [IX_ChiTietPhieuKiem_MaPK] ON [ChiTietPhieuKiem] ([MaPK]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112141328_TaoPhieuKiem'
)
BEGIN
    CREATE INDEX [IX_ChiTietPhieuKiem_MaViTri] ON [ChiTietPhieuKiem] ([MaViTri]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112141328_TaoPhieuKiem'
)
BEGIN
    CREATE INDEX [IX_PhieuKiem_MaKho_ThoiGianBatDau] ON [PhieuKiem] ([MaKho], [ThoiGianBatDau]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112141328_TaoPhieuKiem'
)
BEGIN
    CREATE INDEX [IX_PhieuKiem_MaNguoiLap] ON [PhieuKiem] ([MaNguoiLap]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112141328_TaoPhieuKiem'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PhieuKiem_SoPhieu] ON [PhieuKiem] ([SoPhieu]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112141328_TaoPhieuKiem'
)
BEGIN
    CREATE INDEX [IX_ThamGiaKiem_MaND] ON [ThamGiaKiem] ([MaND]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112141328_TaoPhieuKiem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261112141328_TaoPhieuKiem', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113005629_TaoDongKiem'
)
BEGIN
    CREATE TABLE [DongKiem] (
        [MaDK] int NOT NULL IDENTITY,
        [MaPK] int NOT NULL,
        [MaViTri] int NOT NULL,
        [MaLo] int NOT NULL,
        [SLHeThong] int NOT NULL,
        [SLThucTe] int NULL,
        [LoaiPhatSinh] nvarchar(25) NULL,
        [NguyenNhan] nvarchar(255) NULL,
        [MaViTriDung] int NULL,
        [MaBTDung] int NULL,
        [MaNguoiDem] int NULL,
        [TrangThaiXuLy] nvarchar(20) NOT NULL DEFAULT N'Chưa kiểm',
        [MaNguoiDuyet] int NULL,
        [ThoiGianDuyet] datetime2(0) NULL,
        CONSTRAINT [PK_DongKiem] PRIMARY KEY ([MaDK]),
        CONSTRAINT [CK_DongKiem_ChuaKiem] CHECK (([TrangThaiXuLy] = N'Chưa kiểm' AND [SLThucTe] IS NULL) OR ([TrangThaiXuLy] <> N'Chưa kiểm' AND [SLThucTe] IS NOT NULL)),
        CONSTRAINT [CK_DongKiem_LoaiPhatSinh] CHECK ([LoaiPhatSinh] IS NULL OR [LoaiPhatSinh] IN (N'Thiếu hoặc thừa', N'Hư hỏng hoặc hết hạn', N'Sai vị trí', N'Sai mã hàng')),
        CONSTRAINT [CK_DongKiem_MaBTDung] CHECK (([LoaiPhatSinh] = N'Sai mã hàng' AND [MaBTDung] IS NOT NULL) OR (([LoaiPhatSinh] IS NULL OR [LoaiPhatSinh] <> N'Sai mã hàng') AND [MaBTDung] IS NULL)),
        CONSTRAINT [CK_DongKiem_MaViTriDung] CHECK (([LoaiPhatSinh] = N'Sai vị trí' AND [MaViTriDung] IS NOT NULL) OR (([LoaiPhatSinh] IS NULL OR [LoaiPhatSinh] <> N'Sai vị trí') AND [MaViTriDung] IS NULL)),
        CONSTRAINT [CK_DongKiem_PhatSinh] CHECK ([TrangThaiXuLy] NOT IN (N'Chờ duyệt', N'Đã duyệt', N'Từ chối') OR [LoaiPhatSinh] IS NOT NULL),
        CONSTRAINT [CK_DongKiem_SLHeThong] CHECK ([SLHeThong] >= 0),
        CONSTRAINT [CK_DongKiem_SLThucTe] CHECK ([SLThucTe] IS NULL OR [SLThucTe] >= 0),
        CONSTRAINT [CK_DongKiem_TrangThaiXuLy] CHECK ([TrangThaiXuLy] IN (N'Chưa kiểm', N'Khớp', N'Chờ duyệt', N'Đã duyệt', N'Từ chối')),
        CONSTRAINT [FK_DongKiem_BienThe_MaBTDung] FOREIGN KEY ([MaBTDung]) REFERENCES [BienThe] ([MaBT]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DongKiem_LoHang_MaLo] FOREIGN KEY ([MaLo]) REFERENCES [LoHang] ([MaLo]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DongKiem_NguoiDung_MaNguoiDem] FOREIGN KEY ([MaNguoiDem]) REFERENCES [NguoiDung] ([MaND]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DongKiem_NguoiDung_MaNguoiDuyet] FOREIGN KEY ([MaNguoiDuyet]) REFERENCES [NguoiDung] ([MaND]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DongKiem_PhieuKiem_MaPK] FOREIGN KEY ([MaPK]) REFERENCES [PhieuKiem] ([MaPK]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DongKiem_ViTri_MaViTri] FOREIGN KEY ([MaViTri]) REFERENCES [ViTri] ([MaViTri]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DongKiem_ViTri_MaViTriDung] FOREIGN KEY ([MaViTriDung]) REFERENCES [ViTri] ([MaViTri]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113005629_TaoDongKiem'
)
BEGIN
    CREATE INDEX [IX_DongKiem_MaBTDung] ON [DongKiem] ([MaBTDung]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113005629_TaoDongKiem'
)
BEGIN
    CREATE INDEX [IX_DongKiem_MaLo] ON [DongKiem] ([MaLo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113005629_TaoDongKiem'
)
BEGIN
    CREATE INDEX [IX_DongKiem_MaNguoiDem] ON [DongKiem] ([MaNguoiDem]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113005629_TaoDongKiem'
)
BEGIN
    CREATE INDEX [IX_DongKiem_MaNguoiDuyet] ON [DongKiem] ([MaNguoiDuyet]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113005629_TaoDongKiem'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DongKiem_MaPK_MaViTri_MaLo] ON [DongKiem] ([MaPK], [MaViTri], [MaLo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113005629_TaoDongKiem'
)
BEGIN
    CREATE INDEX [IX_DongKiem_MaViTri] ON [DongKiem] ([MaViTri]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113005629_TaoDongKiem'
)
BEGIN
    CREATE INDEX [IX_DongKiem_MaViTriDung] ON [DongKiem] ([MaViTriDung]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113005629_TaoDongKiem'
)
BEGIN
    INSERT INTO [DongKiem] ([MaPK], [MaViTri], [MaLo], [SLHeThong]) SELECT [MaPK], [MaViTri], [MaLo], SUM([SoLuongHeThong]) FROM [ChiTietPhieuKiem] GROUP BY [MaPK], [MaViTri], [MaLo];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113005629_TaoDongKiem'
)
BEGIN
    DROP TABLE [ChiTietPhieuKiem];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113005629_TaoDongKiem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261113005629_TaoDongKiem', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115135501_KhoaNgoaiPhieuKiem'
)
BEGIN
    ALTER TABLE [PhieuNhap] ADD CONSTRAINT [FK_PhieuNhap_PhieuKiem_MaPK] FOREIGN KEY ([MaPK]) REFERENCES [PhieuKiem] ([MaPK]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115135501_KhoaNgoaiPhieuKiem'
)
BEGIN
    ALTER TABLE [PhieuXuat] ADD CONSTRAINT [FK_PhieuXuat_PhieuKiem_MaPK] FOREIGN KEY ([MaPK]) REFERENCES [PhieuKiem] ([MaPK]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115135501_KhoaNgoaiPhieuKiem'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261115135501_KhoaNgoaiPhieuKiem', N'10.0.0');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    CREATE TABLE [DinhMucKho] (
        [MaKho] int NOT NULL,
        [MaBT] int NOT NULL,
        [TonToiThieu] int NOT NULL,
        [NgayAnToan] int NOT NULL,
        CONSTRAINT [PK_DinhMucKho] PRIMARY KEY ([MaKho], [MaBT]),
        CONSTRAINT [CK_DinhMucKho_KhongAm] CHECK ([TonToiThieu] >= 0 AND [NgayAnToan] >= 0),
        CONSTRAINT [FK_DinhMucKho_BienThe_MaBT] FOREIGN KEY ([MaBT]) REFERENCES [BienThe] ([MaBT]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DinhMucKho_Kho_MaKho] FOREIGN KEY ([MaKho]) REFERENCES [Kho] ([MaKho]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    CREATE TABLE [TuyenVanChuyen] (
        [MaKhoXuat] int NOT NULL,
        [MaKhoNhap] int NOT NULL,
        [DonGiaVC] decimal(18,2) NOT NULL,
        [PhiChuyen] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_TuyenVanChuyen] PRIMARY KEY ([MaKhoXuat], [MaKhoNhap]),
        CONSTRAINT [CK_TuyenVanChuyen_KhacKho] CHECK ([MaKhoXuat] <> [MaKhoNhap]),
        CONSTRAINT [CK_TuyenVanChuyen_KhongAm] CHECK ([DonGiaVC] >= 0 AND [PhiChuyen] >= 0),
        CONSTRAINT [FK_TuyenVanChuyen_Kho_MaKhoNhap] FOREIGN KEY ([MaKhoNhap]) REFERENCES [Kho] ([MaKho]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TuyenVanChuyen_Kho_MaKhoXuat] FOREIGN KEY ([MaKhoXuat]) REFERENCES [Kho] ([MaKho]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    CREATE TABLE [PhieuChuyen] (
        [MaPC] int NOT NULL IDENTITY,
        [SoPhieu] varchar(20) NOT NULL,
        [ThoiGian] datetime2(0) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [MaKhoXuat] int NOT NULL,
        [MaKhoNhap] int NOT NULL,
        [MaDoiTuong] int NOT NULL,
        [MaNguoiLap] int NOT NULL,
        [MaNguoiDuyet] int NULL,
        [DienGiai] nvarchar(500) NULL,
        [TrangThai] nvarchar(20) NOT NULL DEFAULT N'Nháp',
        CONSTRAINT [PK_PhieuChuyen] PRIMARY KEY ([MaPC]),
        CONSTRAINT [CK_PhieuChuyen_KhacKho] CHECK ([MaKhoXuat] <> [MaKhoNhap]),
        CONSTRAINT [CK_PhieuChuyen_NguoiDuyet] CHECK ([TrangThai] IN (N'Nháp', N'Đã hủy') OR [MaNguoiDuyet] IS NOT NULL),
        CONSTRAINT [CK_PhieuChuyen_TrangThai] CHECK ([TrangThai] IN (N'Nháp', N'Đã duyệt', N'Đã xuất', N'Đã nhận', N'Đã hủy')),
        CONSTRAINT [FK_PhieuChuyen_Kho_MaKhoNhap] FOREIGN KEY ([MaKhoNhap]) REFERENCES [Kho] ([MaKho]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhieuChuyen_Kho_MaKhoXuat] FOREIGN KEY ([MaKhoXuat]) REFERENCES [Kho] ([MaKho]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhieuChuyen_NguoiDung_MaDoiTuong] FOREIGN KEY ([MaDoiTuong]) REFERENCES [NguoiDung] ([MaND]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhieuChuyen_NguoiDung_MaNguoiDuyet] FOREIGN KEY ([MaNguoiDuyet]) REFERENCES [NguoiDung] ([MaND]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhieuChuyen_NguoiDung_MaNguoiLap] FOREIGN KEY ([MaNguoiLap]) REFERENCES [NguoiDung] ([MaND]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhieuChuyen_TuyenVanChuyen_MaKhoXuat_MaKhoNhap] FOREIGN KEY ([MaKhoXuat], [MaKhoNhap]) REFERENCES [TuyenVanChuyen] ([MaKhoXuat], [MaKhoNhap]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    CREATE TABLE [ChiTietChuyen] (
        [MaCTC] int NOT NULL IDENTITY,
        [MaPC] int NOT NULL,
        [MaLo] int NOT NULL,
        [MaViTriXuat] int NOT NULL,
        [MaViTriNhap] int NOT NULL,
        [SoLuong] int NOT NULL,
        [SLNhan] int NULL,
        [LyDoLech] nvarchar(255) NULL,
        CONSTRAINT [PK_ChiTietChuyen] PRIMARY KEY ([MaCTC]),
        CONSTRAINT [CK_ChiTietChuyen_KhacO] CHECK ([MaViTriXuat] <> [MaViTriNhap]),
        CONSTRAINT [CK_ChiTietChuyen_LyDoLech] CHECK ([SLNhan] IS NULL OR [SLNhan] = [SoLuong] OR [LyDoLech] IS NOT NULL),
        CONSTRAINT [CK_ChiTietChuyen_SLNhan] CHECK ([SLNhan] >= 0 AND [SLNhan] <= [SoLuong]),
        CONSTRAINT [CK_ChiTietChuyen_SoLuong] CHECK ([SoLuong] > 0),
        CONSTRAINT [FK_ChiTietChuyen_LoHang_MaLo] FOREIGN KEY ([MaLo]) REFERENCES [LoHang] ([MaLo]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietChuyen_PhieuChuyen_MaPC] FOREIGN KEY ([MaPC]) REFERENCES [PhieuChuyen] ([MaPC]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietChuyen_ViTri_MaViTriNhap] FOREIGN KEY ([MaViTriNhap]) REFERENCES [ViTri] ([MaViTri]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ChiTietChuyen_ViTri_MaViTriXuat] FOREIGN KEY ([MaViTriXuat]) REFERENCES [ViTri] ([MaViTri]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    CREATE INDEX [IX_GoiYAI_MaPC] ON [GoiYAI] ([MaPC]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    CREATE INDEX [IX_ChiTietChuyen_MaLo] ON [ChiTietChuyen] ([MaLo]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ChiTietChuyen_MaPC_MaLo_MaViTriNhap] ON [ChiTietChuyen] ([MaPC], [MaLo], [MaViTriNhap]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ChiTietChuyen_MaPC_MaLo_MaViTriXuat] ON [ChiTietChuyen] ([MaPC], [MaLo], [MaViTriXuat]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    CREATE INDEX [IX_ChiTietChuyen_MaViTriNhap] ON [ChiTietChuyen] ([MaViTriNhap]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    CREATE INDEX [IX_ChiTietChuyen_MaViTriXuat] ON [ChiTietChuyen] ([MaViTriXuat]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    CREATE INDEX [IX_DinhMucKho_MaBT] ON [DinhMucKho] ([MaBT]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    CREATE INDEX [IX_PhieuChuyen_MaDoiTuong] ON [PhieuChuyen] ([MaDoiTuong]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    CREATE INDEX [IX_PhieuChuyen_MaKhoNhap] ON [PhieuChuyen] ([MaKhoNhap]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    CREATE INDEX [IX_PhieuChuyen_MaKhoXuat_MaKhoNhap] ON [PhieuChuyen] ([MaKhoXuat], [MaKhoNhap]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    CREATE INDEX [IX_PhieuChuyen_MaKhoXuat_ThoiGian] ON [PhieuChuyen] ([MaKhoXuat], [ThoiGian]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    CREATE INDEX [IX_PhieuChuyen_MaNguoiDuyet] ON [PhieuChuyen] ([MaNguoiDuyet]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    CREATE INDEX [IX_PhieuChuyen_MaNguoiLap] ON [PhieuChuyen] ([MaNguoiLap]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PhieuChuyen_SoPhieu] ON [PhieuChuyen] ([SoPhieu]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    CREATE INDEX [IX_TuyenVanChuyen_MaKhoNhap] ON [TuyenVanChuyen] ([MaKhoNhap]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    ALTER TABLE [GoiYAI] ADD CONSTRAINT [FK_GoiYAI_PhieuChuyen_MaPC] FOREIGN KEY ([MaPC]) REFERENCES [PhieuChuyen] ([MaPC]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261118030632_DieuChuyenKho'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261118030632_DieuChuyenKho', N'10.0.0');
END;

COMMIT;
GO

