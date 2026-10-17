using Abp.Modules;
using Abp.Reflection.Extensions;
using NguyenTatKhoa_2380601071.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace NguyenTatKhoa_2380601071.Web.Startup;

[DependsOn(typeof(NguyenTatKhoa_2380601071WebCoreModule))]
public class NguyenTatKhoa_2380601071WebMvcModule : AbpModule
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfigurationRoot _appConfiguration;

    public NguyenTatKhoa_2380601071WebMvcModule(IWebHostEnvironment env)
    {
        _env = env;
        _appConfiguration = env.GetAppConfiguration();
    }

    public override void PreInitialize()
    {
        Configuration.Navigation.Providers.Add<NguyenTatKhoa_2380601071NavigationProvider>();
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(NguyenTatKhoa_2380601071WebMvcModule).GetAssembly());
    }
}
