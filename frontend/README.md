# TOEICFlow — Flutter sinh viên

Ứng dụng mở danh sách đề Published, làm Part 5–7, lưu/khôi phục đáp án, nộp bài và xem kết quả từ API/SQL thật. Chạy [backend](../backend/README.md) trước, sau đó thực hiện các lệnh dưới đây trong thư mục `frontend`.

```powershell
flutter pub get
flutter run -d chrome --web-hostname 127.0.0.1 --web-port 5200 --dart-define=API_URL=http://127.0.0.1:5000
```

Chạy backend trước ở cổng 5000 với demo Development và CORS đúng origin. Android Emulator dùng API_URL `http://10.0.2.2:5000`. Không có SQL connection string trong Flutter. Các thư mục cũ được giữ nguyên, entry point dùng `lib/toeic`.

## Kiểm thử

```powershell
flutter analyze
flutter test
```

Đã chạy trên Chrome; chưa kiểm thử Android/iOS thật. Thiết kế gốc: [TOEICFlow trên Google Stitch](https://stitch.withgoogle.com/projects/8218148455783376277).

## Getting Started

This project is a starting point for a Flutter application.

A few resources to get you started if this is your first Flutter project:

- [Learn Flutter](https://docs.flutter.dev/get-started/learn-flutter)
- [Write your first Flutter app](https://docs.flutter.dev/get-started/codelab)
- [Flutter learning resources](https://docs.flutter.dev/reference/learning-resources)

For help getting started with Flutter development, view the
[online documentation](https://docs.flutter.dev/), which offers tutorials,
samples, guidance on mobile development, and a full API reference.
