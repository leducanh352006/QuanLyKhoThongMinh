/* S1-04a | Kiem tra tieu chi "xong": ma dung mau, so luong o, rang buoc chan du lieu sai.
   Chay: sqlcmd -S <server> -f 65001 -i 03_kiem_tra_vitri.sql   (moi dong KET QUA phai la DAT) */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;  -- bat buoc cho cot tinh luu san va chi muc (sqlcmd mac dinh OFF)
SET ANSI_NULLS ON;
USE QuanLyKho;
GO

DECLARE @mau varchar(100) = 'K[0-9][0-9]-TA[0-9][0-9]-TG[0-9][0-9]-P[0-9][0-9]-H[0-9][0-9]-KE[0-9][0-9]-O[0-9][0-9]';

SELECT KiemTra = '3 kho', KetQua = IIF((SELECT COUNT(*) FROM dbo.ViTri WHERE Cap = 1) = 3, 'DAT', 'LOI')
UNION ALL SELECT 'Khoang 60 o (55-65)', IIF((SELECT COUNT(*) FROM dbo.ViTri WHERE Cap = 7) BETWEEN 55 AND 65, 'DAT', 'LOI')
UNION ALL SELECT 'Moi o co kich thuoc, tai trong duong',
       IIF(NOT EXISTS (SELECT 1 FROM dbo.ViTri WHERE Cap = 7 AND ISNULL(IIF(Dai > 0 AND Rong > 0 AND Cao > 0 AND TaiTrongToiDa > 0, 1, 0), 0) = 0), 'DAT', 'LOI')
UNION ALL SELECT 'Ma cap o dung mau K01-TA02-TG03-P04-H05-KE06-O07',
       IIF(NOT EXISTS (SELECT 1 FROM dbo.ViTri WHERE Cap = 7 AND MaViTri NOT LIKE @mau), 'DAT', 'LOI')
UNION ALL SELECT 'Co dung ma mau K01-TA02-TG03-P04-H05-KE06-O07',
       IIF(EXISTS (SELECT 1 FROM dbo.ViTri WHERE MaViTri = 'K01-TA02-TG03-P04-H05-KE06-O07'), 'DAT', 'LOI')
UNION ALL SELECT 'Moi o co du 7 cap to tien',
       IIF(NOT EXISTS (SELECT 1 FROM dbo.ViTri WHERE Cap = 7 AND LEN(MaViTri) - LEN(REPLACE(MaViTri, '-', '')) <> 6), 'DAT', 'LOI')
UNION ALL SELECT 'Cay du 7 cap, cha dung cap ngay tren',
       IIF(NOT EXISTS (SELECT 1 FROM dbo.ViTri c JOIN dbo.ViTri p ON p.MaViTri = c.MaCha WHERE p.Cap <> c.Cap - 1), 'DAT', 'LOI')
UNION ALL SELECT 'Co du loai khu: tiep nhan, luu tru, cach ly, xuat',
       IIF((SELECT COUNT(DISTINCT LoaiKhu) FROM dbo.ViTri WHERE Cap = 4) = 4, 'DAT', 'LOI');
GO

/* ---------- Cac thao tac sai phai bi chan (moi dong KET QUA = DAT neu bi chan) ---------- */
CREATE TABLE #R (KiemTra nvarchar(100), KetQua varchar(10));
DECLARE @loi bit;

-- 1. ma sai mau o cap 7
SET @loi = 0;
BEGIN TRY
    INSERT INTO dbo.ViTri (MaViTri, MaCha, Cap, Ten, LoaiKhu, DieuKienBaoQuan, Dai, Rong, Cao, TaiTrongToiDa)
    VALUES ('K01-TA01-TG01-P02-H01-KE01-X99', 'K01-TA01-TG01-P02-H01-KE01', 7, N'sai mau', 'LuuTru', 'Thuong', 1, 1, 1, 10);
END TRY BEGIN CATCH SET @loi = 1; END CATCH
INSERT #R VALUES (N'Chan ma sai mau', IIF(@loi = 1, 'DAT', 'LOI'));

