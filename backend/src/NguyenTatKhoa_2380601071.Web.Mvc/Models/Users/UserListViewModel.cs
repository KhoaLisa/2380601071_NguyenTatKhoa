using NguyenTatKhoa_2380601071.Roles.Dto;
using System.Collections.Generic;

namespace NguyenTatKhoa_2380601071.Web.Models.Users;

public class UserListViewModel
{
    public IReadOnlyList<RoleDto> Roles { get; set; }
}
