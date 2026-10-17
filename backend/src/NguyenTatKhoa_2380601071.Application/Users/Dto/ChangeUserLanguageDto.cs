using System.ComponentModel.DataAnnotations;

namespace NguyenTatKhoa_2380601071.Users.Dto;

public class ChangeUserLanguageDto
{
    [Required]
    public string LanguageName { get; set; }
}