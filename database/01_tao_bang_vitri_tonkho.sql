/* =====================================================================
   S1-04a | Sprint 1, tuan 1
   Tao bang ViTri, TonKho, SoBienDong theo Hinh 2.11 va Bang 2.34
   (muc 2.16.3: Bang 2.31 ma vi tri 7 cap, Bang 2.32 bang du lieu;
    muc 2.16.4: Hinh 2.11 ERD, Bang 2.34 tu dien du lieu)

   Chay: sqlcmd -S <server> -E -C -f 65001 -i 01_tao_bang_vitri_tonkho.sql
   Chay lai duoc (xoa 3 bang cu roi tao lai).

   Cot cua tung bang giong Hinh 2.11 va Bang 2.32, khong them cot nao.
   Khoa ngoai toi Lo, NguoiDung (S1-04b, tuan 2) duoc noi o muc 5.
   ===================================================================== */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
IF DB_ID(N'QuanLyKho') IS NULL
    CREATE DATABASE QuanLyKho COLLATE Vietnamese_100_CI_AS;
GO
USE QuanLyKho;
GO

IF OBJECT_ID(N'dbo.SoBienDong', N'U') IS NOT NULL DROP TABLE dbo.SoBienDong;
IF OBJECT_ID(N'dbo.TonKho',     N'U') IS NOT NULL DROP TABLE dbo.TonKho;
IF OBJECT_ID(N'dbo.ViTri',      N'U') IS NOT NULL DROP TABLE dbo.ViTri;
GO

/* =====================================================================
   1. ViTri - cay vi tri 7 cap (Bang 2.31)
      Cap 1 Kho | 2 Toa | 3 Tang | 4 Phong-day | 5 Hang | 6 Ke | 7 O
      Ma: K01-TA02-TG03-P04-H05-KE06-O07
   ===================================================================== */
