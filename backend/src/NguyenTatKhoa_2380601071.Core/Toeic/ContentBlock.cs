using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;

namespace NguyenTatKhoa_2380601071.Toeic;

public class ContentBlock : Entity<Guid>
{
    public Guid PassageGroupId { get; set; }
    public Guid PassageId { get; set; }
    [Required, StringLength(64)]
    public string SourceBlockId { get; set; }
    public int? SourcePage { get; set; }
    public Passage Passage { get; set; }

    [Required]
    [StringLength(ToeicConsts.MaxDocumentKeyLength)]
    public string DocumentKey { get; set; }

    public int DisplayOrder { get; set; }

    public ContentBlockType Type { get; set; }

    public string TextContent { get; set; }

    public byte[] BinaryData { get; set; }

    [StringLength(ToeicConsts.MaxMediaTypeLength)]
    public string MediaType { get; set; }

    [StringLength(ToeicConsts.MaxFileNameLength)]
    public string FileName { get; set; }

    public PassageGroup PassageGroup { get; set; }
}
