# Tiêu chí chấp nhận cho FR01, FR09, FR10, FR11, FR12 (S1-01)

| | |
|---|---|
| Mã việc | S1-01, Sprint 1, tuần 1 (Bảng 1.9) |
| Người làm | TV1 – Nguyễn Văn Hiếu |
| Nguồn | Báo cáo mục 2.15 (Bảng 2.26), Bảng 2.33; các Bảng 1.5, 1.6, 2.1, 2.8, 2.10, 2.11, 2.12, 2.14, 2.31 đến 2.35 |
| Trạng thái | Bản nháp, chờ cả nhóm duyệt (tiêu chí hoàn thành của S1-01) |
| Việc chờ tài liệu này | S1-03, S1-04, S1-05, S1-06 và các việc Sprint 2, 3 trong Bảng 1.9 |

## 1. Cách đọc tài liệu

- Mỗi tiêu chí viết theo **Given – When – Then**: *Given* là bối cảnh và dữ liệu có sẵn, *When* là thao tác của người dùng, *Then* là kết quả hệ thống phải cho, quan sát được.
- Mã tiêu chí dạng `FR12-03`: số FR, số thứ tự. Mỗi tiêu chí là một ca kiểm thử tối thiểu (S1-06 sẽ gắn ca kiểm thử vào từng mã).
- Con số trong ví dụ là **dữ liệu thử đề xuất**; riêng các ca ở FR12 dùng đúng số liệu ví dụ của báo cáo (mục 2.16.3).
- "Từ chối" nghĩa là hệ thống không ghi gì vào dữ liệu và trả lý do. Lỗi nghiệp vụ trả mã **422** kèm lý do; lỗi xếp hàng kèm **số tối đa còn xếp được** (mục 2.16.4).
- Mọi kiểm tra quyền, kiểm tra ràng buộc diễn ra **ở máy chủ**; ẩn nút trên giao diện không được tính là đạt.

### Bối cảnh chung của các ca kiểm thử

| Mục | Giá trị |
|---|---|
| Kho | 3 kho `K01`, `K02`, `K03`; mỗi kho có khu tiếp nhận, lưu trữ thường, lưu trữ lạnh, cách ly, xuất |
| Ô | Khoảng 60 ô có kích thước trong và tải trọng (S1-04) |
| Biến thể | Khoảng 50 biến thể có kích thước đóng gói và khối lượng một đơn vị |
| Người dùng mẫu | `qtv` (quản trị viên), `qlk1` (quản lý kho, kho `K01`), `tk1` (thủ kho, kho `K01`), `tk2` (thủ kho, kho `K02`), `nvbh` (nhân viên bán hàng) |

## 2. FR01 – Hóa đơn nhập kho

> Là thủ kho, tôi cần lập hóa đơn nhập kho từ đơn mua, ghi số thực nhận, lô và hạn dùng, hệ thống tự tính thành tiền và tổng tiền theo đơn giá, để tồn và giá trị nhập đúng ngay khi nhận hàng. *(Must; Bảng 2.1 dòng 1, 5, 7)*

Hóa đơn nhập kho chính là phiếu nhập kho mua hàng có đơn giá, thành tiền, tổng tiền. Trạng thái phiếu: Lưu tạm → Hoàn tất → Đã hủy (Bảng 2.14 dòng 5).

**FR01-01 · Lập phiếu từ đơn mua**
- **Given** đơn mua của `K01` có dòng biến thể A đặt 100, đã nhận 0, đơn giá 25.000; `tk1` đã đăng nhập
- **When** `tk1` lập phiếu nhập từ dòng đó và ghi thực nhận 40
- **Then** phiếu có trạng thái **Lưu tạm**, đơn giá lấy theo đơn mua, và **tồn kho chưa thay đổi**

**FR01-02 · Chặn nhận vượt số chưa nhận**
- **Given** dòng đơn mua đặt 100, đã nhận 70 (còn chưa nhận 30)
- **When** thủ kho ghi thực nhận 40
- **Then** hệ thống từ chối (422) kèm lý do, không lưu dòng này

**FR01-03 · Số thực nhận phải lớn hơn 0**
- **Given** một dòng phiếu nhập đang soạn
- **When** thủ kho ghi thực nhận 0 hoặc số âm
- **Then** hệ thống từ chối dòng đó và nêu lý do