-- 2. o khong co kich thuoc / tai trong
SET @loi = 0;
BEGIN TRY
    INSERT INTO dbo.ViTri (MaViTri, MaCha, Cap, Ten, LoaiKhu, DieuKienBaoQuan)
    VALUES ('K01-TA01-TG01-P02-H01-KE01-O99', 'K01-TA01-TG01-P02-H01-KE01', 7, N'thieu kich thuoc', 'LuuTru', 'Thuong');
END TRY BEGIN CATCH SET @loi = 1; END CATCH
INSERT #R VALUES (N'Chan o thieu kich thuoc, tai trong', IIF(@loi = 1, 'DAT', 'LOI'));

-- 3. cha sai cap (o gan thang vao hang, bo qua ke)
SET @loi = 0;
BEGIN TRY
    INSERT INTO dbo.ViTri (MaViTri, MaCha, Cap, Ten, LoaiKhu, DieuKienBaoQuan, Dai, Rong, Cao, TaiTrongToiDa)
    VALUES ('K01-TA01-TG01-P02-H01-KE01-O98', 'K01-TA01-TG01-P02-H01', 7, N'sai cha', 'LuuTru', 'Thuong', 1, 1, 1, 10);
END TRY BEGIN CATCH SET @loi = 1; END CATCH
INSERT #R VALUES (N'Chan cha sai cap', IIF(@loi = 1, 'DAT', 'LOI'));

-- 4. TonKho tai vi tri khong phai cap o
SET @loi = 0;
BEGIN TRY
    INSERT INTO dbo.TonKho (MaLo, MaViTri, SoLuong) VALUES ('LO-TEST', 'K01-TA01-TG01-P02-H01-KE01', 1);
END TRY BEGIN CATCH SET @loi = 1; END CATCH
INSERT #R VALUES (N'Chan ton kho ngoai cap o', IIF(@loi = 1, 'DAT', 'LOI'));

-- 5. ton am, so da giu lon hon ton
SET @loi = 0;
BEGIN TRY
    INSERT INTO dbo.TonKho (MaLo, MaViTri, SoLuong, SoLuongDaGiu) VALUES ('LO-TEST', 'K01-TA01-TG01-P02-H01-KE01-O01', 5, 6);
END TRY BEGIN CATCH SET @loi = 1; END CATCH
INSERT #R VALUES (N'Chan so da giu > ton', IIF(@loi = 1, 'DAT', 'LOI'));

-- 6. so bien dong bang 0; ma khoa chong lap trung
SET @loi = 0;
BEGIN TRY
    INSERT INTO dbo.SoBienDong (MaLo, MaViTri, SoLuongThayDoi, LoaiChungTu, MaChungTu, MaKhoaChongLap, NguoiThucHien)
    VALUES ('LO-TEST', 'K01-TA01-TG01-P02-H01-KE01-O01', 0, 'XepHang', 'CT1', 'KHOA-0', 'ND01');
END TRY BEGIN CATCH SET @loi = 1; END CATCH
INSERT #R VALUES (N'Chan so thay doi = 0', IIF(@loi = 1, 'DAT', 'LOI'));

BEGIN TRAN;
INSERT INTO dbo.SoBienDong (MaLo, MaViTri, SoLuongThayDoi, LoaiChungTu, MaChungTu, MaKhoaChongLap, NguoiThucHien)
VALUES ('LO-TEST', 'K01-TA01-TG01-P02-H01-KE01-O01', 5, 'XepHang', 'CT1', 'KHOA-1', 'ND01');
SET @loi = 0;
BEGIN TRY
    INSERT INTO dbo.SoBienDong (MaLo, MaViTri, SoLuongThayDoi, LoaiChungTu, MaChungTu, MaKhoaChongLap, NguoiThucHien)
    VALUES ('LO-TEST', 'K01-TA01-TG01-P02-H01-KE01-O01', 5, 'XepHang', 'CT1', 'KHOA-1', 'ND01');
END TRY BEGIN CATCH SET @loi = 1; END CATCH
ROLLBACK;
INSERT #R VALUES (N'Chan ghi lap cung khoa chong lap', IIF(@loi = 1, 'DAT', 'LOI'));

SELECT * FROM #R;
DROP TABLE #R;
GO
