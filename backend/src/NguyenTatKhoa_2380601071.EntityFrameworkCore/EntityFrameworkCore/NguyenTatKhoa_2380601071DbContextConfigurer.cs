using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace NguyenTatKhoa_2380601071.EntityFrameworkCore;

public static class NguyenTatKhoa_2380601071DbContextConfigurer
{
    public static void Configure(DbContextOptionsBuilder<NguyenTatKhoa_2380601071DbContext> builder, string connectionString)
    {
        builder.UseSqlServer(connectionString);
    }

    public static void Configure(DbContextOptionsBuilder<NguyenTatKhoa_2380601071DbContext> builder, DbConnection connection)
    {
        builder.UseSqlServer(connection);
    }
}
