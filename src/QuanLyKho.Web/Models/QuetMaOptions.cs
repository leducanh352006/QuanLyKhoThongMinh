namespace QuanLyKho.Web.Models;

/// <summary>Tham số của thành phần quét mã dùng chung (partial view <c>_QuetMa</c>).</summary>
/// <param name="Id">id của ô nhập (duy nhất trong trang).</param>
/// <param name="Nhan">Nhãn hiển thị phía trên ô nhập.</param>
/// <param name="GoiY">Chữ mờ trong ô nhập.</param>
/// <param name="LoaiMa">Loại mã đang chờ: "bienthe", "lo" hoặc "o"; trang dùng giá trị này khi xử lý sự kiện quetma:them.</param>
public sealed record QuetMaOptions(string Id, string Nhan, string GoiY, string LoaiMa = "bienthe");
