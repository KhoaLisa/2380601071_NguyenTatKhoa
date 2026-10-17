using System.ComponentModel.DataAnnotations;

namespace NguyenTatKhoa_2380601071.Users.Dto;

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; }

    [Required]
    public string NewPassword { get; set; }
}
