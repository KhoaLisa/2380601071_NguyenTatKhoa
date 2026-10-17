using Abp.Zero.EntityFrameworkCore;
using NguyenTatKhoa_2380601071.Authorization.Roles;
using NguyenTatKhoa_2380601071.Authorization.Users;
using NguyenTatKhoa_2380601071.MultiTenancy;
using NguyenTatKhoa_2380601071.Toeic;
using Microsoft.EntityFrameworkCore;

namespace NguyenTatKhoa_2380601071.EntityFrameworkCore;

public class NguyenTatKhoa_2380601071DbContext : AbpZeroDbContext<Tenant, Role, User, NguyenTatKhoa_2380601071DbContext>
{
    public DbSet<Exam> Exams { get; set; }
    public DbSet<PassageGroup> PassageGroups { get; set; }
    public DbSet<ContentBlock> ContentBlocks { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<QuestionOption> QuestionOptions { get; set; }
    public DbSet<ExamAttempt> ExamAttempts { get; set; }
    public DbSet<AttemptQuestion> AttemptQuestions { get; set; }
    public DbSet<ExamPart> ExamParts { get; set; }
    public DbSet<Passage> Passages { get; set; }
    public DbSet<AttemptChoice> AttemptChoices { get; set; }

    public NguyenTatKhoa_2380601071DbContext(DbContextOptions<NguyenTatKhoa_2380601071DbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.ToTable("ToeicExams", table =>
                table.HasCheckConstraint("CK_ToeicExams_DurationMinutes", "[DurationMinutes] > 0"));
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => new { x.Status, x.PublishedAtUtc });
            entity.HasIndex(x => x.SourceHash);
            entity.ToTable("ToeicExams", t => t.HasCheckConstraint("CK_ToeicExams_Status", "[Status] IN (0,1,2)"));
        });

        modelBuilder.Entity<ExamPart>(entity =>
        {
            entity.ToTable("ToeicExamParts", t => t.HasCheckConstraint("CK_ToeicExamParts_Values", "[PartNumber] IN (5,6,7) AND [DisplayOrder] > 0"));
            entity.HasIndex(x => new { x.ExamId, x.PartNumber }).IsUnique();
            entity.HasIndex(x => new { x.ExamId, x.DisplayOrder }).IsUnique();
            entity.HasAlternateKey(x => new { x.Id, x.ExamId, x.PartNumber });
            entity.HasOne(x => x.Exam).WithMany(x => x.Parts).HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PassageGroup>(entity =>
        {
            entity.ToTable("ToeicPassageGroups");
            entity.ToTable("ToeicPassageGroups", t => t.HasCheckConstraint("CK_ToeicPassageGroups_Values", "[DisplayOrder] > 0 AND [Part] IN (6,7) AND [FirstQuestionNumber] <= [LastQuestionNumber]"));
            entity.HasAlternateKey(x => new { x.Id, x.ExamId, x.ExamPartId });
            entity.HasIndex(x => new { x.ExamId, x.ExternalId }).IsUnique();
            entity.HasIndex(x => new { x.ExamId, x.DisplayOrder }).IsUnique();
            entity.HasOne(x => x.Exam).WithMany(x => x.PassageGroups)
                .HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ExamPart).WithMany(x => x.Groups)
                .HasForeignKey(x => new { x.ExamPartId, x.ExamId, x.Part })
                .HasPrincipalKey(x => new { x.Id, x.ExamId, x.PartNumber }).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Passage>(entity =>
        {
            entity.ToTable("ToeicPassages", t => t.HasCheckConstraint("CK_ToeicPassages_Order", "[DisplayOrder] > 0"));
            entity.HasIndex(x => new { x.PassageGroupId, x.SourcePassageId }).IsUnique();
            entity.HasIndex(x => new { x.PassageGroupId, x.DisplayOrder }).IsUnique();
            entity.HasAlternateKey(x => new { x.Id, x.PassageGroupId });
            entity.HasOne(x => x.PassageGroup).WithMany(x => x.Passages).HasForeignKey(x => x.PassageGroupId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ContentBlock>(entity =>
        {
            entity.ToTable("ToeicContentBlocks");
            entity.ToTable("ToeicContentBlocks", t =>
            {
                t.HasCheckConstraint("CK_ToeicContentBlocks_Order", "[DisplayOrder] > 0");
                t.HasCheckConstraint("CK_ToeicContentBlocks_Content", "([Type]=2 AND [BinaryData] IS NOT NULL AND DATALENGTH([BinaryData])>0 AND [MediaType] IS NOT NULL AND [MediaType] LIKE 'image/%' AND [TextContent] IS NULL) OR ([Type] IN (1,3) AND [TextContent] IS NOT NULL AND [BinaryData] IS NULL)");
            });
            entity.HasIndex(x => new { x.PassageId, x.SourceBlockId }).IsUnique();
            entity.HasIndex(x => new { x.PassageId, x.DisplayOrder }).IsUnique();
            entity.HasIndex(x => new { x.PassageGroupId, x.DisplayOrder }).IsUnique();
            entity.HasOne(x => x.PassageGroup).WithMany(x => x.ContentBlocks)
                .HasForeignKey(x => x.PassageGroupId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Passage).WithMany(x => x.Blocks)
                .HasForeignKey(x => new { x.PassageId, x.PassageGroupId })
                .HasPrincipalKey(x => new { x.Id, x.PassageGroupId }).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.ToTable("ToeicQuestions", table =>
                table.HasCheckConstraint("CK_ToeicQuestions_OriginalNumber", "[OriginalNumber] BETWEEN 101 AND 200"));
            entity.HasIndex(x => new { x.ExamId, x.OriginalNumber }).IsUnique();
            entity.HasIndex(x => new { x.ExamId, x.DisplayOrder }).IsUnique();
            entity.HasIndex(x => new { x.ExamId, x.SourceQuestionId }).IsUnique();
            entity.HasAlternateKey(x => new { x.Id, x.ExamId });
            entity.ToTable("ToeicQuestions", t => t.HasCheckConstraint("CK_ToeicQuestions_PartAndGroup", "[DisplayOrder]>0 AND (([Part]=5 AND [OriginalNumber] BETWEEN 101 AND 130 AND [PassageGroupId] IS NULL) OR ([Part]=6 AND [OriginalNumber] BETWEEN 131 AND 146 AND [PassageGroupId] IS NOT NULL) OR ([Part]=7 AND [OriginalNumber] BETWEEN 147 AND 200 AND [PassageGroupId] IS NOT NULL))"));
            entity.HasOne(x => x.Exam).WithMany(x => x.Questions)
                .HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.PassageGroup).WithMany(x => x.Questions)
                .HasForeignKey(x => new { x.PassageGroupId, x.ExamId, x.ExamPartId })
                .HasPrincipalKey(x => new { x.Id, x.ExamId, x.ExamPartId }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ExamPart).WithMany(x => x.Questions)
                .HasForeignKey(x => new { x.ExamPartId, x.ExamId, x.Part })
                .HasPrincipalKey(x => new { x.Id, x.ExamId, x.PartNumber }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CorrectOption).WithMany()
                .HasForeignKey(x => new { x.CorrectOptionId, x.Id })
                .HasPrincipalKey(x => new { x.Id, x.QuestionId }).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QuestionOption>(entity =>
        {
            entity.ToTable("ToeicQuestionOptions");
            entity.ToTable("ToeicQuestionOptions", t => t.HasCheckConstraint("CK_ToeicQuestionOptions_Values", "[Key] IN ('A','B','C','D') AND [DisplayOrder]>0"));
            entity.HasAlternateKey(x => new { x.Id, x.QuestionId });
            entity.HasIndex(x => x.QuestionId).IsUnique().HasFilter("[IsCorrect] = 1").HasDatabaseName("UX_ToeicQuestionOptions_OneCorrect");
            entity.HasIndex(x => new { x.QuestionId, x.Key }).IsUnique();
            entity.HasIndex(x => new { x.QuestionId, x.DisplayOrder }).IsUnique();
            entity.HasOne(x => x.Question).WithMany(x => x.Options)
                .HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExamAttempt>(entity =>
        {
            entity.ToTable("ToeicExamAttempts", table =>
                table.HasCheckConstraint("CK_ToeicExamAttempts_Counts", "[CorrectCount] >= 0 AND [IncorrectCount] >= 0 AND [SkippedCount] >= 0 AND [TotalQuestions] >= 0"));
            entity.Property(x => x.CorrectPercentage).HasPrecision(5, 2);
            entity.HasAlternateKey(x => new { x.Id, x.ExamId });
            entity.ToTable("ToeicExamAttempts", t =>
            {
                t.HasCheckConstraint("CK_ToeicExamAttempts_FinalResult", "[Status] IN (0,1,2) AND [DurationMinutesSnapshot]>0 AND [DueAtUtc]>=[StartedAtUtc] AND [CorrectPercentage] BETWEEN 0 AND 100 AND (([Status]=0 AND [SubmittedAtUtc] IS NULL) OR ([Status] IN (1,2) AND [SubmittedAtUtc] IS NOT NULL AND [CorrectCount]+[IncorrectCount]+[SkippedCount]=[TotalQuestions]))");
            });
            entity.HasIndex(x => new { x.ExamId, x.UserId, x.StartedAtUtc });
            entity.HasIndex(x => new { x.UserId, x.ExamId }).IsUnique().HasFilter("[Status]=0");
            entity.HasIndex(x => new { x.Status, x.DueAtUtc });
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Exam).WithMany(x => x.Attempts)
                .HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AttemptQuestion>(entity =>
        {
            entity.ToTable("ToeicAttemptQuestions");
            entity.ToTable("ToeicAttemptQuestions", t => t.HasCheckConstraint("CK_ToeicAttemptQuestions_Order", "[DisplayOrder]>0"));
            entity.HasAlternateKey(x => new { x.Id, x.QuestionId });
            entity.HasIndex(x => new { x.AttemptId, x.QuestionId }).IsUnique();
            entity.HasIndex(x => new { x.AttemptId, x.DisplayOrder }).IsUnique();
            entity.HasOne(x => x.Attempt).WithMany(x => x.Questions)
                .HasForeignKey(x => new { x.AttemptId, x.ExamId })
                .HasPrincipalKey(x => new { x.Id, x.ExamId }).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Question).WithMany()
                .HasForeignKey(x => new { x.QuestionId, x.ExamId })
                .HasPrincipalKey(x => new { x.Id, x.ExamId }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.SelectedOption).WithMany()
                .HasForeignKey(x => new { x.SelectedOptionId, x.QuestionId })
                .HasPrincipalKey(x => new { x.Id, x.QuestionId }).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AttemptChoice>(entity =>
        {
            entity.ToTable("ToeicAttemptChoices", t => t.HasCheckConstraint("CK_ToeicAttemptChoices_Order", "[DisplayOrder] BETWEEN 1 AND 4"));
            entity.HasIndex(x => new { x.AttemptQuestionId, x.DisplayOrder }).IsUnique();
            entity.HasIndex(x => new { x.AttemptQuestionId, x.ChoiceId }).IsUnique();
            entity.HasOne(x => x.AttemptQuestion).WithMany(x => x.Choices)
                .HasForeignKey(x => new { x.AttemptQuestionId, x.QuestionId })
                .HasPrincipalKey(x => new { x.Id, x.QuestionId }).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Choice).WithMany()
                .HasForeignKey(x => new { x.ChoiceId, x.QuestionId })
                .HasPrincipalKey(x => new { x.Id, x.QuestionId }).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
