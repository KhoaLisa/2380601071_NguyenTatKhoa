# TOEICFlow audit — 17/10/2026

Đã kiểm tra source thực tế trước khi sửa chức năng. Root không có git; repository Flutter đang có thay đổi chưa commit. Không reset hoặc tạo lại project. Không có AGENTS.md áp dụng trong workspace.

## Nền tảng xác minh

- ASP.NET Boilerplate 10.2.0, MVC+jQuery, .NET 9 (`global.json` SDK9.0.319), EF Core SQL Server9.0.5.
- SQL Server `.\SQLEXPRESS`, database `ToeicReadingDb`, Windows Authentication. Đầu tác vụ: 10 bảng nghiệp vụ, 33 migration, 0 đề/0 lượt; FK đang bật và trusted.
- Source đã tìm theo đường dẫn người dùng cung cấp: `D:\Downloads\Reading v07.pdf` (29 trang), `TOEIC_Reading_v07_co_tag.docx`, `Đề thi giữa kỳ v2.pdf` (4 trang). Bản sao giữ nguyên trong `docs/source/`.
- Stitch chính thức của tác vụ: https://stitch.withgoogle.com/projects/8218148455783376277. Đã đọc12màn hình và design system, tảiHTML tham chiếu trong `docs/design`. Không dùng project Stitch cũ.

## Phát hiện trước sửa

| Phần | Đã có | Vấn đề/cần hoàn thiện |
|---|---|---|
| Database | Schema10bảng, FKghép, filteredindex, migration/SQL/setup | Không cần tạo database hoặc schema khác |
| Parser | ZIP/XML, nối Wordrun, bảng/ảnh, legacy tags | Chưa đọc single-bracket v07; kiểmtra nhóm/hint/cờ/scopes chưa đủ; nhómrỗng có thể gây Min()exception |
| Management | Import/update/publish/stats theo ABP | Thiếu validate-only, deepcopy Draft; cần sanitize bảng, preserve v07metadata |
| StudentAPI | Start/resume/save/cancel/submit/score | Thiếu history, directions/parts, workerthu bài; cần concurrenttest |
| MVC | ViewsExams dùng DB | Home có số liệu mẫu, AdminLTE mặcđịnh, thiếu search/filter và publishconfirm; checkbox shuffle binding sai thứtự |
| Flutter | Model/API/list/attempt/result | Main cònLogin, theme chưa Stitch, thiếu instructions/grid/persistence; bảng bị flatten; wide-layout overflow; auto-submitretry và pending-save race |
| Identity | ABPUser/Role/Permission/JWT | Cần demoaccessDevelopment riêng teacher/student, không mở anonymous quảntrị |

## Phương án đã chọn

Giữ10bảng và33migration. Bổ sung parser v07 từWordthật, chia sẻ validation/backendfinalizer; dùng SQLtransactionlock hiện có. Thêm workerABP15giây và kiểmtra deadline trên mọi APIliênquan. UI website luônGiảngviên; Flutter luônSinhviên, independentdeviceidentity; không phânvai theoUserAgent/kíchthước.

Developmentdemo cần opt-in và giới hạn địa chỉ/origin; dùng Identityprincipal/JWTthật với quyền tối thiểu. Production giữ xác thực thông thường. Không dùng chungadmin hoặc sharedattempt cho sinhviên.

Kiểmthử dữliệu giả trong transactionrollback; dữliệuReadingv07 lấy nguyênfile nguồn, không tựbịa đápán/gợiý. Các sốliệu thốngkê UI lấy từSQL và hiểnthị empty nếu chưa có lượt thật.
