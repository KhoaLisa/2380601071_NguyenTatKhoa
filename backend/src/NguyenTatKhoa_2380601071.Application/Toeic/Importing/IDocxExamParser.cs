namespace NguyenTatKhoa_2380601071.Toeic.Importing;

public interface IDocxExamParser
{
    ParsedExamDocument Parse(byte[] docxBytes);
}
