using Abp.Authorization;
using Abp.Runtime.Session;
using NguyenTatKhoa_2380601071.Configuration.Dto;
using System.Threading.Tasks;

namespace NguyenTatKhoa_2380601071.Configuration;

[AbpAuthorize]
public class ConfigurationAppService : NguyenTatKhoa_2380601071AppServiceBase, IConfigurationAppService
{
    public async Task ChangeUiTheme(ChangeUiThemeInput input)
    {
        await SettingManager.ChangeSettingForUserAsync(AbpSession.ToUserIdentifier(), AppSettingNames.UiTheme, input.Theme);
    }
}
