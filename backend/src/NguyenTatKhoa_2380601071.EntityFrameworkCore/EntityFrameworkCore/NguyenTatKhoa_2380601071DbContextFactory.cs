using NguyenTatKhoa_2380601071.Configuration;
using NguyenTatKhoa_2380601071.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;

namespace NguyenTatKhoa_2380601071.EntityFrameworkCore;

/* This class is needed to run "dotnet ef ..." commands from command line on development. Not used anywhere else */
public class NguyenTatKhoa_2380601071DbContextFactory : IDesignTimeDbContextFactory<NguyenTatKhoa_2380601071DbContext>
{
    public NguyenTatKhoa_2380601071DbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<NguyenTatKhoa_2380601071DbContext>();

        /*
         You can provide an environmentName parameter to the AppConfigurations.Get method. 
         In this case, AppConfigurations will try to read appsettings.{environmentName}.json.
         Use Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") method or from string[] args to get environment if necessary.
         https://docs.microsoft.com/en-us/ef/core/cli/dbcontext-creation?tabs=dotnet-core-cli#args
         */
        var configuration = AppConfigurations.Get(WebContentDirectoryFinder.CalculateContentRootFolder(),
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development");

        NguyenTatKhoa_2380601071DbContextConfigurer.Configure(builder, configuration.GetConnectionString(NguyenTatKhoa_2380601071Consts.ConnectionStringName));

        return new NguyenTatKhoa_2380601071DbContext(builder.Options);
    }
}
