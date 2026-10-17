using Abp;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using Abp.Modules;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NguyenTatKhoa_2380601071;
using NguyenTatKhoa_2380601071.EntityFrameworkCore;
using NguyenTatKhoa_2380601071.EntityFrameworkCore.Seed;

namespace Toeic.DatabaseCheck;

[DependsOn(typeof(NguyenTatKhoa_2380601071ApplicationModule), typeof(NguyenTatKhoa_2380601071EntityFrameworkModule))]
public class DatabaseCheckModule : AbpModule
{
    public DatabaseCheckModule(NguyenTatKhoa_2380601071EntityFrameworkModule efModule) => efModule.SkipDbSeed = true;
    public override void PreInitialize()
    {
        Configuration.DefaultNameOrConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? throw new InvalidOperationException("Set ConnectionStrings__Default, or use scripts/setup-toeic-database.ps1.");
        Configuration.BackgroundJobs.IsJobExecutionEnabled = false;
    }
    public override void Initialize()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        NguyenTatKhoa_2380601071.Identity.IdentityRegistrar.Register(services);
        Castle.Windsor.MsDependencyInjection.WindsorRegistrationHelper.CreateServiceProvider(IocManager.IocContainer, services);
        IocManager.RegisterAssemblyByConvention(typeof(DatabaseCheckModule).Assembly);
    }
}

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try
        {
            var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
            var target = new SqlConnectionStringBuilder(connection);
            if (string.IsNullOrWhiteSpace(target.InitialCatalog) || new[] { "master", "model", "msdb", "tempdb" }.Contains(target.InitialCatalog.ToLowerInvariant()))
                throw new InvalidOperationException("An explicit application database is required.");
            using var bootstrapper = AbpBootstrapper.Create<DatabaseCheckModule>();
            bootstrapper.Initialize();
            if (args.Contains("--seed"))
            {
                SeedHelper.SeedHostDb(bootstrapper.IocManager);
                Console.WriteLine("Template host/tenant identity seed completed; no TOEIC exam data seeded.");
            }
            var manager = bootstrapper.IocManager.Resolve<IUnitOfWorkManager>();
            using (var uow = manager.Begin(new UnitOfWorkOptions { IsTransactional = false }))
            {
                var context = await bootstrapper.IocManager.Resolve<IDbContextProvider<NguyenTatKhoa_2380601071DbContext>>().GetDbContextAsync();
                await context.Database.OpenConnectionAsync();
                Console.WriteLine($"DbContext connected: {context.Database.GetDbConnection().DataSource} / {context.Database.GetDbConnection().Database}");
                var pending = (await context.Database.GetPendingMigrationsAsync()).ToArray();
                if (pending.Length > 0 || context.Database.HasPendingModelChanges()) throw new InvalidOperationException("Pending migrations or model changes detected.");
                Console.WriteLine($"Applied migrations: {(await context.Database.GetAppliedMigrationsAsync()).Count()}; pending: 0");
                Console.WriteLine($"Stored TOEIC exams: {await context.Exams.CountAsync()}; questions: {await context.Questions.CountAsync()}; attempts: {await context.ExamAttempts.CountAsync()}");
                await uow.CompleteAsync();
            }
            if (args.Contains("--test")) await SqlServerChecks.RunAsync(bootstrapper.IocManager);
            if (args.Contains("--integration-test")) await FullIntegrationChecks.RunAsync(bootstrapper.IocManager);
            if (args.Contains("--concurrency-test")) await ConcurrencyChecks.RunAsync(bootstrapper.IocManager);
            if (args.Contains("--cleanup-ui-attempts")) await UiAttemptCleanup.RunAsync(bootstrapper.IocManager);
            if (args.Contains("--cleanup-last-ui-attempt")) await UiAttemptCleanup.RunAsync(bootstrapper.IocManager, currentOnly: true);
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
