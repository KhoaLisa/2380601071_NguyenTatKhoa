using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using Abp.Runtime.Session;
using Microsoft.EntityFrameworkCore;
using NguyenTatKhoa_2380601071.EntityFrameworkCore;
using NguyenTatKhoa_2380601071.Toeic;
using NguyenTatKhoa_2380601071.Toeic.Dto;
using System.Transactions;
using System.Diagnostics;

namespace Toeic.DatabaseCheck;

public static class ConcurrencyChecks
{
    // Independent SQL transactions cannot see an outer uncommitted fixture.
    // Only this exact, uniquely marked temporary exam is committed and later removed.
    public static async Task RunAsync(IIocResolver resolver)
    {
        var examId = Guid.NewGuid();
        long userId;
        try
        {
            using (var setup = resolver.Resolve<IUnitOfWorkManager>().Begin(new UnitOfWorkOptions { Scope = TransactionScopeOption.RequiresNew, IsTransactional = true }))
            {
                var context = await Context(resolver);
                userId = await context.Users.Where(x => x.TenantId == null && x.UserName == "admin").Select(x => x.Id).SingleAsync();
                var exam = SqlServerChecks.CreateExam();
                examId = exam.Id;
                exam.Code = "E2E-CONCURRENCY-" + examId.ToString("N");
                exam.SourceExamId = "e2e-concurrency-check";
                context.Exams.Add(exam);
                await context.SaveChangesAsync();
                foreach (var question in exam.Questions) question.CorrectOptionId = question.Options.Single(x => x.IsCorrect).Id;
                await setup.CompleteAsync();
            }
            var startsTiming = Stopwatch.StartNew();
            var starts = await Task.WhenAll(
                InScope(resolver, userId, async service => await service.StartAsync(new StartExamInput { ExamId = examId })),
                InScope(resolver, userId, async service => await service.StartAsync(new StartExamInput { ExamId = examId })));
            var started = starts[0];
            Require(starts[0].AttemptId == starts[1].AttemptId && starts[0].StartedAtUtc == starts[1].StartedAtUtc && starts[0].DueAtUtc == starts[1].DueAtUtc,
                "two concurrent Starts create or resume exactly the same attempt and deadline");
            Require(started.DueAtUtc - started.StartedAtUtc == TimeSpan.FromMinutes(75), "new attempt receives the full 75 minutes");
            Console.WriteLine($"Concurrent Start pair: {startsTiming.ElapsedMilliseconds} ms.");
            using (var verify = resolver.Resolve<IUnitOfWorkManager>().Begin(new UnitOfWorkOptions { Scope = TransactionScopeOption.RequiresNew, IsTransactional = false }))
            {
                var context = await Context(resolver);
                Require(await context.ExamAttempts.CountAsync(x => x.ExamId == examId && x.Status == AttemptStatus.InProgress) == 1 &&
                    await context.AttemptQuestions.CountAsync(x => x.AttemptId == started.AttemptId) == 100 &&
                    await context.AttemptChoices.CountAsync(x => x.AttemptQuestion.AttemptId == started.AttemptId) == 400,
                    "concurrent Start persists one active attempt with 100 questions and 400 choices");
                await verify.CompleteAsync();
            }
            using (var heldExam = resolver.Resolve<IUnitOfWorkManager>().Begin(new UnitOfWorkOptions { Scope = TransactionScopeOption.RequiresNew, IsTransactional = true }))
            {
                await resolver.Resolve<IToeicTransactionLock>().AcquireAsync("exam:" + examId);
                var resumeTiming = Stopwatch.StartNew();
                var resumed = await InScope(resolver, userId, async service => await service.StartAsync(new StartExamInput { ExamId = examId }));
                Require(resumed.ResumedExistingAttempt && resumed.AttemptId == started.AttemptId &&
                    resumed.StartedAtUtc == started.StartedAtUtc && resumed.DueAtUtc == started.DueAtUtc,
                    "active resume does not wait for the global exam lock or reset the deadline");
                Console.WriteLine($"Resume while exam lock is held: {resumeTiming.ElapsedMilliseconds} ms.");
                await heldExam.CompleteAsync();
            }
            var pair = await Task.WhenAll(
                InScope(resolver, userId, async service => await service.SubmitAsync(new SubmitAttemptInput { AttemptId = started.AttemptId })),
                InScope(resolver, userId, async service => await service.SubmitAsync(new SubmitAttemptInput { AttemptId = started.AttemptId })));
            Require(pair[0].AttemptId == pair[1].AttemptId && pair[0].SubmittedAtUtc == pair[1].SubmittedAtUtc && pair[0].SkippedCount == pair[1].SkippedCount, "two concurrent submits return exactly the same persisted result");

            var expiring = await InScope(resolver, userId, async service => await service.StartAsync(new StartExamInput { ExamId = examId }));
            using (var prepare = resolver.Resolve<IUnitOfWorkManager>().Begin(new UnitOfWorkOptions { Scope = TransactionScopeOption.RequiresNew, IsTransactional = true }))
            {
                var context = await Context(resolver);
                var attempt = await context.ExamAttempts.SingleAsync(x => x.Id == expiring.AttemptId);
                attempt.StartedAtUtc = DateTime.UtcNow.AddMinutes(-76);
                attempt.DueAtUtc = DateTime.UtcNow.AddSeconds(-2);
                await prepare.CompleteAsync();
            }
            // No student request between expiration and collection: prove the worker's collector can finish it.
            using (var collector = resolver.ResolveAsDisposable<ExpiredAttemptCollector>()) await collector.Object.CollectAsync();
            var collected = await InScope(resolver, userId, async service => await service.GetAttemptAsync(new GetAttemptInput { AttemptId = expiring.AttemptId }));
            Require(collected.Status == AttemptStatus.Expired && collected.Result.SkippedCount == 100, "background collector finalizes expiration without a student request");
            var repeated = await InScope(resolver, userId, async service => await service.SubmitAsync(new SubmitAttemptInput { AttemptId = expiring.AttemptId }));
            Require(repeated.SubmittedAtUtc == collected.Result.SubmittedAtUtc, "manual submit after automatic finalization returns the stored result");
        }
        finally
        {
            using var cleanup = resolver.Resolve<IUnitOfWorkManager>().Begin(new UnitOfWorkOptions { Scope = TransactionScopeOption.RequiresNew, IsTransactional = true });
            var context = await Context(resolver);
            var marker = await context.Exams.IgnoreQueryFilters().Where(x => x.Id == examId).Select(x => new { x.Code, x.SourceExamId }).FirstOrDefaultAsync();
            if (marker != null)
            {
                if (marker.SourceExamId != "e2e-concurrency-check" || marker.Code != "E2E-CONCURRENCY-" + examId.ToString("N"))
                    throw new InvalidOperationException("Temporary exam ownership check failed. Cleanup was stopped.");
                await context.Database.ExecuteSqlInterpolatedAsync($@"
DELETE c FROM ToeicAttemptChoices c JOIN ToeicAttemptQuestions q ON q.Id=c.AttemptQuestionId WHERE q.ExamId={examId};
DELETE FROM ToeicAttemptQuestions WHERE ExamId={examId};
DELETE FROM ToeicExamAttempts WHERE ExamId={examId};
UPDATE ToeicQuestions SET CorrectOptionId=NULL WHERE ExamId={examId};
DELETE o FROM ToeicQuestionOptions o JOIN ToeicQuestions q ON q.Id=o.QuestionId WHERE q.ExamId={examId};
DELETE FROM ToeicQuestions WHERE ExamId={examId};
DELETE b FROM ToeicContentBlocks b JOIN ToeicPassageGroups g ON g.Id=b.PassageGroupId WHERE g.ExamId={examId};
DELETE p FROM ToeicPassages p JOIN ToeicPassageGroups g ON g.Id=p.PassageGroupId WHERE g.ExamId={examId};
DELETE FROM ToeicPassageGroups WHERE ExamId={examId};
DELETE FROM ToeicExamParts WHERE ExamId={examId};
DELETE FROM ToeicExams WHERE Id={examId};");
                await cleanup.CompleteAsync();
                Console.WriteLine("Temporary concurrency exam and only its dependent test rows were removed.");
            }
        }
    }

    private static async Task<T> InScope<T>(IIocResolver resolver, long userId, Func<IStudentExamAppService, Task<T>> action)
    {
        using var identity = resolver.Resolve<IAbpSession>().Use(null, userId);
        using var uow = resolver.Resolve<IUnitOfWorkManager>().Begin(new UnitOfWorkOptions { Scope = TransactionScopeOption.RequiresNew, IsTransactional = true });
        using var service = resolver.ResolveAsDisposable<IStudentExamAppService>();
        var result = await action(service.Object);
        await uow.CompleteAsync();
        return result;
    }
    private static Task<NguyenTatKhoa_2380601071DbContext> Context(IIocResolver resolver) => resolver.Resolve<IDbContextProvider<NguyenTatKhoa_2380601071DbContext>>().GetDbContextAsync();
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException("FAILED: " + message); Console.WriteLine("PASS: " + message); }
}
