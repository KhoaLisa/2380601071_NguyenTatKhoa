using Abp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NguyenTatKhoa_2380601071.Toeic;

public class Passage : Entity<Guid>
{
    public Guid PassageGroupId { get; set; }
    [Required, StringLength(64)]
    public string SourcePassageId { get; set; }
    [StringLength(64)]
    public string Kind { get; set; }
    public int DisplayOrder { get; set; }
    [Required, StringLength(256)]
    public string BlankQuestionNumbers { get; set; } = "[]";
    public PassageGroup PassageGroup { get; set; }
    public ICollection<ContentBlock> Blocks { get; set; } = new List<ContentBlock>();
}
