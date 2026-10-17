using System.ComponentModel.DataAnnotations;

namespace NguyenTatKhoa_2380601071.Configuration.Dto;

public class ChangeUiThemeInput
{
    [Required]
    [StringLength(32)]
    public string Theme { get; set; }
}
