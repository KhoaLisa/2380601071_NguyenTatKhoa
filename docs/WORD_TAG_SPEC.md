# Bộ tag Word Reading v07 đã kiểm chứng

Nguồn: [TOEIC_Reading_v07_co_tag.docx](source/TOEIC_Reading_v07_co_tag.docx), bản gốc người dùng cung cấp tại `D:\Downloads\TOEIC_Reading_v07_co_tag.docx`. Bản trong workspace giữ nguyên bytes, SHA-256 `377224A295316A02C1ABABE3C2F7163A887EC3C3AD98174AD76623294FC78E63`.

Đã đọc đặc tả ở đầu Word và kiểm thử parser với chính tệp này: 100 câu, 400 lựa chọn, 100 gợi ý, 19 nhóm, 27 tài liệu và 27 ảnh nhúng. Part 5 có 30 câu 101–130; Part 6 có 16 câu 131–146 trong bốn nhóm; Part 7 có 54 câu 147–200 trong 15 nhóm. Thời lượng 75 phút.

Word nêu rõ đáp án/gợi ý là bộ biên soạn theo nội dung PDF, chưa đối chiếu đáp án chính thức của tác giả vì PDF không kèm bảng đáp án. Ứng dụng giữ nguyên dữ liệu đó; không tự đoán/sửa đáp án và không tuyên bố đây là đáp án chính thức của tác giả.

## Phạm vi và cú pháp

Parser chỉ đọc từ `[EXAM ...]` đến `[/EXAM]`; bỏ phần hướng dẫn trước đó, header/footer và số trang. Tên tag phân biệt hoa/thường; thuộc tính dùng chữ thường, giá trị trong dấu nháy kép. Tất cả thuộc tính liệt kê dưới đây đều bắt buộc; boolean chỉ nhận chính xác `true` hoặc `false`.

```text
[EXAM version="1.0" id="reading_v07" minutes="75" question_count="100"]
[TITLE] Reading v07 [/TITLE]
[DIRECTIONS] Hướng dẫn chung [/DIRECTIONS]
[PART number="5" shuffle_groups="false"]
[DIRECTIONS] Hướng dẫn Part 5 [/DIRECTIONS]
[QUESTION id="q101" number="101" shuffle="true" shuffle_options="true"]
[STEM] Nội dung câu hỏi [/STEM]
[A] Lựa chọn A [/A]
[B] Lựa chọn B [/B]
[C] Lựa chọn C [/C]
[D] Lựa chọn D [/D]
[CORRECT] A [/CORRECT]
[HINT] Gợi ý đã biên soạn [/HINT]
[/QUESTION]
... các câu còn lại ...
[/PART]
... PART 6 rồi PART 7 ...
[/EXAM]
```

Đây là minh họa cú pháp rút gọn, không phải tệp đủ 100 câu để import.

## Nhóm, tài liệu và ảnh

Trong Part 6/7, mỗi GROUP có các PASSAGE trước danh sách QUESTION. `first`/`last` phải khớp chính xác các số câu trong nhóm, theo thứ tự. Các nhóm và câu con không được đảo. Nhiều PASSAGE được giữ nguyên thứ tự tài liệu gốc.

```text
[GROUP id="g131_134" first="131" last="134" shuffle="false"]
[PASSAGE id="g131_134_p1" kind="letter"]
[BLANKS] 131,132,133,134 [/BLANKS]
[IMAGE id="g131_134_p1_img" source_page="5"]
  (một đoạn Word chứa đúng một ảnh inline được nhúng)
[/IMAGE]
[/PASSAGE]
... QUESTION 131, 132, 133, 134 ...
[/GROUP]
```

`BLANKS` chỉ dùng cho Part 6 và đứng trước IMAGE. Danh sách số câu phải khớp toàn bộ câu trong nhóm, không thiếu, trùng hoặc trỏ sang nhóm khác. Trong v07, nội dung bài đọc/chỗ trống/bảng/tin nhắn và dấu [1]–[4] nằm trong ảnh cắt PDF; parser giữ ảnh gốc và metadata BLANKS, không tìm chuỗi `{{BLANK:131}}` trong ảnh và không OCR để thay nội dung.

Tag mở IMAGE, đoạn có ảnh và tag đóng IMAGE phải ở ba đoạn riêng. Ảnh phải là In Line with Text, dùng `a:blip r:embed` trỏ đến quan hệ ảnh nội bộ `word/document.xml.rels`. Ảnh ngoài, ảnh nổi, text box, OLE, thiếu bytes, sai chữ ký/MIME hoặc nhiều ảnh trong cùng IMAGE bị từ chối. ID nghiệp vụ lấy từ `IMAGE.id`, không lấy từ tên nội bộ `word/media/imageN.png`. Giữ `source_page` trên ContentBlock.

## Văn bản và định danh

Ghép các `w:t` của một đoạn theo đúng thứ tự, không tự chèn khoảng trắng khi Word chia tag thành nhiều run. Các trường TITLE/DIRECTIONS/BLANKS/STEM/A/B/C/D/CORRECT/HINT mở và đóng ngay trong cùng đoạn. Thứ tự trong QUESTION bắt buộc là STEM → A → B → C → D → CORRECT → HINT; STEM chỉ được rỗng ở Part 6.

