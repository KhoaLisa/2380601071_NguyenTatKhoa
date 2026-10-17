using NguyenTatKhoa_2380601071.Toeic;
using NguyenTatKhoa_2380601071.Toeic.Importing;
using Shouldly;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Xunit;

namespace NguyenTatKhoa_2380601071.Tests.Toeic;

public class V07DocxExamParser_Tests
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private readonly DocxExamParser _parser = new DocxExamParser();
    private static byte[] Source => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "SourceFixtures", "TOEIC_Reading_v07_co_tag.docx"));

    [Fact]
    public void Reads_actual_v07_word_preserving_all_content_and_source_identity()
    {
        var parsed = _parser.Parse(Source);
        parsed.Errors.ShouldBeEmpty();
        parsed.Code.ShouldBe("reading_v07");
        parsed.TagDialect.ShouldBe("v07");
        parsed.Title.ShouldBe("Reading v07");
        parsed.DurationMinutes.ShouldBe(75);
        parsed.DeclaredQuestionCount.ShouldBe(100);
        parsed.Parts.Select(x => (int)x.Number).ShouldBe(new[] { 5, 6, 7 });
        parsed.Parts.ShouldAllBe(x => !string.IsNullOrWhiteSpace(x.Directions) && !x.AllowShuffleGroups);
        parsed.Groups.Count.ShouldBe(19);
        parsed.Groups.Count(x => x.Part == ToeicPart.Part6).ShouldBe(4);
        parsed.Groups.Count(x => x.Part == ToeicPart.Part7).ShouldBe(15);
        parsed.Groups.Sum(x => x.Passages.Count).ShouldBe(27);
        parsed.Groups.SelectMany(x => x.ContentBlocks).Count().ShouldBe(27);
        parsed.Groups.SelectMany(x => x.ContentBlocks).ShouldAllBe(x => x.BinaryData.Length > 0 && x.MediaType == "image/png" && x.SourcePage > 0 && x.SourceBlockId.EndsWith("_img"));
        parsed.Questions.Select(x => x.OriginalNumber).ShouldBe(Enumerable.Range(101, 100));
        parsed.Questions.Sum(x => x.Options.Count).ShouldBe(400);
        parsed.Questions.ShouldAllBe(x => !string.IsNullOrWhiteSpace(x.Hint) && x.SourceQuestionId == "q" + x.OriginalNumber);
        parsed.Questions.Single(x => x.OriginalNumber == 101).CorrectOptionKey.ShouldBe("A");
        parsed.Questions.Single(x => x.OriginalNumber == 102).CorrectOptionKey.ShouldBe("D");
        parsed.Questions.Where(x => !x.AllowOptionShuffle).Select(x => x.OriginalNumber).ShouldBe(new[] { 163, 167 });
        parsed.Groups.Where(x => x.Part == ToeicPart.Part6).SelectMany(x => x.Passages).SelectMany(x => x.BlankQuestionNumbers).ShouldBe(Enumerable.Range(131, 16));
        parsed.Groups.Single(x => x.ExternalId == "g196_200").Passages.Select(x => x.Kind).ShouldBe(new[] { "email", "flyer", "schedule" });
    }

    [Fact]
    public void Joins_split_run_tags_and_decodes_entities_once_without_treating_positions_as_tags()
    {
        var bytes = Mutate(document =>
        {
            var paragraph = Paragraph(document, "[QUESTION id=\"q101\"");
            var text = Text(paragraph);
            paragraph.RemoveNodes();
            paragraph.Add(new XElement(W + "r", new XElement(W + "t", text.Substring(0, 11))));
            paragraph.Add(new XElement(W + "r", new XElement(W + "t", text.Substring(11))));
            SetText(Paragraph(document, "[STEM] If"), "[STEM] [1] [2] [3] [4] &#91;A&#93; &amp; &amp;#91; [/STEM]");
        });
        var parsed = _parser.Parse(bytes);
        parsed.Errors.ShouldBeEmpty();
        parsed.Questions.First().Prompt.ShouldBe("[1] [2] [3] [4] [A] & &#91;");
    }

    [Theory]
    [InlineData("[QUESTION id=\"q101\"", " shuffle_options=\"true\"", "", "MISSING_ATTRIBUTE")]
    [InlineData("[QUESTION id=\"q101\"", "shuffle=\"true\"", "shuffle=\"True\"", "INVALID_BOOLEAN")]
    [InlineData("[QUESTION id=\"q101\"", "id=\"q101\"", "id=\"q102\"", "DUPLICATE_ID")]
    [InlineData("[QUESTION id=\"q163\"", "shuffle_options=\"false\"", "shuffle_options=\"true\"", "POSITION_OPTION_SHUFFLE")]
    [InlineData("[CORRECT] A", "[CORRECT] A [/CORRECT]", "[CORRECT] E [/CORRECT]", "INVALID_CORRECT")]
    [InlineData("[BLANKS] 131", "131,132,133,134", "131,132,133,135", "INVALID_BLANK_REFERENCE")]
    [InlineData("[GROUP id=\"g131_134\"", "last=\"134\"", "last=\"135\"", "GROUP_QUESTION_RANGE")]
    [InlineData("[PART number=\"6\"", "number=\"6\"", "number=\"7\"", "INVALID_PART_ORDER")]
    [InlineData("[IMAGE id=\"g131_134", "source_page=\"5\"", "source_page=\"0\"", "INVALID_SOURCE_PAGE")]
    public void Rejects_invalid_actual_v07_fields(string prefix, string oldValue, string newValue, string errorCode)
    {
        var bytes = Mutate(document =>
        {
            var paragraph = Paragraph(document, prefix);
            SetText(paragraph, Text(paragraph).Replace(oldValue, newValue));
        });
        var parsed = _parser.Parse(bytes);
        parsed.IsValid.ShouldBeFalse();
        parsed.Errors.ShouldContain(x => x.Code == errorCode);
        parsed.Errors.ShouldNotContain(x => x.Code == "PARSE_FAILURE");
    }

    [Fact]
    public void Rejects_missing_hint_and_duplicate_option_with_useful_errors()
    {
        var bytes = Mutate(document =>
        {
            Paragraph(document, "[HINT]").Remove();
            SetText(Paragraph(document, "[B] your"), "[A] your [/A]");
        });
        var parsed = _parser.Parse(bytes);
        parsed.IsValid.ShouldBeFalse();
        parsed.Errors.ShouldContain(x => x.Code == "INVALID_QUESTION_FIELD_ORDER");
        parsed.Errors.ShouldContain(x => x.Code == "INVALID_HINT");
        parsed.Errors.ShouldContain(x => x.Code == "INVALID_OPTIONS");
    }

    [Fact]
    public void Rejects_mismatched_closing_tags()
    {
        var bytes = Mutate(document => SetText(Paragraph(document, "[/QUESTION]"), "[/PART]"));
        _parser.Parse(bytes).Errors.ShouldContain(x => x.Code == "MISMATCHED_CLOSE");
    }

    [Fact]
    public void Rejects_missing_embedded_image_and_external_image_relationship()
    {
        var missing = Mutate(_ => { }, skipEntry: "word/media/image1.png");
        _parser.Parse(missing).Errors.ShouldContain(x => x.Code == "MISSING_EMBEDDED_IMAGE");
        var external = Mutate(_ => { }, relationshipChange: document =>
        {
            var image = document.Descendants().First(x => ((string)x.Attribute("Type") ?? "").EndsWith("/image"));
            image.SetAttributeValue("TargetMode", "External");
            image.SetAttributeValue("Target", "https://example.invalid/image.png");
        });
        _parser.Parse(external).Errors.ShouldContain(x => x.Code == "BROKEN_IMAGE_RELATIONSHIP");
    }

    [Fact]
    public void Rejects_more_than_one_image_inside_an_image_tag()
    {
        var bytes = Mutate(document =>
        {
            var drawing = document.Descendants(W + "drawing").First();
            drawing.Parent.Add(new XElement(drawing));
        });
        var parsed = _parser.Parse(bytes);
        parsed.Errors.ShouldContain(x => x.Code == "INVALID_IMAGE_PARAGRAPH");
        parsed.Errors.ShouldContain(x => x.Code == "IMAGE_COUNT");
    }

    [Theory]
    [InlineData("corrupt")]
    [InlineData("truncated")]
    [InlineData("svg-disguised")]
    [InlineData("mismatched-mime")]
    [InlineData("active-override-mime")]
    [InlineData("empty")]
    [InlineData("oversized")]
    public void Rejects_unsafe_and_mislabeled_images_in_the_real_word_package(string scenario)
    {
        var bytes = Mutate(_ => { }, imageChange: original => scenario switch
        {
            "corrupt" => new byte[] { 1, 2, 3, 4 },
            "truncated" => original.Take(24).ToArray(),
            "svg-disguised" => Encoding.UTF8.GetBytes("<svg onload='alert(1)'/>"),
            "empty" => Array.Empty<byte>(),
            "oversized" => new byte[ExamImageValidator.MaxBytes + 1],
            _ => original
        }, contentTypeChange: document =>
        {
            if (scenario == "mismatched-mime")
                document.Descendants().First(x => (string)x.Attribute("Extension") == "png").SetAttributeValue("ContentType", "image/jpeg");
            if (scenario == "active-override-mime")
                document.Root.Add(new XElement(document.Root.Name.Namespace + "Override",
                    new XAttribute("PartName", "/word/media/image1.png"), new XAttribute("ContentType", "image/svg+xml")));
        });
        var parsed = _parser.Parse(bytes);
        parsed.IsValid.ShouldBeFalse();
        parsed.Errors.ShouldContain(x => x.Code == "INVALID_IMAGE_BYTES" || x.Code == "MISSING_EMBEDDED_IMAGE");
        parsed.Errors.ShouldNotContain(x => x.Code == "PARSE_FAILURE");
    }

    [Fact]
    public void Does_not_treat_preamble_and_headers_as_exam_content()
    {
        var bytes = Mutate(document =>
        {
            document.Root.Element(W + "body").AddFirst(new XElement(W + "p", new XElement(W + "r", new XElement(W + "t", "A. B. C. D. Instructions outside EXAM [STEM]"))));
        });
        _parser.Parse(bytes).Errors.ShouldBeEmpty();
    }

    private static XElement Paragraph(XDocument document, string prefix) => document.Root.Element(W + "body").Elements(W + "p").First(x => Text(x).TrimStart().StartsWith(prefix, StringComparison.Ordinal));
    private static string Text(XElement paragraph) => string.Concat(paragraph.Descendants(W + "t").Select(x => (string)x));
    private static void SetText(XElement paragraph, string value)
    {
        paragraph.RemoveNodes();
        paragraph.Add(new XElement(W + "r", new XElement(W + "t", value)));
    }
    private static byte[] Mutate(Action<XDocument> change, string skipEntry = null, Action<XDocument> relationshipChange = null,
        Func<byte[], byte[]> imageChange = null, Action<XDocument> contentTypeChange = null)
    {
        using var original = new ZipArchive(new MemoryStream(Source), ZipArchiveMode.Read);
        using var output = new MemoryStream();
        using (var edited = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            foreach (var entry in original.Entries.Where(x => x.FullName != skipEntry))
            {
                var target = edited.CreateEntry(entry.FullName);
                using var input = entry.Open();
                using var targetStream = target.Open();
                if (entry.FullName == "word/document.xml" || (entry.FullName == "word/_rels/document.xml.rels" && relationshipChange != null) ||
                    (entry.FullName == "[Content_Types].xml" && contentTypeChange != null))
                {
                    var document = XDocument.Load(input);
                    if (entry.FullName == "word/document.xml") change(document);
                    else if (entry.FullName == "[Content_Types].xml") contentTypeChange(document);
                    else relationshipChange(document);
                    document.Save(targetStream);
                }
                else if (entry.FullName == "word/media/image1.png" && imageChange != null)
                {
                    using var image = new MemoryStream();
                    input.CopyTo(image);
                    targetStream.Write(imageChange(image.ToArray()));
                }
                else input.CopyTo(targetStream);
            }
        }
        return output.ToArray();
    }
}