**FR01-04 · Mỗi dòng nhập sinh một lô mới**
- **Given** phiếu nhập có 2 dòng cùng biến thể A, thực nhận 40 và 10
- **When** thủ kho lưu phiếu
- **Then** có **2 lô** với mã do hệ thống sinh, **không trùng nhau** và không trùng lô đã có

**FR01-05 · Bắt buộc hạn dùng khi biến thể theo dõi hạn dùng**
- **Given** biến thể B bật theo dõi hạn dùng, biến thể C không bật
- **When** thủ kho lưu dòng B để trống hạn dùng; sau đó lưu dòng C để trống hạn dùng
- **Then** dòng B bị **từ chối** (thiếu hạn dùng); dòng C **được lưu**

**FR01-06 · Thành tiền và tổng tiền do hệ thống tính**
- **Given** phiếu có dòng 1: thực nhận 40, đơn giá 25.000; dòng 2: thực nhận 10, đơn giá 12.500
- **When** thủ kho (hoặc quản lý kho) xem phiếu
- **Then** thành tiền dòng 1 là **1.000.000**, dòng 2 là **125.000**, tổng tiền là **1.125.000**; các giá trị này **không sửa tay** được, và đổi số thực nhận thì tính lại ngay

**FR01-07 · Phân loại chất lượng quyết định khu đặt hàng**
- **Given** phiếu có dòng đánh dấu **đạt** và dòng đánh dấu **lỗi**
- **When** quản lý kho xác nhận hoàn tất phiếu (FR01-08)
- **Then** lô đạt nằm ở ô thuộc **khu tiếp nhận**, lô lỗi nằm ở ô thuộc **khu cách ly**; **cả hai không tính vào tồn khả dụng**

**FR01-08 · Xác nhận hoàn tất ghi tồn trong một transaction**
- **Given** phiếu ở trạng thái Lưu tạm, hợp lệ, của kho `K01`
- **When** `qlk1` xác nhận hoàn tất
- **Then** phiếu chuyển **Hoàn tất**; mỗi lô có đúng một dòng sổ biến động với số thay đổi bằng số thực nhận; tồn tăng đúng số đó; **tổng biến động của lô bằng số tồn của lô**; nhật ký ghi người xác nhận và thời điểm. Nếu một bước ghi lỗi thì **không có dòng nào được ghi** và phiếu vẫn Lưu tạm

**FR01-09 · Bấm lặp không ghi hai lần**
- **Given** yêu cầu xác nhận phiếu mang `Idempotency-Key = K`
- **When** yêu cầu đó được gửi hai lần liên tiếp (bấm đúp hoặc mạng thử lại)
- **Then** tồn chỉ tăng **một lần**; sổ biến động có **một** bộ dòng; lần gửi sau trả lại kết quả của lần đầu

**FR01-10 · Phiếu hoàn tất không sửa, hủy bằng bút toán đảo**
- **Given** phiếu đã Hoàn tất
- **When** người dùng sửa dòng phiếu; sau đó quản lý kho hủy phiếu
- **Then** việc sửa bị **từ chối**; việc hủy chuyển phiếu **Đã hủy**, ghi thêm dòng sổ biến động **đảo dấu** (không sửa, không xóa dòng cũ), tồn trở về giá trị trước khi nhập

**FR01-11 · Chỉ làm việc ở kho được giao**
- **Given** `tk2` chỉ được giao kho `K02`; đơn mua thuộc `K01`
- **When** `tk2` lập phiếu nhập từ đơn đó, hoặc lập phiếu có kho khác kho của đơn
- **Then** hệ thống **từ chối**; không phiếu nào được tạo. Phiếu luôn có kho trùng kho của đơn mua (Bảng 2.34 dòng 11)

**FR01-12 · Phân quyền theo vai cho bước xác nhận**
- **Given** phiếu Lưu tạm của `K01`
- **When** `tk1` (thủ kho) hoặc `nvbh` gọi thẳng API xác nhận hoàn tất, bỏ qua giao diện
- **Then** yêu cầu bị **từ chối**, phiếu và tồn **không đổi** (chỉ quản lý kho xác nhận, Bảng 2.1 dòng 7)

*Nằm ngoài FR01:* nhập hàng trả lại, nhập khác (FR15), tách lệnh nhập theo kho, đề xuất nhập bổ sung G3.

## 3. FR09 – Tài khoản và phân quyền

> Là quản trị viên, tôi cần tạo, khóa tài khoản và gán vai, kho, để người đã nghỉ không còn truy cập. *(Must; Bảng 2.11 dòng 2 đến 4)*

