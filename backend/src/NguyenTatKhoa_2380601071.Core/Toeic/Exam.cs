using Abp.Domain.Entities.Auditing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NguyenTatKhoa_2380601071.Toeic;

public class Exam : FullAuditedEntity<Guid>
{
    [Required]
    [StringLength(ToeicConsts.MaxCodeLength)]
    public string Code { get; set; }

    [Required]
    [StringLength(ToeicConsts.MaxTitleLength)]
    public string Title { get; set; }

    [Required, StringLength(64)]
    public string SourceExamId { get; set; }
    [Required, StringLength(32)]
    public string TagVersion { get; set; } = "1.0";
    public string Directions { get; set; }
    public int DeclaredQuestionCount { get; set; }
    [StringLength(256)]
    public string SourceFileName { get; set; }
    [StringLength(64)]
    public string SourceHash { get; set; }
    [Timestamp]
    public byte[] RowVersion { get; set; }
    public ICollection<ExamPart> Parts { get; set; } = new List<ExamPart>();

    public int DurationMinutes { get; set; } = ToeicConsts.OfficialDurationMinutes;

    public ExamStatus Status { get; set; } = ExamStatus.Draft;

    public int Version { get; set; } = 1;

    public bool IsOfficialFormat { get; set; }

    public bool IsLocked { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public ICollection<PassageGroup> PassageGroups { get; set; } = new List<PassageGroup>();

    public ICollection<Question> Questions { get; set; } = new List<Question>();

    public ICollection<ExamAttempt> Attempts { get; set; } = new List<ExamAttempt>();
}
