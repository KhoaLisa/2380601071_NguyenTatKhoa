using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NguyenTatKhoa_2380601071.Toeic.Importing;

/// <summary>Implements the tag specification embedded in TOEIC_Reading_v07_co_tag.docx.</summary>
internal sealed class V07DocxExamParser
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace Wp = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
    private static readonly XNamespace Rel = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace Ct = "http://schemas.openxmlformats.org/package/2006/content-types";
    private static readonly Regex StructuralTag = new Regex(@"^\[(?<close>/)?(?<name>[A-Z][A-Z0-9_]*)(?<attrs>[^\]]*)\]$", RegexOptions.Compiled);
    private static readonly Regex Attribute = new Regex(@"(?:^|\s+)(?<name>[a-z][a-z0-9_]*)=""(?<value>[^""]*)""", RegexOptions.Compiled);
    private static readonly Regex FieldTag = new Regex(@"^\[(?<name>TITLE|DIRECTIONS|BLANKS|STEM|A|B|C|D|CORRECT|HINT)\](?<value>.*)\[/\k<name>\]$", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex ReservedTag = new Regex(@"\[/?(?:EXAM|TITLE|DIRECTIONS|PART|GROUP|PASSAGE|BLANKS|IMAGE|QUESTION|STEM|A|B|C|D|CORRECT|HINT)(?:\s|\])", RegexOptions.Compiled);
    private static readonly string[] QuestionFields = { "STEM", "A", "B", "C", "D", "CORRECT", "HINT" };

    private readonly ZipArchive _archive;
    private readonly ParsedExamDocument _result = new ParsedExamDocument { TagDialect = "v07", IsOfficial = true };
    private readonly Stack<string> _scopes = new Stack<string>();
    private readonly Dictionary<string, XElement> _relationships;
    private readonly Dictionary<string, string> _contentTypes;
    private readonly HashSet<string> _questionIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> _groupIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> _passageIds = new HashSet<string>(StringComparer.Ordinal);
    private readonly HashSet<string> _imageIds = new HashSet<string>(StringComparer.Ordinal);
    private ParsedExamPart _part;
    private ParsedPassageGroup _group;
    private ParsedPassage _passage;
    private ParsedQuestion _question;
    private ParsedContentBlock _image;
    private int _imageCount;
    private int _questionField;
    private int _examField;
    private bool _partDirections;
    private bool _groupQuestionsStarted;
    private bool _blanksSeen;
    private bool _examSeen;
    private bool _examClosed;

    private V07DocxExamParser(ZipArchive archive)
    {
        _archive = archive;
        _relationships = ReadXml("word/_rels/document.xml.rels")?.Root?.Elements(Rel + "Relationship")
            .Where(x => x.Attribute("Id") != null).GroupBy(x => (string)x.Attribute("Id"))
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal) ?? new Dictionary<string, XElement>();
        var contentTypes = ReadXml("[Content_Types].xml");
        _contentTypes = contentTypes?.Root?.Elements(Ct + "Default")
            .Where(x => x.Attribute("Extension") != null).GroupBy(x => (string)x.Attribute("Extension"))
            .ToDictionary(x => x.Key, x => (string)x.First().Attribute("ContentType"), StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, string>();
        foreach (var item in contentTypes?.Root?.Elements(Ct + "Override") ?? Enumerable.Empty<XElement>())
        {
            var partName = (string)item.Attribute("PartName");
            if (partName != null) _contentTypes[partName.TrimStart('/')] = (string)item.Attribute("ContentType");
        }
    }

    public static bool IsV07(XElement body) => body.Elements(W + "p")
        .Any(p => Text(p).TrimStart().StartsWith("[EXAM", StringComparison.Ordinal) && !Text(p).TrimStart().StartsWith("[[", StringComparison.Ordinal));

    public static ParsedExamDocument Parse(ZipArchive archive, XElement body)
    {
        var parser = new V07DocxExamParser(archive);
        var index = 0;
        foreach (var element in body.Elements())
        {
            index++;
            var text = element.Name == W + "p" ? Text(element).Trim() : "";
            // The Word specification explicitly excludes the entire preamble and header/footer.
            if (!parser._examSeen && !text.StartsWith("[EXAM", StringComparison.Ordinal)) continue;
            if (parser._examClosed)
            {
                if (text.StartsWith("[EXAM", StringComparison.Ordinal)) parser.Error("Đoạn " + index, "MULTIPLE_EXAMS", "Một tệp chỉ được chứa một EXAM.", "Tách mỗi đề ra một tệp riêng.");
                continue;
            }
            parser.ReadElement(element, text, "Đoạn " + index);
        }
        if (!parser._examSeen) parser.Error("EXAM", "MISSING_EXAM", "Không tìm thấy tag mở EXAM.", "Giữ nguyên tag [EXAM ...] của đề.");
        if (!parser._examClosed || parser._scopes.Count > 0)
            parser.Error("Cuối tệp", "UNCLOSED_TAG", "Còn tag chưa đóng: " + string.Join(", ", parser._scopes.Reverse()), "Đóng mọi tag theo đúng thứ tự lồng.");
        parser.Validate();
        return parser._result;
    }

    private void ReadElement(XElement element, string text, string location)
    {
        if (element.Name != W + "p")
        {
            if (element.Name != W + "sectPr") Error(location, "UNSUPPORTED_BODY_ELEMENT", "Nội dung trong EXAM phải là các đoạn tag hoặc ảnh inline.", "Giữ bảng/bài đọc trong ảnh được nhúng bên trong IMAGE theo đặc tả Word.");
            return;
        }
        var embedded = element.Descendants(A + "blip").ToArray();
        if (element.Descendants(Wp + "anchor").Any() || element.Descendants(W + "txbxContent").Any() || element.Descendants(W + "object").Any())
            Error(location, "UNSUPPORTED_IMAGE_LAYOUT", "Không chấp nhận ảnh nổi, text box hoặc OLE.", "Chèn ảnh bằng In Line with Text bên trong IMAGE.");
        if (embedded.Length > 0)
        {
            if (Scope != "IMAGE") Error(location, "IMAGE_OUTSIDE_SCOPE", "Ảnh nằm ngoài IMAGE.", "Đặt đúng một ảnh inline giữa [IMAGE ...] và [/IMAGE].");
            else
            {
                if (!string.IsNullOrWhiteSpace(text) || embedded.Length != 1)
                    Error(location, "INVALID_IMAGE_PARAGRAPH", "Đoạn IMAGE chỉ được chứa đúng một ảnh và không có text.", "Tách tag mở, ảnh inline và tag đóng thành ba đoạn riêng.");
                foreach (var blip in embedded) ReadImage(element, blip, location);
            }
            return;
        }
        if (text.Length == 0) return;
        var field = FieldTag.Match(text);
        if (field.Success)
        {
            var raw = field.Groups["value"].Value.Trim();
            if (ReservedTag.IsMatch(raw)) Error(location, "UNESCAPED_TAG_IN_TEXT", "Nội dung trường chứa tag dành riêng chưa mã hóa.", "Mã hóa [ thành &#91; và ] thành &#93; trong nội dung văn bản.");
            ReadField(field.Groups["name"].Value, WebUtility.HtmlDecode(raw), location);
            return;
        }
        var tag = StructuralTag.Match(text);
        if (!tag.Success)
        {
            Error(location, "MALFORMED_TAG", "Đoạn trong EXAM không khớp tag v07 hợp lệ.", "Dùng đúng chữ hoa, dấu nháy kép và tag đóng cùng tên; mỗi tag cấu trúc ở một đoạn riêng.");
            return;
        }
        var name = tag.Groups["name"].Value;
        var rawAttributes = tag.Groups["attrs"].Value;
        if (tag.Groups["close"].Success)
        {
            if (!string.IsNullOrWhiteSpace(rawAttributes)) Error(location, "ATTRIBUTES_ON_CLOSE", "Tag đóng không có thuộc tính.", "Xóa thuộc tính khỏi tag đóng.");
            Close(name, location);
            return;
        }
        var attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match attribute in Attribute.Matches(rawAttributes))
        {
            var key = attribute.Groups["name"].Value;
            if (!attributes.TryAdd(key, WebUtility.HtmlDecode(attribute.Groups["value"].Value)))
                Error(location, "DUPLICATE_ATTRIBUTE", "Thuộc tính " + key + " bị lặp.", "Giữ đúng một giá trị cho mỗi thuộc tính.");
        }
        if (!string.IsNullOrWhiteSpace(Attribute.Replace(rawAttributes, "")))
            Error(location, "INVALID_ATTRIBUTES", "Thuộc tính phải dùng tên chữ thường và giá trị trong dấu nháy kép.", "Giữ đúng cú pháp thuộc tính của Word nguồn.");
        Open(name, attributes, location);
    }

    private void Open(string name, Dictionary<string, string> attrs, string location)
    {
        switch (name)
        {
            case "EXAM":
                if (_examSeen || Scope != null) { Error(location, "NESTED_EXAM", "EXAM bị lồng hoặc trùng.", "Chỉ giữ một EXAM gốc."); return; }
                _examSeen = true;
                Attributes(attrs, location, "version", "id", "minutes", "question_count");
                _result.SpecificationVersion = Value(attrs, "version");
                _result.SourceExamId = Id(attrs, "id", null, location);
                _result.Code = _result.SourceExamId;
                _result.DurationMinutes = Integer(attrs, "minutes", location);
                _result.DeclaredQuestionCount = Integer(attrs, "question_count", location);
                _scopes.Push(name);
                break;
            case "PART":
                Attributes(attrs, location, "number", "shuffle_groups");
                if (!Parent("EXAM", location, name)) return;
                if (_examField != 2) Error(location, "MISSING_EXAM_FIELDS", "EXAM cần TITLE rồi DIRECTIONS trước PART.", "Bổ sung đủ hai trường theo thứ tự.");
                var number = Integer(attrs, "number", location);
                if (number != 5 + _result.Parts.Count || number > 7) Error(location, "INVALID_PART_ORDER", "PART phải xuất hiện đúng thứ tự 5, 6, 7, mỗi phần một lần.", "Giữ đúng ba PART trong Word nguồn.");
                _part = new ParsedExamPart { Number = (ToeicPart)number, DisplayOrder = _result.Parts.Count + 1, AllowShuffleGroups = Boolean(attrs, "shuffle_groups", location) };
                if (_part.AllowShuffleGroups) Error(location, "GROUP_SHUFFLE_NOT_ALLOWED", "v07 giữ nguyên thứ tự các nhóm.", "Đặt shuffle_groups=\"false\".");
                _result.Parts.Add(_part);
                _partDirections = false;
                _scopes.Push(name);
                break;
            case "GROUP":
                Attributes(attrs, location, "id", "first", "last", "shuffle");
                if (!Parent("PART", location, name)) return;
                if (!_partDirections) Error(location, "MISSING_PART_DIRECTIONS", "Mỗi PART phải bắt đầu bằng DIRECTIONS.", "Thêm DIRECTIONS trước GROUP.");
                if (_part.Number == ToeicPart.Part5) Error(location, "PART5_HAS_GROUP", "Part 5 chỉ chứa câu hỏi độc lập.", "Đưa QUESTION trực tiếp vào PART 5.");
                _group = new ParsedPassageGroup
                {
                    ExternalId = Id(attrs, "id", _groupIds, location), Part = _part.Number,
                    FirstQuestionNumber = Integer(attrs, "first", location), LastQuestionNumber = Integer(attrs, "last", location),
                    DisplayOrder = _result.Groups.Count + 1, AllowQuestionShuffle = Boolean(attrs, "shuffle", location)
                };
                if (_group.AllowQuestionShuffle) Error(location, "GROUP_SHUFFLE_NOT_ALLOWED", "v07 giữ nguyên thứ tự nhóm và câu con.", "Đặt shuffle=\"false\" trên GROUP.");
                _result.Groups.Add(_group);
                _groupQuestionsStarted = false;
                _scopes.Push(name);
                break;
            case "PASSAGE":
                Attributes(attrs, location, "id", "kind");
                if (!Parent("GROUP", location, name)) return;
                if (_groupQuestionsStarted) Error(location, "PASSAGE_AFTER_QUESTION", "Tất cả PASSAGE phải đứng trước các QUESTION trong nhóm.", "Chuyển tài liệu lên trước danh sách câu hỏi.");
                _passage = new ParsedPassage
                {
                    Id = Id(attrs, "id", _passageIds, location), Kind = Value(attrs, "kind"), DisplayOrder = _group.Passages.Count + 1
                };
                if (string.IsNullOrWhiteSpace(_passage.Kind) || _passage.Kind.Length > 64) Error(location, "INVALID_PASSAGE_KIND", "kind của PASSAGE bị thiếu hoặc quá dài.", "Giữ loại tài liệu hợp lệ từ Word nguồn.");
                _group.Passages.Add(_passage);
                _group.DocumentKeys.Add(_passage.Id);
                _blanksSeen = false;
                _scopes.Push(name);
                break;
            case "IMAGE":
                Attributes(attrs, location, "id", "source_page");
                if (!Parent("PASSAGE", location, name)) return;
                if (_part.Number == ToeicPart.Part6 && !_blanksSeen) Error(location, "MISSING_BLANKS", "PASSAGE Part 6 cần BLANKS trước IMAGE.", "Giữ danh sách số câu trong BLANKS của Word nguồn.");
                _image = new ParsedContentBlock
                {
                    SourceBlockId = Id(attrs, "id", _imageIds, location), SourcePage = Integer(attrs, "source_page", location),
                    DocumentKey = _passage.Id, DisplayOrder = _group.ContentBlocks.Count + 1, Type = ContentBlockType.Image
                };
                if (_image.SourcePage <= 0) Error(location, "INVALID_SOURCE_PAGE", "source_page phải là số nguyên dương.", "Điền số trang nguồn đúng theo PDF.");
                _imageCount = 0;
                _scopes.Push(name);
                break;
            case "QUESTION":
                Attributes(attrs, location, "id", "number", "shuffle", "shuffle_options");
                if (Scope != "PART" && Scope != "GROUP") { Error(location, "QUESTION_WRONG_PARENT", "QUESTION phải ở trong PART 5 hoặc GROUP Part 6/7.", "Đóng PASSAGE trước danh sách QUESTION."); return; }
                if (!_partDirections) Error(location, "MISSING_PART_DIRECTIONS", "Mỗi PART phải bắt đầu bằng DIRECTIONS.", "Thêm DIRECTIONS trước câu hỏi.");
                if ((_part.Number == ToeicPart.Part5) != (Scope == "PART")) Error(location, "QUESTION_WRONG_GROUP", "Câu Part 5 độc lập; câu Part 6/7 cần nhóm.", "Giữ đúng quan hệ nhóm/câu trong Word nguồn.");
                _question = new ParsedQuestion
                {
                    SourceQuestionId = Id(attrs, "id", _questionIds, location), OriginalNumber = Integer(attrs, "number", location),
                    Part = _part.Number, GroupExternalId = Scope == "GROUP" ? _group.ExternalId : null,
                    DisplayOrder = _result.Questions.Count + 1, AllowQuestionShuffle = Boolean(attrs, "shuffle", location),
                    AllowOptionShuffle = Boolean(attrs, "shuffle_options", location)
                };
                if (_part.Number != ToeicPart.Part5 && _question.AllowQuestionShuffle) Error(location, "QUESTION_SHUFFLE_NOT_ALLOWED", "Chỉ Part 5 được đảo thứ tự câu.", "Đặt shuffle=\"false\" cho câu Part 6 và Part 7.");
                if ((_question.OriginalNumber == 163 || _question.OriginalNumber == 167) && _question.AllowOptionShuffle)
                    Error(location, "POSITION_OPTION_SHUFFLE", "Câu 163 và 167 gắn với vị trí chèn câu nên không đảo lựa chọn.", "Đặt shuffle_options=\"false\" cho hai câu này.");
                _result.Questions.Add(_question);
                _groupQuestionsStarted = true;
                _questionField = 0;
                _scopes.Push(name);
                break;
            default:
                Error(location, "UNKNOWN_TAG", "Tag " + name + " không được hỗ trợ trong đặc tả v07.", "Dùng đúng bộ tag được định nghĩa trong Word nguồn.");
                break;
        }
    }

    private void ReadField(string name, string value, string location)
    {
        if (name == "TITLE" || (name == "DIRECTIONS" && Scope == "EXAM"))
        {
            if (!Parent("EXAM", location, name)) return;
            var expected = _examField == 0 ? "TITLE" : _examField == 1 ? "DIRECTIONS" : null;
            if (name != expected || _result.Parts.Count > 0) Error(location, "INVALID_EXAM_FIELD_ORDER", "EXAM cần TITLE, DIRECTIONS rồi ba PART, mỗi trường một lần.", "Sửa thứ tự trường theo đặc tả.");
            if (name == "TITLE") _result.Title = value; else _result.Directions = value;
            _examField++;
            if (string.IsNullOrWhiteSpace(value)) Error(location, "EMPTY_FIELD", name + " không được để trống.", "Điền nội dung của trường.");
            return;
        }
        if (name == "DIRECTIONS")
        {
            if (!Parent("PART", location, name)) return;
            if (_partDirections || _result.Questions.Any(x => x.Part == _part.Number) || _result.Groups.Any(x => x.Part == _part.Number))
                Error(location, "INVALID_PART_DIRECTIONS", "DIRECTIONS phải xuất hiện một lần ở đầu PART.", "Chuyển DIRECTIONS lên đầu phần.");
            _part.Directions = value;
            _partDirections = true;
            if (string.IsNullOrWhiteSpace(value)) Error(location, "EMPTY_FIELD", "DIRECTIONS không được để trống.", "Điền hướng dẫn cho phần.");
            return;
        }
        if (name == "BLANKS")
        {
            if (!Parent("PASSAGE", location, name)) return;
            if (_part.Number != ToeicPart.Part6 || _blanksSeen || _group.ContentBlocks.Any(x => x.DocumentKey == _passage.Id))
                Error(location, "INVALID_BLANKS_SCOPE", "BLANKS chỉ xuất hiện một lần trước IMAGE của PASSAGE Part 6.", "Đặt danh sách số câu đúng vị trí theo Word nguồn.");
            _blanksSeen = true;
            foreach (var item in value.Split(','))
            {
                if (int.TryParse(item.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var number)) _passage.BlankQuestionNumbers.Add(number);
                else Error(location, "INVALID_BLANK_NUMBER", "BLANKS phải là danh sách số câu phân cách bằng dấu phẩy.", "Ví dụ: [BLANKS] 131,132,133,134 [/BLANKS].");
            }
            return;
        }
        if (!Parent("QUESTION", location, name)) return;
        if (_questionField >= QuestionFields.Length || QuestionFields[_questionField] != name)
            Error("Câu " + _question.OriginalNumber, "INVALID_QUESTION_FIELD_ORDER", "QUESTION cần đúng thứ tự STEM, A, B, C, D, CORRECT, HINT, mỗi trường một lần.", "Bổ sung trường còn thiếu hoặc xóa trường trùng.");
        _questionField++;
        switch (name)
        {
            case "STEM": _question.Prompt = value; break;
            case "CORRECT": _question.CorrectOptionKey = value; break;
            case "HINT": _question.Hint = value; break;
            case "A": case "B": case "C": case "D":
                _question.Options.Add(new ParsedQuestionOption { Key = name, DisplayOrder = name[0] - 'A' + 1, Text = value });
                break;
            default: Error(location, "FIELD_WRONG_PARENT", name + " không phải trường của QUESTION.", "Đặt trường vào đúng phạm vi."); break;
        }
    }

    private void Close(string name, string location)
    {
        if (Scope != name)
        {
            Error(location, "MISMATCHED_CLOSE", "Tag đóng " + name + " không khớp phạm vi " + (Scope ?? "gốc") + ".", "Đóng tag con trước tag cha, đúng tên chữ hoa.");
            return;
        }
        switch (name)
        {
            case "IMAGE":
                if (_imageCount != 1 || _image.BinaryData == null) Error(location, "IMAGE_COUNT", "Mỗi IMAGE phải có đúng một ảnh inline đọc được.", "Chèn lại ảnh trực tiếp trong Word, không liên kết ngoài.");
                _group.ContentBlocks.Add(_image);
                _image = null;
                break;
            case "PASSAGE":
                if (!_group.ContentBlocks.Any(x => x.DocumentKey == _passage.Id)) Error(location, "EMPTY_PASSAGE", "PASSAGE chưa có IMAGE.", "Bổ sung ảnh bài đọc trong PASSAGE.");
                _passage = null;
                break;
            case "QUESTION":
                if (_questionField != QuestionFields.Length) Error("Câu " + _question.OriginalNumber, "MISSING_QUESTION_FIELDS", "QUESTION thiếu một hoặc nhiều trường bắt buộc.", "Giữ đủ STEM, A, B, C, D, CORRECT, HINT.");
                _question = null;
                break;
            case "GROUP": _group = null; break;
            case "PART":
                if (!_partDirections) Error(location, "MISSING_PART_DIRECTIONS", "PART thiếu DIRECTIONS.", "Thêm hướng dẫn ở đầu PART.");
                _part = null;
                break;
            case "EXAM": _examClosed = true; break;
        }
        _scopes.Pop();
    }

    private void ReadImage(XElement paragraph, XElement blip, string location)
    {
        _imageCount++;
        if (!paragraph.Descendants(Wp + "inline").Any() || blip.Attribute(R + "link") != null)
        {
            Error(location, "EXTERNAL_OR_FLOATING_IMAGE", "Ảnh phải là inline và được nhúng qua r:embed.", "Chèn lại ảnh bằng Insert Pictures, In Line with Text.");
            return;
        }
        var id = (string)blip.Attribute(R + "embed");
        if (id == null || !_relationships.TryGetValue(id, out var relationship) ||
            (string)relationship.Attribute("TargetMode") == "External" ||
            !((string)relationship.Attribute("Type") ?? "").EndsWith("/image", StringComparison.Ordinal))
        {
            Error(location, "BROKEN_IMAGE_RELATIONSHIP", "Quan hệ ảnh nhúng không hợp lệ: " + id, "Không dùng ảnh liên kết ngoài; chèn lại ảnh trong Word.");
            return;
        }
        var target = ((string)relationship.Attribute("Target") ?? "").Replace('\\', '/');
        var uri = new Uri(new Uri("https://docx.invalid/word/document.xml"), target);
        var path = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        var entry = uri.Host == "docx.invalid" ? _archive.GetEntry(path) : null;
        if (entry == null || entry.Length == 0 || entry.Length > ExamImageValidator.MaxBytes)
        {
            Error(location, "MISSING_EMBEDDED_IMAGE", "Không đọc được bytes ảnh nhúng hoặc ảnh quá lớn.", "Nhúng lại ảnh hợp lệ dưới 10 MB.");
            return;
        }
        byte[] bytes;
        using (var input = entry.Open())
        using (var output = new MemoryStream()) { input.CopyTo(output); bytes = output.ToArray(); }
        var extension = Path.GetExtension(path).TrimStart('.');
        if ((!_contentTypes.TryGetValue(path, out var declared) && !_contentTypes.TryGetValue(extension, out declared)) ||
            string.IsNullOrWhiteSpace(declared) ||
            !ExamImageValidator.TryGetMediaType(bytes, path, declared, out var mediaType))
        {
            Error(location, "INVALID_IMAGE_BYTES", "Ảnh không có chữ ký PNG/JPEG/GIF/BMP/WebP hợp lệ hoặc phần mở rộng/MIME không khớp.", "Chèn ảnh PNG/JPEG hợp lệ trong Word.");
            return;
        }
        _image.BinaryData = bytes;
        _image.MediaType = mediaType;
        _image.FileName = Path.GetFileName(path);
    }

    private void Validate()
    {
        if (_result.SpecificationVersion != "1.0") Error("EXAM", "UNSUPPORTED_VERSION", "Chỉ hỗ trợ bộ tag version=\"1.0\" trong Word v07 đã kiểm chứng.", "Dùng đúng tệp Word nguồn.");
        if (string.IsNullOrWhiteSpace(_result.Title) || _result.Title.Length > 256) Error("TITLE", "INVALID_TITLE", "TITLE bị thiếu hoặc quá 256 ký tự.", "Điền tên đề hợp lệ.");
        if (_result.DurationMinutes != 75 || _result.DeclaredQuestionCount != 100) Error("EXAM", "OFFICIAL_METADATA", "Đề v07 cần minutes=\"75\" và question_count=\"100\".", "Giữ đúng thông tin đề 100 câu/75 phút.");
        if (!_result.Parts.Select(x => (int)x.Number).SequenceEqual(new[] { 5, 6, 7 })) Error("EXAM", "OFFICIAL_PART_SET", "Cần đúng ba PART 5, 6, 7 theo thứ tự.", "Bổ sung phần còn thiếu.");
        if (!_result.Questions.Select(x => x.OriginalNumber).OrderBy(x => x).SequenceEqual(Enumerable.Range(101, 100))) Error("EXAM", "OFFICIAL_QUESTION_SET", "Cần đủ 100 câu liên tục 101–200, không trùng.", "Đối chiếu số câu với Word nguồn.");
        if (_result.Groups.Count(x => x.Part == ToeicPart.Part6) != 4 || _result.Groups.Count(x => x.Part == ToeicPart.Part7) != 15) Error("EXAM", "OFFICIAL_GROUP_COUNT", "Part 6 cần bốn nhóm; Part 7 cần 15 nhóm.", "Giữ đúng cấu trúc 19 nhóm của đề.");
        foreach (var question in _result.Questions)
        {
            var location = "Câu " + question.OriginalNumber;
            var correctPart = question.OriginalNumber <= 130 ? ToeicPart.Part5 : question.OriginalNumber <= 146 ? ToeicPart.Part6 : ToeicPart.Part7;
            if (question.Part != correctPart || question.OriginalNumber < 101 || question.OriginalNumber > 200) Error(location, "QUESTION_WRONG_PART", "Số câu không thuộc PART hiện tại.", "Part 5: 101–130; Part 6: 131–146; Part 7: 147–200.");
            if ((question.Part != ToeicPart.Part6 && string.IsNullOrWhiteSpace(question.Prompt)) || question.Prompt?.Length > 8000) Error(location, "INVALID_STEM", "STEM bị thiếu hoặc quá dài; chỉ Part 6 được để rỗng.", "Điền đúng nội dung câu hỏi, tối đa 8000 ký tự.");
            if (string.IsNullOrWhiteSpace(question.Hint) || question.Hint.Length > 4000) Error(location, "INVALID_HINT", "HINT không được rỗng hoặc quá 4000 ký tự.", "Bổ sung gợi ý đã biên soạn của câu.");
            if (!question.Options.Select(x => x.Key).SequenceEqual(new[] { "A", "B", "C", "D" }) || question.Options.Any(x => string.IsNullOrWhiteSpace(x.Text) || x.Text.Length > 4000)) Error(location, "INVALID_OPTIONS", "Cần đủ bốn lựa chọn A–D không rỗng, mỗi lựa chọn tối đa 4000 ký tự.", "Sửa trường thiếu, trùng hoặc rỗng.");
            if (!new[] { "A", "B", "C", "D" }.Contains(question.CorrectOptionKey)) Error(location, "INVALID_CORRECT", "CORRECT phải là đúng một nhãn A, B, C hoặc D.", "Giữ đáp án đã biên soạn trong Word nguồn.");
        }
        foreach (var group in _result.Groups)
        {
            var location = "Nhóm " + group.ExternalId;
            var actual = _result.Questions.Where(x => x.GroupExternalId == group.ExternalId).Select(x => x.OriginalNumber).ToArray();
            if (group.FirstQuestionNumber < 131 || group.LastQuestionNumber > 200 || group.FirstQuestionNumber > group.LastQuestionNumber ||
                !actual.SequenceEqual(Enumerable.Range(group.FirstQuestionNumber, Math.Max(0, Math.Min(100, group.LastQuestionNumber - group.FirstQuestionNumber + 1)))))
                Error(location, "GROUP_QUESTION_RANGE", "Nhóm không chứa đúng các câu từ first đến last theo thứ tự.", "Giữ câu hỏi trong đúng nhóm của Word nguồn.");
            if (group.Passages.Count == 0) Error(location, "EMPTY_GROUP", "Nhóm chưa có PASSAGE.", "Bổ sung bài đọc trước danh sách câu hỏi.");
            if (group.Part == ToeicPart.Part6)
            {
                if (actual.Length != 4) Error(location, "PART6_GROUP_SIZE", "Nhóm Part 6 phải có bốn câu.", "Giữ mỗi nhóm đúng bốn câu từ 131–146.");
                var blanks = group.Passages.SelectMany(x => x.BlankQuestionNumbers).ToArray();
                if (!blanks.OrderBy(x => x).SequenceEqual(actual.OrderBy(x => x))) Error(location, "INVALID_BLANK_REFERENCE", "BLANKS không khớp chính xác danh sách câu trong nhóm.", "Không thiếu, lặp hoặc tham chiếu câu của nhóm khác.");
            }
        }
    }

    private string Scope => _scopes.Count == 0 ? null : _scopes.Peek();
    private static string Text(XElement paragraph) => string.Concat(paragraph.Descendants(W + "t").Select(x => (string)x));
    private XDocument ReadXml(string path)
    {
        var entry = _archive.GetEntry(path);
        if (entry == null) return null;
        using var stream = entry.Open();
        return XDocument.Load(stream);
    }
    private bool Parent(string expected, string location, string name)
    {
        if (Scope == expected) return true;
        Error(location, "INVALID_NESTING", name + " phải nằm trong " + expected + ", hiện tại là " + (Scope ?? "gốc") + ".", "Sửa cấu trúc lồng tag theo Word nguồn.");
        return false;
    }
    private void Attributes(Dictionary<string, string> attrs, string location, params string[] required)
    {
        foreach (var name in required.Where(x => !attrs.ContainsKey(x))) Error(location, "MISSING_ATTRIBUTE", "Thiếu thuộc tính bắt buộc " + name + ".", "Không tự dùng giá trị mặc định; bổ sung thuộc tính từ Word nguồn.");
        foreach (var name in attrs.Keys.Except(required)) Error(location, "UNKNOWN_ATTRIBUTE", "Thuộc tính " + name + " không được định nghĩa.", "Dùng đúng tên chữ thường trong đặc tả Word.");
    }
    private static string Value(Dictionary<string, string> attrs, string name) => attrs.TryGetValue(name, out var value) ? value : null;
    private string Id(Dictionary<string, string> attrs, string name, HashSet<string> ids, string location)
    {
        var value = Value(attrs, name);
        if (string.IsNullOrWhiteSpace(value) || value.Length > 64) Error(location, "INVALID_ID", "ID bị thiếu hoặc dài quá 64 ký tự.", "Điền định danh ổn định hợp lệ.");
        else if (ids != null && !ids.Add(value)) Error(location, "DUPLICATE_ID", "ID " + value + " bị trùng trong cùng loại.", "Giữ ID duy nhất của từng đối tượng.");
        return value ?? "";
    }
    private int Integer(Dictionary<string, string> attrs, string name, string location)
    {
        if (int.TryParse(Value(attrs, name), NumberStyles.None, CultureInfo.InvariantCulture, out var number)) return number;
        Error(location, "INVALID_INTEGER", name + " phải là số nguyên không âm.", "Điền giá trị số hợp lệ từ Word nguồn.");
        return 0;
    }
    private bool Boolean(Dictionary<string, string> attrs, string name, string location)
    {
        var value = Value(attrs, name);
        if (value == "true") return true;
        if (value != "false") Error(location, "INVALID_BOOLEAN", name + " chỉ nhận true hoặc false phân biệt chữ hoa/thường.", "Bổ sung cờ bắt buộc đúng cú pháp.");
        return false;
    }
    private void Error(string location, string code, string message, string fix) => _result.Errors.Add(new ImportValidationError { Location = location, Code = code, Message = message, Fix = fix });
}