**FR09-01 · Tạo người dùng hợp lệ**
- **Given** `qtv` đã đăng nhập
- **When** `qtv` tạo người dùng có tên, email chưa dùng, vai **thủ kho**, được giao kho `K01`
- **Then** tài khoản được tạo ở trạng thái **Đang hoạt động**; người đó đăng nhập được và chỉ thấy kho `K01`

**FR09-02 · Trường bắt buộc và email không trùng**
- **Given** đã có tài khoản dùng email `a@x.vn`
- **When** `qtv` tạo người dùng thiếu tên, hoặc thiếu email, hoặc thiếu vai, hoặc dùng email `a@x.vn`
- **Then** mỗi trường hợp đều bị **từ chối** kèm lý do, không tài khoản nào được tạo

**FR09-03 · Thủ kho và quản lý kho phải có kho**
- **Given** `qtv` đang tạo người dùng vai thủ kho (hoặc quản lý kho)
- **When** `qtv` không chọn kho nào
- **Then** bị **từ chối**; vai quản lý kho và thủ kho nhận được **một hoặc nhiều kho**

**FR09-04 · Mỗi người một vai**
- **Given** một người dùng đang có vai thủ kho
- **When** `qtv` gán thêm vai quản lý kho cho cùng tài khoản
- **Then** tài khoản chỉ còn **một vai** (đổi vai thay vì thêm vai)

**FR09-05 · Phân quyền theo vai kiểm tra ở máy chủ**
- **Given** `nvbh` đăng nhập (chỉ được xem tồn, tạo đơn đặt hàng, đề nghị nhập hàng trả lại)
- **When** `nvbh` gọi trực tiếp API lập phiếu nhập, hoặc API tạo người dùng, không qua giao diện
- **Then** cả hai yêu cầu bị **từ chối**, dữ liệu không đổi. Tương tự, `tk1` gọi API quản trị bị từ chối

**FR09-06 · Phân quyền theo kho kiểm tra ở máy chủ**
- **Given** `tk1` chỉ được giao `K01`; có phiếu nhập của `K02`
- **When** `tk1` mở hoặc thao tác trên phiếu của `K02` (kể cả gọi API trực tiếp)
- **Then** bị **từ chối**; `tk1` **không thấy** chứng từ của `K02` trong danh sách

**FR09-07 · Khóa tài khoản mất quyền ngay**
- **Given** `tk1` đang đăng nhập và đang dùng hệ thống
- **When** `qtv` khóa tài khoản `tk1`
- **Then** yêu cầu **kế tiếp** của `tk1` bị từ chối ngay (không chờ hết phiên); đăng nhập lại bị từ chối; trạng thái hiển thị **Đã khóa**

**FR09-08 · Mở khóa**
- **Given** tài khoản `tk1` đã khóa
- **When** `qtv` mở khóa
- **Then** `tk1` đăng nhập được lại với đúng vai và kho trước đó

**FR09-09 · Không xóa cứng tài khoản đã có thao tác**
- **Given** `tk1` đã lập phiếu nhập
- **When** `qtv` xóa tài khoản `tk1`
- **Then** việc xóa bị **từ chối**; chỉ có thể **khóa**; phiếu cũ vẫn hiện đúng người lập

**FR09-10 · Luôn còn ít nhất một quản trị viên hoạt động**
- **Given** hệ thống chỉ còn một quản trị viên Đang hoạt động
- **When** khóa tài khoản đó, hoặc đổi vai của họ sang vai khác
- **Then** bị **từ chối**

**FR09-11 · Mật khẩu không bao giờ hiển thị**
- **Given** `qtv` mở danh sách và chi tiết người dùng, hoặc xuất danh sách
- **When** xem mọi màn hình, phản hồi API và tệp xuất
- **Then** **không có mật khẩu** (hay bản băm) ở bất kỳ đâu

**FR09-12 · Chỉ quản trị viên xem và sửa tài khoản người khác**
- **Given** `qlk1` đăng nhập
- **When** `qlk1` xem danh sách người dùng hoặc sửa vai, kho của người khác
- **Then** bị **từ chối**

**FR09-13 · Gán kho là một dữ liệu duy nhất**
- **Given** `qtv` gán `tk1` thêm kho `K03` ở màn hình **Người dùng**
- **When** `qtv` mở màn hình **Danh mục kho** của `K03`
- **Then** `tk1` đã có trong danh sách người dùng của `K03` (và ngược lại), không có hai bản dữ liệu lệch nhau (Bảng 2.10 dòng 3)

