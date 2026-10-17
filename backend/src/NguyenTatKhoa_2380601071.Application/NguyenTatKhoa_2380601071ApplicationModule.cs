using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;
using NguyenTatKhoa_2380601071.Authorization;
using Abp.Threading.BackgroundWorkers;
using NguyenTatKhoa_2380601071.Toeic;

namespace NguyenTatKhoa_2380601071;

[DependsOn(
    typeof(NguyenTatKhoa_2380601071CoreModule),
    typeof(AbpAutoMapperModule))]
public class NguyenTatKhoa_2380601071ApplicationModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.Authorization.Providers.Add<NguyenTatKhoa_2380601071AuthorizationProvider>();
    }

    public override void Initialize()
    {
        var thisAssembly = typeof(NguyenTatKhoa_2380601071ApplicationModule).GetAssembly();

        IocManager.RegisterAssemblyByConvention(thisAssembly);

        Configuration.Modules.AbpAutoMapper().Configurators.Add(
            // Scan the assembly for classes which inherit from AutoMapper.Profile
            cfg => cfg.AddMaps(thisAssembly)
        );
    }

    public override void PostInitialize()
    {
        if (Configuration.BackgroundJobs.IsJobExecutionEnabled)
            IocManager.Resolve<IBackgroundWorkerManager>().Add(IocManager.Resolve<ExpiredAttemptWorker>());
    }
}
