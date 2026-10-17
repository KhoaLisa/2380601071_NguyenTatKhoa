# TOEICFlow — 2380601071 Nguyễn Tất Khoa

Ứng dụng giữa kỳ TOEIC Reading: website ASP.NET Core MVC dành cho giảng viên, Flutter dành cho sinh viên, API ASP.NET Boilerplate và SQL Server `ToeicReadingDb`. Giữ nguyên stack và database hiện có.

## Chức năng

- Giảng viên: kiểm tra và nhập Word có tag, xem trước, sửa bản nháp, thay ảnh, sao chép, xuất bản đề và xem thống kê từ SQL Server.
- Sinh viên: danh sách đề đã xuất bản, làm bài Part 5–7, lưu/đổi/hủy đáp án, khôi phục lượt thi, đếm ngược 75 phút, nộp bài và xem kết quả.
- Backend chấm điểm, kiểm tra hạn thi, thu bài quá hạn và chống nộp trùng; không trả đáp án/gợi ý trong dữ liệu làm bài.

## Cấu trúc và công nghệ

| Thư mục | Nội dung |
|---|---|
| `backend/` | ASP.NET Boilerplate 10.2.0, MVC + jQuery, .NET 9, EF Core SQL Server 9.0.5; có migrations và kiểm thử. |
| `frontend/` | Flutter/Dart dành cho sinh viên. |
| `docs/` | Tài liệu kỹ thuật, Word/PDF nguồn, thiết kế, hướng dẫn chạy và bằng chứng kiểm thử. |
| `scripts/` | Thiết lập database trên máy mới. |
| `tools/` | Công cụ kiểm thử trình duyệt, tạo Word mẫu và đọc PDF; không tự chạy cùng ứng dụng. |

## Chạy và thiết kế

Cần .NET SDK 9.0.319, SQL Server Express và Flutter tương thích `frontend/pubspec.yaml`. Làm theo [hướng dẫn chạy](docs/RUN.md); máy mới cần cấu hình SQL và áp migrations trước, không có file database thật hoặc dữ liệu cá nhân trong repo.

- Website giảng viên: `http://127.0.0.1:5000`
- Flutter Chrome: `http://127.0.0.1:5200`
- Chế độ demo Development không có màn đăng nhập; không dùng chế độ này để triển khai công khai.

Xem [hướng dẫn chạy](docs/RUN.md), [kiểm thử](docs/TESTING.md), [API](docs/API_CONTRACT.md), [tag Word v07](docs/WORD_TAG_SPEC.md), [database](docs/database/README.md) và [giới hạn](docs/KNOWN_ISSUES.md).

Nguồn đề người dùng cung cấp nằm trong `docs/source/`. Giao diện theo Stitch `8218148455783376277`, bản xuất trong `docs/design/`. Đáp án và gợi ý giữ theo Word, không tự thêm đáp án “chính thức”.

Thiết kế gốc: [TOEICFlow trên Google Stitch](https://stitch.withgoogle.com/projects/8218148455783376277).

## Kết quả kiểm thử gần nhất

Ngày 17/10/2026: backend 58/58 test, web 1/1 test, Flutter 24/24 test; build solution không có lỗi/cảnh báo, Flutter analyze không có vấn đề. Luồng Chrome thật nhập đề → làm bài → nộp → thống kê đã qua; xem [phạm vi và bằng chứng](docs/TESTING.md). Chưa kiểm thử Android/iOS thật, không coi Chrome viewport mobile là bằng chứng chạy thiết bị.

SDK local, cache, thư mục build, log và `appsettings.Local.json` không được đưa lên GitHub. Không dùng khóa JWT mẫu để triển khai công khai; tạo khóa riêng trong biến môi trường hoặc cấu hình local theo hướng dẫn chạy.

Log kiểm thử cũ được lưu tại `logs/archive/`. Cấu hình `.vscode/settings.json` chỉ ẩn SDK/cache, đầu ra build và log khỏi Explorer, không xóa chúng; có thể bỏ các mục `files.exclude` để hiện lại. Các công cụ trong `.dotnet`, `.dotnet_home` và `.tools` vẫn cần cho quy trình chạy/kiểm thử hiện tại.