**FR09-14 · Ghi nhật ký phân quyền và khóa**
- **Given** `qtv` vừa tạo, đổi vai, gán kho, khóa một tài khoản
- **When** `qtv` tra nhật ký lọc theo loại thao tác
- **Then** mỗi thao tác có một dòng ghi **ai làm, vào lúc nào, trên tài khoản nào**; nhật ký chỉ thêm, **không sửa, không xóa**, chỉ quản trị viên xem

## 4. FR10 – Sản phẩm và biến thể

> Là quản trị viên, tôi cần thêm sản phẩm, biến thể theo thuộc tính, cờ hạn dùng và điều kiện bảo quản, để kho nhận được mọi loại hàng. *(Must; Bảng 2.8 dòng 2 đến 5)*

**FR10-01 · Thêm sản phẩm hợp lệ**
- **Given** `qtv` đã đăng nhập, chưa có sản phẩm mã `SUA01`
- **When** `qtv` thêm sản phẩm mã `SUA01`, tên "Sữa tươi", đơn vị tính "hộp", điều kiện bảo quản **lạnh**
- **Then** sản phẩm được lưu, hiện trong danh sách; mỗi sản phẩm chỉ có **một** đơn vị tính

**FR10-02 · Trường bắt buộc và mã duy nhất**
- **Given** đã có sản phẩm mã `SUA01`
- **When** thêm sản phẩm thiếu mã, hoặc thiếu tên, hoặc thiếu điều kiện bảo quản, hoặc trùng mã `SUA01`
- **Then** mỗi trường hợp **bị từ chối** kèm lý do

**FR10-03 · Điều kiện bảo quản chỉ có hai giá trị**
- **Given** form sản phẩm
- **When** nhập điều kiện bảo quản khác "thường" và "lạnh" (gọi API với giá trị lạ)
- **Then** bị **từ chối**

**FR10-04 · Tạo biến thể từ tổ hợp thuộc tính**
- **Given** sản phẩm `SUA01` có thuộc tính **dung tích** {180 ml, 1 L} và **loại** {có đường, không đường}, cả hai đang Sử dụng
- **When** `qtv` tạo biến thể
- **Then** hệ thống tạo **4 biến thể**, mỗi tổ hợp đúng **một** biến thể, mỗi biến thể có **mã riêng không trùng**

**FR10-05 · Không trùng tổ hợp, không trùng tên thuộc tính**
- **Given** `SUA01` đã có biến thể (1 L, không đường)
- **When** tạo thêm biến thể cùng tổ hợp đó; hoặc thêm thuộc tính có tên trùng thuộc tính đã có trong cùng sản phẩm
- **Then** cả hai bị **từ chối**

**FR10-06 · Kích thước đóng gói và khối lượng là số dương**
- **Given** biến thể (1 L, không đường)
- **When** khai báo dài 0,125 m, rộng 0,075 m, cao 0,200 m, khối lượng 1,050 kg
- **Then** giá trị được lưu **đúng như nhập, không làm tròn sai** (3 chữ số thập phân); nhập **0, số âm hoặc chữ** vào bất kỳ ô nào thì bị **từ chối**

**FR10-07 · Tồn tối thiểu, tối đa theo biến thể và kho**
- **Given** biến thể (1 L, không đường), kho `K01`
- **When** khai báo tồn tối thiểu 20, tối đa 200; sau đó thử tối thiểu 50, tối đa 50, và tối thiểu 80, tối đa 30
- **Then** cặp 20 và 200 được lưu riêng cho `K01`; hai cặp sau **bị từ chối** (tối đa phải lớn hơn tối thiểu)

**FR10-08 · Bật, tắt theo dõi hạn dùng khi chưa có tồn**
- **Given** sản phẩm chưa có tồn ở bất kỳ kho nào
- **When** `qtv` bật theo dõi hạn dùng, rồi tắt lại
- **Then** cả hai thao tác thành công và **ghi nhật ký** người và thời điểm

**FR10-09 · Không đổi cờ hạn dùng khi đã có tồn**
- **Given** sản phẩm đã có tồn
- **When** `qtv` đổi thiết lập theo dõi hạn dùng
- **Then** bị **từ chối** kèm lý do

**FR10-10 · Ngừng sử dụng, không xóa cứng**
- **Given** sản phẩm đã có biến động tồn
- **When** `qtv` xóa sản phẩm; sau đó chuyển sang **Ngừng sử dụng**
- **Then** việc xóa bị **từ chối**; sau khi ngừng sử dụng, sản phẩm **không chọn được ở chứng từ mới** nhưng **vẫn xem được tồn và lịch sử**

