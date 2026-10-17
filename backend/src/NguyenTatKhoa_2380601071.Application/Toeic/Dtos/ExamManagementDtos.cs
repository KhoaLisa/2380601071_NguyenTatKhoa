using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NguyenTatKhoa_2380601071.Toeic.Dto;

public class ExamListItemDto : EntityDto<Guid>
{
    public string Code { get; set; }
    public string Title { get; set; }
    public int DurationMinutes { get; set; }
    public ExamStatus Status { get; set; }
    public int Version { get; set; }
    public bool IsLocked { get; set; }
    public bool IsOfficialFormat { get; set; }
    public int QuestionCount { get; set; }
    public int CompletedAttemptCount { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}

public class ExamDetailDto : ExamListItemDto
{
    public string SourceExamId { get; set; }
    public string TagVersion { get; set; }
    public string Directions { get; set; }
    public List<ExamPartManagementDto> Parts { get; set; } = new List<ExamPartManagementDto>();
    public List<PassageGroupManagementDto> Groups { get; set; } = new List<PassageGroupManagementDto>();
    public List<QuestionManagementDto> Questions { get; set; } = new List<QuestionManagementDto>();
}

public class ExamPartManagementDto
{
    public ToeicPart Number { get; set; }
    public int DisplayOrder { get; set; }
    public string Directions { get; set; }
}

public class PassageGroupManagementDto : EntityDto<Guid>
{
    public string ExternalId { get; set; }
    public string Title { get; set; }
    public ToeicPart Part { get; set; }
    public int DisplayOrder { get; set; }
    public bool AllowQuestionShuffle { get; set; }
    public int FirstQuestionNumber { get; set; }
    public int LastQuestionNumber { get; set; }
    public List<PassageManagementDto> Passages { get; set; } = new List<PassageManagementDto>();
    public List<ContentBlockManagementDto> ContentBlocks { get; set; } = new List<ContentBlockManagementDto>();
}

public class PassageManagementDto : EntityDto<Guid>
{
    public string SourcePassageId { get; set; }
    public string Kind { get; set; }
    public int DisplayOrder { get; set; }
    public string BlankQuestionNumbers { get; set; }
}

public class ContentBlockManagementDto : EntityDto<Guid>
{
    public Guid PassageId { get; set; }
    public string SourceBlockId { get; set; }
    public int? SourcePage { get; set; }
    public string DocumentKey { get; set; }
    public int DisplayOrder { get; set; }
    public ContentBlockType Type { get; set; }
    public string TextContent { get; set; }
    public string MediaType { get; set; }
    public string FileName { get; set; }
    public string ContentUrl { get; set; }
}

public class QuestionManagementDto : EntityDto<Guid>
{
    public string SourceQuestionId { get; set; }
    public Guid? PassageGroupId { get; set; }
    public ToeicPart Part { get; set; }
    public int OriginalNumber { get; set; }
    public int DisplayOrder { get; set; }
    public string Prompt { get; set; }
    public string Hint { get; set; }
    public bool AllowQuestionShuffle { get; set; }
    public bool AllowOptionShuffle { get; set; }
    public Guid? CorrectOptionId { get; set; }
    public List<QuestionOptionManagementDto> Options { get; set; } = new List<QuestionOptionManagementDto>();
}

public class QuestionOptionManagementDto : EntityDto<Guid>
{
    public string Key { get; set; }
    public int DisplayOrder { get; set; }
    public string Text { get; set; }
}

public class ImportExamInput
{
    [Required]
    public string FileName { get; set; }

    [Required]
    public byte[] Content { get; set; }
}

public class CopyExamToDraftInput : EntityDto<Guid>
{
    [StringLength(ToeicConsts.MaxCodeLength)]
    public string Code { get; set; }

    [StringLength(ToeicConsts.MaxTitleLength)]
    public string Title { get; set; }
}

public class ExamValidationResultDto
{
    public Guid ExamId { get; set; }
    public bool CanPublish { get; set; }
    public List<ImportValidationErrorDto> Errors { get; set; } = new List<ImportValidationErrorDto>();
}

public class ImportExamResultDto
{
    public bool AlreadyImported { get; set; }
    public bool Succeeded { get; set; }
    public Guid? ExamId { get; set; }
    public string Code { get; set; }
    public int QuestionCount { get; set; }
    public List<ImportValidationErrorDto> Errors { get; set; } = new List<ImportValidationErrorDto>();
}

public class ImportValidationErrorDto
{
    public string Location { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }
    public string Fix { get; set; }
}

public class UpdateExamInput : EntityDto<Guid>
{
    [Required]
    [StringLength(ToeicConsts.MaxTitleLength)]
    public string Title { get; set; }

    [Range(1, 600)]
    public int DurationMinutes { get; set; }

    public List<UpdateContentBlockInput> ContentBlocks { get; set; } = new List<UpdateContentBlockInput>();
    public List<UpdateQuestionInput> Questions { get; set; } = new List<UpdateQuestionInput>();
}

public class UpdateContentBlockInput : EntityDto<Guid>
{
    public int DisplayOrder { get; set; }
    public string TextContent { get; set; }
}

public class ReplaceExamImageInput
{
    public Guid ExamId { get; set; }
    public Guid BlockId { get; set; }
    [Required, StringLength(256)]
    public string FileName { get; set; }
    [Required]
    public byte[] Content { get; set; }
}

public class UpdateQuestionInput : EntityDto<Guid>
{
    [StringLength(ToeicConsts.MaxPromptLength)]
    public string Prompt { get; set; }

    [StringLength(ToeicConsts.MaxHintLength)]
    public string Hint { get; set; }

    public bool AllowQuestionShuffle { get; set; }
    public bool AllowOptionShuffle { get; set; }
    public Guid CorrectOptionId { get; set; }
    public List<UpdateQuestionOptionInput> Options { get; set; } = new List<UpdateQuestionOptionInput>();
}

public class UpdateQuestionOptionInput : EntityDto<Guid>
{
    [Required]
    [StringLength(ToeicConsts.MaxOptionTextLength)]
    public string Text { get; set; }
}

public class ExamStatisticsDto
{
    public Guid ExamId { get; set; }
    public string ExamTitle { get; set; }
    public int AttemptCount { get; set; }
    public int CompletedAttemptCount { get; set; }
    public decimal AverageCorrectPercentage { get; set; }
    public int DistinctLearnerCount { get; set; }
    public List<QuestionErrorStatisticDto> TopIncorrectQuestions { get; set; } = new List<QuestionErrorStatisticDto>();
}

public class QuestionErrorStatisticDto
{
    public Guid QuestionId { get; set; }
    public int OriginalNumber { get; set; }
    public int IncorrectCount { get; set; }
    public int SkippedCount { get; set; }
}
