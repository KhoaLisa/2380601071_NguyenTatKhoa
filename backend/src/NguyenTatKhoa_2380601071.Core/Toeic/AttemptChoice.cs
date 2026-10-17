using Abp.Domain.Entities;
using System;

namespace NguyenTatKhoa_2380601071.Toeic;

public class AttemptChoice : Entity<Guid>
{
    public Guid AttemptQuestionId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid ChoiceId { get; set; }
    public int DisplayOrder { get; set; }
    public AttemptQuestion AttemptQuestion { get; set; }
    public QuestionOption Choice { get; set; }
}
