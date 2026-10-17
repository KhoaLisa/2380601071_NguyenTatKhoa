using Abp.Dependency;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NguyenTatKhoa_2380601071.Toeic.Importing;

public class DocxExamParser : IDocxExamParser, ITransientDependency
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace Ct = "http://schemas.openxmlformats.org/package/2006/content-types";
    private static readonly Regex TagRegex = new Regex(@"^\s*\[\[(?<close>/)?(?<name>[A-Z][A-Z0-9-]*)(?<attrs>[^\]]*)\]\](?<tail>.*)$", RegexOptions.Compiled);
    private static readonly Regex AttributeRegex = new Regex(@"(?<name>[A-Za-z][A-Za-z0-9]*)\s*=\s*""(?<value>(?:\\.|[^""])*)""", RegexOptions.Compiled);
    private static readonly Regex BlankRegex = new Regex(@"\{\{BLANK:(?<number>\d{3})\}\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly HashSet<string> KnownTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "TOEIC-READING", "EXAM", "PART", "GROUP", "DOCUMENT", "QUESTION",
        "PROMPT", "OPTION", "ANSWER", "HINT"
    };

    public ParsedExamDocument Parse(byte[] docxBytes)
    {
        var result = new ParsedExamDocument();
        if (docxBytes == null || docxBytes.Length == 0)
        {
            AddError(result, "Tệp", "EMPTY_FILE", "Tệp .docx rỗng.", "Chọn tệp Word có nội dung và tag TOEIC Reading.");
            return result;
        }

        try
        {
            using var stream = new MemoryStream(docxBytes, writable: false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
            if (archive.Entries.Count > 5000 || archive.Entries.Sum(x => x.Length) > 100L * 1024 * 1024)
            {
                AddError(result, "Tệp", "DOCX_EXPANSION_LIMIT", "Gói Word chứa quá nhiều dữ liệu giải nén.", "Nén ảnh trong Word, giữ nội dung dưới 100 MB sau giải nén.");
                return result;
            }
            var documentEntry = archive.GetEntry("word/document.xml");
            if (documentEntry == null)
            {
                AddError(result, "Tệp", "INVALID_DOCX", "Không tìm thấy word/document.xml.", "Lưu lại tài liệu bằng định dạng .docx chuẩn.");
                return result;
            }

            var relationships = ReadRelationships(archive);
            var contentTypes = ReadContentTypes(archive);
            XDocument document;
            using (var documentStream = documentEntry.Open())
            {
                document = XDocument.Load(documentStream, LoadOptions.PreserveWhitespace);
            }

            var state = new ParserState(result, archive, relationships, contentTypes);
            var body = document.Root?.Element(W + "body");
            if (body == null)
            {
                AddError(result, "Tệp", "INVALID_DOCX", "Tài liệu không có phần thân Word.", "Tạo lại tệp .docx từ Microsoft Word hoặc LibreOffice.");
                return result;
            }

            if (V07DocxExamParser.IsV07(body)) return V07DocxExamParser.Parse(archive, body);

            var index = 0;
            foreach (var element in body.Elements())
            {
                index++;
                if (element.Name == W + "p")
                {
                    ParseParagraph(state, element, index);
                }
                else if (element.Name == W + "tbl")
                {
                    ParseTable(state, element, index);
                }
            }

            FinishOpenScopes(state);
            Validate(result);
        }
        catch (InvalidDataException)
        {
            AddError(result, "Tệp", "INVALID_DOCX", "Tệp không phải gói .docx hợp lệ hoặc đã bị hỏng.", "Mở tệp trong Word và dùng Save As → Word Document (.docx).");
        }
        catch (Exception ex)
        {
            AddError(result, "Tệp", "PARSE_FAILURE", "Không thể đọc tệp: " + ex.Message, "Kiểm tra lại cấu trúc .docx và đặc tả tag phiên bản 1.0.");
        }

        return result;
    }

    private static void ParseParagraph(ParserState state, XElement paragraph, int index)
    {
        var text = string.Concat(paragraph.Descendants(W + "t").Select(x => (string)x));
        var location = "Đoạn " + index.ToString(CultureInfo.InvariantCulture);
        var isEscapedTag = text.TrimStart().StartsWith(@"\[[", StringComparison.Ordinal);

        if (!isEscapedTag && TryReadTag(text, out var tag))
        {
            HandleTag(state, tag, location);
        }
        else if (!isEscapedTag && text.TrimStart().StartsWith("[[", StringComparison.Ordinal))
        {
            AddError(state.Result, location, "MALFORMED_TAG", "Cú pháp tag không hợp lệ.", "Kiểm tra dấu [[...]], tên tag và thuộc tính trong dấu nháy kép.");
        }
        else if (paragraph.Descendants(A + "blip").Any())
        {
            var buffer = new StringBuilder();
            foreach (var item in paragraph.Descendants())
            {
                if (item.Name == W + "t") buffer.Append((string)item);
                else if (item.Name == W + "tab") buffer.Append('\t');
                else if (item.Name == W + "br") buffer.Append('\n');
                else if (item.Name == A + "blip")
                {
                    if (!string.IsNullOrWhiteSpace(buffer.ToString())) HandlePlainText(state, Unescape(buffer.ToString()), location);
                    buffer.Clear();
                    var imageId = (string)item.Attribute(R + "embed");
                    if (imageId == null) AddError(state.Result, location, "EXTERNAL_IMAGE", "Ảnh không được nhúng trong Word.", "Chèn ảnh trực tiếp, không dùng liên kết ngoài.");
                    else AddImageBlock(state, imageId, location);
                }
            }
            if (!string.IsNullOrWhiteSpace(buffer.ToString())) HandlePlainText(state, Unescape(buffer.ToString()), location);
            return;
        }
        else if (!string.IsNullOrWhiteSpace(text))
        {
            HandlePlainText(state, Unescape(text), location);
        }

        foreach (var relationshipId in paragraph.Descendants(A + "blip")
                     .Select(x => (string)x.Attribute(R + "embed"))
                     .Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            AddImageBlock(state, relationshipId, location);
        }
    }

    private static void ParseTable(ParserState state, XElement table, int index)
    {
        var location = "Bảng " + index.ToString(CultureInfo.InvariantCulture);
        if (state.CurrentDocumentKey == null || state.CurrentGroup == null)
        {
            AddError(state.Result, location, "TABLE_OUTSIDE_DOCUMENT", "Bảng nằm ngoài [[DOCUMENT]].", "Đặt bảng giữa [[DOCUMENT ...]] và [[/DOCUMENT]].");
            return;
        }

        var html = new StringBuilder("<table><tbody>");
        foreach (var row in table.Elements(W + "tr"))
        {
            html.Append("<tr>");
            foreach (var cell in row.Elements(W + "tc"))
            {
                var value = string.Join("\n", cell.Elements(W + "p")
                    .Select(p => string.Concat(p.Descendants(W + "t").Select(x => (string)x)))
                    .Where(x => !string.IsNullOrWhiteSpace(x)));
                html.Append("<td>").Append(WebUtility.HtmlEncode(Unescape(value))).Append("</td>");
            }
            html.Append("</tr>");
        }
        html.Append("</tbody></table>");

        state.CurrentGroup.ContentBlocks.Add(new ParsedContentBlock
        {
            DocumentKey = state.CurrentDocumentKey,
            DisplayOrder = state.CurrentGroup.ContentBlocks.Count + 1,
            Type = ContentBlockType.Table,
            TextContent = html.ToString()
        });
    }

    private static void HandleTag(ParserState state, ParsedTag tag, string location)
    {
        if (!KnownTags.Contains(tag.Name))
        {
            AddError(state.Result, location, "UNKNOWN_TAG", $"Tag [[{tag.Name}]] không được hỗ trợ.", "Dùng đúng các tag trong đặc tả TOEIC-DOCX 1.0.");
            return;
        }

        if (tag.IsClosing)
        {
            HandleClosingTag(state, tag.Name, location);
            return;
        }

        switch (tag.Name)
        {
            case "TOEIC-READING":
                state.Result.SpecificationVersion = Get(tag, "version");
                break;
            case "EXAM":
                state.Result.Code = Get(tag, "code");
                state.Result.Title = Get(tag, "title");
                state.Result.DurationMinutes = GetInt(tag, "duration", ToeicConsts.OfficialDurationMinutes, state.Result, location);
                state.Result.IsOfficial = GetBool(tag, "official", false, state.Result, location);
                break;
            case "PART":
                if (state.CurrentPart != null || state.CurrentGroup != null || state.CurrentQuestion != null || state.CurrentDocumentKey != null)
                    AddError(state.Result, location, "NESTED_PART", "PART mới mở khi phạm vi trước chưa đóng.", "Đóng các tag con và PART trước khi mở phần mới.");
                var partNumber = GetInt(tag, "number", 0, state.Result, location);
                if (partNumber != 5 && partNumber != 6 && partNumber != 7)
                {
                    AddError(state.Result, location, "INVALID_PART", "PART chỉ nhận 5, 6 hoặc 7.", "Sửa thành [[PART number=\"5\"]], 6 hoặc 7.");
                }
                else
                {
                    state.CurrentPart = (ToeicPart)partNumber;
                    if (state.Result.Parts.Any(x => x.Number == state.CurrentPart))
                        AddError(state.Result, location, "DUPLICATE_PART", "PART bị khai báo lặp.", "Mỗi phần chỉ được xuất hiện một lần.");
                    state.Result.Parts.Add(new ParsedExamPart { Number = (ToeicPart)partNumber, DisplayOrder = state.Result.Parts.Count + 1 });
                }
                break;
            case "GROUP":
                OpenGroup(state, tag, location);
                break;
            case "DOCUMENT":
                if (state.CurrentGroup == null)
                {
                    AddError(state.Result, location, "DOCUMENT_OUTSIDE_GROUP", "DOCUMENT phải nằm trong GROUP.", "Mở [[GROUP ...]] trước DOCUMENT.");
                    return;
                }
                if (state.CurrentDocumentKey != null)
                {
                    AddError(state.Result, location, "NESTED_DOCUMENT", "Không được lồng DOCUMENT.", "Đóng DOCUMENT hiện tại trước khi mở tài liệu mới.");
                }
                if (state.CurrentQuestion != null)
                    AddError(state.Result, location, "DOCUMENT_INSIDE_QUESTION", "DOCUMENT không được nằm trong QUESTION.", "Đóng QUESTION trước tài liệu bài đọc.");
                state.CurrentDocumentKey = Get(tag, "id");
                if (string.IsNullOrWhiteSpace(state.CurrentDocumentKey))
                {
                    AddError(state.Result, location, "MISSING_DOCUMENT_ID", "DOCUMENT thiếu id.", "Thêm id duy nhất, ví dụ [[DOCUMENT id=\"email-1\"]].");
                    state.CurrentDocumentKey = "invalid-document-" + location.Replace(" ", "-");
                }
                if (state.CurrentGroup.DocumentKeys.Contains(state.CurrentDocumentKey, StringComparer.OrdinalIgnoreCase))
                    AddError(state.Result, location, "DUPLICATE_DOCUMENT", "ID DOCUMENT bị trùng trong nhóm.", "Giữ một ID duy nhất cho mỗi tài liệu.");
                state.CurrentGroup.DocumentKeys.Add(state.CurrentDocumentKey);
                break;
            case "QUESTION":
                OpenQuestion(state, tag, location);
                break;
            case "PROMPT":
                StartQuestionField(state, "PROMPT", tag.Tail, location);
                break;
            case "OPTION":
                OpenOption(state, tag, location);
                break;
            case "ANSWER":
                if (state.CurrentQuestion == null)
                {
                    AddError(state.Result, location, "ANSWER_OUTSIDE_QUESTION", "ANSWER nằm ngoài QUESTION.", "Đặt ANSWER trước [[/QUESTION]].");
                }
                else
                {
                    state.CurrentQuestion.CorrectOptionKey = (Get(tag, "key") ?? tag.Tail.Trim()).ToUpperInvariant();
                    state.CurrentQuestionField = null;
                }
                break;
            case "HINT":
                StartQuestionField(state, "HINT", tag.Tail, location);
                break;
        }
    }

    private static void HandleClosingTag(ParserState state, string name, string location)
    {
        switch (name)
        {
            case "PART":
                if (state.CurrentPart == null)
                    AddError(state.Result, location, "UNEXPECTED_CLOSE", "Gặp /PART nhưng chưa mở PART.", "Xóa tag đóng thừa hoặc thêm tag mở tương ứng.");
                if (state.CurrentQuestion != null || state.CurrentGroup != null || state.CurrentDocumentKey != null)
                {
                    AddError(state.Result, location, "UNCLOSED_SCOPE", "PART đóng khi GROUP, DOCUMENT hoặc QUESTION chưa đóng.", "Đóng các tag con theo thứ tự trước [[/PART]].");
                }
                state.CurrentPart = null;
                break;
            case "GROUP":
                if (state.CurrentGroup == null)
                    AddError(state.Result, location, "UNEXPECTED_CLOSE", "Gặp /GROUP nhưng chưa mở GROUP.", "Xóa tag đóng thừa hoặc thêm tag mở tương ứng.");
                if (state.CurrentDocumentKey != null || state.CurrentQuestion != null)
                {
                    AddError(state.Result, location, "UNCLOSED_SCOPE", "GROUP đóng khi DOCUMENT hoặc QUESTION chưa đóng.", "Đóng tag con trước [[/GROUP]].");
                }
                state.CurrentDocumentKey = null;
                state.CurrentQuestion = null;
                state.CurrentQuestionField = null;
                state.CurrentGroup = null;
                break;
            case "DOCUMENT":
                if (state.CurrentDocumentKey == null)
                {
                    AddError(state.Result, location, "UNEXPECTED_CLOSE", "Gặp [[/DOCUMENT]] nhưng chưa mở DOCUMENT.", "Xóa tag đóng thừa hoặc thêm tag mở tương ứng.");
                }
                state.CurrentDocumentKey = null;
                break;
            case "QUESTION":
                if (state.CurrentQuestion == null)
                {
                    AddError(state.Result, location, "UNEXPECTED_CLOSE", "Gặp [[/QUESTION]] nhưng chưa mở QUESTION.", "Xóa tag đóng thừa hoặc thêm tag mở tương ứng.");
                }
                state.CurrentQuestion = null;
                state.CurrentQuestionField = null;
                state.CurrentOption = null;
                break;
            case "PROMPT":
            case "OPTION":
            case "HINT":
                state.CurrentQuestionField = null;
                state.CurrentOption = null;
                break;
            case "TOEIC-READING":
            case "EXAM":
                break;
            default:
                AddError(state.Result, location, "UNEXPECTED_CLOSE", $"Tag [[/{name}]] không có phạm vi đóng hợp lệ.", "Kiểm tra lại quy tắc lồng tag.");
                break;
        }
    }

    private static void OpenGroup(ParserState state, ParsedTag tag, string location)
    {
        if (state.CurrentPart != ToeicPart.Part6 && state.CurrentPart != ToeicPart.Part7)
        {
            AddError(state.Result, location, "GROUP_WRONG_PART", "GROUP chỉ dùng cho Part 6 hoặc Part 7.", "Bỏ GROUP ở Part 5 hoặc chuyển nó vào PART 6/7.");
        }
        if (state.CurrentGroup != null)
        {
            AddError(state.Result, location, "NESTED_GROUP", "Không được lồng GROUP.", "Đóng GROUP hiện tại trước khi mở nhóm mới.");
        }

        var group = new ParsedPassageGroup
        {
            ExternalId = Get(tag, "id"),
            Title = Get(tag, "title"),
            Part = state.CurrentPart ?? 0,
            DisplayOrder = GetInt(tag, "order", state.Result.Groups.Count + 1, state.Result, location),
            AllowQuestionShuffle = GetBool(tag, "shuffleQuestions", false, state.Result, location)
        };
        state.Result.Groups.Add(group);
        state.CurrentGroup = group;
    }

    private static void OpenQuestion(ParserState state, ParsedTag tag, string location)
    {
        if (state.CurrentPart == null)
        {
            AddError(state.Result, location, "QUESTION_OUTSIDE_PART", "QUESTION phải nằm trong PART.", "Mở PART 5, 6 hoặc 7 trước QUESTION.");
        }
        if (state.CurrentDocumentKey != null)
        {
            AddError(state.Result, location, "QUESTION_INSIDE_DOCUMENT", "QUESTION không được nằm trong DOCUMENT.", "Đóng DOCUMENT trước khi khai báo câu hỏi.");
        }
        if (state.CurrentQuestion != null)
        {
            AddError(state.Result, location, "NESTED_QUESTION", "Không được lồng QUESTION.", "Đóng câu hỏi hiện tại trước câu tiếp theo.");
        }

        var question = new ParsedQuestion
        {
            Part = state.CurrentPart ?? 0,
            GroupExternalId = state.CurrentGroup?.ExternalId,
            OriginalNumber = GetInt(tag, "number", 0, state.Result, location),
            DisplayOrder = GetInt(tag, "order", state.Result.Questions.Count + 1, state.Result, location),
            AllowQuestionShuffle = GetBool(tag, "shuffleQuestion", false, state.Result, location),
            AllowOptionShuffle = GetBool(tag, "shuffleOptions", false, state.Result, location)
        };
        state.Result.Questions.Add(question);
        state.CurrentQuestion = question;
        state.CurrentQuestionField = null;
        state.CurrentOption = null;
    }

    private static void OpenOption(ParserState state, ParsedTag tag, string location)
    {
        if (state.CurrentQuestion == null)
        {
            AddError(state.Result, location, "OPTION_OUTSIDE_QUESTION", "OPTION nằm ngoài QUESTION.", "Đặt OPTION bên trong QUESTION.");
            return;
        }

        var key = (Get(tag, "key") ?? string.Empty).ToUpperInvariant();
        var option = new ParsedQuestionOption
        {
            Key = key,
            DisplayOrder = state.CurrentQuestion.Options.Count + 1,
            Text = Unescape(tag.Tail.Trim())
        };
        state.CurrentQuestion.Options.Add(option);
        state.CurrentOption = option;
        state.CurrentQuestionField = "OPTION";
    }

    private static void StartQuestionField(ParserState state, string field, string tail, string location)
    {
        if (state.CurrentQuestion == null)
        {
            AddError(state.Result, location, field + "_OUTSIDE_QUESTION", field + " nằm ngoài QUESTION.", "Đặt tag này bên trong QUESTION.");
            return;
        }
        state.CurrentQuestionField = field;
        state.CurrentOption = null;
        var value = Unescape(tail.Trim());
        if (field == "PROMPT")
        {
            state.CurrentQuestion.Prompt = value;
        }
        else if (field == "HINT")
        {
            state.CurrentQuestion.Hint = value;
        }
    }

    private static void HandlePlainText(ParserState state, string text, string location)
    {
        if (state.CurrentQuestion != null && state.CurrentQuestionField != null)
        {
            if (state.CurrentQuestionField == "PROMPT")
            {
                state.CurrentQuestion.Prompt = AppendLine(state.CurrentQuestion.Prompt, text);
            }
            else if (state.CurrentQuestionField == "HINT")
            {
                state.CurrentQuestion.Hint = AppendLine(state.CurrentQuestion.Hint, text);
            }
            else if (state.CurrentQuestionField == "OPTION" && state.CurrentOption != null)
            {
                state.CurrentOption.Text = AppendLine(state.CurrentOption.Text, text);
            }
            return;
        }

        if (state.CurrentDocumentKey != null && state.CurrentGroup != null)
        {
            state.CurrentGroup.ContentBlocks.Add(new ParsedContentBlock
            {
                DocumentKey = state.CurrentDocumentKey,
                DisplayOrder = state.CurrentGroup.ContentBlocks.Count + 1,
                Type = ContentBlockType.Text,
                TextContent = text
            });
            return;
        }

        AddError(state.Result, location, "UNTAGGED_TEXT", "Có nội dung nằm ngoài DOCUMENT hoặc trường của QUESTION.", "Đặt nội dung bài đọc trong DOCUMENT; đặt nội dung câu hỏi sau PROMPT/OPTION/HINT.");
    }

    private static void AddImageBlock(ParserState state, string relationshipId, string location)
    {
        if (state.CurrentDocumentKey == null || state.CurrentGroup == null)
        {
            AddError(state.Result, location, "IMAGE_OUTSIDE_DOCUMENT", "Ảnh nhúng nằm ngoài DOCUMENT.", "Đặt ảnh giữa [[DOCUMENT ...]] và [[/DOCUMENT]].");
            return;
        }
        if (!state.Relationships.TryGetValue(relationshipId, out var target))
        {
            AddError(state.Result, location, "BROKEN_IMAGE_RELATIONSHIP", $"Không tìm thấy quan hệ ảnh {relationshipId}.", "Chèn lại ảnh trực tiếp vào Word, không dùng ảnh liên kết ngoài.");
            return;
        }

        var path = ResolvePartPath("word/document.xml", target);
        var entry = state.Archive.GetEntry(path);
        if (entry == null)
        {
            AddError(state.Result, location, "MISSING_EMBEDDED_IMAGE", $"Không tìm thấy ảnh nhúng {path}.", "Chèn ảnh bằng Insert → Pictures để ảnh được nhúng trong .docx.");
            return;
        }
        if (entry.Length == 0 || entry.Length > ExamImageValidator.MaxBytes)
        {
            AddError(state.Result, location, "INVALID_IMAGE_BYTES", "Ảnh nhúng phải có nội dung và không vượt quá 10 MB.", "Chèn ảnh PNG/JPEG/GIF/BMP/WebP hợp lệ dưới 10 MB.");
            return;
        }

        byte[] bytes;
        using (var input = entry.Open())
        using (var output = new MemoryStream())
        {
            input.CopyTo(output);
            bytes = output.ToArray();
        }
        if (!ExamImageValidator.TryGetMediaType(bytes, path, GetMediaType(path, state.ContentTypes), out var mediaType))
        {
            AddError(state.Result, location, "INVALID_IMAGE_BYTES", "Ảnh nhúng không hợp lệ hoặc phần mở rộng/MIME không khớp nội dung.", "Chèn ảnh PNG/JPEG/GIF/BMP/WebP hợp lệ; không dùng SVG, HTML hoặc đổi đuôi tệp.");
            return;
        }

        state.CurrentGroup.ContentBlocks.Add(new ParsedContentBlock
        {
            DocumentKey = state.CurrentDocumentKey,
            DisplayOrder = state.CurrentGroup.ContentBlocks.Count + 1,
            Type = ContentBlockType.Image,
            BinaryData = bytes,
            MediaType = mediaType,
            FileName = Path.GetFileName(path)
        });
    }

    private static void FinishOpenScopes(ParserState state)
    {
        if (state.CurrentPart != null)
            AddError(state.Result, "Cuối tệp", "UNCLOSED_PART", "PART chưa được đóng.", "Thêm [[/PART]].");
        if (state.CurrentQuestion != null)
        {
            AddError(state.Result, "Cuối tệp", "UNCLOSED_QUESTION", "QUESTION chưa được đóng.", "Thêm [[/QUESTION]].");
        }
        if (state.CurrentDocumentKey != null)
        {
            AddError(state.Result, "Cuối tệp", "UNCLOSED_DOCUMENT", "DOCUMENT chưa được đóng.", "Thêm [[/DOCUMENT]].");
        }
        if (state.CurrentGroup != null)
        {
            AddError(state.Result, "Cuối tệp", "UNCLOSED_GROUP", "GROUP chưa được đóng.", "Thêm [[/GROUP]].");
        }
    }

    private static void Validate(ParsedExamDocument result)
    {
        if (result.SpecificationVersion != "1.0")
        {
            AddError(result, "TOEIC-READING", "UNSUPPORTED_VERSION", "Thiếu hoặc sai version đặc tả.", "Dùng [[TOEIC-READING version=\"1.0\"]].");
        }
        if (string.IsNullOrWhiteSpace(result.Code))
        {
            AddError(result, "EXAM", "MISSING_EXAM_CODE", "EXAM thiếu code.", "Thêm code ổn định và duy nhất.");
        }
        if (string.IsNullOrWhiteSpace(result.Title))
        {
            AddError(result, "EXAM", "MISSING_EXAM_TITLE", "EXAM thiếu title.", "Thêm tên đề thi.");
        }
        if (result.DurationMinutes <= 0)
        {
            AddError(result, "EXAM", "INVALID_DURATION", "Thời lượng phải lớn hơn 0.", "Đặt duration=\"75\" cho đề chính thức.");
        }
        if (result.Questions.Count == 0)
            AddError(result, "EXAM", "EMPTY_EXAM", "Đề chưa có câu hỏi.", "Bổ sung các QUESTION hợp lệ trước khi import.");
        if (result.Code?.Length > ToeicConsts.MaxCodeLength || result.Title?.Length > ToeicConsts.MaxTitleLength)
            AddError(result, "EXAM", "METADATA_TOO_LONG", "Mã đề hoặc tên đề vượt độ dài cho phép.", "Mã đề tối đa 64 ký tự; tên đề tối đa 256 ký tự.");
        if (result.Groups.Any(x => x.DisplayOrder <= 0) || result.Groups.GroupBy(x => x.DisplayOrder).Any(x => x.Count() > 1) ||
            result.Questions.Any(x => x.DisplayOrder <= 0) || result.Questions.GroupBy(x => x.DisplayOrder).Any(x => x.Count() > 1))
            AddError(result, "EXAM", "INVALID_ORDER", "Thứ tự nhóm hoặc câu bị trùng/không dương.", "Dùng order dương và duy nhất trong đề.");

        foreach (var duplicate in result.Groups.Where(x => !string.IsNullOrWhiteSpace(x.ExternalId)).GroupBy(x => x.ExternalId, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1))
        {
            AddError(result, "GROUP " + duplicate.Key, "DUPLICATE_GROUP", "ID nhóm bị trùng.", "Đổi id để mỗi GROUP là duy nhất trong đề.");
        }
        foreach (var group in result.Groups)
        {
            if (string.IsNullOrWhiteSpace(group.ExternalId))
            {
                AddError(result, "GROUP", "MISSING_GROUP_ID", "GROUP thiếu id.", "Thêm id ổn định cho nhóm.");
            }
            if (group.Part != ToeicPart.Part6 && group.Part != ToeicPart.Part7)
            {
                AddError(result, "GROUP " + group.ExternalId, "INVALID_GROUP_PART", "GROUP không thuộc Part 6/7.", "Chuyển GROUP vào đúng PART.");
            }
            if (group.ContentBlocks.Count == 0)
            {
                AddError(result, "GROUP " + group.ExternalId, "EMPTY_GROUP", "Nhóm không có khối nội dung.", "Thêm ít nhất một DOCUMENT chứa text, bảng hoặc ảnh.");
            }
            if (!result.Questions.Any(x => x.GroupExternalId == group.ExternalId))
                AddError(result, "GROUP " + group.ExternalId, "GROUP_WITHOUT_QUESTIONS", "Nhóm không có câu hỏi liên quan.", "Đặt các QUESTION tương ứng bên trong nhóm.");
            foreach (var key in group.DocumentKeys.Where(key => !group.ContentBlocks.Any(x => x.DocumentKey == key)))
                AddError(result, "DOCUMENT " + key, "EMPTY_DOCUMENT", "Tài liệu không có nội dung.", "Bổ sung text, bảng hoặc ảnh trước khi import.");
            if (group.ExternalId?.Length > 64 || group.DocumentKeys.Any(x => x.Length > 64))
                AddError(result, "GROUP " + group.ExternalId, "ID_TOO_LONG", "ID nhóm hoặc tài liệu quá 64 ký tự.", "Rút gọn ID nguồn hợp lệ.");
        }

        foreach (var duplicate in result.Questions.GroupBy(x => x.OriginalNumber).Where(x => x.Count() > 1))
        {
            AddError(result, "Câu " + duplicate.Key, "DUPLICATE_QUESTION", "Số câu bị trùng.", "Mỗi số câu 101–200 chỉ xuất hiện một lần.");
        }

        foreach (var question in result.Questions)
        {
            var location = "Câu " + question.OriginalNumber;
            var rangeIsValid = question.Part switch
            {
                ToeicPart.Part5 => question.OriginalNumber >= ToeicConsts.Part5FirstQuestion && question.OriginalNumber <= ToeicConsts.Part5LastQuestion,
                ToeicPart.Part6 => question.OriginalNumber >= ToeicConsts.Part6FirstQuestion && question.OriginalNumber <= ToeicConsts.Part6LastQuestion,
                ToeicPart.Part7 => question.OriginalNumber >= ToeicConsts.Part7FirstQuestion && question.OriginalNumber <= ToeicConsts.Part7LastQuestion,
                _ => false
            };
            if (!rangeIsValid)
            {
                AddError(result, location, "QUESTION_WRONG_PART", "Số câu không thuộc khoảng của PART hiện tại.", "Part 5: 101–130; Part 6: 131–146; Part 7: 147–200.");
            }
            if (question.Part == ToeicPart.Part5 && question.GroupExternalId != null)
            {
                AddError(result, location, "PART5_HAS_GROUP", "Câu Part 5 không được phụ thuộc bài đọc chung.", "Đặt câu Part 5 trực tiếp trong PART, ngoài GROUP.");
            }
            if ((question.Part == ToeicPart.Part6 || question.Part == ToeicPart.Part7) && string.IsNullOrWhiteSpace(question.GroupExternalId))
            {
                AddError(result, location, "MISSING_GROUP", "Câu Part 6/7 phải thuộc GROUP.", "Đặt QUESTION bên trong GROUP tương ứng.");
            }
            if ((question.Part == ToeicPart.Part5 || question.Part == ToeicPart.Part7) && string.IsNullOrWhiteSpace(question.Prompt))
            {
                AddError(result, location, "MISSING_PROMPT", "Câu hỏi thiếu nội dung PROMPT.", "Thêm [[PROMPT]] và nội dung câu hỏi.");
            }

            var keys = question.Options.Select(x => x.Key).OrderBy(x => x).ToArray();
            if (question.Options.Count != 4 || !keys.SequenceEqual(new[] { "A", "B", "C", "D" }))
            {
                AddError(result, location, "INVALID_OPTIONS", "Mỗi câu phải có đúng bốn lựa chọn A, B, C, D.", "Thêm đủ bốn OPTION với key duy nhất.");
            }
            if (question.Options.Any(x => string.IsNullOrWhiteSpace(x.Text)))
            {
                AddError(result, location, "EMPTY_OPTION", "Lựa chọn không được để trống.", "Điền nội dung cho mọi OPTION.");
            }
            if (question.Prompt?.Length > 8000 || question.Hint?.Length > 4000 || question.Options.Any(x => x.Text?.Length > 4000))
                AddError(result, location, "QUESTION_TEXT_TOO_LONG", "Trường câu hỏi vượt độ dài cho phép.", "PROMPT tối đa 8000, HINT và OPTION tối đa 4000 ký tự.");
            if (result.IsOfficial && string.IsNullOrWhiteSpace(question.Hint))
                AddError(result, location, "MISSING_HINT", "Đề chính thức phải có gợi ý không rỗng cho từng câu.", "Bổ sung HINT đã được xác minh.");
            if (string.IsNullOrWhiteSpace(question.CorrectOptionKey) || question.Options.All(x => !string.Equals(x.Key, question.CorrectOptionKey, StringComparison.OrdinalIgnoreCase)))
            {
                AddError(result, location, "INVALID_ANSWER", "ANSWER phải trỏ tới một lựa chọn A–D hiện có.", "Sửa [[ANSWER key=\"A\"]] theo đáp án đã xác minh.");
            }
        }

        foreach (var group in result.Groups.Where(x => x.Part == ToeicPart.Part6))
        {
            var allText = string.Join("\n", group.ContentBlocks.Where(x => x.Type != ContentBlockType.Image).Select(x => x.TextContent));
            var blanks = BlankRegex.Matches(allText).Cast<Match>().Select(x => int.Parse(x.Groups["number"].Value, CultureInfo.InvariantCulture)).ToList();
            var questionNumbers = result.Questions.Where(x => x.GroupExternalId == group.ExternalId).Select(x => x.OriginalNumber).ToList();
            foreach (var number in questionNumbers.Where(x => blanks.Count(y => y == x) != 1))
            {
                AddError(result, "GROUP " + group.ExternalId, "INVALID_BLANK_REFERENCE", $"Câu {number} phải có đúng một tham chiếu {{{{BLANK:{number}}}}}.", "Thêm hoặc sửa tham chiếu chỗ trống trong DOCUMENT.");
            }
            foreach (var number in blanks.Where(x => !questionNumbers.Contains(x)).Distinct())
            {
                AddError(result, "GROUP " + group.ExternalId, "ORPHAN_BLANK", $"Chỗ trống {number} không có QUESTION tương ứng trong nhóm.", "Thêm câu hỏi hoặc sửa số trong BLANK.");
            }
        }

        if (result.IsOfficial)
        {
            if (result.Groups.Count(x => x.Part == ToeicPart.Part6) != 4 || result.Groups.Count(x => x.Part == ToeicPart.Part7) != 15)
                AddError(result, "EXAM", "OFFICIAL_GROUP_COUNT", "Đề chính thức cần bốn nhóm Part 6 và 15 nhóm Part 7.", "Giữ đúng cấu trúc nhóm của đề.");
            foreach (var group in result.Groups.Where(x => x.Part == ToeicPart.Part6 && result.Questions.Count(q => q.GroupExternalId == x.ExternalId) != 4))
                AddError(result, "GROUP " + group.ExternalId, "PART6_GROUP_SIZE", "Nhóm Part 6 phải có bốn câu.", "Chia 16 câu 131–146 thành bốn nhóm, mỗi nhóm bốn câu.");
            var actual = result.Questions.Select(x => x.OriginalNumber).OrderBy(x => x).ToArray();
            var expected = Enumerable.Range(ToeicConsts.Part5FirstQuestion, ToeicConsts.OfficialQuestionCount).ToArray();
            if (!actual.SequenceEqual(expected))
            {
                var missing = expected.Except(actual).ToArray();
                var extra = actual.Except(expected).ToArray();
                AddError(result, "EXAM", "OFFICIAL_QUESTION_SET", $"Đề chính thức phải đủ 100 câu 101–200. Thiếu: {string.Join(", ", missing)}; ngoài phạm vi: {string.Join(", ", extra)}.", "Bổ sung đúng dải Part 5/6/7 trước khi import.");
            }
            if (result.DurationMinutes != ToeicConsts.OfficialDurationMinutes)
            {
                AddError(result, "EXAM", "OFFICIAL_DURATION", "Đề chính thức phải có thời lượng 75 phút.", "Đặt duration=\"75\".");
            }
        }
    }

    private static bool TryReadTag(string text, out ParsedTag tag)
    {
        var match = TagRegex.Match(text ?? string.Empty);
        if (!match.Success)
        {
            tag = null;
            return false;
        }

        var attrsText = match.Groups["attrs"].Value;
        var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match attributeMatch in AttributeRegex.Matches(attrsText))
        {
            attrs[attributeMatch.Groups["name"].Value] = Unescape(attributeMatch.Groups["value"].Value);
        }
        tag = new ParsedTag
        {
            Name = match.Groups["name"].Value.ToUpperInvariant(),
            IsClosing = match.Groups["close"].Success,
            Attributes = attrs,
            Tail = match.Groups["tail"].Value ?? string.Empty
        };
        return true;
    }

    private static Dictionary<string, string> ReadRelationships(ZipArchive archive)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var entry = archive.GetEntry("word/_rels/document.xml.rels");
        if (entry == null) return result;
        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        foreach (var relationship in document.Descendants(Rel + "Relationship"))
        {
            var id = (string)relationship.Attribute("Id");
            var target = (string)relationship.Attribute("Target");
            var mode = (string)relationship.Attribute("TargetMode");
            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(target) && !string.Equals(mode, "External", StringComparison.OrdinalIgnoreCase))
            {
                result[id] = target;
            }
        }
        return result;
    }

    private static Dictionary<string, string> ReadContentTypes(ZipArchive archive)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var entry = archive.GetEntry("[Content_Types].xml");
        if (entry == null) return result;
        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        foreach (var item in document.Descendants(Ct + "Default"))
        {
            var extension = (string)item.Attribute("Extension");
            var contentType = (string)item.Attribute("ContentType");
            if (!string.IsNullOrWhiteSpace(extension) && !string.IsNullOrWhiteSpace(contentType)) result[extension] = contentType;
        }
        foreach (var item in document.Descendants(Ct + "Override"))
        {
            var partName = (string)item.Attribute("PartName");
            var contentType = (string)item.Attribute("ContentType");
            if (!string.IsNullOrWhiteSpace(partName) && !string.IsNullOrWhiteSpace(contentType)) result[partName.TrimStart('/')] = contentType;
        }
        return result;
    }

    private static string ResolvePartPath(string sourcePart, string target)
    {
        var segments = sourcePart.Split('/').ToList();
        segments.RemoveAt(segments.Count - 1);
        foreach (var segment in target.Replace('\\', '/').Split('/'))
        {
            if (segment == "..")
            {
                if (segments.Count > 0) segments.RemoveAt(segments.Count - 1);
            }
            else if (segment != "." && segment.Length > 0)
            {
                segments.Add(segment);
            }
        }
        return string.Join("/", segments);
    }

    private static string GetMediaType(string path, Dictionary<string, string> contentTypes)
    {
        if (contentTypes.TryGetValue(path, out var overrideValue)) return overrideValue;
        var extension = Path.GetExtension(path).TrimStart('.');
        if (contentTypes.TryGetValue(extension, out var value)) return value;
        return "application/octet-stream";
    }

    private static string Get(ParsedTag tag, string name) => tag.Attributes.TryGetValue(name, out var value) ? value : null;

    private static int GetInt(ParsedTag tag, string name, int defaultValue, ParsedExamDocument result, string location)
    {
        var raw = Get(tag, name);
        if (string.IsNullOrWhiteSpace(raw)) return defaultValue;
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) return value;
        AddError(result, location, "INVALID_INTEGER", $"Thuộc tính {name} phải là số nguyên.", $"Sửa {name}=\"{defaultValue}\" hoặc giá trị số hợp lệ.");
        return defaultValue;
    }

    private static bool GetBool(ParsedTag tag, string name, bool defaultValue, ParsedExamDocument result, string location)
    {
        var raw = Get(tag, name);
        if (string.IsNullOrWhiteSpace(raw)) return defaultValue;
        if (bool.TryParse(raw, out var value)) return value;
        AddError(result, location, "INVALID_BOOLEAN", $"Thuộc tính {name} chỉ nhận true hoặc false.", $"Sửa {name}=\"{defaultValue.ToString().ToLowerInvariant()}\".");
        return defaultValue;
    }

    private static string AppendLine(string current, string value) => string.IsNullOrWhiteSpace(current) ? value : current + "\n" + value;

    private static string Unescape(string value) => (value ?? string.Empty)
        .Replace("\\[[", "[[")
        .Replace("\\]]", "]]" )
        .Replace("\\\"", "\"")
        .Replace("\\\\", "\\");

    private static void AddError(ParsedExamDocument result, string location, string code, string message, string fix)
    {
        result.Errors.Add(new ImportValidationError { Location = location, Code = code, Message = message, Fix = fix });
    }

    private sealed class ParserState
    {
        public ParserState(ParsedExamDocument result, ZipArchive archive, Dictionary<string, string> relationships, Dictionary<string, string> contentTypes)
        {
            Result = result;
            Archive = archive;
            Relationships = relationships;
            ContentTypes = contentTypes;
        }

        public ParsedExamDocument Result { get; }
        public ZipArchive Archive { get; }
        public Dictionary<string, string> Relationships { get; }
        public Dictionary<string, string> ContentTypes { get; }
        public ToeicPart? CurrentPart { get; set; }
        public ParsedPassageGroup CurrentGroup { get; set; }
        public string CurrentDocumentKey { get; set; }
        public ParsedQuestion CurrentQuestion { get; set; }
        public ParsedQuestionOption CurrentOption { get; set; }
        public string CurrentQuestionField { get; set; }
    }

    private sealed class ParsedTag
    {
        public string Name { get; set; }
        public bool IsClosing { get; set; }
        public Dictionary<string, string> Attributes { get; set; }
        public string Tail { get; set; }
    }
}
