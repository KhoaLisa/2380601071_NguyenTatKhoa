using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NguyenTatKhoa_2380601071.EntityFrameworkCore;
using System.Text.Json;

namespace Toeic.DatabaseCheck;

public static class UiAttemptCleanup
{
    public static async Task RunAsync(IIocResolver resolver, bool currentOnly = false)
    {
        // Only explicit attempt IDs recorded by our browser test are in scope.
        // The exam and any user's other attempts are always retained.
        using var state = JsonDocument.Parse(await File.ReadAllTextAsync("docs/testing/evidence/browser-state.json"));
        var root = state.RootElement;
        var examId = root.GetProperty("uiExamId").GetGuid();
        var sourceId = root.GetProperty("examId").GetGuid();
        var code = root.GetProperty("uiCode").GetString();
        if (examId == sourceId || code == null || !code.StartsWith("E2E-UI-")) throw new InvalidOperationException("Unsafe UI cleanup target.");
        var ids = currentOnly ? new[] { root.GetProperty("attemptId").GetGuid() }
            : root.GetProperty("uiAttemptIds").EnumerateArray().Select(x => x.GetGuid()).Distinct().ToArray();
        if (currentOnly && !root.GetProperty("uiAttemptIds").EnumerateArray().Any(x => x.GetGuid() == ids[0]))
            throw new InvalidOperationException("Current attempt was not recorded by the UI test.");
        var preserved = root.TryGetProperty("preservedAttemptIds", out var values)
            ? values.EnumerateArray().Select(x => x.GetGuid()).ToArray() : Array.Empty<Guid>();
        if (ids.Intersect(preserved).Any()) throw new InvalidOperationException("A preserved attempt was included in cleanup.");
        using var uow = resolver.Resolve<IUnitOfWorkManager>().Begin(new UnitOfWorkOptions { IsTransactional = true });
        var context = await resolver.Resolve<IDbContextProvider<NguyenTatKhoa_2380601071DbContext>>().GetDbContextAsync();
        var exam = await context.Exams.AsNoTracking().SingleAsync(x => x.Id == examId);
        if (exam.Code != code || !exam.Title.StartsWith("E2E Browser")) throw new InvalidOperationException("UI exam ownership check failed.");
        var existing = await context.ExamAttempts.AsNoTracking().Where(x => ids.Contains(x.Id)).ToArrayAsync();
        if (existing.Any(x => x.ExamId != examId)) throw new InvalidOperationException("An attempt does not belong to the exact UI test exam.");
        foreach (var attempt in existing)
        {
            await context.Database.ExecuteSqlInterpolatedAsync($@"
DELETE c FROM ToeicAttemptChoices c JOIN ToeicAttemptQuestions q ON q.Id=c.AttemptQuestionId WHERE q.AttemptId={attempt.Id};
DELETE FROM ToeicAttemptQuestions WHERE AttemptId={attempt.Id};
DELETE FROM ToeicExamAttempts WHERE Id={attempt.Id} AND ExamId={examId};");
        }
        await uow.CompleteAsync();
        Console.WriteLine($"Removed {existing.Length} explicitly recorded automated UI attempts. The exam and all other attempts were retained.");
    }
}
