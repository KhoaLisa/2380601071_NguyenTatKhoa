using Abp.Events.Bus;
using Abp.Modules;
using Abp.Reflection.Extensions;
using NguyenTatKhoa_2380601071.Configuration;
using NguyenTatKhoa_2380601071.EntityFrameworkCore;
using NguyenTatKhoa_2380601071.Migrator.DependencyInjection;
using Castle.MicroKernel.Registration;
using Microsoft.Extensions.Configuration;

namespace NguyenTatKhoa_2380601071.Migrator;

[DependsOn(typeof(NguyenTatKhoa_2380601071EntityFrameworkModule))]
public class NguyenTatKhoa_2380601071MigratorModule : AbpModule
{
    private readonly IConfigurationRoot _appConfiguration;

    public NguyenTatKhoa_2380601071MigratorModule(NguyenTatKhoa_2380601071EntityFrameworkModule abpProjectNameEntityFrameworkModule)
    {
        abpProjectNameEntityFrameworkModule.SkipDbSeed = true;

        _appConfiguration = AppConfigurations.Get(
            typeof(NguyenTatKhoa_2380601071MigratorModule).GetAssembly().GetDirectoryPathOrNull()
        );
    }

    public override void PreInitialize()
    {
        Configuration.DefaultNameOrConnectionString = _appConfiguration.GetConnectionString(
            NguyenTatKhoa_2380601071Consts.ConnectionStringName
        );

        Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
        Configuration.ReplaceService(
            typeof(IEventBus),
            () => IocManager.IocContainer.Register(
                Component.For<IEventBus>().Instance(NullEventBus.Instance)
            )
        );
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(NguyenTatKhoa_2380601071MigratorModule).GetAssembly());
        ServiceCollectionRegistrar.Register(IocManager);
    }
}