CREATE TABLE dbo.ViTri
(
    MaViTri         varchar(40)   NOT NULL,   -- Bang 2.34 dong 2: varchar(40)
    MaCha           varchar(40)   NULL,       -- tu tham chieu; rong chi o cap kho
    Cap             tinyint       NOT NULL,   -- 1 den 7
    Ten             nvarchar(300) NOT NULL,   -- ten ghi du duong dan (Bang 2.12 dong 1)
    LoaiKhu         varchar(20)   NULL,       -- cap 4: TiepNhan | LuuTru | CachLy | Xuat
    DieuKienBaoQuan varchar(10)   NULL,       -- cap 4: Thuong | Lanh
    Dai             decimal(8,3)  NULL,       -- cap 7, met
    Rong            decimal(8,3)  NULL,
    Cao             decimal(8,3)  NULL,
    TaiTrongToiDa   decimal(10,3) NULL,       -- cap 7, kg
    DangKhoa        bit           NOT NULL CONSTRAINT DF_ViTri_DangKhoa DEFAULT (0),   -- khoa kiem ke

    CONSTRAINT PK_ViTri PRIMARY KEY (MaViTri),
    CONSTRAINT FK_ViTri_MaCha FOREIGN KEY (MaCha) REFERENCES dbo.ViTri (MaViTri),

    CONSTRAINT CK_ViTri_Cap CHECK (Cap BETWEEN 1 AND 7),
    -- MaCha chi rong o cap kho
    CONSTRAINT CK_ViTri_MaCha CHECK ((Cap = 1 AND MaCha IS NULL) OR (Cap > 1 AND MaCha IS NOT NULL)),
    -- dung mau Bang 2.31 theo tung cap; moi cap them mot doan vao ma cha
    CONSTRAINT CK_ViTri_Mau CHECK
    (
        CASE Cap
            WHEN 1 THEN IIF(MaViTri LIKE 'K[0-9][0-9]', 1, 0)
            WHEN 2 THEN IIF(MaViTri LIKE 'K[0-9][0-9]-TA[0-9][0-9]', 1, 0)
            WHEN 3 THEN IIF(MaViTri LIKE 'K[0-9][0-9]-TA[0-9][0-9]-TG[0-9][0-9]', 1, 0)
            WHEN 4 THEN IIF(MaViTri LIKE 'K[0-9][0-9]-TA[0-9][0-9]-TG[0-9][0-9]-P[0-9][0-9]', 1, 0)
            WHEN 5 THEN IIF(MaViTri LIKE 'K[0-9][0-9]-TA[0-9][0-9]-TG[0-9][0-9]-P[0-9][0-9]-H[0-9][0-9]', 1, 0)
            WHEN 6 THEN IIF(MaViTri LIKE 'K[0-9][0-9]-TA[0-9][0-9]-TG[0-9][0-9]-P[0-9][0-9]-H[0-9][0-9]-KE[0-9][0-9]', 1, 0)
            WHEN 7 THEN IIF(MaViTri LIKE 'K[0-9][0-9]-TA[0-9][0-9]-TG[0-9][0-9]-P[0-9][0-9]-H[0-9][0-9]-KE[0-9][0-9]-O[0-9][0-9]', 1, 0)
        END = 1
    ),
    CONSTRAINT CK_ViTri_TienTo CHECK (MaCha IS NULL OR LEFT(MaViTri, LEN(MaCha) + 1) = MaCha + '-'),
    -- Bang 2.31 dong 4: loai khu (tiep nhan, luu tru, cach ly, xuat), dieu kien bao quan (thuong, lanh)
    CONSTRAINT CK_ViTri_LoaiKhu CHECK (LoaiKhu IS NULL OR LoaiKhu IN ('TiepNhan', 'LuuTru', 'CachLy', 'Xuat')),
    CONSTRAINT CK_ViTri_DieuKien CHECK (DieuKienBaoQuan IS NULL OR DieuKienBaoQuan IN ('Thuong', 'Lanh')),
    CONSTRAINT CK_ViTri_Khu CHECK (Cap <> 4 OR (LoaiKhu IS NOT NULL AND DieuKienBaoQuan IS NOT NULL)),
    -- Bang 2.34 dong 3, 4: kich thuoc, tai trong lon hon 0; bat buoc khi Cap = 7
    CONSTRAINT CK_ViTri_KichThuoc CHECK
    (
        (Dai IS NULL OR Dai > 0) AND (Rong IS NULL OR Rong > 0) AND (Cao IS NULL OR Cao > 0)
        AND (TaiTrongToiDa IS NULL OR TaiTrongToiDa > 0)
    ),
    CONSTRAINT CK_ViTri_O CHECK
    (
        Cap <> 7 OR (Dai IS NOT NULL AND Rong IS NOT NULL AND Cao IS NOT NULL AND TaiTrongToiDa IS NOT NULL)
    )
);
GO
CREATE INDEX IX_ViTri_MaCha ON dbo.ViTri (MaCha);
GO

