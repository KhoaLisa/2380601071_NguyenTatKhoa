using Abp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NguyenTatKhoa_2380601071.Toeic;

public class ExamAttempt : Entity<Guid>
{
    public Guid ExamId { get; set; }

    public long UserId { get; set; }
    public int DurationMinutesSnapshot { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public DateTime DueAtUtc { get; set; }

    public DateTime? SubmittedAtUtc { get; set; }

    public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;

    public int TotalQuestions { get; set; }

    public int CorrectCount { get; set; }

    public int IncorrectCount { get; set; }

    public int SkippedCount { get; set; }

    public decimal CorrectPercentage { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; }

    public Exam Exam { get; set; }

    public ICollection<AttemptQuestion> Questions { get; set; } = new List<AttemptQuestion>();
}
