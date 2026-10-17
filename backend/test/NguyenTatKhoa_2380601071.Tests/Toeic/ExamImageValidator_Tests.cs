using Abp.UI;
using NguyenTatKhoa_2380601071.Toeic;
using Shouldly;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace NguyenTatKhoa_2380601071.Tests.Toeic;

public class ExamImageValidator_Tests
{
    public static IEnumerable<object[]> SafeImages()
    {
        yield return new object[] { SafeRasterFixtures.Png, "passage.PNG", "image/png" };
        yield return new object[] { SafeRasterFixtures.Gif, "passage.gif", "image/gif" };
        yield return new object[] { SafeRasterFixtures.Jpeg, "passage.jpg", "image/jpeg" };
        yield return new object[] { SafeRasterFixtures.Jpeg, "passage.jpeg", "image/jpeg" };
        yield return new object[] { SafeRasterFixtures.Bmp, "passage.bmp", "image/bmp" };
        yield return new object[] { SafeRasterFixtures.WebP, "passage.webp", "image/webp" };
    }

    [Theory]
    [MemberData(nameof(SafeImages))]
    public void Recognizes_only_supported_rasters_with_matching_extension_and_mime(byte[] content, string name, string mime)
    {
        ExamImageValidator.GetMediaType(content, name).ShouldBe(mime);
        ExamImageValidator.TryGetMediaType(content, name, mime, out var detected).ShouldBeTrue();
        detected.ShouldBe(mime);
        ExamImageValidator.TryGetMediaType(content, name, "image/svg+xml", out _).ShouldBeFalse();
        ExamImageValidator.TryGetMediaType(content, "image.svg", mime, out _).ShouldBeFalse();
        ExamImageValidator.TryGetMediaType(content, name, "text/html", out _).ShouldBeFalse();
        ExamImageValidator.TryGetMediaType(content.Take(12).ToArray(), name, mime, out _).ShouldBeFalse();
    }

    [Fact]
    public void Rejects_empty_oversized_corrupt_active_content_and_header_only_images()
    {
        foreach (var content in new[]
        {
            Array.Empty<byte>(), new byte[ExamImageValidator.MaxBytes + 1], new byte[] { 1, 2, 3, 4 },
            Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg' onload='alert(1)'/>"),
            Encoding.UTF8.GetBytes("<html><script>alert(1)</script></html>"), SafeRasterFixtures.Png.Take(24).ToArray()
        })
        {
            ExamImageValidator.TryGetMediaType(content, "passage.png", "image/png", out var mime).ShouldBeFalse();
            mime.ShouldBeNull();
            Should.Throw<UserFriendlyException>(() => ExamImageValidator.GetMediaType(content, "passage.png"));
        }
        ExamImageValidator.TryGetMediaType(null, "passage.png", "image/png", out _).ShouldBeFalse();
        ExamImageValidator.TryGetMediaType(SafeRasterFixtures.Png, null, "image/png", out _).ShouldBeFalse();
        ExamImageValidator.TryGetMediaType(SafeRasterFixtures.Png, "passage.jpg", "image/jpeg", out _).ShouldBeFalse();
        ExamImageValidator.TryGetMediaType(SafeRasterFixtures.Png, "passage.png", "image/png; charset=utf-8", out _).ShouldBeFalse();
    }

    [Fact]
    public void Rejects_zero_or_excessive_png_dimensions_and_missing_trailer()
    {
        var png = SafeRasterFixtures.Png;
        BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(16, 4), 0);
        ExamImageValidator.TryGetMediaType(png, "passage.png", "image/png", out _).ShouldBeFalse();
        png = SafeRasterFixtures.Png;
        BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(16, 4), 100_000);
        ExamImageValidator.TryGetMediaType(png, "passage.png", "image/png", out _).ShouldBeFalse();
        png = SafeRasterFixtures.Png;
        png[^1] = 0;
        ExamImageValidator.TryGetMediaType(png, "passage.png", "image/png", out _).ShouldBeFalse();
    }

    [Fact]
    public void Publication_defense_rejects_previously_stored_unsafe_or_mislabeled_content()
    {
        var exam = new Exam { Id = Guid.NewGuid(), Title = "Image validation" };
        var group = new PassageGroup { Id = Guid.NewGuid(), Part = ToeicPart.Part7 };
        var passage = new Passage { Id = Guid.NewGuid() };
        var block = new ContentBlock { Id = Guid.NewGuid(), PassageId = passage.Id, Type = ContentBlockType.Image,
            FileName = "passage.png", MediaType = "image/png", BinaryData = SafeRasterFixtures.Png, DisplayOrder = 1 };
        group.Passages.Add(passage);
        group.ContentBlocks.Add(block);
        exam.PassageGroups.Add(group);
        ExamStructureValidator.Validate(exam, false).ShouldNotContain(x => x.Code == "INVALID_CONTENT");
        block.MediaType = "image/svg+xml";
        ExamStructureValidator.Validate(exam, false).ShouldContain(x => x.Code == "INVALID_CONTENT");
        block.MediaType = "image/png";
        block.BinaryData = Encoding.UTF8.GetBytes("<script>alert(1)</script>");
        ExamStructureValidator.Validate(exam, false).ShouldContain(x => x.Code == "INVALID_CONTENT");
    }
}

internal static class SafeRasterFixtures
{
    public static byte[] Png => Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jgWQAAAAASUVORK5CYII=");
    public static byte[] Gif => Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");
    public static byte[] WebP => Convert.FromBase64String("UklGRiIAAABXRUJQVlA4IBYAAAAwAQCdASoBAAEADsD+JaQAA3AAAAAA");
    public static byte[] Jpeg => Convert.FromBase64String("/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAP//////////////////////////////////////////////////////////////////////////////////////2wBDAf//////////////////////////////////////////////////////////////////////////////////////wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAf/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAAX/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwCdAAr/2Q==");
    public static byte[] Bmp
    {
        get
        {
            var bytes = new byte[58];
            Encoding.ASCII.GetBytes("BM").CopyTo(bytes, 0);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(2, 4), 58);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(10, 4), 54);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(14, 4), 40);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(18, 4), 1);
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(22, 4), 1);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(26, 2), 1);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(28, 2), 24);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(34, 4), 4);
            return bytes;
        }
    }
}
