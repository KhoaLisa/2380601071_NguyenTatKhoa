using Abp.AspNetCore;
using Abp.AspNetCore.TestBase;
using Abp.Modules;
using Abp.Reflection.Extensions;
using NguyenTatKhoa_2380601071.EntityFrameworkCore;
using NguyenTatKhoa_2380601071.Web.Startup;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace NguyenTatKhoa_2380601071.Web.Tests;

[DependsOn(
    typeof(NguyenTatKhoa_2380601071WebMvcModule),
    typeof(AbpAspNetCoreTestBaseModule)
)]
public class NguyenTatKhoa_2380601071WebTestModule : AbpModule
{
    public NguyenTatKhoa_2380601071WebTestModule(NguyenTatKhoa_2380601071EntityFrameworkModule abpProjectNameEntityFrameworkModule)
    {
        abpProjectNameEntityFrameworkModule.SkipDbContextRegistration = true;
    }

    public override void PreInitialize()
    {
        Configuration.UnitOfWork.IsTransactional = false; //EF Core InMemory DB does not support transactions.
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(NguyenTatKhoa_2380601071WebTestModule).GetAssembly());
    }

    public override void PostInitialize()
    {
        IocManager.Resolve<ApplicationPartManager>()
            .AddApplicationPartsIfNotAddedBefore(typeof(NguyenTatKhoa_2380601071WebMvcModule).Assembly);
    }
}