using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.EntityFrameworkCore;
using Abp.EntityFrameworkCore.Repositories;

namespace NguyenTatKhoa_2380601071.EntityFrameworkCore.Repositories;

/// <summary>
/// Base class for custom repositories of the application.
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TPrimaryKey">Primary key type of the entity</typeparam>
public abstract class NguyenTatKhoa_2380601071RepositoryBase<TEntity, TPrimaryKey> : EfCoreRepositoryBase<NguyenTatKhoa_2380601071DbContext, TEntity, TPrimaryKey>
    where TEntity : class, IEntity<TPrimaryKey>
{
    protected NguyenTatKhoa_2380601071RepositoryBase(IDbContextProvider<NguyenTatKhoa_2380601071DbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    // Add your common methods for all repositories
}

/// <summary>
/// Base class for custom repositories of the application.
/// This is a shortcut of <see cref="NguyenTatKhoa_2380601071RepositoryBase{TEntity,TPrimaryKey}"/> for <see cref="int"/> primary key.
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
public abstract class NguyenTatKhoa_2380601071RepositoryBase<TEntity> : NguyenTatKhoa_2380601071RepositoryBase<TEntity, int>, IRepository<TEntity>
    where TEntity : class, IEntity<int>
{
    protected NguyenTatKhoa_2380601071RepositoryBase(IDbContextProvider<NguyenTatKhoa_2380601071DbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    // Do not add any method here, add to the class above (since this inherits it)!!!
}
