using Abp.Authorization;
using NguyenTatKhoa_2380601071.Authorization.Roles;
using NguyenTatKhoa_2380601071.Authorization.Users;

namespace NguyenTatKhoa_2380601071.Authorization;

public class PermissionChecker : PermissionChecker<Role, User>
{
    public PermissionChecker(UserManager userManager)
        : base(userManager)
    {
    }
}
