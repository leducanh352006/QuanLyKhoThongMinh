/*
 * Thành phần quét mã dùng chung (Bảng 1.6 dòng 6 của báo cáo).
 * Markup: Views/Shared/_QuetMa.cshtml. Mọi màn hình quét mã dùng chung một thành phần này.
 *
 * - Ô nhập tự lấy focus; máy quét mã vạch (giả lập bàn phím) hoặc ứng dụng quét trên điện thoại gõ mã rồi gửi Enter.
 * - Mỗi lần Enter phát sự kiện "quetma:them" (nổi bọt lên document), xóa ô và trả focus để quét tiếp:
 *       document.addEventListener("quetma:them", function (e) { e.detail.giaTri; e.detail.loaiMa; e.detail.nguon; });
 *   nguon là "ban-phim" hoặc "camera".
 * - Nếu trình duyệt có BarcodeDetector và trang chạy trên HTTPS (hoặc localhost), nút "Quét bằng camera" hiện ra.
 */
(function (global) {
    "use strict";

    var KHOANG_CHONG_TRUNG_MS = 1500;   // cùng một mã trong khoảng này chỉ tính một lần khi quét bằng camera
    var CHU_KY_QUET_MS = 250;

    function coHoTroCamera() {
        return typeof global.BarcodeDetector === "function" &&
            global.isSecureContext === true &&
            !!(global.navigator && global.navigator.mediaDevices && global.navigator.mediaDevices.getUserMedia);
    }

    function khoiTao(goc) {
        if (!goc || goc.__quetMaDaKhoiTao) { return; }
        goc.__quetMaDaKhoiTao = true;

        var o = goc.querySelector("[data-quet-ma-input]");
        var nutCamera = goc.querySelector("[data-quet-ma-camera]");
        var video = goc.querySelector("[data-quet-ma-video]");
        var thongBao = goc.querySelector("[data-quet-ma-thongbao]");
        var loaiMa = goc.getAttribute("data-loai-ma") || "bienthe";
        if (!o) { return; }

        function them(giaTri, nguon) {
            giaTri = (giaTri || "").trim();
            if (!giaTri) { return false; }
            goc.dispatchEvent(new CustomEvent("quetma:them", {
                bubbles: true,
                detail: { giaTri: giaTri, loaiMa: loaiMa, nguon: nguon, goc: goc }
            }));
            if (thongBao) { thongBao.textContent = "Đã thêm: " + giaTri; }
            return true;
        }

        o.addEventListener("keydown", function (e) {
            if (e.key !== "Enter") { return; }
            e.preventDefault();                       // không gửi form khi quét
            if (them(o.value, "ban-phim")) { o.value = ""; }
            o.focus();
        });

        goc.addEventListener("quetma:focus", function () { o.focus(); });

        if (!global.document.activeElement || global.document.activeElement === global.document.body) {
            o.focus();
        }

        // ---- quét bằng camera (chỉ khi trình duyệt hỗ trợ) ----
        if (!nutCamera || !video || !coHoTroCamera()) { return; }
        nutCamera.hidden = false;

        var luong = null, boDem = null, boQuet = null, maGanNhat = "", lucGanNhat = 0;

        function dung() {
            if (boDem) { global.clearInterval(boDem); boDem = null; }
            if (luong) { luong.getTracks().forEach(function (t) { t.stop(); }); luong = null; }
            video.srcObject = null;
            video.hidden = true;
            nutCamera.textContent = "Quét bằng camera";
            nutCamera.setAttribute("aria-pressed", "false");
        }

        function bat() {
            boQuet = boQuet || new global.BarcodeDetector();
            return global.navigator.mediaDevices.getUserMedia({ video: { facingMode: "environment" } })
                .then(function (s) {
                    luong = s;
                    video.srcObject = s;
                    video.hidden = false;
                    nutCamera.textContent = "Tắt camera";
                    nutCamera.setAttribute("aria-pressed", "true");
                    return video.play();
                })
                .then(function () {
                    boDem = global.setInterval(function () {
                        if (!luong || video.readyState < 2) { return; }
                        boQuet.detect(video).then(function (kq) {
                            if (!kq || !kq.length) { return; }
                            var v = kq[0].rawValue, bayGio = Date.now();
                            var trung = v === maGanNhat && bayGio - lucGanNhat < KHOANG_CHONG_TRUNG_MS;
                            maGanNhat = v; lucGanNhat = bayGio;
                            if (!trung) { them(v, "camera"); }
                        }).catch(function () { /* khung hình lỗi: bỏ qua, quét khung sau */ });
                    }, CHU_KY_QUET_MS);
                })
                .catch(function (loi) {
                    dung();
                    if (thongBao) { thongBao.textContent = "Không mở được camera: " + (loi && loi.message ? loi.message : loi); }
                });
        }

        nutCamera.addEventListener("click", function () { if (luong) { dung(); } else { bat(); } });
        global.document.addEventListener("visibilitychange", function () { if (global.document.hidden) { dung(); } });
        global.addEventListener("pagehide", dung);
    }

    function khoiTaoTatCa(goc) {
        Array.prototype.forEach.call((goc || global.document).querySelectorAll("[data-quet-ma]"), khoiTao);
    }

    global.QuetMa = { khoiTao: khoiTao, khoiTaoTatCa: khoiTaoTatCa };

    if (global.document.readyState === "loading") {
        global.document.addEventListener("DOMContentLoaded", function () { khoiTaoTatCa(); });
    } else {
        khoiTaoTatCa();
    }
})(window);
