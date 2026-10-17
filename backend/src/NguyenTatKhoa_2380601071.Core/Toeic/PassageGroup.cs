using Abp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NguyenTatKhoa_2380601071.Toeic;

public class PassageGroup : Entity<Guid>
{
    public Guid ExamId { get; set; }
    public Guid ExamPartId { get; set; }
    public int FirstQuestionNumber { get; set; }
    public int LastQuestionNumber { get; set; }
    public ExamPart ExamPart { get; set; }
    public ICollection<Passage> Passages { get; set; } = new List<Passage>();

    public ToeicPart Part { get; set; }

    [Required]
    [StringLength(ToeicConsts.MaxExternalIdLength)]
    public string ExternalId { get; set; }

    [StringLength(ToeicConsts.MaxTitleLength)]
    public string Title { get; set; }

    public int DisplayOrder { get; set; }

    public bool AllowQuestionShuffle { get; set; }

    public Exam Exam { get; set; }

    public ICollection<ContentBlock> ContentBlocks { get; set; } = new List<ContentBlock>();

    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
