# Phạm vi và giới hạn

- Đã chạy trên Windows, SQL Server Express thật và Chrome với viewport mobile/desktop. Chưa kiểm thử Android Emulator, thiết bị Android thật hoặc iOS.
- Không đăng nhập là demo Development opt-in, giới hạn IP/origin/quyền. Mã cài đặt không phải danh tính sinh viên đã xác minh. Production cần xác thực thật, không công khai demo endpoint.
- Word giữ nguyên đáp án/gợi ý người dùng cung cấp; chưa có xác nhận độc lập là đáp án chính thức của nhà xuất bản. Không OCR để tự sửa nội dung/đáp án PDF.
- Nội dung trong 27 ảnh bài đọc vẫn là ảnh, có phóng to. Sửa chữ bên trong ảnh bằng cách thay ảnh tương ứng; không có trình sửa ảnh/OCR.
- Worker thu bài mỗi 15 giây và API kiểm tra deadline. Khi backend tắt, không có worker; khởi động lại sẽ thu bài đã quá hạn, không cấp thêm giờ.
- Đã sửa cảnh báo `bundleconfig.json`: bundler template chỉ chạy khi thực sự có cấu hình; TOEICFlow dùng static assets trực tiếp. Build solution cuối đạt 0 lỗi/0 cảnh báo. Utility kiểm thử có thể còn NU1900 khi không truy cập được feed; không tắt audit để che cảnh báo và chưa khẳng định hoàn tất audit mọi dependency.
- Build solution phát hiện AngleSharp 1.3.0 trong project Web.Tests có advisory. Đã cập nhật riêng dependency test lên 1.5.0 theo [bản vá của maintainer](https://github.com/AngleSharp/AngleSharp/security/advisories/GHSA-pgww-w46g-26qg), restore/build và Web test đã qua.
- Đã xử lý Start chậm: chặn gửi trùng, budget riêng 45 giây, đọc lịch sử một lần khi mất phản hồi để phục hồi lượt đã commit; resume không chờ khóa chung của đề và không đổi hạn nộp. Deadline mới được chốt sau khi tạo snapshot. Hai Start đồng thời và resume khi khóa đề bị giữ đã qua kiểm thử SQL. Cold-start vẫn phụ thuộc tài nguyên máy/server; không cam kết dưới 20 giây trên mọi môi trường hoặc mọi tải.
- MVC dùng Google Fonts với fallback offline; Flutter đóng gói Inter và CanvasKit local. Chưa audit pixel-perfect, accessibility toàn diện hoặc stress test tải lớn.
- Không cung cấp xóa đề hoặc sửa trực tiếp đề Published; dùng bản sao Draft. Giữ 10 bảng nghiệp vụ và 33 migration hiện có.

Lỗi GET nền ghi đè lựa chọn mới đã được sửa bằng revision theo câu hỏi và cập nhật đúng model hiện tại; GET cũ không được ghi đè kết quả cuối. Nhập Word legacy/v07 và trả ảnh dùng chung chính sách raster an toàn; SVG/HTML giả ảnh bị chặn. Các file ảnh không đạt chính sách mới trong dữ liệu cũ bị endpoint từ chối thay vì phát MIME tùy ý; thay bằng raster hợp lệ nếu cần, không tự xóa hàng dữ liệu.
