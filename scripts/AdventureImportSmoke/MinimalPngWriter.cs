using System.IO.Compression;

namespace AdventureImportSmoke;

// Тестовая заглушка: у QuestPDF Placeholders.Image() встроенная картинка фиксировано маленькая
// (64x51 px), она визуально масштабируется под layout, но реальное разрешение не меняется —
// не проходит порог размера "это похоже на боевую карту" у PdfPigAdventureParser. Чтобы
// проверить путь извлечения картинки целиком (не только байтовый декод), генерируем свой
// валидный PNG нужного разрешения вручную — сигнатура + IHDR + IDAT (zlib raw RGB) + IEND,
// без внешних библиотек рисования (System.Drawing на macOS ненадёжен, тянуть SkiaSharp/
// ImageSharp ради одной тестовой картинки избыточно).
internal static class MinimalPngWriter
{
    internal static byte[] CreateSolidColorPng(int width, int height, byte r, byte g, byte b)
    {
        using var ms = new MemoryStream();
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]); // PNG signature

        WriteChunk(ms, "IHDR", BuildIhdr(width, height));
        WriteChunk(ms, "IDAT", BuildIdat(width, height, r, g, b));
        WriteChunk(ms, "IEND", []);

        return ms.ToArray();
    }

    private static byte[] BuildIhdr(int width, int height)
    {
        var data = new byte[13];
        WriteUInt32BE(data, 0, (uint)width);
        WriteUInt32BE(data, 4, (uint)height);
        data[8] = 8;  // bit depth
        data[9] = 2;  // color type: RGB truecolor
        // compression(10)/filter(11)/interlace(12) default to 0
        return data;
    }

    private static byte[] BuildIdat(int width, int height, byte r, byte g, byte b)
    {
        using var raw = new MemoryStream();
        for (var y = 0; y < height; y++)
        {
            raw.WriteByte(0); // filter type: None
            for (var x = 0; x < width; x++)
            {
                raw.WriteByte(r);
                raw.WriteByte(g);
                raw.WriteByte(b);
            }
        }

        var rawBytes = raw.ToArray();
        using var compressed = new MemoryStream();
        compressed.Write([0x78, 0x9C]); // zlib header (default compression)
        using (var deflate = new DeflateStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
            deflate.Write(rawBytes, 0, rawBytes.Length);
        compressed.Write(BitConverter.GetBytes(Adler32(rawBytes)).Reverse().ToArray());

        return compressed.ToArray();
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        var lengthBytes = new byte[4];
        WriteUInt32BE(lengthBytes, 0, (uint)data.Length);
        output.Write(lengthBytes);
        output.Write(typeBytes);
        output.Write(data);

        var crc = Crc32(typeBytes.Concat(data).ToArray());
        var crcBytes = new byte[4];
        WriteUInt32BE(crcBytes, 0, crc);
        output.Write(crcBytes);
    }

    private static void WriteUInt32BE(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    private static uint Adler32(byte[] data)
    {
        const uint modAdler = 65521;
        uint a = 1, bAcc = 0;
        foreach (var byteVal in data)
        {
            a = (a + byteVal) % modAdler;
            bAcc = (bAcc + a) % modAdler;
        }
        return (bAcc << 16) | a;
    }

    private static readonly uint[] Crc32Table = BuildCrc32Table();

    private static uint[] BuildCrc32Table()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            table[n] = c;
        }
        return table;
    }

    private static uint Crc32(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var byteVal in data)
            crc = Crc32Table[(crc ^ byteVal) & 0xFF] ^ (crc >> 8);
        return crc ^ 0xFFFFFFFFu;
    }
}