**FR10-11 · Thuộc tính đã phát sinh chứng từ không xóa, không sửa**
- **Given** thuộc tính "dung tích" đã có biến thể xuất hiện trên một phiếu nhập
- **When** `qtv` xóa hoặc đổi tên thuộc tính đó
- **Then** bị **từ chối**

**FR10-12 · Chỉ quản trị viên thêm, sửa danh mục**
- **Given** `qlk1`, `tk1`, `nvbh` lần lượt đăng nhập
- **When** mỗi người gọi API thêm hoặc sửa sản phẩm, biến thể
- **Then** cả ba bị **từ chối**; họ **tra cứu** được danh mục

**FR10-13 · Thêm ngành hàng mới không sửa mã nguồn**
- **Given** hệ thống đang chạy, `qtv` đăng nhập
- **When** `qtv` tạo nhóm hàng mới (ví dụ "Linh kiện điện tử") với thuộc tính riêng ("công suất"), thêm một sản phẩm và biến thể thuộc nhóm đó
- **Then** biến thể mới **chọn được** ở đơn mua và hóa đơn nhập, **không cần sửa mã hay triển khai lại**

## 5. FR11 – Vị trí 7 cấp

> Là quản trị viên, tôi cần khai báo vị trí theo 7 cấp (kho, tòa, tầng, phòng – dãy, hàng, kệ, ô), loại vị trí, kích thước và tải trọng của ô, để hàng được xếp đúng chỗ. *(Must; Bảng 2.10; Bảng 2.12 dòng 1, 3; Bảng 2.31)*

**FR11-01 · Tạo đủ 7 cấp và sinh mã đúng mẫu**
- **Given** `qtv` đã đăng nhập, kho `K01` đã có
- **When** `qtv` tạo lần lượt Tòa 2, Tầng 3, Phòng-dãy 4, Hàng 5, Kệ 6, Ô 7 dưới `K01`
- **Then** ô có mã **`K01-TA02-TG03-P04-H05-KE06-O07`**, cấp = 7, tên ghi **đủ đường dẫn** từ kho đến ô

**FR11-02 · Mỗi vị trí đúng một cha, đúng cấp ngay trên**
- **Given** một tầng cấp 3 đã có
- **When** tạo vị trí cấp 5 (hàng) gắn thẳng vào tầng (bỏ qua cấp 4); hoặc tạo vị trí cấp 2 không có cha
- **Then** cả hai **bị từ chối**; chỉ **cấp kho** được không có cha

**FR11-03 · Mã vị trí không trùng trong kho**
- **Given** ô `K01-TA02-TG03-P04-H05-KE06-O07` đã có
- **When** tạo thêm một ô cùng mã dưới cùng kệ
- **Then** bị **từ chối**

**FR11-04 · Hàng chỉ nằm ở cấp ô**
- **Given** kệ `…-KE06` (cấp 6) có 2 ô
- **When** hệ thống ghi tồn vào kệ (không phải ô); sau đó có tồn ở hai ô con
- **Then** việc ghi tồn vào cấp khác ô bị **từ chối**; tồn hiển thị của kệ bằng **tổng tồn các ô con** (cấp cha cộng gộp cấp con)

**FR11-05 · Ô bắt buộc kích thước trong và tải trọng**
- **Given** đang tạo một ô
- **When** để trống dài, rộng, cao hoặc tải trọng tối đa; hoặc nhập 0 hay số âm
- **Then** bị **từ chối** (kích thước và tải trọng của ô bắt buộc, là số dương); ô hợp lệ (1 × 1 × 1 m, 50 kg) được lưu **không làm tròn sai**

**FR11-06 · Loại khu và điều kiện bảo quản**
- **Given** đang khai báo một phòng-dãy (cấp 4)
- **When** chọn loại khu **tiếp nhận**, **lưu trữ**, **cách ly** hoặc **xuất**; rồi thử một giá trị ngoài bốn loại này
- **Then** bốn loại hợp lệ được lưu, mỗi vị trí thuộc **đúng một** loại; giá trị ngoài danh sách **bị từ chối**. Điều kiện bảo quản chỉ nhận **thường** hoặc **lạnh**

