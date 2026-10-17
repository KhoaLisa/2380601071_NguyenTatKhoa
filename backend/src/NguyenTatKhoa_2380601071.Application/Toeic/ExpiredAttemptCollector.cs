using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.Threading.BackgroundWorkers;
using Abp.Threading.Timers;
using Castle.Core.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace NguyenTatKhoa_2380601071.Toeic;

public class ExpiredAttemptCollector : ITransientDependency
{
    private readonly IUnitOfWorkManager _uow;
    private readonly IRepository<ExamAttempt, Guid> _attempts;
    private readonly IToeicTransactionLock _transactionLock;
    private readonly AttemptFinalizer _finalizer;
    private readonly IAbpSession _session;
    public ILogger Logger { get; set; } = NullLogger.Instance;

    public ExpiredAttemptCollector(IUnitOfWorkManager uow, IRepository<ExamAttempt, Guid> attempts,
        IToeicTransactionLock transactionLock, AttemptFinalizer finalizer, IAbpSession session)
    { _uow = uow; _attempts = attempts; _transactionLock = transactionLock; _finalizer = finalizer; _session = session; }

    public async Task<int> CollectAsync()
    {
        using var hostSession = _session.Use(null, null);
        List<Guid> ids;
        using (var read = _uow.Begin(new UnitOfWorkOptions { Scope = TransactionScopeOption.RequiresNew, IsTransactional = false }))
        {
            var now = DateTime.UtcNow;
            ids = await _attempts.GetAll().AsNoTracking().Where(x => x.Status == AttemptStatus.InProgress && x.DueAtUtc <= now)
                .OrderBy(x => x.DueAtUtc).Select(x => x.Id).Take(100).ToListAsync();
            await read.CompleteAsync();
        }
        var finalized = 0;
        foreach (var id in ids)
        {
            try
            {
                using var write = _uow.Begin(new UnitOfWorkOptions { Scope = TransactionScopeOption.RequiresNew, IsTransactional = true, IsolationLevel = IsolationLevel.ReadCommitted });
                await _transactionLock.AcquireAsync("attempt:" + id);
                var attempt = await _attempts.FirstOrDefaultAsync(id);
                if (attempt != null && attempt.Status == AttemptStatus.InProgress && attempt.DueAtUtc <= DateTime.UtcNow)
                {
                    await _finalizer.FinalizeAsync(attempt, AttemptStatus.Expired, attempt.DueAtUtc);
                    finalized++;
                }
                await write.CompleteAsync();
            }
            catch (Exception error) { Logger.Error("Cannot finalize an expired TOEIC attempt; the next sweep will retry.", error); }
        }
        return finalized;
    }
}

public class ExpiredAttemptWorker : AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly IIocResolver _resolver;
    public ExpiredAttemptWorker(AbpAsyncTimer timer, IIocResolver resolver) : base(timer)
    { Timer.Period = 15000; Timer.RunOnStart = true; _resolver = resolver; }

    protected override async Task DoWorkAsync()
    {
        using var collector = _resolver.ResolveAsDisposable<ExpiredAttemptCollector>();
        await collector.Object.CollectAsync();
    }
}
