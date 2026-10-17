using Abp.Localization;
using Abp.Modules;
using Abp.Reflection.Extensions;
using Abp.Runtime.Security;
using Abp.Timing;
using Abp.Zero;
using Abp.Zero.Configuration;
using NguyenTatKhoa_2380601071.Authorization.Roles;
using NguyenTatKhoa_2380601071.Authorization.Users;
using NguyenTatKhoa_2380601071.Configuration;
using NguyenTatKhoa_2380601071.Localization;
using NguyenTatKhoa_2380601071.MultiTenancy;
using NguyenTatKhoa_2380601071.Timing;

namespace NguyenTatKhoa_2380601071;

[DependsOn(typeof(AbpZeroCoreModule))]
public class NguyenTatKhoa_2380601071CoreModule : AbpModule
{
    public override void PreInitialize()
    {
        Configuration.UnitOfWork.IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;
        Configuration.Auditing.IsEnabledForAnonymousUsers = true;

        // Declare entity types
        Configuration.Modules.Zero().EntityTypes.Tenant = typeof(Tenant);
        Configuration.Modules.Zero().EntityTypes.Role = typeof(Role);
        Configuration.Modules.Zero().EntityTypes.User = typeof(User);

        NguyenTatKhoa_2380601071LocalizationConfigurer.Configure(Configuration.Localization);

        // Enable this line to create a multi-tenant application.
        Configuration.MultiTenancy.IsEnabled = NguyenTatKhoa_2380601071Consts.MultiTenancyEnabled;

        // Configure roles
        AppRoleConfig.Configure(Configuration.Modules.Zero().RoleManagement);

        Configuration.Settings.Providers.Add<AppSettingProvider>();

        Configuration.Localization.Languages.Add(new LanguageInfo("fa", "فارسی", "famfamfam-flags ir"));

        Configuration.Settings.SettingEncryptionConfiguration.DefaultPassPhrase = NguyenTatKhoa_2380601071Consts.DefaultPassPhrase;
        SimpleStringCipher.DefaultPassPhrase = NguyenTatKhoa_2380601071Consts.DefaultPassPhrase;
    }

    public override void Initialize()
    {
        IocManager.RegisterAssemblyByConvention(typeof(NguyenTatKhoa_2380601071CoreModule).GetAssembly());
    }

    public override void PostInitialize()
    {
        IocManager.Resolve<AppTimes>().StartupTime = Clock.Now;
    }
}
