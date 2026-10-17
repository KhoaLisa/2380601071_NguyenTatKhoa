using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;

namespace NguyenTatKhoa_2380601071.Toeic;

public class QuestionOption : Entity<Guid>
{
    public Guid QuestionId { get; set; }
    public bool IsCorrect { get; set; }

    [Required]
    [StringLength(1)]
    public string Key { get; set; }

    public int DisplayOrder { get; set; }

    [Required]
    [StringLength(ToeicConsts.MaxOptionTextLength)]
    public string Text { get; set; }

    public Question Question { get; set; }
}
