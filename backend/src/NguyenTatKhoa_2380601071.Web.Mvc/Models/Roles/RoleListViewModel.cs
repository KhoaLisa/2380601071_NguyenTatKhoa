using NguyenTatKhoa_2380601071.Roles.Dto;
using System.Collections.Generic;

namespace NguyenTatKhoa_2380601071.Web.Models.Roles;

public class RoleListViewModel
{
    public IReadOnlyList<PermissionDto> Permissions { get; set; }
}
