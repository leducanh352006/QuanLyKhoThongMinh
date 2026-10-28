using System.Globalization;
using System.Text;

namespace QuanLyKho.Web.Areas.NhapKho.Services;

/// <summary>
/// Tạo mã vạch Code 128 (bộ ký tự B: chữ, số và ký hiệu ASCII 32 đến 126) để in tem, không cần thư viện ngoài.
/// Máy quét mã vạch thông thường và <c>BarcodeDetector</c> của trình duyệt (thành phần quét dùng chung) đều đọc được Code 128.
/// </summary>
public static class Code128
{
    // 106 mẫu: mỗi chữ số là độ rộng (số module) của vạch rồi khoảng trắng xen kẽ, bắt đầu bằng vạch. Mỗi ký hiệu rộng 11 module.
    // Thứ tự: giá trị 0 đến 105, rồi mẫu kết thúc (7 chữ số, rộng 13 module).
    private static readonly string[] Mau = (
        "212222 222122 222221 121223 121322 131222 122213 122312 132212 221213 221312 231212 112232 122132 122231 113222 " +
        "123122 123221 223211 221132 221231 213212 223112 312131 311222 321122 321221 312212 322112 322211 212123 212321 " +
        "232121 111323 131123 131321 112313 132113 132311 211313 231113 231311 112133 112331 132131 113123 113321 133121 " +
        "313121 211331 231131 213113 213311 213131 311123 311321 331121 312113 312311 332111 314111 221411 431111 111224 " +
        "111422 121124 121421 141122 141221 112214 112412 122114 122411 142112 142211 241211 221114 413111 241112 134111 " +
        "111242 121142 121241 114212 124112 124211 411212 421112 421211 212141 214121 412121 111143 111341 131141 114113 " +
        "114311 411113 411311 113141 114131 311141 411131 211412 211214 211232 2331112").Split(' ');

    private const int BatDauB = 104;
    private const int KetThuc = 106;
    private const int LeTrong = 10;   // vùng trống hai đầu, tối thiểu 10 module theo chuẩn

    /// <summary>Mã vạch dùng được khi không rỗng và chỉ gồm ký tự ASCII in được (không dấu tiếng Việt).</summary>
    public static bool HopLe(string? s) => !string.IsNullOrEmpty(s) && s.All(c => c >= ' ' && c <= '~');

    /// <summary>Độ rộng lần lượt của vạch, khoảng trắng, vạch... (đơn vị module). Trả về null nếu <paramref name="s"/> không mã hóa được.</summary>
    public static int[]? MaHoa(string? s)
    {
        if (!HopLe(s)) return null;
        var ky = new List<int> { BatDauB };
        var tong = BatDauB;
        for (var i = 0; i < s!.Length; i++)
        {
            var v = s[i] - ' ';
            ky.Add(v);
            tong += v * (i + 1);
        }
        ky.Add(tong % 103);   // ký tự kiểm tra
        ky.Add(KetThuc);
        return ky.SelectMany(v => Mau[v].Select(c => c - '0')).ToArray();
    }

    /// <summary>Ảnh SVG của mã vạch (vạch đen trên nền trong suốt), mỗi module rộng 0,25 mm. Cao do CSS đặt. Trả về null nếu không mã hóa được.</summary>
    public static string? Svg(string? s)
    {
        var rong = MaHoa(s);
        if (rong is null) return null;
        var tong = LeTrong * 2 + rong.Sum();
        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" class=\"vach\" viewBox=\"0 0 {tong} 1\" preserveAspectRatio=\"none\" ")
          .Append($"shape-rendering=\"crispEdges\" aria-hidden=\"true\" style=\"width:{(tong * 0.25).ToString(CultureInfo.InvariantCulture)}mm\"><path fill=\"#000\" d=\"");
        var x = LeTrong;
        for (var i = 0; i < rong.Length; i++)
        {
            if (i % 2 == 0) sb.Append($"M{x} 0h{rong[i]}v1h-{rong[i]}z");   // chỉ vẽ vạch, khoảng trắng để trống
            x += rong[i];
        }
        return sb.Append("\"/></svg>").ToString();
    }
}
