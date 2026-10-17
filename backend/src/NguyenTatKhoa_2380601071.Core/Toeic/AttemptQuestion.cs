using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace NguyenTatKhoa_2380601071.Toeic;

public class AttemptQuestion : Entity<Guid>
{
    public Guid AttemptId { get; set; }
    public Guid ExamId { get; set; }

    public Guid QuestionId { get; set; }

    public int DisplayOrder { get; set; }

    [Required]
    [StringLength(200)]
    public string OptionOrderCsv { get; set; }

    public Guid? SelectedOptionId { get; set; }

    public DateTime? AnsweredAtUtc { get; set; }

    public bool? IsCorrect { get; set; }
    public ICollection<AttemptChoice> Choices { get; set; } = new List<AttemptChoice>();

    public ExamAttempt Attempt { get; set; }

    public Question Question { get; set; }

    public QuestionOption SelectedOption { get; set; }
}
