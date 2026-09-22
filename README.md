# Video Timeline Marker

Hệ thống ứng dụng web hỗ trợ đánh dấu, phân tích và quản lý các mốc thời gian trong video, được xây dựng trên nền tảng ASP.NET MVC 5, Entity Framework 6 và SQL Server LocalDB.

## 1. Giới thiệu tổng quan

Ứng dụng cho phép người dùng tải lên các video, hệ thống tự động nhận diện thời lượng và tạo trục thời gian tương ứng. Người dùng có thể đánh dấu các khoảng thời gian (segment), phân loại giữa thao tác chuẩn và sự cố hoặc thao tác thừa, đặt nhãn chú thích và xuất dữ liệu thống kê dưới định dạng JSON.

Toàn bộ dữ liệu video và các đoạn đánh dấu được lưu trữ lâu dài trong cơ sở dữ liệu SQL Server LocalDB.

## 2. Yêu cầu hệ thống

- Hệ điều hành: Windows 10 hoặc Windows 11.
- Môi trường phát triển: Visual Studio 2019, 2022 hoặc mới hơn (đã cài đặt workload ASP.NET and web development).
- .NET Framework: Phiên bản 4.7.2 trở lên (mặc định có sẵn trên Windows).
- Cơ sở dữ liệu: SQL Server LocalDB (đi kèm sẵn khi cài đặt Visual Studio).

## 3. Hướng dẫn cài đặt và khởi chạy ứng dụng

### Cách 1: Chạy trực tiếp bằng Visual Studio

1. Mở tập tin VideoTimelineApp.sln bằng Visual Studio.
2. Nhấp chuột phải vào Solution trong cửa sổ Solution Explorer, chọn "Restore NuGet Packages" để tải các thư viện cần thiết.
3. Nhấn phím F5 (hoặc tổ hợp Ctrl + F5 để chạy không debug).
4. Hệ thống sẽ tự động khởi tạo cơ sở dữ liệu VideoTimelineDb trong SQL Server LocalDB ở lần chạy đầu tiên.

### Cách 2: Khôi phục gói thư viện bằng PowerShell

1. Mở PowerShell tại thư mục gốc của dự án.
2. Chạy lệnh:
   powershell -ExecutionPolicy Bypass -File .\setup.ps1
3. Mở VideoTimelineApp.sln trong Visual Studio và nhấn F5 để bắt đầu sử dụng.

## 4. Hướng dẫn sử dụng chi tiết

### 4.1. Quản lý thư viện video (Trang chủ)

- Tải lên video mới:
  - Nhập tên video vào ô "Tên video" (nếu để trống, hệ thống sẽ tự động dùng tên tập tin gốc).
  - Kéo thả tập tin video vào khung nhận tập tin hoặc bấm vào "bấm chọn file".
  - Các định dạng hỗ trợ: MP4, MOV, MKV, AVI, WEBM (dung lượng tối đa 500 MB).
  - Nhấn nút "Upload & Mở Timeline" để tiến hành tải lên máy chủ.
- Xem danh sách video:
  - Các video đã tải lên sẽ hiển thị dưới dạng thẻ thông tin trong mục "Thư viện video".
  - Thông tin hiển thị bao gồm: thời lượng, dung lượng tập tin, số đoạn đã đánh dấu, thời điểm tải lên và các nhãn đánh dấu xem trước.
- Mở giao diện đánh dấu: Nhấn nút "Mở Timeline" trên thẻ video tương ứng.
- Xoá video: Nhấn nút biểu tượng thùng rác để xoá tập tin video vật lý trên máy chủ và toàn bộ dữ liệu mốc thời gian liên quan trong cơ sở dữ liệu.

### 4.2. Giao diện đánh dấu Timeline (Trang Player)

1. Phát video và điều khiển:
   - Sử dụng thanh điều khiển video mặc định hoặc bấm nút "Play/Pause" trên thanh công cụ.
   - Nhấp chuột trực tiếp vào bất kỳ vị trí nào trên thanh thời gian (Timeline) để tua video đến giây tương ứng.
   - Khi rê chuột trên thanh Timeline, khung hiển thị nhỏ sẽ hiển thị chính xác thời gian tại vị trí con trỏ.

2. Đánh dấu một giai đoạn:
   - Bước 1: Chọn loại đoạn:
     - "Bình thường": Dùng để ghi nhận các thao tác đúng, quy trình chuẩn (màu sắc tự động thay đổi theo dải màu sắc nét).
     - "Sự cố": Dùng để ghi nhận các lỗi, sự cố hoặc thao tác thừa (hiển thị màu đỏ kèm hoạ tiết sọc chéo).
   - Bước 2: Nhập tên hoặc chú thích vào ô "Nhãn đoạn" (không bắt buộc).
   - Bước 3: Phát video đến thời điểm bắt đầu cần đánh dấu, nhấn nút "Đặt điểm đầu" (hoặc nhấn phím S trên bàn phím).
   - Bước 4: Cho video chạy đến thời điểm kết thúc, nhấn nút "Đặt điểm cuối" (hoặc nhấn phím E trên bàn phím).
   - Hệ thống sẽ tự động kiểm tra tính hợp lệ và lưu đoạn vào cơ sở dữ liệu qua REST API.

3. Cơ chế chống đè mốc thời gian (Overlap Prevention):
   - Hệ thống ngăn chặn việc tạo các đoạn đè lên nhau.
   - Nếu khoảng thời gian dự định chọn bị trùng lặp với một đoạn đã tồn tại, hệ thống sẽ hiển thị cảnh báo màu đỏ và không cho phép lưu nhằm bảo đảm tính chính xác của dữ liệu.

