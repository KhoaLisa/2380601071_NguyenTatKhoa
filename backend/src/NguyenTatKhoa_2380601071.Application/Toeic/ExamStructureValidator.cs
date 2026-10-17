using NguyenTatKhoa_2380601071.Toeic.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace NguyenTatKhoa_2380601071.Toeic;

public static class ExamStructureValidator
{
    public static List<ImportValidationErrorDto> Validate(Exam exam, bool requireOfficial)
    {
        var errors = new List<ImportValidationErrorDto>();
        void Error(string location, string code, string message, string fix) => errors.Add(new ImportValidationErrorDto
            { Location = location, Code = code, Message = message, Fix = fix });

        if (string.IsNullOrWhiteSpace(exam.Title) || exam.Title.Length > ToeicConsts.MaxTitleLength)
            Error("EXAM", "INVALID_TITLE", "Tên đề bị thiếu hoặc vượt 256 ký tự.", "Điền tên hợp lệ.");
        if (exam.DurationMinutes < 1 || exam.DurationMinutes > 600)
            Error("EXAM", "INVALID_DURATION", "Thời lượng phải từ 1 đến 600 phút.", "Đặt 75 phút cho đề chính thức.");
        if (exam.Questions.Count == 0) Error("EXAM", "EMPTY_EXAM", "Đề chưa có câu hỏi.", "Nhập các câu hỏi trước khi xuất bản.");
        if (exam.Questions.GroupBy(x => x.OriginalNumber).Any(x => x.Count() > 1) ||
            exam.Questions.Any(x => x.DisplayOrder <= 0) || exam.Questions.GroupBy(x => x.DisplayOrder).Any(x => x.Count() > 1))
            Error("EXAM", "INVALID_QUESTION_ORDER", "Số câu hoặc thứ tự câu bị trùng/không hợp lệ.", "Mỗi câu phải có số và thứ tự duy nhất.");

        foreach (var question in exam.Questions)
        {
            var location = "Câu " + question.OriginalNumber;
            var validRange = question.Part switch
            {
                ToeicPart.Part5 => question.OriginalNumber >= 101 && question.OriginalNumber <= 130,
                ToeicPart.Part6 => question.OriginalNumber >= 131 && question.OriginalNumber <= 146,
                ToeicPart.Part7 => question.OriginalNumber >= 147 && question.OriginalNumber <= 200,
                _ => false
            };
            if (!validRange) Error(location, "QUESTION_WRONG_PART", "Số câu không nằm trong phần tương ứng.", "Part 5: 101–130; Part 6: 131–146; Part 7: 147–200.");
            if ((question.Part == ToeicPart.Part5 && question.PassageGroupId.HasValue) ||
                (question.Part != ToeicPart.Part5 && !exam.PassageGroups.Any(g => g.Id == question.PassageGroupId && g.Part == question.Part)))
                Error(location, "INVALID_QUESTION_GROUP", "Câu hỏi liên kết sai nhóm/phần.", "Part 5 độc lập; Part 6/7 thuộc nhóm cùng phần.");
            if ((question.Part != ToeicPart.Part6 && string.IsNullOrWhiteSpace(question.Prompt)) ||
                question.Prompt?.Length > ToeicConsts.MaxPromptLength)
                Error(location, "INVALID_PROMPT", "Nội dung câu hỏi bị thiếu hoặc quá dài.", "Điền nội dung tối đa 8000 ký tự.");
            if (question.Hint?.Length > ToeicConsts.MaxHintLength || (requireOfficial && string.IsNullOrWhiteSpace(question.Hint)))
                Error(location, "INVALID_HINT", "Gợi ý bị thiếu hoặc quá 4000 ký tự.", "Bổ sung gợi ý đã được xác minh cho mỗi câu.");
            if (question.Options.Count != 4 || !question.Options.Select(x => x.Key).OrderBy(x => x).SequenceEqual(new[] { "A", "B", "C", "D" }) ||
                question.Options.Select(x => x.DisplayOrder).OrderBy(x => x).SequenceEqual(new[] { 1, 2, 3, 4 }) == false ||
                question.Options.Any(x => string.IsNullOrWhiteSpace(x.Text) || x.Text.Length > ToeicConsts.MaxOptionTextLength) ||
                question.Options.Count(x => x.IsCorrect) != 1 || question.CorrectOptionId == null ||
                !question.Options.Any(x => x.Id == question.CorrectOptionId && x.IsCorrect))
                Error(location, "INVALID_OPTIONS", "Cần đúng bốn lựa chọn A–D và một đáp án đúng thuộc câu hỏi.", "Kiểm tra nội dung, thứ tự 1–4 và đáp án đúng.");
        }

        foreach (var group in exam.PassageGroups)
        {
            var location = "Nhóm " + group.ExternalId;
            var questions = exam.Questions.Where(x => x.PassageGroupId == group.Id).ToArray();
            if (questions.Length == 0 || group.ContentBlocks.Count == 0 || group.Passages.Count == 0)
                Error(location, "EMPTY_GROUP", "Nhóm phải có tài liệu và câu hỏi liên quan.", "Bổ sung đủ nội dung và câu hỏi trong nhóm.");
            if (group.ContentBlocks.Any(x => !group.Passages.Any(p => p.Id == x.PassageId)) ||
                group.Passages.Any(p => !group.ContentBlocks.Any(x => x.PassageId == p.Id)))
                Error(location, "INVALID_PASSAGE_LINK", "Khối nội dung không liên kết đúng tài liệu hoặc tài liệu rỗng.", "Giữ mỗi khối gắn với một tài liệu trong cùng nhóm.");
            if (group.ContentBlocks.Any(x => x.DisplayOrder <= 0) || group.ContentBlocks.GroupBy(x => x.DisplayOrder).Any(x => x.Count() > 1))
                Error(location, "INVALID_CONTENT_ORDER", "Thứ tự khối nội dung không hợp lệ.", "Dùng thứ tự dương duy nhất trong nhóm.");
            if (group.ContentBlocks.Any(x => x.Type == ContentBlockType.Image
                ? string.IsNullOrWhiteSpace(x.MediaType) || !ExamImageValidator.TryGetMediaType(x.BinaryData, x.FileName, x.MediaType, out _)
                : string.IsNullOrWhiteSpace(x.TextContent)))
                Error(location, "INVALID_CONTENT", "Nội dung hoặc dữ liệu ảnh bị thiếu.", "Điền nội dung và nhúng ảnh hợp lệ.");
            if (group.Part == ToeicPart.Part6)
            {
                var content = string.Join("\n", group.ContentBlocks.Where(x => x.Type != ContentBlockType.Image).Select(x => x.TextContent));
                int[] blanks;
                if (group.ContentBlocks.Any(x => x.Type != ContentBlockType.Image))
                    blanks = Regex.Matches(content, @"\{\{BLANK:(\d{3})\}\}", RegexOptions.IgnoreCase).Cast<Match>().Select(x => int.Parse(x.Groups[1].Value)).ToArray();
                else
                {
                    try { blanks = group.Passages.SelectMany(x => JsonSerializer.Deserialize<int[]>(x.BlankQuestionNumbers ?? "[]")).ToArray(); }
                    catch (JsonException) { blanks = Array.Empty<int>(); }
                }
                if (questions.Any(q => blanks.Count(x => x == q.OriginalNumber) != 1) || blanks.Any(x => questions.All(q => q.OriginalNumber != x)))
                    Error(location, "INVALID_BLANK_REFERENCE", "Mỗi câu Part 6 phải có đúng một chỗ trống trong đúng nhóm.", "Đối chiếu BLANKS của v07; với tài liệu legacy, sửa {{BLANK:131}} trong bài đọc.");
                if (requireOfficial && questions.Length != 4)
                    Error(location, "PART6_GROUP_SIZE", "Mỗi nhóm Part 6 phải có bốn câu.", "Chia 16 câu 131–146 thành bốn nhóm, mỗi nhóm bốn câu.");
            }
        }

        if (requireOfficial)
        {
            if (!exam.Questions.Select(x => x.OriginalNumber).OrderBy(x => x).SequenceEqual(Enumerable.Range(101, 100)))
                Error("EXAM", "OFFICIAL_QUESTION_SET", "Đề xuất bản phải đủ 100 câu từ 101 đến 200.", "Bổ sung đúng 30 câu Part 5, 16 câu Part 6 và 54 câu Part 7.");
            if (exam.DurationMinutes != 75) Error("EXAM", "OFFICIAL_DURATION", "Đề xuất bản phải có thời lượng 75 phút.", "Đặt thời lượng 75 phút.");
            if (exam.PassageGroups.Count(x => x.Part == ToeicPart.Part6) != 4 || exam.PassageGroups.Count(x => x.Part == ToeicPart.Part7) != 15)
                Error("EXAM", "OFFICIAL_GROUP_COUNT", "Đề xuất bản cần bốn nhóm Part 6 và 15 nhóm Part 7.", "Chia nội dung đúng các nhóm gốc đã xác minh.");
            if (!exam.Parts.Select(x => x.PartNumber).OrderBy(x => x).SequenceEqual(new[] { ToeicPart.Part5, ToeicPart.Part6, ToeicPart.Part7 }))
                Error("EXAM", "OFFICIAL_PART_SET", "Đề phải có đúng Part 5, Part 6 và Part 7.", "Kiểm tra đủ ba phần của đề.");
        }
        return errors;
    }
}
