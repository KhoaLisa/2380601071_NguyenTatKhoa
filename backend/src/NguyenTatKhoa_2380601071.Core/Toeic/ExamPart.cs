using Abp.Domain.Entities;
using System;
using System.Collections.Generic;

namespace NguyenTatKhoa_2380601071.Toeic;

public class ExamPart : Entity<Guid>
{
    public Guid ExamId { get; set; }
    public ToeicPart PartNumber { get; set; }
    public string Directions { get; set; }
    public int DisplayOrder { get; set; }
    public bool AllowShuffleGroups { get; set; }
    public Exam Exam { get; set; }
    public ICollection<PassageGroup> Groups { get; set; } = new List<PassageGroup>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
