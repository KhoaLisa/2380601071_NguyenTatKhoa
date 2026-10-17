using Abp.EntityFrameworkCore.Configuration;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Zero.EntityFrameworkCore;
using NguyenTatKhoa_2380601071.EntityFrameworkCore.Seed;

namespace NguyenTatKhoa_2380601071.EntityFrameworkCore;

[DependsOn(
    typeof(NguyenTatKhoa_2380601071CoreModule),
    typeof(AbpZeroCoreEntityFrameworkCoreModule))]
public class NguyenTatKhoa_2380601071EntityFrameworkModule : AbpModule
{
    /* Used it tests to skip dbcontext registration, in order to use in-memory database of EF Core */
    public bool SkipDbContextRegistration { get; set; }

    public bool SkipDbSeed { get; set; }

    public override void PreInitialize()
    {
        if (!SkipDbContextRegistration)
        {
            Configuration.Modules.AbpEfCore().AddDbContext<NguyenTatKhoa_2380601071DbContext>(options =>
            {
                if (options.ExistingConnection != null)
                {
                    NguyenTatKhoa_2380601071DbContextConfigurer.Configure(options.DbContextOptions, options.ExistingConnection);
                }
                else
                {
                    NguyenTatKhoa_2380601071DbContextConfigurer.Configure(options.DbContextOptions, options.ConnectionString);
                }
            });
        }
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(NguyenTatKhoa_2380601071EntityFrameworkModule).GetAssembly());
    }

    public override void PostInitialize()
    {
        if (!SkipDbSeed)
        {
            SeedHelper.SeedHostDb(IocManager);
        }
    }
}
