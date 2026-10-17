using Abp.UI;
using System;
using System.Buffers.Binary;
using System.IO;

namespace NguyenTatKhoa_2380601071.Toeic;

public static class ExamImageValidator
{
    public const int MaxBytes = 10 * 1024 * 1024;

    // Share the same safe-raster policy for Word import, editing and serving.
    // This checks the container/header, not a complete image decode. Never use a
    // caller's MIME value to serve active content such as SVG or HTML.
    public static string GetMediaType(byte[] content, string fileName)
    {
        if (!TryGetMediaType(content, fileName, null, out var mediaType))
            throw new UserFriendlyException("Chỉ nhận ảnh PNG, JPEG, GIF, BMP hoặc WebP hợp lệ, tối đa 10 MB; phần mở rộng phải khớp nội dung.");
        return mediaType;
    }

    public static bool TryGetMediaType(byte[] content, string fileName, string declaredMediaType, out string mediaType)
    {
        mediaType = null;
        if (content == null || content.Length == 0 || content.Length > MaxBytes) return false;
        var expected = Path.GetExtension(fileName ?? "").ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => null
        };
        if (expected == null || (declaredMediaType != null &&
            !string.Equals(expected, declaredMediaType, StringComparison.OrdinalIgnoreCase))) return false;
        var bytes = content.AsSpan();
        var valid = expected switch
        {
            "image/png" => bytes.Length >= 57 &&
                bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) &&
                BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(8, 4)) == 13 &&
                bytes.Slice(12, 4).SequenceEqual("IHDR"u8) &&
                Dimensions(BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(16, 4)), BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(20, 4))) &&
                bytes[^12..].SequenceEqual(new byte[] { 0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130 }),
            "image/jpeg" => bytes.Length >= 12 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff &&
                bytes[^2] == 0xff && bytes[^1] == 0xd9,
            "image/gif" => bytes.Length >= 26 &&
                (bytes[..6].SequenceEqual("GIF87a"u8) || bytes[..6].SequenceEqual("GIF89a"u8)) && bytes[^1] == 0x3b &&
                Dimensions(BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(6, 2)), BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(8, 2))),
            "image/bmp" => bytes.Length >= 54 && bytes[..2].SequenceEqual("BM"u8) &&
                BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(2, 4)) == bytes.Length &&
                BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(10, 4)) >= 54 &&
                BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(10, 4)) < bytes.Length &&
                BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(14, 4)) >= 40 &&
                Dimensions(BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(18, 4)), Math.Abs((long)BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(22, 4)))),
            "image/webp" => bytes.Length >= 26 && bytes[..4].SequenceEqual("RIFF"u8) &&
                bytes.Slice(8, 4).SequenceEqual("WEBP"u8) &&
                BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(4, 4)) == bytes.Length - 8 &&
                (bytes.Slice(12, 4).SequenceEqual("VP8 "u8) || bytes.Slice(12, 4).SequenceEqual("VP8L"u8) || bytes.Slice(12, 4).SequenceEqual("VP8X"u8)),
            _ => false
        };
        if (!valid) return false;
        mediaType = expected;
        return true;
    }

    private static bool Dimensions(long width, long height) => width > 0 && height > 0 &&
        width <= 20_000 && height <= 20_000 && width * height <= 100_000_000;
}
