using Abp.Authorization;
using Abp.Localization;
using Abp.MultiTenancy;

namespace NguyenTatKhoa_2380601071.Authorization;

public class NguyenTatKhoa_2380601071AuthorizationProvider : AuthorizationProvider
{
    public override void SetPermissions(IPermissionDefinitionContext context)
    {
        context.CreatePermission(PermissionNames.Pages_Users, L("Users"));
        context.CreatePermission(PermissionNames.Pages_Users_Activation, L("UsersActivation"));
        context.CreatePermission(PermissionNames.Pages_Roles, L("Roles"));
        context.CreatePermission(PermissionNames.Pages_Tenants, L("Tenants"), multiTenancySides: MultiTenancySides.Host);
        var exams = context.CreatePermission(PermissionNames.Pages_Exams, L("ToeicExams"));
        exams.CreateChildPermission(PermissionNames.Pages_Exams_Manage, L("ToeicExamsManage"));
        exams.CreateChildPermission(PermissionNames.Pages_Exams_Statistics, L("ToeicExamsStatistics"));
    }

    private static ILocalizableString L(string name)
    {
        return new LocalizableString(name, NguyenTatKhoa_2380601071Consts.LocalizationSourceName);
    }
}