4. Chỉnh sửa và quản lý đoạn:
   - Sửa nhãn đoạn: Nhấn vào biểu tượng cây bút chì trên thẻ đoạn tương ứng, nhập nhãn mới trong hộp thoại và bấm Lưu.
   - Xoá từng đoạn: Nhấn vào biểu tượng thùng rác trên thẻ của đoạn cần xoá.
   - Hoàn tác: Nhấn nút "Undo" trên thanh công cụ để xoá nhanh đoạn vừa tạo gần nhất.
   - Xoá tất cả: Nhấn nút "Xoá tất cả" trên thanh công cụ để làm mới toàn bộ trục thời gian của video.

5. Xuất dữ liệu báo cáo (Export JSON):
   - Nhấn nút "Xuất JSON" trên thanh công cụ.
   - Hệ thống sẽ tải xuống một tập tin .json chứa đầy đủ các chỉ số thống kê (tổng số đoạn, số lượng sự cố, tổng thời gian ghi nhận, tỷ lệ phần trăm bao phủ) và danh sách chi tiết từng đoạn theo giây bắt đầu, giây kết thúc và thời lượng.

### 4.3. Danh sách phím tắt

- Phím Space: Phát hoặc tạm dừng video (Play / Pause).
- Phím S: Đặt mốc thời gian bắt đầu tại vị trí hiện tại của video.
- Phím E: Đặt mốc thời gian kết thúc và lưu đoạn.
- Phím Mũi tên trái: Tua lùi 5 giây.
- Phím Mũi tên phải: Tua tiến 5 giây.

## 5. Cấu trúc mã nguồn dự án

- App_Start:
  - RouteConfig.cs: Cấu hình điều hướng MVC.
  - WebApiConfig.cs: Cấu hình REST API và định dạng dữ liệu trả về dạng JSON (camelCase).
  - FilterConfig.cs: Cấu hình bộ lọc lỗi toàn cục.
- Controllers:
  - HomeController.cs: Quản lý danh sách video, xử lý tải lên và xoá video.
  - VideoController.cs: Quản lý hiển thị trang phát video và nạp dữ liệu ban đầu.
  - SegmentsApiController.cs: Cung cấp các điểm cuối RESTful API cho dữ liệu đoạn (GET, POST, PUT, DELETE, PATCH).
- Models:
  - VideoSession.cs: Lớp thực thể lưu trữ thông tin video tải lên.
  - Segment.cs: Lớp thực thể lưu trữ thông tin các đoạn mốc thời gian.
- Data:
  - AppDbContext.cs: Lớp ngữ cảnh Entity Framework 6, thiết lập cấu hình bảng và quan hệ xoá theo tầng (Cascade Delete).
- Views:
  - Home/Index.cshtml: Giao diện trang chủ và thư viện video.
  - Video/Player.cshtml: Giao diện phát video và trục thời gian đánh dấu.
  - Shared/_Layout.cshtml: Giao diện bố cục khung nền tối (Dark Theme).
- Content/Site.css: Tập tin định kiểu toàn bộ ứng dụng, hỗ trợ giao diện tương thích (Responsive) trên cả máy tính và điện thoại.
- Scripts/timeline.js: Tập tin kịch bản phía máy khách xử lý điều khiển trục thời gian, tương tác API và xuất tệp JSON.
- Services/CloudinaryService.cs: Lớp dịch vụ xử lý tải video lên Cloudinary theo cơ chế chia nhỏ luồng dữ liệu (chunked upload) và xoá video trên đám mây.
- uploads/videos: Thư mục lưu trữ các tập tin video vật lý của người dùng khi chạy ở chế độ cục bộ.

## 6. Danh sách các điểm cuối REST API

- GET /api/segments/{videoId}: Lấy danh sách toàn bộ các đoạn đánh dấu thuộc về một video.
- POST /api/segments: Tạo một đoạn đánh dấu mới (có kiểm tra chống đè mốc thời gian từ phía máy chủ).
- PUT /api/segments/{id}: Cập nhật nhãn chú thích của đoạn theo ID.
- DELETE /api/segments/{id}: Xoá đoạn khỏi cơ sở dữ liệu theo ID.
- PATCH /api/segments/video/{videoId}/duration: Cập nhật thời lượng chính xác của video sau khi trình duyệt đọc thông tin từ tập tin.

## 7. Cấu hình lưu trữ đám mây Cloudinary

Mặc định, ứng dụng hỗ trợ cơ chế lưu trữ kép linh hoạt:
- Nếu chưa cấu hình Cloudinary: Ứng dụng tự động chuyển sang chế độ lưu trữ cục bộ vào thư mục uploads/videos/.
- Khi muốn kích hoạt lưu trữ đám mây Cloudinary:
  1. Đăng ký tài khoản miễn phí tại trang web cloudinary.com.
  2. Lấy 3 thông số trong trang điều khiển Dashboard: Cloud Name, API Key, API Secret.
  3. Mở tập tin Web.config và điền các giá trị vào phần appSettings:
     - CloudinaryCloudName: Nhập tên Cloud Name của bạn.
     - CloudinaryApiKey: Nhập mã API Key.
     - CloudinaryApiSecret: Nhập mã API Secret.
  4. Khởi động lại ứng dụng. Toàn bộ video tải lên từ thời điểm này sẽ được stream trực tiếp lên Cloudinary, không tốn dung lượng ổ cứng máy chủ và phát qua CDN với tốc độ tối ưu.