**FR11-07 · Vị trí đã có hàng không xóa, chỉ khóa**
- **Given** ô `O07` đang có tồn
- **When** `qtv` xóa ô; sau đó khóa ô
- **Then** xóa bị **từ chối**; sau khi khóa, ô **không còn xuất hiện** trong danh sách ô để xếp hàng mới

**FR11-08 · Chỉ quản trị viên khai báo, sửa vị trí**
- **Given** `qlk1` và `tk1` đăng nhập
- **When** mỗi người gọi API thêm hoặc sửa vị trí
- **Then** cả hai bị **từ chối**; họ **xem** được vị trí của kho được giao

**FR11-09 · Kho chưa có vị trí lưu trữ thì chưa nhập hàng được**
- **Given** kho `K04` vừa tạo, chưa có ô thuộc khu lưu trữ
- **When** lập lệnh nhập hoặc xếp hàng cho `K04`
- **Then** bị **từ chối** kèm lý do chưa có vị trí lưu trữ (Bảng 2.10 dòng 5)

**FR11-10 · Dữ liệu thử đạt cỡ yêu cầu**
- **Given** sau khi chạy script dữ liệu thử (S1-04)
- **When** đếm vị trí theo cấp
- **Then** có **3 kho** và **khoảng 60 ô** (55 đến 65), mỗi ô có kích thước và tải trọng dương, mọi mã ô đúng mẫu 7 cấp, đủ 4 loại khu

## 6. FR12 – Xếp hàng có kiểm tra sức chứa

> Là thủ kho, tôi cần hệ thống kiểm tra ô còn vừa kích thước, đủ không gian và tải trọng trước khi xếp hàng, để không xếp quá sức chứa của ô. *(Must; Bảng 2.1 dòng 6; Bảng 2.12 dòng 3, 6; Bảng 2.33)*

Quy tắc kiểm tra theo Bảng 2.33: (1) ba chiều đóng gói xếp tăng dần không lớn hơn ba chiều trong của ô xếp tăng dần (cho phép xoay); (2) đủ không gian; (3) đủ tải trọng; (4) `q_max` = nhỏ nhất của số theo không gian và phần nguyên của (tải trọng còn lại chia khối lượng một đơn vị); (5) ô thuộc khu lưu trữ, đúng điều kiện bảo quản, không khóa kiểm kê; (6) khóa dòng ô, kiểm tra lại và ghi trong một transaction. Tính bằng kiểu decimal.

**Các ca dùng chung** (số liệu của mục 2.16.3). Ô **Ô1**: trống, 1 × 1 × 1 m, tải trọng tối đa 50 kg. Biến thể **Thùng**: 0,5 × 0,5 × 0,4 m, 12 kg một đơn vị. Theo không gian xếp được 2 × 2 × 2 = **8** thùng; theo tải trọng chỉ được **4** thùng (48 kg).

**FR12-01 · Xếp hợp lệ**
- **Given** `tk1` (kho `K01`); lô L1 gồm 4 Thùng đạt kiểm, đang ở ô thuộc **khu tiếp nhận**; Ô1 thuộc khu lưu trữ thường, không khóa, đúng điều kiện bảo quản
- **When** `tk1` xếp 4 Thùng của L1 vào Ô1
- **Then** thành công; **khu tiếp nhận −4**, **Ô1 +4**, hai dòng sổ biến động ghi trong **cùng một transaction**; từ lúc này 4 Thùng **tính vào tồn khả dụng**; tổng biến động của L1 vẫn bằng tổng tồn của L1

**FR12-02 · Chặn khi vượt tải trọng, báo số tối đa**
- **Given** như FR12-01 nhưng L1 có 5 Thùng
- **When** `tk1` xếp 5 Thùng vào Ô1 (60 kg > 50 kg)
- **Then** bị **chặn (422)**, kèm lý do vượt tải trọng và **`q_max = 4`**; màn hình hiển thị lý do và số tối đa; **không ghi gì**, tồn giữ nguyên. Sau đó xếp 4 thì thành công

**FR12-03 · Chặn khi không vừa kích thước dù thể tích nhỏ**
- **Given** biến thể **Thanh** dài 1,2 m, rộng 0,05 m, cao 0,05 m, nhẹ
- **When** `tk1` xếp 1 Thanh vào Ô1 (cạnh ô tối đa 1 m)
- **Then** bị **chặn ở kiểm tra 1** (không vừa kích thước) dù thể tích rất nhỏ; không ghi gì

