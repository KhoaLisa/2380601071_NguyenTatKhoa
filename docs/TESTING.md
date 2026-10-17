# Kiểm thử TOEICFlow

Ngày 17/10/2026, môi trường Windows / .NET 9 / SQL Server Express / Flutter Chrome. Không coi build thành công là thay thế kiểm thử luồng.

Sau bản sửa: solution build 0 lỗi/0 cảnh báo; unit backend 58/58, Flutter analyze không có vấn đề và Flutter test 24/24. SQL thật xác minh cặp Start đồng thời 652 ms, resume khi giữ khóa đề 42 ms trong lượt đo đó; đây là số đo một lượt, không phải SLA.

Lượt Chrome cuối đã hoàn tất ngày 17/10/2026: đọc đề 100 câu, lưu/đổi/xóa, phục hồi sau refresh, Part 6/7 mobile và desktop, nộp/kết quả/thống kê, ảnh với MIME/CSP an toàn; không có page error. Web test 1/1 đạt; Production smoke chặn bootstrap và token demo.

Đối chiếu snapshot xác nhận cả 4 đề và 11 kết quả có trước vẫn giữ nguyên. Có thêm 3 lượt tự động mới trên bản E2E, được giữ lại. Không xóa đề hoặc kết quả người dùng khi chuẩn bị source nộp bài. Chi tiết định danh và snapshot chỉ lưu cục bộ, không đưa dữ liệu lượt thi lên repo source.

## Các kiểm thử và phạm vi

- Unit backend: parser legacy/v07, Word nguồn 100 câu thật, copy Draft, kiểm tra cấu trúc, bảng an toàn; bổ sung ảnh hỏng/SVG/giả MIME/phần mở rộng/ảnh quá lớn, dùng chung chính sách ảnh cho import/sửa/đọc.
- Flutter: API identity/ABP, Start single-flight, timeout riêng, phục hồi Start đã commit khi mất phản hồi, không tự gửi POST trùng; widget test mô phỏng GET cũ tới trong/sau lúc lưu đáp án, rollback lỗi lưu trên model hiện tại, GET không làm mất kết quả, cảnh báo lỗi lưu trước khi nộp và nộp quá hạn.
- SQL thật: kết nối/migrations/constraints; save/change/clear/restore; không lộ đáp án; chấm/nộp trùng/hết hạn/thống kê. Các kịch bản thông thường dùng outer transaction không Complete, tự rollback.
- SQL concurrency: hai Start đồng thời phải cùng lượt/hạn, một lượt đang làm có đúng 100 câu và 400 lựa chọn; resume không bị chặn bởi khóa chung của đề; hai Submit đồng thời cùng kết quả; collector thu bài không cần request học viên.
- Chrome thật: MVC kiểm tra tệp lỗi, Word thật, xem trước/sửa/thay ảnh/xuất bản; Flutter danh sách/hướng dẫn/Part5–7/lưu/đổi/xóa/khôi phục/nộp/kết quả; thống kê SQL, quyền sở hữu lượt, CORS và demo Production. Screenshot và trạng thái máy chạy nằm trong `docs/testing/evidence/`.

Các JSON state, định danh kiểm thử và snapshot database chỉ lưu trên máy phát triển, được bỏ qua khi đưa source lên GitHub; ảnh bằng chứng được đưa kèm. Không dùng repo source thay cho bản sao lưu dữ liệu người dùng.

## Chạy lại

Từ root, thiết lập DOTNET_ROOT / DOTNET_CLI_HOME / PATH theo [RUN.md](RUN.md). Dừng đúng backend trước khi build MVC nếu DLL bị khóa.

```powershell
dotnet build backend/NguyenTatKhoa_2380601071.sln --no-restore
dotnet test backend/test/NguyenTatKhoa_2380601071.Tests --no-build --no-restore
dotnet test backend/test/NguyenTatKhoa_2380601071.Web.Tests --no-build --no-restore
dotnet build backend/tools/Toeic.DatabaseCheck --no-restore
$env:ConnectionStrings__Default = 'Server=.\SQLEXPRESS;Database=ToeicReadingDb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;'
dotnet backend/tools/Toeic.DatabaseCheck/bin/Debug/net9.0/Toeic.DatabaseCheck.dll --test --integration-test --concurrency-test
```

Không có flags thì utility chỉ kiểm tra kết nối/migration/số đếm. Không thêm `--seed` hoặc `--cleanup-ui-attempts` nếu chỉ cần chẩn đoán. `--concurrency-test` tạo đề riêng có mã+GUID+marker kiểm chứng, sau đó xóa duy nhất đề tạm và các hàng con của nó. Không đụng đề hoặc kết quả người dùng.

```powershell
Set-Location frontend
flutter analyze
flutter test --reporter expanded
flutter build web --dart-define=API_URL=http://127.0.0.1:5000 --no-web-resources-cdn
```

Các mock HTTP/widget chỉ trong thư mục test, không seed vào SQL hoặc UI thật.

## An toàn khi thử Chrome

Repo không chứa các dependency kiểm thử cài local. Nếu cần chạy lại công cụ trình duyệt/PDF, cài từ root sau khi đã cài Node.js/npm, Python và Chrome:

```powershell
npm install --prefix .tools/browser --no-save playwright-core@1.55.0
python -m pip install --target .tools/pdf pypdf==6.21.0
```

Các lệnh cài này không chạy kịch bản nghiệp vụ. Script trình duyệt hiện trỏ tới Chrome Windows; script PDF đọc file gốc ở `D:/Downloads`, cần điều chỉnh đường dẫn nếu máy khác.

Các script `tools/browser-smoke.cjs` và `tools/browser-integration.cjs` dùng Playwright local tại `.tools/browser/node_modules/playwright-core` và Chrome đã cài. Chúng thực hiện thao tác ghi thật vào bản sao đề được ghi GUID/code trong `browser-state.json`. Không chạy khi chỉ muốn xem trạng thái code. Không xóa hoặc ghi đè state nếu muốn dọn chính xác lượt tự động.

Người dùng đã yêu cầu GIỮ các lượt trên bản E2E. Không xóa bản đề đó hoặc các lượt không nằm trong danh sách tự động; nếu dọn lượt tự động thì kiểm tra GUID, mã đề, danh sách preservedAttemptIds trước. Không dùng lệnh xóa toàn database/bảng.

`--cleanup-last-ui-attempt` chỉ dọn duy nhất `attemptId` hiện tại nếu nó có trong `uiAttemptIds` và đúng đề E2E/code, không thuộc `preservedAttemptIds`; dùng cho lượt smoke mới nhất. Flag dọn toàn danh sách tự động không được dùng để thay thế kiểm tra phạm vi. Snapshot `database-before-final-smoke.json` / `database-after-final-smoke.json` dùng đối chiếu các đề và kết quả trước/sau.

## Giới hạn

Chrome viewport mobile không thay thế Android/iOS thật. Cold-start trước đó từng chậm 20–22 giây; sửa mới giảm khóa khi resume, chốt đủ thời lượng, có timeout/recovery và log chậm. Chưa có bằng chứng để cam kết hiệu năng dưới mọi mức tải. Không tắt TLS hoặc audit NuGet để che cảnh báo mạng.