Các ký hiệu [1], [2], [3], [4] là nội dung, không phải tag. Khi cần chứa nguyên văn chuỗi giống tag dành riêng, mã hóa `[` thành `&#91;`, `]` thành `&#93;`, `&` thành `&amp;` và dấu nháy trong thuộc tính thành `&quot;`. Giải mã entity đúng một lần sau khi tách tag.

ID QUESTION/GROUP/PASSAGE/IMAGE phải duy nhất trong loại của chúng trong đề. Database dùng GUID làm PK; ID nguồn và `OriginalNumber` lưu riêng. Định danh nguồn của lựa chọn được suy ra từ `SourceQuestionId + "_" + Key`, ví dụ `q101_A`. Lượt thi lưu option GUID và thứ tự hiển thị ổn định; chấm theo option ID, không theo chữ A ở vị trí sau đảo.

Part 5 cho phép đảo câu trong cùng phần theo `shuffle`; Part 6/7 giữ nguyên nhóm và câu con. `shuffle_options` chỉ áp dụng cho các lựa chọn của chính câu. Hai câu 163 và 167 giữ `shuffle_options="false"` vì gắn với vị trí chèn câu trong bài đọc.

## Validate, import và chỉnh sửa

- `ExamManagement/ValidateWord`: kiểm tra tệp và trả lỗi có `location/code/message/fix`; không ghi đề. `succeeded=true` tại endpoint này chỉ có nghĩa tệp hợp lệ, `examId` vẫn null.
- `ExamManagement/Import`: validate toàn bộ trước khi truy cập ghi SQL, rồi lưu đề/parts/groups/passages/blocks/questions/options trong một ABP Unit of Work có transaction. Ảnh lưu trong SQL cùng transaction, không có tài nguyên tạm tồn tại ngoài transaction. Hai lần SaveChanges chỉ giải quyết FK vòng câu/đáp án; chỉ commit một lần khi toàn bộ thành công.
- Import lại cùng SHA-256 trả đề đã nhập và `alreadyImported=true`; không tạo đề trùng.
- `ExamManagement/Validate`: kiểm tra lại cấu trúc và điều kiện xuất bản sau chỉnh sửa, trả `canPublish` cùng danh sách lỗi.
- Chỉ sửa Draft chưa có lượt thi. `CopyToDraft` tạo GUID mới cho toàn bộ graph, giữ nội dung/ID nguồn/thứ tự, đặt lại Draft và bỏ lịch sử/kết quả của đề gốc.
- Table editor chỉ chấp nhận table/thead/tbody/tfoot/tr/th/td/br và rowspan/colspan hợp lệ; từ chối script, event handler, ảnh/liên kết/DOCTYPE.

File upload tối đa 25 MB; gói giải nén tối đa 100 MB và 5000 entries. Lỗi thiếu trường, thuộc tính, lựa chọn, HINT, CORRECT, ID trùng, scope sai, thứ tự sai, ảnh thiếu và số câu/nhóm sai đều làm thất bại toàn bộ import.

Mỗi ảnh nhúng tối đa 10 MB, chỉ nhận PNG/JPEG/GIF/BMP/WebP với chữ ký/header ảnh, phần mở rộng và MIME khai báo khớp nhau. Chính sách dùng chung cho v07, legacy, thay ảnh và endpoint đọc ảnh; SVG/HTML hoặc bytes giả ảnh bị từ chối. Endpoint ảnh còn có `nosniff` và CSP sandbox; không phát lại MIME tùy ý lấy từ Word hay database. Kiểm tra container/header không thay thế một trình giải mã ảnh đầy đủ.

## Tương thích và kiểm thử

Parser vẫn đọc định dạng TEST legacy `[[TOEIC-READING version="1.0"]]`, DOCUMENT/PROMPT/OPTION/ANSWER và `{{BLANK:n}}` để giữ các fixture trước đây. Định dạng đó không phải cú pháp Word v07. Fixture TEST trong `docs/fixtures` không được dùng thay cho đề thật; bộ 100 câu legacy đã được cập nhật thành 4 nhóm Part 6 và 15 nhóm Part 7.

Chạy unit tests bằng SDK local từ project root:

```powershell
$env:DOTNET_CLI_HOME = 'E:\flutter_application_1\.dotnet_home'
& .\.dotnet\dotnet.exe test .\backend\test\NguyenTatKhoa_2380601071.Tests\NguyenTatKhoa_2380601071.Tests.csproj --no-restore
```

Kiểm thử gồm tệp v07 thật, tag chia run, entity decode một lần, vị trí [1]–[4], thuộc tính/cờ bắt buộc, ID trùng, CORRECT/option/HINT lỗi, BLANKS lệch nhóm, ảnh thiếu/ngoài/nhiều ảnh và tag đóng sai. Các kiểm tra SQL Server tích hợp xác minh validate/import/reimport, số lượng thực, Draft Copy không dùng lại GUID, sửa/validate/publish; dữ liệu kiểm thử được rollback.