**FR12-04 · Không dùng thể tích đơn thuần cho ô một biến thể**
- **Given** ô **Ô2** trống 1 × 1 × 1 m, tải trọng tối đa 100 kg; biến thể **Kiện** 0,4 × 1 × 0,5 m, 5 kg (số thử đề xuất, để tải trọng không phải ràng buộc)
- **When** `tk1` xếp 5 Kiện vào Ô2
- **Then** bị **chặn** với **`q_max = 4`** (xếp theo lưới, thử các cách xoay, chỉ được 4), **không phải 5** như khi chia thể tích

**FR12-05 · Có tính hàng đang có trong ô**
- **Given** Ô1 đang có sẵn 3 Thùng (36 kg); lô có 2 Thùng ở khu tiếp nhận
- **When** `tk1` xếp 2 Thùng vào Ô1
- **Then** bị **chặn** với **`q_max = 1`** (theo không gian còn 5, theo tải trọng còn lại 14 kg chỉ thêm được 1); xếp 1 Thùng thì thành công

**FR12-06 · Ô nhiều biến thể tính theo hệ số lấp đầy**
- **Given** ô **Ô3** 1 × 1 × 1 m, tải trọng đủ lớn, hệ số lấp đầy của kho là mặc định **0,85**; Ô3 đã chứa biến thể khác chiếm 0,3 m³; biến thể mới có thể tích một đơn vị 0,1 m³
- **When** `tk1` xếp 6 đơn vị biến thể mới vào Ô3
- **Then** bị **chặn** với **`q_max = 5`** vì (1 × 0,85 − 0,3) ÷ 0,1 = 5,5, lấy phần nguyên; xếp 5 thành công. Đổi hệ số lấp đầy của kho thì kết quả đổi theo (cấu hình theo kho, không sửa mã)

**FR12-07 · Ca biên: bằng đúng giới hạn vẫn được xếp**
- **Given** ô 1 × 1 × 1 m, tải trọng tối đa 50 kg; kiện **không lớn hơn** ô ở cả ba chiều (ví dụ 1 × 1 × 1 m) và nặng đúng 50 kg
- **When** `tk1` xếp 1 kiện vào ô trống đó
- **Then** **thành công** (quy tắc là "không lớn hơn"); xếp thêm 1 kiện nữa thì **bị chặn** (`q_max = 0`)

**FR12-08 · Ô phải thuộc khu lưu trữ**
- **Given** ô thuộc khu **tiếp nhận** (hoặc **cách ly**, hoặc **xuất**) còn rất nhiều chỗ
- **When** `tk1` chọn ô đó làm nơi xếp
- **Then** bị **chặn**; ô đó **không có** trong danh sách ô gợi ý

**FR12-09 · Ô phải đúng điều kiện bảo quản**
- **Given** lô sản phẩm bảo quản **lạnh**; ô lưu trữ **thường** còn chỗ
- **When** `tk1` xếp lô vào ô thường
- **Then** bị **chặn**; ô lưu trữ lạnh còn chỗ thì xếp được

**FR12-10 · Ô đang khóa kiểm kê**
- **Given** ô đang bị khóa (kiểm kê), còn chỗ
- **When** `tk1` xếp vào ô đó
- **Then** bị **chặn**; ô **không chọn được** và hiển thị riêng trên sơ đồ

**FR12-11 · Không xếp vượt số đang có ở khu tiếp nhận**
- **Given** lô L1 còn 4 Thùng ở khu tiếp nhận
- **When** `tk1` xếp 6 Thùng của L1
- **Then** bị **chặn**, tồn khu tiếp nhận **không âm**, không ghi gì

**FR12-12 · Hai thủ kho cùng xếp vào một ô**
- **Given** Ô1 trống (chứa tối đa 4 Thùng); `tk1` và một thủ kho khác của `K01` mỗi người có 3 Thùng cần xếp
- **When** cả hai gửi yêu cầu xếp 3 Thùng vào Ô1 **cùng lúc**
- **Then** **đúng một** yêu cầu thành công; yêu cầu còn lại **hủy giao dịch và báo thử lại** (hoặc báo `q_max = 1` khi thử lại); Ô1 **không bao giờ vượt** 4 Thùng hay 50 kg

**FR12-13 · Bấm lặp không xếp hai lần**
- **Given** yêu cầu xếp 4 Thùng mang `Idempotency-Key = K`
- **When** gửi lại cùng yêu cầu đó
- **Then** Ô1 chỉ có **4 Thùng**, không phải 8; sổ biến động chỉ có một bộ dòng

