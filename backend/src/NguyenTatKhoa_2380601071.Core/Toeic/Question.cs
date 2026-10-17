using Abp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NguyenTatKhoa_2380601071.Toeic;

public class Question : Entity<Guid>
{
    public Guid ExamId { get; set; }
    public Guid ExamPartId { get; set; }
    [Required, StringLength(64)]
    public string SourceQuestionId { get; set; }
    public ExamPart ExamPart { get; set; }

    public Guid? PassageGroupId { get; set; }

    public ToeicPart Part { get; set; }

    public int OriginalNumber { get; set; }

    public int DisplayOrder { get; set; }

    [StringLength(ToeicConsts.MaxPromptLength)]
    public string Prompt { get; set; }

    [StringLength(ToeicConsts.MaxHintLength)]
    public string Hint { get; set; }

    public bool AllowQuestionShuffle { get; set; }

    public bool AllowOptionShuffle { get; set; }

    public Guid? CorrectOptionId { get; set; }

    public Exam Exam { get; set; }

    public PassageGroup PassageGroup { get; set; }

    public QuestionOption CorrectOption { get; set; }

    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
}
