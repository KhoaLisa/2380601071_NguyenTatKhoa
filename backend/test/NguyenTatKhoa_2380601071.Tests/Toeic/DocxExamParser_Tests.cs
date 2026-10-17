using NguyenTatKhoa_2380601071.Toeic;
using NguyenTatKhoa_2380601071.Toeic.Importing;
using Shouldly;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Xunit;

namespace NguyenTatKhoa_2380601071.Tests.Toeic;

public class DocxExamParser_Tests
{
    private readonly DocxExamParser _parser = new DocxExamParser();

    [Fact]
    public void Parses_split_run_tags_table_and_embedded_image()
    {
        var document = """
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
              <w:body>
                <w:p><w:r><w:t>[[TOEIC-</w:t></w:r><w:r><w:t>READING version="1.0"]]</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[EXAM code="TEST-PARSER" title="Dữ liệu kiểm thử" duration="10" official="false"]]</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[PART number="5"]]</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[QUESTION number="101" shuffleOptions="true"]]</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[PROMPT]]The office is ___ today.</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[OPTION key="A"]]open</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[OPTION key="B"]]opening</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[OPTION key="C"]]opened</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[OPTION key="D"]]opens</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[ANSWER key="A"]]</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[HINT]]Dữ liệu thử, không phải đề thật.</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[/QUESTION]]</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[/PART]]</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[PART number="7"]]</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[GROUP id="g7" title="Nhóm kiểm thử"]]</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[DOCUMENT id="doc-1"]]</w:t></w:r></w:p>
                <w:p><w:r><w:t>Sample passage.</w:t></w:r></w:p>
                <w:tbl><w:tr><w:tc><w:p><w:r><w:t>Cell A</w:t></w:r></w:p></w:tc></w:tr></w:tbl>
                <w:p><w:r><w:drawing><a:blip r:embed="rIdImage1"/></w:drawing></w:r></w:p>
                <w:p><w:r><w:t>[[/DOCUMENT]]</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[QUESTION number="147"]]</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[PROMPT]]What is stated?</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[OPTION key="A"]]A</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[OPTION key="B"]]B</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[OPTION key="C"]]C</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[OPTION key="D"]]D</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[ANSWER key="B"]]</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[/QUESTION]]</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[/GROUP]]</w:t></w:r></w:p>
                <w:p><w:r><w:t>[[/PART]]</w:t></w:r></w:p>
              </w:body>
            </w:document>
            """;

        var parsed = _parser.Parse(CreateDocx(document, includeImage: true));

        parsed.Errors.ShouldBeEmpty();
        parsed.Code.ShouldBe("TEST-PARSER");
        parsed.Questions.Count.ShouldBe(2);
        parsed.Questions[0].AllowOptionShuffle.ShouldBeTrue();
        parsed.Groups.Single().ContentBlocks.Select(x => x.Type)
            .ShouldBe(new[] { ContentBlockType.Text, ContentBlockType.Table, ContentBlockType.Image });
        parsed.Groups.Single().ContentBlocks.Last().BinaryData.ShouldBe(SafeRasterFixtures.Png);
        parsed.Groups.Single().ContentBlocks.Last().MediaType.ShouldBe("image/png");
    }

    [Fact]
    public void Rejects_official_exam_when_question_set_is_incomplete()
    {
        var document = """
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>
              <w:p><w:r><w:t>[[TOEIC-READING version="1.0"]]</w:t></w:r></w:p>
              <w:p><w:r><w:t>[[EXAM code="BAD-OFFICIAL" title="Không đầy đủ" duration="75" official="true"]]</w:t></w:r></w:p>
              <w:p><w:r><w:t>[[PART number="5"]]</w:t></w:r></w:p>
              <w:p><w:r><w:t>[[QUESTION number="101"]]</w:t></w:r></w:p>
              <w:p><w:r><w:t>[[PROMPT]]Prompt</w:t></w:r></w:p>
              <w:p><w:r><w:t>[[OPTION key="A"]]A</w:t></w:r></w:p>
              <w:p><w:r><w:t>[[OPTION key="B"]]B</w:t></w:r></w:p>
              <w:p><w:r><w:t>[[OPTION key="C"]]C</w:t></w:r></w:p>
              <w:p><w:r><w:t>[[OPTION key="D"]]D</w:t></w:r></w:p>
              <w:p><w:r><w:t>[[ANSWER key="A"]]</w:t></w:r></w:p>
              <w:p><w:r><w:t>[[/QUESTION]]</w:t></w:r></w:p>
              <w:p><w:r><w:t>[[/PART]]</w:t></w:r></w:p>
            </w:body></w:document>
            """;

        var parsed = _parser.Parse(CreateDocx(document));

        parsed.IsValid.ShouldBeFalse();
        parsed.Errors.ShouldContain(x => x.Code == "OFFICIAL_QUESTION_SET");
    }

    [Fact]
    public void Does_not_treat_plain_abcd_text_as_options()
    {
        var document = """
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>
              <w:p><w:r><w:t>[[TOEIC-READING version="1.0"]]</w:t></w:r></w:p>
              <w:p><w:r><w:t>[[EXAM code="PLAIN-ABCD" title="Plain text" duration="10" official="false"]]</w:t></w:r></w:p>
              <w:p><w:r><w:t>[[PART number="7"]]</w:t></w:r></w:p>
              <w:p><w:r><w:t>[[GROUP id="g"]]</w:t></w:r></w:p>
              <w:p><w:r><w:t>[[DOCUMENT id="d"]]</w:t></w:r></w:p>
              <w:p><w:r><w:t>A. alpha B. beta C. gamma D. delta</w:t></w:r></w:p>
              <w:p><w:r><w:t>[[/DOCUMENT]]</w:t></w:r></w:p>
              <w:p><w:r><w:t>[[/GROUP]]</w:t></w:r></w:p>
              <w:p><w:r><w:t>[[/PART]]</w:t></w:r></w:p>
            </w:body></w:document>
            """;

        var parsed = _parser.Parse(CreateDocx(document));

        parsed.Questions.ShouldBeEmpty();
        parsed.Groups.Single().ContentBlocks.Single().TextContent.ShouldContain("A. alpha");
    }

