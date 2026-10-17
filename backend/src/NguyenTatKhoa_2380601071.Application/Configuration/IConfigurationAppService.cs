using NguyenTatKhoa_2380601071.Configuration.Dto;
using System.Threading.Tasks;

namespace NguyenTatKhoa_2380601071.Configuration;

public interface IConfigurationAppService
{
    Task ChangeUiTheme(ChangeUiThemeInput input);
}
