# TOEICFlow API contract

Backend giữ convention ASP.NET Boilerplate, không có API song song. Base URL Development đề xuất `http://localhost:5000`; Flutter Chrome origin `http://localhost:5200` hoặc `http://127.0.0.1:5200`. Android emulator dùng `http://10.0.2.2:5000`; thiết bị thật dùng địa chỉ LAN đã cấu hình rõ và allowlist demo phù hợp.

## Envelope và kiểu dữ liệu

```json
{"result":{},"targetUrl":null,"success":true,"error":null,"unAuthorizedRequest":false,"__abp":true}
```

Danh sách nằm ở `result.items`, không phải JSON array gốc. Lỗi HTTP/ABP có `success:false`, `error.message`, `error.details`, có thể có `error.validationErrors` chứa message/members. Không hiển thị success khi `success:false`. GUID là chuỗi; UserId của ABP là số bigint. Thời gian nghiệp vụ ISO8601 UTC với hậu tốZ; nullable giữJSONnull. Cácenum serialize số: Exam Draft0/Published1/Archived2; Attempt InProgress0/Submitted1/Expired2; Part5/6/7; Block Text1/Image2/Table3.

Authorization dùng `Bearer <accessToken>`. Các API AppService vẫn kiểm tra auth/permission, không chuyển toàn bộ sang anonymous.

## Development demo không có màn đăng nhập

Chỉ bật trong `Development` với `DemoAccess:Enabled=true` và địa chỉ/origin được phép. MVC auto-session giảng viên có đúng quyền TOEIC, không cấp Users/Roles/Tenants. Flutter tạo `installationId` ngẫu nhiên32bytes(64hex) và lưu cục bộ, gọi:

`POST /api/demo/student-session`

```json
{"installationId":"64-hex-character-random-id"}
```

Kết quả `result`: `{accessToken,expireInSeconds,userId}`. Mỗiinstallation ánhxạ một host user role-less; không dùngadmin hoặc mộtlượtthi dùngchung. Token giữ trongbộnhớ, cóthể bootstraplại bằnginstallationId đãlưu khi mởapp. Production tắtendpointdemo và dùng auth thôngthường, không giả rằng cấu hìnhdemo là đăngnhập production.

## StudentExam

Prefix `/api/services/app/StudentExam/`.

| Method / route | Request | Result |
|---|---|---|
| GET GetPublishedExams | không | items với id,code,title,durationMinutes,questionCount,canStart,directions,parts |
| POST Start | `{examId}` | attemptId,startedAtUtc,dueAtUtc,resumedExistingAttempt |
| GET GetAttempt | query attemptId | attemptId,examId,examTitle,directions,parts,serverNowUtc,startedAtUtc,dueAtUtc,status,groups,questions,result |
| GET GetMyAttempts | không | items của đúng ngườihiệntại, gồm trạngthái và result cho bàinộp |
| POST SaveAnswer | `{attemptId,questionId,selectedOptionId}` | accepted,isFinalized,result |
| POST Submit | `{attemptId}` | AttemptResultDto |

`selectedOptionId:null` là hủy lựa chọn. Lựa chọn phải thuộc câu và câu phải thuộc lượt của người gọi. Nếu vừa hết hạn, SaveAnswer trả `accepted:false,isFinalized:true,result` và commit kết quả; Flutter hiển thị kết quả đó. Khi đã nộp từ trước, ghi mới bị từchối.

Start được tuần tự hóa theo `(examId,userId)`; phục hồi lượt đang làm không cần chờ khóa chung của đề. Khi tạo mới, backend vẫn lấy khóa đề và kiểm tra Published lại. Sau khi lưu đủ 100 câu/400 lựa chọn, server chốt StartedAtUtc/DueAtUtc trong cùng transaction để thời gian tạo snapshot không trừ vào 75 phút thi. Resume giữ nguyên hai mốc này.

Flutter chặn các Start trùng đang chạy cho cùng đề. Riêng Start chờ tối đa 45 giây, API đọc/lưu thông thường vẫn 20 giây. Nếu mất kết nối/timeout không rõ kết quả ghi, client đọc lịch sử của chính người dùng một lần để phục hồi lượt đang làm và hạn nộp đã lưu; không tự gửi lại POST Start một cách mù quáng. Không tìm được lượt thì báo trạng thái chưa xác nhận, không tự tạo giờ mới. Đây là cơ chế phục hồi, không phải cam kết mọi server khởi động nhanh dưới một ngưỡng cố định.

`parts`: `{partNumber,directions,questionCount}`. `groups`: `{id,title,part,displayOrder,contentBlocks}`; blockcó documentKey/displayOrder/type/textContent/contentUrl/mediaType. API attempt không chứa đápánđúng hoặc Hint. Option `{id,key,text}`; key là vịtríhiểnthị sauđảo, chấmtheoid. Dữliệuảnh tải riêng qua `GET /api/toeic/content/{blockId}` kèmxácthực; khôngtrảbytesảnh trong danh sáchđề.

Kết quả: `{attemptId,status,submittedAtUtc,totalQuestions,correctCount,incorrectCount,skippedCount,correctPercentage}`. Đúng+sai+bỏqua=tổng; phầntrămđúng=đúng/tổng×100, khôngđổiTOEIC/10. Nộplại trảkếtquảđãlưu. Hạnnộpgiữnguyênkhi refresh. WorkerABP quét15giây vàmọiAPIđọc/ghi/nộp liênquan vẫntựkiểmtrahạn.

## ExamManagement

Prefix `/api/services/app/ExamManagement/`; yêu cầu Pages.Exams, Manage hoặc Statistics tươngứng.

| Method / route | Request | Result |
|---|---|---|
| GET GetAll | không | items đềvàtrạngthái/lượtlàmthật |
| GET Get | query id | chi tiết giảngviên, cóđápán/gợiý |
| POST ValidateWord | `{fileName,content}` | Succeeded/Errors/QuestionCount; khôngghiđề |
| POST Import | `{fileName,content}` | Succeeded/AlreadyImported/ExamId/Errors |
| PUT Update | UpdateExamInput | lưuDraft vàvalidate, đềđãdùngbịtừchối |
| POST ReplaceImage | `{examId,blockId,fileName,content}` | thay ảnh Draft; bytes JSON base64, tối đa 10 MB |
| POST Validate | `{id}` hoặc queryid theoSwagger thực tế | CanPublish/Errors |
| POST Publish | `{id}` | xuấtbản sauvalidate |
| POST CopyToDraft | `{id,code?,title?}` | chi tiết bảnDraft mới với IDmới |
| GET GetStatistics | queryid | lượt/ sốngười/ trungbình/ top10SQL |

Trong dynamicAPI, content củaImportExamInput là bytes JSONbase64 để truyềnHTTP; database vẫn lưubytesảnh varbinary(max), khônglưu base64 vào cộtvănbản. MVCupload dùng multipart `.docx` và gọi chungApplicationService. Importbịlỗi khôngđểlạidữliệu/ảnh.

CácRazoraction hiệntạicó cùngnghiệpvụ dưới `/Exams`, khôngdựngAPIplaceholder riêng. Mặcđịnh `/` làdashboardgiảngviên; Fluttermặcđịnh làdanhsáchđềsinhviên.
