using NguyenTatKhoa_2380601071.Toeic;
using System.Collections.Generic;

namespace NguyenTatKhoa_2380601071.Toeic.Importing;

public class ParsedExamDocument
{
    public string SpecificationVersion { get; set; }
    public string Code { get; set; }
    public string Title { get; set; }
    public int DurationMinutes { get; set; } = ToeicConsts.OfficialDurationMinutes;
    public bool IsOfficial { get; set; }
    public string TagDialect { get; set; } = "legacy";
    public string SourceExamId { get; set; }
    public string Directions { get; set; }
    public int DeclaredQuestionCount { get; set; }
    public List<ParsedExamPart> Parts { get; } = new List<ParsedExamPart>();
    public List<ParsedPassageGroup> Groups { get; } = new List<ParsedPassageGroup>();
    public List<ParsedQuestion> Questions { get; } = new List<ParsedQuestion>();
    public List<ImportValidationError> Errors { get; } = new List<ImportValidationError>();
    public bool IsValid => Errors.Count == 0;
}

public class ParsedExamPart
{
    public ToeicPart Number { get; set; }
    public string Directions { get; set; }
    public bool AllowShuffleGroups { get; set; }
    public int DisplayOrder { get; set; }
}

public class ParsedPassageGroup
{
    public string ExternalId { get; set; }
    public string Title { get; set; }
    public ToeicPart Part { get; set; }
    public int DisplayOrder { get; set; }
    public bool AllowQuestionShuffle { get; set; }
    public List<string> DocumentKeys { get; } = new List<string>();
    public int FirstQuestionNumber { get; set; }
    public int LastQuestionNumber { get; set; }
    public List<ParsedPassage> Passages { get; } = new List<ParsedPassage>();
    public List<ParsedContentBlock> ContentBlocks { get; } = new List<ParsedContentBlock>();
}

public class ParsedPassage
{
    public string Id { get; set; }
    public string Kind { get; set; }
    public int DisplayOrder { get; set; }
    public List<int> BlankQuestionNumbers { get; } = new List<int>();
}

public class ParsedContentBlock
{
    public string SourceBlockId { get; set; }
    public int? SourcePage { get; set; }
    public string DocumentKey { get; set; }
    public int DisplayOrder { get; set; }
    public ContentBlockType Type { get; set; }
    public string TextContent { get; set; }
    public byte[] BinaryData { get; set; }
    public string MediaType { get; set; }
    public string FileName { get; set; }
}

public class ParsedQuestion
{
    public string SourceQuestionId { get; set; }
    public ToeicPart Part { get; set; }
    public string GroupExternalId { get; set; }
    public int OriginalNumber { get; set; }
    public int DisplayOrder { get; set; }
    public string Prompt { get; set; }
    public string Hint { get; set; }
    public bool AllowQuestionShuffle { get; set; }
    public bool AllowOptionShuffle { get; set; }
    public string CorrectOptionKey { get; set; }
    public List<ParsedQuestionOption> Options { get; } = new List<ParsedQuestionOption>();
}

public class ParsedQuestionOption
{
    public string Key { get; set; }
    public int DisplayOrder { get; set; }
    public string Text { get; set; }
}

public class ImportValidationError
{
    public string Location { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }
    public string Fix { get; set; }
}
