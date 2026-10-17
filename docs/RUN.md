# Chạy TOEICFlow trên Windows

## Backend / website giảng viên

Cần SQL Server Express `.\SQLEXPRESS` và tài khoản Windows có quyền vào `ToeicReadingDb`. Dự án dùng SDK .NET 9.0.319, EF Core 9.0.5, ASP.NET Boilerplate 10.2.0 và Flutter tương thích `frontend/pubspec.yaml` (máy phát triển đặt ở `C:\src\flutter`). Repo không chứa SDK/cache. Cài SDK hệ thống đúng phiên bản trong `global.json`, hoặc dùng SDK local nếu đã có. Nếu backend đang chạy, dừng đúng tiến trình đó trước khi build để tránh khóa DLL.

```powershell
Set-Location E:\flutter_application_1 # thay bằng thư mục clone trên máy của bạn
if (Test-Path -LiteralPath "$PWD\.dotnet\dotnet.exe") {
    $env:DOTNET_ROOT = "$PWD\.dotnet"
    $env:PATH = "$env:DOTNET_ROOT;$env:PATH"
}
$env:DOTNET_CLI_HOME = "$PWD\.dotnet_home"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
dotnet restore backend/NguyenTatKhoa_2380601071.sln
dotnet build backend/src/NguyenTatKhoa_2380601071.Web.Mvc --no-restore
dotnet run --project backend/src/NguyenTatKhoa_2380601071.Web.Mvc --no-build --no-launch-profile -- --urls http://127.0.0.1:5000
```

Cấu hình `backend/src/NguyenTatKhoa_2380601071.Web.Mvc/appsettings.Local.json` theo mẫu `docs/database/appsettings.Local.example.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Server=.\\SQLEXPRESS;Database=ToeicReadingDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;"
  },
  "DemoAccess": {"Enabled": true, "AllowedRemoteAddresses": []},
  "App": {"CorsOrigins": "http://localhost:5200,http://127.0.0.1:5200"}
}
```

Environment `ConnectionStrings__Default`, nếu có, ghi đè JSON; kiểm tra tránh trỏ nhầm database. Không commit cấu hình local hoặc secret. MVC phục vụ cả website và API, không cần chạy Web.Host đồng thời. `/` luôn là dashboard giảng viên, kể cả trên điện thoại.

Trước khi khởi động trên máy mới, tạo khóa JWT riêng trong cùng cửa sổ PowerShell chạy backend (lưu an toàn để tái sử dụng khi khởi động lại, không commit lên Git):

```powershell
$taskJwtBytes = New-Object byte[] 32
$taskJwtRng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
try { $taskJwtRng.GetBytes($taskJwtBytes) } finally { $taskJwtRng.Dispose() }
$env:Authentication__JwtBearer__SecurityKey = [Convert]::ToBase64String($taskJwtBytes)
```

Có thể đặt khóa đó ở `Authentication:JwtBearer:SecurityKey` trong `appsettings.Local.json` thay cho biến môi trường. Không in/chia sẻ khóa và không dùng giá trị mẫu cho Production. Nếu chạy Web.Host riêng, cấu hình khóa tương ứng cho tiến trình đó.

Database máy này đã có 33 migration. Không tạo lại/drop/reset. Chỉ trên máy mới dùng `scripts/setup-toeic-database.ps1` theo [tài liệu database](database/README.md); setup chỉ ghi cấu hình kết nối, bổ sung demo/origin theo mẫu nếu cần.

## Flutter sinh viên

Trong PowerShell khác:

```powershell
Set-Location E:\flutter_application_1\frontend
C:\src\flutter\bin\flutter.bat pub get
C:\src\flutter\bin\flutter.bat run -d chrome --web-hostname 127.0.0.1 --web-port 5200 --dart-define=API_URL=http://127.0.0.1:5000
```

Hoặc phục vụ bản build:

```powershell
C:\src\flutter\bin\flutter.bat build web --dart-define=API_URL=http://127.0.0.1:5000 --no-web-resources-cdn
python -m http.server 5200 --bind 127.0.0.1 --directory build/web
```

Nếu 5200 đang dùng, dừng đúng web server cũ trước khi chạy server khác. Flutter mở danh sách đề Published. Mã cài đặt ngẫu nhiên lưu bằng shared_preferences giúp khôi phục đúng người/lượt; token demo chỉ ở bộ nhớ và được cấp lại theo mã này. Không xóa dữ liệu trình duyệt khi đang thi. Flutter không chứa kết nối SQL.

## Nhập đề thật

Chọn `docs/source/TOEIC_Reading_v07_co_tag.docx` trên trang **Nhập đề từ Word**:

1. Kiểm tra tệp: báo lỗi, chưa ghi database.
2. Nhập thành bản nháp: lưu toàn bộ trong transaction; nhập lại cùng file mở đề đã có.
3. Xem trước, sửa câu/lựa chọn/đáp án/gợi ý hoặc thay ảnh, rồi kiểm tra lại.
4. Xác nhận xuất bản; tải lại Flutter để thấy đề.
5. Published không sửa trực tiếp; sao chép thành Draft để tạo phiên bản mới.

Nguồn có 100 câu, 400 lựa chọn, 19 nhóm, 27 passage/ảnh, 100 gợi ý. Part 6 có STEM rỗng có chủ đích, chỗ trống trong ảnh và metadata BLANKS.

## Android, iOS và production

Android Emulator: `--dart-define=API_URL=http://10.0.2.2:5000`; backend cần bind địa chỉ emulator truy cập được và allowlist đúng IP nếu không phải loopback. HTTP chỉ bật cho Android debug/profile; release dùng HTTPS.

Thiết bị thật: URL LAN/HTTPS rõ trong `API_URL`, cấu hình bind/firewall và `DemoAccess:AllowedRemoteAddresses` đúng thiết bị. Không wildcard CORS hay mở toàn mạng. iOS chưa chạy trên máy Windows này; dùng HTTPS chứng chỉ hợp lệ, không tắt ATS/TLS toàn cục.

Production cần HTTPS, JWT key riêng trong secret store, xác thực thật và cấp quyền giảng viên rõ. Demo endpoint bị tắt ngay cả khi giữ nhầm `DemoAccess:Enabled=true`. Mã cài đặt demo không phải danh tính sinh viên đã xác minh.
