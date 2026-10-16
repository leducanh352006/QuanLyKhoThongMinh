/* =====================================================================
   S1-04a | Du lieu thu cho ViTri: 3 kho, 61 o co kich thuoc va tai trong.
   Chay sau 01_tao_bang_vitri_tonkho.sql:
       sqlcmd -S <server> -E -C -f 65001 -i 02_du_lieu_thu_vitri.sql
   Chay lai duoc (xoa du lieu vi tri cu; TonKho, SoBienDong cung bi xoa).

   Moi kho: Toa TA01, Tang TG01 gom 5 phong-day
     P01 TiepNhan | P02 LuuTru Thuong | P03 LuuTru Lanh | P04 CachLy | P05 Xuat
   Rieng kho 1 co them Toa TA02, Tang TG03, Phong P04 (luu tru), Hang H05,
   Ke KE06, O O07 de co dung ma mau K01-TA02-TG03-P04-H05-KE06-O07 (muc 2.16.3).
   Ten vi tri ghi du duong dan, vi du: Kho 1 / Toa 2 / Tang 3 / Phong 4 / Hang 5 / Ke 6 / O 7.
   ===================================================================== */
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
GO
USE QuanLyKho;
GO

BEGIN TRAN;

DELETE FROM dbo.SoBienDong;
DELETE FROM dbo.TonKho;
DECLARE @c tinyint = 7;
WHILE @c >= 1
BEGIN
    DELETE FROM dbo.ViTri WHERE Cap = @c;
    SET @c -= 1;
END

/* Bo cuc moi kho: phong, loai khu, dieu kien bao quan, so hang, so ke moi hang, so o moi ke, kich thuoc o (m), tai trong (kg) */
CREATE TABLE #Phong
(
    Phong tinyint, LoaiKhu varchar(20), DieuKien varchar(10),
    SoHang tinyint, SoKe tinyint, SoO tinyint,
    Dai decimal(8,3), Rong decimal(8,3), Cao decimal(8,3), TaiTrong decimal(10,3)
);
INSERT INTO #Phong VALUES
 (1, 'TiepNhan', 'Thuong', 1, 1, 4, 1.200, 1.000, 1.200, 500.000),
 (2, 'LuuTru',   'Thuong', 2, 1, 5, 1.000, 1.000, 1.000,  50.000),   -- 10 o
 (3, 'LuuTru',   'Lanh',   1, 1, 3, 0.800, 0.600, 0.500,  40.000),
 (4, 'CachLy',   'Thuong', 1, 1, 2, 1.000, 1.000, 1.000,  50.000),
 (5, 'Xuat',     'Thuong', 1, 1, 1, 1.200, 1.000, 1.200, 500.000);   -- 4+10+3+2+1 = 20 o

DECLARE @Kho TABLE (K tinyint);
INSERT INTO @Kho VALUES (1), (2), (3);

-- Cap 1: Kho
INSERT INTO dbo.ViTri (MaViTri, MaCha, Cap, Ten)
SELECT FORMATMESSAGE('K%02d', K), NULL, 1, CONCAT(N'Kho ', K) FROM @Kho;

-- Cap 2: Toa 1 moi kho, kho 1 them Toa 2
INSERT INTO dbo.ViTri (MaViTri, MaCha, Cap, Ten)
SELECT FORMATMESSAGE('K%02d-TA%02d', K, T), FORMATMESSAGE('K%02d', K), 2, CONCAT(N'Kho ', K, N' / Tòa ', T)
FROM (SELECT K, 1 AS T FROM @Kho UNION ALL SELECT 1, 2) x;

-- Cap 3: Tang 1 cua Toa 1; Tang 3 cua Toa 2 (kho 1)
INSERT INTO dbo.ViTri (MaViTri, MaCha, Cap, Ten)
SELECT FORMATMESSAGE('K%02d-TA%02d-TG%02d', K, T, G), FORMATMESSAGE('K%02d-TA%02d', K, T), 3,
       CONCAT(N'Kho ', K, N' / Tòa ', T, N' / Tầng ', G)
FROM (SELECT K, 1 AS T, 1 AS G FROM @Kho UNION ALL SELECT 1, 2, 3) x;

-- Cap 4: Phong - day
INSERT INTO dbo.ViTri (MaViTri, MaCha, Cap, Ten, LoaiKhu, DieuKienBaoQuan)
SELECT FORMATMESSAGE('K%02d-TA01-TG01-P%02d', k.K, p.Phong), FORMATMESSAGE('K%02d-TA01-TG01', k.K), 4,
       CONCAT(N'Kho ', k.K, N' / Tòa 1 / Tầng 1 / Phòng ', p.Phong), p.LoaiKhu, p.DieuKien
FROM @Kho k CROSS JOIN #Phong p;