    [Theory]
    [InlineData("corrupt")]
    [InlineData("truncated")]
    [InlineData("svg")]
    [InlineData("html-disguised")]
    [InlineData("mismatched-extension")]
    [InlineData("mismatched-mime")]
    [InlineData("active-mime")]
    [InlineData("active-override-mime")]
    [InlineData("empty")]
    [InlineData("oversized")]
    public void Legacy_import_rejects_unsafe_invalid_and_mislabeled_images(string scenario)
    {
        var bytes = SafeRasterFixtures.Png;
        var name = "test.png";
        var mime = "image/png";
        string overrideMime = null;
        switch (scenario)
        {
            case "corrupt": bytes = new byte[] { 1, 2, 3, 4 }; break;
            case "truncated": bytes = bytes.Take(24).ToArray(); break;
            case "svg": bytes = Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg' onload='alert(1)'/>"); name = "test.svg"; mime = "image/svg+xml"; break;
            case "html-disguised": bytes = Encoding.UTF8.GetBytes("<!doctype html><script>alert(1)</script>"); break;
            case "mismatched-extension": name = "test.jpg"; mime = "image/jpeg"; break;
            case "mismatched-mime": mime = "image/jpeg"; break;
            case "active-mime": mime = "image/svg+xml"; break;
            case "active-override-mime": overrideMime = "text/html"; break;
            case "empty": bytes = Array.Empty<byte>(); break;
            case "oversized": bytes = new byte[ExamImageValidator.MaxBytes + 1]; break;
        }
        var parsed = _parser.Parse(CreateDocx(ImageDocument, true, bytes, name, mime, overrideMime));
        parsed.Errors.ShouldContain(x => x.Code == "INVALID_IMAGE_BYTES");
        parsed.Errors.ShouldNotContain(x => x.Code == "PARSE_FAILURE");
        parsed.Groups.SelectMany(x => x.ContentBlocks).ShouldNotContain(x => x.Type == ContentBlockType.Image);
    }

    [Fact]
    public void Legacy_import_accepts_a_safe_image_with_an_explicit_content_type_override()
    {
        var parsed = _parser.Parse(CreateDocx(ImageDocument, true, overrideMediaType: "image/png"));
        parsed.Errors.ShouldNotContain(x => x.Code == "INVALID_IMAGE_BYTES");
        parsed.Groups.Single().ContentBlocks.Single(x => x.Type == ContentBlockType.Image).MediaType.ShouldBe("image/png");
    }

    private const string ImageDocument = """
        <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><w:body>
          <w:p><w:r><w:t>[[TOEIC-READING version="1.0"]]</w:t></w:r></w:p>
          <w:p><w:r><w:t>[[EXAM code="IMAGE-TEST" title="Image test" duration="10" official="false"]]</w:t></w:r></w:p>
          <w:p><w:r><w:t>[[PART number="7"]]</w:t></w:r></w:p>
          <w:p><w:r><w:t>[[GROUP id="g"]]</w:t></w:r></w:p>
          <w:p><w:r><w:t>[[DOCUMENT id="d"]]</w:t></w:r></w:p>
          <w:p><w:r><w:t>Test passage.</w:t></w:r></w:p>
          <w:p><w:r><w:drawing><a:blip r:embed="rIdImage1"/></w:drawing></w:r></w:p>
          <w:p><w:r><w:t>[[/DOCUMENT]]</w:t></w:r></w:p>
          <w:p><w:r><w:t>[[/GROUP]]</w:t></w:r></w:p>
          <w:p><w:r><w:t>[[/PART]]</w:t></w:r></w:p>
        </w:body></w:document>
        """;

    private static byte[] CreateDocx(string documentXml, bool includeImage = false, byte[] imageBytes = null,
        string imageFileName = "test.png", string declaredMediaType = "image/png", string overrideMediaType = null)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            var contentTypes = $$"""
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Default Extension="{{Path.GetExtension(imageFileName).TrimStart('.')}}" ContentType="{{declaredMediaType}}"/>
                </Types>
                """;
            if (overrideMediaType != null)
                contentTypes = contentTypes.Replace("</Types>", $"<Override PartName=\"/word/media/{imageFileName}\" ContentType=\"{overrideMediaType}\"/></Types>");
            Write(archive, "[Content_Types].xml", contentTypes);
            Write(archive, "word/document.xml", documentXml);
            if (includeImage)
            {
                Write(archive, "word/_rels/document.xml.rels", $$"""
                    <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                      <Relationship Id="rIdImage1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="media/{{imageFileName}}"/>
                    </Relationships>
                    """);
                var image = archive.CreateEntry("word/media/" + imageFileName);
                using var stream = image.Open();
                stream.Write(imageBytes ?? SafeRasterFixtures.Png);
            }
        }
        return output.ToArray();
    }

    private static void Write(ZipArchive archive, string path, string value)
    {
        var entry = archive.CreateEntry(path);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(value);
    }
}
