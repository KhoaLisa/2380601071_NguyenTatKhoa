using Abp.UI;
using NguyenTatKhoa_2380601071.Toeic;
using Shouldly;
using System;
using System.Linq;
using Xunit;

namespace NguyenTatKhoa_2380601071.Tests.Toeic;

public class ExamManagementRules_Tests
{
    [Fact]
    public void Image_editor_accepts_png_and_rejects_svg_mismatched_or_oversized_uploads()
    {
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jgWQAAAAASUVORK5CYII=");
        ExamImageValidator.GetMediaType(png, "passage.png").ShouldBe("image/png");
        Should.Throw<UserFriendlyException>(() => ExamImageValidator.GetMediaType(png, "passage.jpg"));
        Should.Throw<UserFriendlyException>(() => ExamImageValidator.GetMediaType(System.Text.Encoding.UTF8.GetBytes("<svg onload='alert(1)'/>"), "passage.svg"));
        Should.Throw<UserFriendlyException>(() => ExamImageValidator.GetMediaType(new byte[ExamImageValidator.MaxBytes + 1], "large.png"));
        Should.Throw<UserFriendlyException>(() => ExamImageValidator.GetMediaType(Array.Empty<byte>(), "empty.png"));
    }

    [Fact]
    public void Draft_copy_remaps_every_relationship_and_never_shares_image_bytes_or_results()
    {
        var source = CreateSource();
        var copy = ExamDraftFactory.Create(source, "copy-code", "Copied draft");
        copy.Id.ShouldNotBe(source.Id);
        copy.Status.ShouldBe(ExamStatus.Draft);
        copy.IsLocked.ShouldBeFalse();
        copy.PublishedAtUtc.ShouldBeNull();
        copy.Attempts.ShouldBeEmpty();
        copy.Parts.Single().Id.ShouldNotBe(source.Parts.Single().Id);
        var group = copy.PassageGroups.Single();
        group.Id.ShouldNotBe(source.PassageGroups.Single().Id);
        group.ExamId.ShouldBe(copy.Id);
        group.ExamPartId.ShouldBe(copy.Parts.Single().Id);
        var passage = group.Passages.Single();
        passage.Id.ShouldNotBe(source.PassageGroups.Single().Passages.Single().Id);
        passage.PassageGroupId.ShouldBe(group.Id);
        var block = group.ContentBlocks.Single();
        block.PassageId.ShouldBe(passage.Id);
        block.PassageGroupId.ShouldBe(group.Id);
        block.SourceBlockId.ShouldBe("image-source-id");
        block.SourcePage.ShouldBe(9);
        block.BinaryData[0] = 100;
        source.PassageGroups.Single().ContentBlocks.Single().BinaryData[0].ShouldBe((byte)1);
        var question = copy.Questions.Single();
        question.Id.ShouldNotBe(source.Questions.Single().Id);
        question.ExamId.ShouldBe(copy.Id);
        question.PassageGroupId.ShouldBe(group.Id);
        question.ExamPartId.ShouldBe(copy.Parts.Single().Id);
        question.CorrectOptionId.ShouldBe(question.Options.Single(x => x.IsCorrect).Id);
        question.Options.ShouldAllBe(x => x.QuestionId == question.Id);
        question.Options.Select(x => x.Id).Intersect(source.Questions.Single().Options.Select(x => x.Id)).ShouldBeEmpty();
        question.Options.First().Text = "changed";
        source.Questions.Single().Options.First().Text.ShouldBe("Option A");
    }

    [Theory]
    [InlineData("<table onclick=\"alert(1)\"><tr><td>text</td></tr></table>")]
    [InlineData("<table><tr><td><script>alert(1)</script></td></tr></table>")]
    [InlineData("<table><tr><td><img src=\"x\" onerror=\"alert(1)\" /></td></tr></table>")]
    [InlineData("<!DOCTYPE table [<!ENTITY attack SYSTEM 'file:///secret'>]><table>&attack;</table>")]
    public void Table_editor_rejects_executable_content_and_external_entities(string html)
    {
        Should.Throw<UserFriendlyException>(() => TableContentSanitizer.Normalize(html));
    }

    [Fact]
    public void Table_editor_preserves_text_and_safe_row_column_spans()
    {
        var html = TableContentSanitizer.Normalize("<table><tbody><tr><td colspan=\"2\">Email &amp; notice</td></tr></tbody></table>");
        html.ShouldContain("colspan=\"2\"");
        html.ShouldContain("Email &amp; notice");
    }

    [Fact]
    public void Publication_checks_group_counts_and_requires_verified_hints()
    {
        var source = CreateSource();
        var errors = ExamStructureValidator.Validate(source, true);
        errors.ShouldContain(x => x.Code == "OFFICIAL_GROUP_COUNT");
        errors.ShouldContain(x => x.Code == "OFFICIAL_QUESTION_SET");
        source.Questions.Single().Hint = "";
        ExamStructureValidator.Validate(source, true).ShouldContain(x => x.Code == "INVALID_HINT");
    }

    private static Exam CreateSource()
    {
        var exam = new Exam
        {
            Id = Guid.NewGuid(), Code = "source", Title = "Source", SourceExamId = "source-id",
            IsLocked = true, Status = ExamStatus.Published, PublishedAtUtc = DateTime.UtcNow,
            Directions = "Instructions", DeclaredQuestionCount = 1, SourceHash = new string('A', 64)
        };
        var part = new ExamPart { Id = Guid.NewGuid(), ExamId = exam.Id, PartNumber = ToeicPart.Part7, DisplayOrder = 1, Directions = "Part instructions" };
        exam.Parts.Add(part);
        var group = new PassageGroup { Id = Guid.NewGuid(), ExamId = exam.Id, ExamPartId = part.Id, Part = ToeicPart.Part7, ExternalId = "group", FirstQuestionNumber = 147, LastQuestionNumber = 147, DisplayOrder = 1 };
        exam.PassageGroups.Add(group);
        var passage = new Passage { Id = Guid.NewGuid(), PassageGroupId = group.Id, SourcePassageId = "passage", Kind = "email", DisplayOrder = 1 };
        group.Passages.Add(passage);
        group.ContentBlocks.Add(new ContentBlock
        {
            Id = Guid.NewGuid(), PassageGroupId = group.Id, PassageId = passage.Id, SourceBlockId = "image-source-id",
            DocumentKey = "passage", SourcePage = 9, DisplayOrder = 1, Type = ContentBlockType.Image,
            BinaryData = new byte[] { 1, 2, 3 }, MediaType = "image/png"
        });
        var question = new Question { Id = Guid.NewGuid(), ExamId = exam.Id, ExamPartId = part.Id, PassageGroupId = group.Id, SourceQuestionId = "q147", Part = ToeicPart.Part7, OriginalNumber = 147, DisplayOrder = 1, Prompt = "Question?", Hint = "Source hint", AllowOptionShuffle = true };
        exam.Questions.Add(question);
        for (var index = 0; index < 4; index++)
        {
            var option = new QuestionOption { Id = Guid.NewGuid(), QuestionId = question.Id, Key = ((char)('A' + index)).ToString(), Text = "Option " + (char)('A' + index), DisplayOrder = index + 1, IsCorrect = index == 2 };
            question.Options.Add(option);
            if (option.IsCorrect) question.CorrectOptionId = option.Id;
        }
        exam.Attempts.Add(new ExamAttempt { Id = Guid.NewGuid(), ExamId = exam.Id, UserId = 1, Status = AttemptStatus.Submitted });
        return exam;
    }
}