-- Cap 5: Hang
INSERT INTO dbo.ViTri (MaViTri, MaCha, Cap, Ten)
SELECT FORMATMESSAGE('K%02d-TA01-TG01-P%02d-H%02d', k.K, p.Phong, h.n), FORMATMESSAGE('K%02d-TA01-TG01-P%02d', k.K, p.Phong), 5,
       CONCAT(N'Kho ', k.K, N' / Tòa 1 / Tầng 1 / Phòng ', p.Phong, N' / Hàng ', h.n)
FROM @Kho k CROSS JOIN #Phong p JOIN (VALUES (1),(2),(3)) h(n) ON h.n <= p.SoHang;

-- Cap 6: Ke
INSERT INTO dbo.ViTri (MaViTri, MaCha, Cap, Ten)
SELECT FORMATMESSAGE('K%02d-TA01-TG01-P%02d-H%02d-KE%02d', k.K, p.Phong, h.n, e.n),
       FORMATMESSAGE('K%02d-TA01-TG01-P%02d-H%02d', k.K, p.Phong, h.n), 6,
       CONCAT(N'Kho ', k.K, N' / Tòa 1 / Tầng 1 / Phòng ', p.Phong, N' / Hàng ', h.n, N' / Kệ ', e.n)
FROM @Kho k CROSS JOIN #Phong p
JOIN (VALUES (1),(2),(3)) h(n) ON h.n <= p.SoHang
JOIN (VALUES (1),(2),(3)) e(n) ON e.n <= p.SoKe;

-- Cap 7: O (co kich thuoc ben trong va tai trong toi da)
INSERT INTO dbo.ViTri (MaViTri, MaCha, Cap, Ten, Dai, Rong, Cao, TaiTrongToiDa)
SELECT FORMATMESSAGE('K%02d-TA01-TG01-P%02d-H%02d-KE%02d-O%02d', k.K, p.Phong, h.n, e.n, o.n),
       FORMATMESSAGE('K%02d-TA01-TG01-P%02d-H%02d-KE%02d', k.K, p.Phong, h.n, e.n), 7,
       CONCAT(N'Kho ', k.K, N' / Tòa 1 / Tầng 1 / Phòng ', p.Phong, N' / Hàng ', h.n, N' / Kệ ', e.n, N' / Ô ', o.n),
       p.Dai, p.Rong, p.Cao, p.TaiTrong
FROM @Kho k CROSS JOIN #Phong p
JOIN (VALUES (1),(2),(3)) h(n) ON h.n <= p.SoHang
JOIN (VALUES (1),(2),(3)) e(n) ON e.n <= p.SoKe
JOIN (VALUES (1),(2),(3),(4),(5)) o(n) ON o.n <= p.SoO;

-- Ma mau muc 2.16.3: K01-TA02-TG03-P04-H05-KE06-O07 = Kho 1, Toa 2, Tang 3, Phong 4, Hang 5, Ke 6, O 7
INSERT INTO dbo.ViTri (MaViTri, MaCha, Cap, Ten, LoaiKhu, DieuKienBaoQuan) VALUES
 ('K01-TA02-TG03-P04', 'K01-TA02-TG03', 4, N'Kho 1 / Tòa 2 / Tầng 3 / Phòng 4', 'LuuTru', 'Thuong');
INSERT INTO dbo.ViTri (MaViTri, MaCha, Cap, Ten) VALUES
 ('K01-TA02-TG03-P04-H05', 'K01-TA02-TG03-P04', 5, N'Kho 1 / Tòa 2 / Tầng 3 / Phòng 4 / Hàng 5'),
 ('K01-TA02-TG03-P04-H05-KE06', 'K01-TA02-TG03-P04-H05', 6, N'Kho 1 / Tòa 2 / Tầng 3 / Phòng 4 / Hàng 5 / Kệ 6');
INSERT INTO dbo.ViTri (MaViTri, MaCha, Cap, Ten, Dai, Rong, Cao, TaiTrongToiDa) VALUES
 ('K01-TA02-TG03-P04-H05-KE06-O07', 'K01-TA02-TG03-P04-H05-KE06', 7,
  N'Kho 1 / Tòa 2 / Tầng 3 / Phòng 4 / Hàng 5 / Kệ 6 / Ô 7', 1.000, 1.000, 1.000, 50.000);

DROP TABLE #Phong;
COMMIT;
GO

SELECT Cap, SoViTri = COUNT(*) FROM dbo.ViTri GROUP BY Cap ORDER BY Cap;
SELECT Kho = LEFT(MaViTri, 3), SoO = COUNT(*), TongTaiTrongKg = SUM(TaiTrongToiDa)
FROM dbo.ViTri WHERE Cap = 7 GROUP BY LEFT(MaViTri, 3) ORDER BY 1;
GO