**FR12-14 · Danh sách ô phù hợp**
- **Given** lô L1 gồm 4 Thùng cần xếp; trong kho có ô đúng bảo quản còn chỗ, ô khác bảo quản, ô đã đầy, ô đang khóa
- **When** gọi `GET /api/vi-tri/o-phu-hop?maLo=L1&soLuong=4`
- **Then** chỉ trả ô **đúng bảo quản, không khóa, còn đủ chỗ**, kèm không gian và tải trọng còn lại của từng ô; ô đầy, ô khóa, ô sai bảo quản **không có** trong kết quả

**FR12-15 · Chỉ xếp ở kho được giao**
- **Given** `tk2` chỉ được giao `K02`; lô và ô thuộc `K01`
- **When** `tk2` xếp lô của `K01` vào ô của `K01`
- **Then** bị **từ chối**; không ghi gì

## 7. Truy vết và kiểm thử

| FR | Việc triển khai (Bảng 1.9) | Cách kiểm chứng chính |
|---|---|---|
| FR01 | S2-03 (API đơn mua, hóa đơn nhập), S2-04 (cổng ghi tồn), S2-05, S2-06 (màn hình) | Unit test tính tiền, ràng buộc; test tích hợp SQL Server cho FR01-08 đến FR01-10 |
| FR09 | S2-01 (đăng nhập, phân quyền theo vai và kho) | Test phân quyền 4 vai, từng kho; kiểm tra nhật ký |
| FR10 | S1-03 (ERD, từ điển dữ liệu), S2-02, S2-07 | Unit test API danh mục; kịch bản thêm ngành hàng mới |
| FR11 | S1-04 (script, dữ liệu thử), S3-01, S3-04 | Script kiểm tra `03_kiem_tra_vitri.sql` (đã có), test API cây vị trí |
| FR12 | S3-02 (dịch vụ sức chứa, API xếp hàng), S3-03, S3-05 | Unit test các ca biên FR12-01 đến FR12-07; test hai người xếp cùng lúc FR12-12 |

Ca kiểm thử của Sprint 2, 3 (S1-06, TV5) phải phủ **mỗi mã tiêu chí ít nhất một ca**.

## 8. Điểm cần nhóm chốt khi duyệt

1. **Ai thấy đơn giá, thành tiền, tổng tiền?** Bảng 2.1 dòng 5 ghi chỉ quản lý kho và quản trị viên xem, sửa đơn giá; nhưng Bảng 2.35 dòng 2 cho màn hình lập hóa đơn của thủ kho hiện thành tiền và tổng tiền. Tài liệu này giả định **thủ kho nhập số lượng, lô, hạn dùng; quản lý kho thấy đơn giá và tiền**. Cần nhóm xác nhận.
2. **Mã lỗi khi vượt quyền.** Báo cáo chỉ nêu 422 cho lỗi nghiệp vụ. Đề xuất **403** cho "không có quyền" (vai hoặc kho), 401 cho chưa đăng nhập hoặc tài khoản khóa.
3. **Lô ở khu cách ly có được xếp vào ô lưu trữ không?** Báo cáo nói hàng lỗi tách sang khu cách ly, nhưng chưa nói cách xử lý tiếp. Tài liệu này **chưa có** tiêu chí cho ca đó.
4. **Khi nào lệnh nhập chuyển Hoàn thành?** Hình 2.10 ghi Chờ kiểm đếm → Hoàn thành khi xếp hàng; chưa rõ là sau khi xếp hết số thực nhận hay sau từng lô.
5. **Ca xếp đồng thời (FR12-12):** Bảng 2.33 dòng 6 ghi "hủy giao dịch, báo thử lại". Cần chốt người thua nhận thông báo gì.
6. **Số liệu thử** (đơn giá, ngưỡng tồn, hệ số 0,85 ở FR12-06) là đề xuất để S1-06 dựng dữ liệu; đổi được khi nhóm thấy cần.

## 9. Phiếu duyệt của nhóm

| Thành viên | Đã đọc và đồng ý | Góp ý |
|---|---|---|
| TV1 – Nguyễn Văn Hiếu (tác giả) | | |
| TV2 – Trần Tiến Đức | | |
| TV3 – Dương Tuấn Anh | | |
| TV4 – Lê Đức Anh | | |
| TV5 – Nguyễn Minh Hiếu | | |

Tiêu chí hoàn thành của S1-01: **cả nhóm duyệt** và tài liệu nằm trong `docs/`.
