# TOEICFlow — Backend giảng viên và API

Nguyễn Tất Khoa — 2380601071. ASP.NET Boilerplate 10.2.0, MVC + jQuery, .NET 9, EF Core SQL Server 9.0.5.

Chức năng: kiểm tra/nhập Word có tag, xem trước, sửa bản nháp, thay ảnh, sao chép, xuất bản, thống kê; API cho Flutter làm bài, lưu đáp án, khôi phục lượt thi, nộp và chấm điểm. Server quản lý thời hạn và thu bài quá hạn.

## Chạy trên Windows

Cần .NET SDK 9.0.319 theo `global.json` trong thư mục này, SQL Server Express và tài khoản Windows có quyền vào `ToeicReadingDb`. Repo không chứa database thật, SDK/cache hoặc cấu hình riêng tư.

Từ thư mục clone, mở PowerShell:

```powershell
cd backend
dotnet restore NguyenTatKhoa_2380601071.sln
dotnet tool restore
Copy-Item appsettings.Local.example.json src/NguyenTatKhoa_2380601071.Web.Mvc/appsettings.Local.json
Copy-Item appsettings.Local.example.json src/NguyenTatKhoa_2380601071.Migrator/appsettings.Local.json
```

Sửa `ConnectionStrings:Default` trong hai file local theo SQL Server của bạn; không commit chúng. Mẫu dùng Windows Authentication, không có mật khẩu SQL. Nếu dùng Web.Host riêng, tạo cấu hình local tương tự cho project đó.

Trong cửa sổ chạy backend, dùng môi trường phát triển và khóa JWT riêng:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$taskJwtBytes = New-Object byte[] 32
$taskJwtRng = [Security.Cryptography.RandomNumberGenerator]::Create()
try { $taskJwtRng.GetBytes($taskJwtBytes) } finally { $taskJwtRng.Dispose() }
$env:Authentication__JwtBearer__SecurityKey = [Convert]::ToBase64String($taskJwtBytes)
```

Lưu khóa an toàn để dùng lại khi khởi động, không đưa lên GitHub. Biến môi trường `ConnectionStrings__Default`, nếu có, được ưu tiên hơn cấu hình JSON.

Chỉ trên máy mới hoặc khi cần áp migrations, chạy Migrator sau khi đã xác nhận đúng server/database:

```powershell
dotnet run --project src/NguyenTatKhoa_2380601071.Migrator -- -q
```

Không drop/reset database đang có. Sau đó chạy web/API:

```powershell
dotnet run --project src/NguyenTatKhoa_2380601071.Web.Mvc --no-launch-profile -- --urls http://127.0.0.1:5000
```

Mở `http://127.0.0.1:5000`. MVC phục vụ cả website giảng viên và API, không cần chạy Web.Host đồng thời. Flutter chạy riêng theo [hướng dẫn frontend](../frontend/README.md).

Demo không đăng nhập chỉ dành cho Development/loopback. Production cần xác thực thật, HTTPS, khóa riêng và quyền giảng viên; không dùng khóa JWT mẫu để triển khai công khai.

## Kiểm thử

```powershell
dotnet test test/NguyenTatKhoa_2380601071.Tests
dotnet test test/NguyenTatKhoa_2380601071.Web.Tests
```

Word v07 dùng cho kiểm thử parser nằm trong `test/SourceFixtures/`, giữ nguyên file nguồn. File đó không tự được seed vào SQL. Các lớp database-check trong `tools/` chỉ chạy khi được gọi riêng, không tự tạo lượt thi khi chạy website.