-- Vi tri con phai o dung cap ngay duoi vi tri cha (Bang 2.34 dong 2: Cap tu 1 den 7)
CREATE TRIGGER dbo.TR_ViTri_CapCha ON dbo.ViTri AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.ViTri p ON p.MaViTri = i.MaCha WHERE p.Cap <> i.Cap - 1)
    BEGIN
        RAISERROR (N'Vi tri con phai o cap ngay duoi vi tri cha (Cap = Cap cha + 1).', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

/* =====================================================================
   2. TonKho - ton theo (lo, vi tri); PK (MaLo, MaViTri)
      Bang tong hop co chu dich, cap nhat cung transaction voi SoBienDong.
   ===================================================================== */
CREATE TABLE dbo.TonKho
(
    MaLo         varchar(20)   NOT NULL,                                     -- Bang 2.34 dong 1
    MaViTri      varchar(40)   NOT NULL,
    SoLuong      decimal(14,3) NOT NULL CONSTRAINT DF_TonKho_SoLuong DEFAULT (0),
    SoLuongDaGiu decimal(14,3) NOT NULL CONSTRAINT DF_TonKho_DaGiu DEFAULT (0),

    CONSTRAINT PK_TonKho PRIMARY KEY (MaLo, MaViTri),
    CONSTRAINT FK_TonKho_ViTri FOREIGN KEY (MaViTri) REFERENCES dbo.ViTri (MaViTri),
    -- Bang 2.34 dong 5: ton khong am; so da giu khong lon hon ton
    CONSTRAINT CK_TonKho_SoLuong CHECK (SoLuong >= 0),
    CONSTRAINT CK_TonKho_DaGiu CHECK (SoLuongDaGiu >= 0 AND SoLuongDaGiu <= SoLuong)
);
GO
CREATE INDEX IX_TonKho_MaViTri ON dbo.TonKho (MaViTri);
GO

/* =====================================================================
   3. SoBienDong - so ghi moi lan thay doi ton (theo lo va vi tri)
   ===================================================================== */
CREATE TABLE dbo.SoBienDong
(
    MaBienDong      bigint        IDENTITY(1,1) NOT NULL,
    MaLo            varchar(20)   NOT NULL,
    MaViTri         varchar(40)   NOT NULL,
    NguoiThucHien   varchar(20)   NOT NULL,                                  -- -> NguoiDung.MaNguoiDung
    SoLuongThayDoi  decimal(14,3) NOT NULL,
    LoaiChungTu     varchar(20)   NOT NULL,
    MaChungTu       varchar(20)   NOT NULL,
    MaKhoaChongLap  varchar(64)   NOT NULL,                                  -- Bang 2.34 dong 10
    ThoiDiem        datetime2     NOT NULL CONSTRAINT DF_SoBienDong_ThoiDiem DEFAULT (SYSDATETIME()),

    CONSTRAINT PK_SoBienDong PRIMARY KEY (MaBienDong),
    CONSTRAINT UQ_SoBienDong_MaKhoaChongLap UNIQUE (MaKhoaChongLap),       -- ERD: U
    CONSTRAINT FK_SoBienDong_ViTri FOREIGN KEY (MaViTri) REFERENCES dbo.ViTri (MaViTri),
    CONSTRAINT CK_SoBienDong_SoLuong CHECK (SoLuongThayDoi <> 0)            -- Bang 2.34 dong 5
);
GO
CREATE INDEX IX_SoBienDong_Lo_ViTri ON dbo.SoBienDong (MaLo, MaViTri);
GO

/* =====================================================================
   4. Hang chi nam o cap o (Bang 2.31 dong 7, Bang 2.12 dong 1)
   ===================================================================== */
CREATE TRIGGER dbo.TR_TonKho_CapO ON dbo.TonKho AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.ViTri v ON v.MaViTri = i.MaViTri WHERE v.Cap <> 7)
    BEGIN
        RAISERROR (N'Hang chi nam o cap o (Cap = 7).', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO
CREATE TRIGGER dbo.TR_SoBienDong_CapO ON dbo.SoBienDong AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN dbo.ViTri v ON v.MaViTri = i.MaViTri WHERE v.Cap <> 7)
    BEGIN
        RAISERROR (N'Hang chi nam o cap o (Cap = 7).', 16, 1);
        ROLLBACK TRANSACTION;
    END
END;
GO

/* =====================================================================
   5. Noi khoa ngoai toi Lo, NguoiDung (Hinh 2.11). Hai bang nay thuoc
      S1-04b (tuan 2). Doan nay bo qua neu bang chua co; sau khi S1-04b
      tao xong, chay lai doan nay (hoac cac lenh ALTER ben duoi).
   ===================================================================== */
IF OBJECT_ID(N'dbo.Lo', N'U') IS NOT NULL
BEGIN
    IF OBJECT_ID(N'FK_TonKho_Lo', N'F') IS NULL
        ALTER TABLE dbo.TonKho ADD CONSTRAINT FK_TonKho_Lo FOREIGN KEY (MaLo) REFERENCES dbo.Lo (MaLo);
    IF OBJECT_ID(N'FK_SoBienDong_Lo', N'F') IS NULL
        ALTER TABLE dbo.SoBienDong ADD CONSTRAINT FK_SoBienDong_Lo FOREIGN KEY (MaLo) REFERENCES dbo.Lo (MaLo);
END
IF OBJECT_ID(N'dbo.NguoiDung', N'U') IS NOT NULL AND OBJECT_ID(N'FK_SoBienDong_NguoiDung', N'F') IS NULL
    ALTER TABLE dbo.SoBienDong ADD CONSTRAINT FK_SoBienDong_NguoiDung
        FOREIGN KEY (NguoiThucHien) REFERENCES dbo.NguoiDung (MaNguoiDung);
GO

PRINT N'Da tao xong ViTri, TonKho, SoBienDong.';
GO
