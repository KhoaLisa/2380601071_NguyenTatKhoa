using Abp.Application.Services.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NguyenTatKhoa_2380601071.Toeic.Dto;

public class PublishedExamDto : EntityDto<Guid>
{
    public string Code { get; set; }
    public string Title { get; set; }
    public int DurationMinutes { get; set; }
    public int QuestionCount { get; set; }
    public bool CanStart { get; set; }
    public string Directions { get; set; }
    public List<StudentExamPartDto> Parts { get; set; } = new List<StudentExamPartDto>();
}

public class StudentExamPartDto
{
    public ToeicPart PartNumber { get; set; }
    public string Directions { get; set; }
    public int QuestionCount { get; set; }
}

public class MyExamAttemptDto
{
    public Guid AttemptId { get; set; }
    public Guid ExamId { get; set; }
    public string ExamTitle { get; set; }
    public AttemptStatus Status { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime DueAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public AttemptResultDto Result { get; set; }
}

public class StartExamInput
{
    public Guid ExamId { get; set; }
}

public class StartExamOutput
{
    public Guid AttemptId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime DueAtUtc { get; set; }
    public bool ResumedExistingAttempt { get; set; }
}

public class GetAttemptInput
{
    public Guid AttemptId { get; set; }
}

public class AttemptDto
{
    public Guid AttemptId { get; set; }
    public Guid ExamId { get; set; }
    public string ExamTitle { get; set; }
    public string Directions { get; set; }
    public List<StudentExamPartDto> Parts { get; set; } = new List<StudentExamPartDto>();
    public DateTime ServerNowUtc { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime DueAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public AttemptStatus Status { get; set; }
    public List<StudentPassageGroupDto> Groups { get; set; } = new List<StudentPassageGroupDto>();
    public List<StudentQuestionDto> Questions { get; set; } = new List<StudentQuestionDto>();
    public AttemptResultDto Result { get; set; }
}

public class StudentPassageGroupDto : EntityDto<Guid>
{
    public string Title { get; set; }
    public ToeicPart Part { get; set; }
    public int DisplayOrder { get; set; }
    public List<StudentContentBlockDto> ContentBlocks { get; set; } = new List<StudentContentBlockDto>();
}

public class StudentContentBlockDto : EntityDto<Guid>
{
    public string DocumentKey { get; set; }
    public int DisplayOrder { get; set; }
    public ContentBlockType Type { get; set; }
    public string TextContent { get; set; }
    public string ContentUrl { get; set; }
    public string MediaType { get; set; }
}

public class StudentQuestionDto : EntityDto<Guid>
{
    public Guid? PassageGroupId { get; set; }
    public ToeicPart Part { get; set; }
    public int OriginalNumber { get; set; }
    public int DisplayOrder { get; set; }
    public string Prompt { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public List<StudentQuestionOptionDto> Options { get; set; } = new List<StudentQuestionOptionDto>();
}

public class StudentQuestionOptionDto : EntityDto<Guid>
{
    public string Key { get; set; }
    public string Text { get; set; }
}

public class SaveAnswerInput
{
    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid? SelectedOptionId { get; set; }
}

public class SaveAnswerOutput
{
    public bool Accepted { get; set; }
    public bool IsFinalized { get; set; }
    public AttemptResultDto Result { get; set; }
}

public class SubmitAttemptInput
{
    public Guid AttemptId { get; set; }
}

public class AttemptResultDto
{
    public Guid AttemptId { get; set; }
    public AttemptStatus Status { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectCount { get; set; }
    public int IncorrectCount { get; set; }
    public int SkippedCount { get; set; }
    public decimal CorrectPercentage { get; set; }
}
