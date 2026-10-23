using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Threading;
using UniversalRPG.Rm2k.Rendering;

namespace UniversalRPG.Rgss;

/// <summary>Bounded non-interlaced PNG decoding; no native engine calls or foreign code.</summary>
internal sealed class RgssPngImage
{
    internal const int MaximumFileBytes = 32 * 1024 * 1024;
    private static readonly uint[] CrcTable = BuildCrcTable();
    private readonly byte[] _compressed;
    private readonly byte[] _palette;
    private readonly byte[] _transparency;
    private readonly int _type;
    private readonly int _depth;
    internal int Width { get; }
    internal int Height { get; }

    internal RgssPngImage(byte[] data, CancellationToken cancellation)
    {
        if (data.Length > MaximumFileBytes || data.Length < 33
            || !data.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
            throw Invalid("Invalid PNG signature or encoded size limit exceeded.");
        using var compressed = new MemoryStream();
        byte[] palette = Array.Empty<byte>(), transparency = Array.Empty<byte>();
        bool header = false, paletteSeen = false, transparencySeen = false;
        bool imageData = false, dataClosed = false, end = false;
        var offset = 8;
        var chunks = 0;
        while (offset < data.Length)
        {
            cancellation.ThrowIfCancellationRequested();
            if (++chunks > 4096 || data.Length - offset < 12) throw Invalid("Invalid PNG chunk count or truncated chunk.");
            var lengthValue = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset, 4));
            if (lengthValue > data.Length - offset - 12) throw Invalid("Truncated PNG chunk.");
            var length = (int)lengthValue;
            var name = data.AsSpan(offset + 4, 4);
            foreach (var letter in name)
                if (!(letter >= 'A' && letter <= 'Z' || letter >= 'a' && letter <= 'z'))
                    throw Invalid("Invalid PNG chunk name.");
            if ((name[2] & 32) != 0) throw Invalid("Invalid PNG reserved chunk bit.");
            var expectedCrc = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 8 + length, 4));
            if (Crc(data.AsSpan(offset + 4, length + 4), cancellation) != expectedCrc)
                throw Invalid("PNG chunk CRC mismatch.");
            var kind = System.Text.Encoding.ASCII.GetString(name);
            var body = data.AsSpan(offset + 8, length);
            if (!header && kind != "IHDR") throw Invalid("PNG must begin with IHDR.");
            if (imageData && kind != "IDAT") dataClosed = true;
            switch (kind)
            {
                case "IHDR":
                    if (header || length != 13) throw Invalid("Invalid or duplicate PNG IHDR.");
                    Width = BinaryPrimitives.ReadInt32BigEndian(body[..4]);
                    Height = BinaryPrimitives.ReadInt32BigEndian(body.Slice(4, 4));
                    _depth = body[8]; _type = body[9];
                    if (Width <= 0 || Height <= 0 || Width > RgssBitmapStore.MaximumDimension
                        || Height > RgssBitmapStore.MaximumDimension)
                        throw Invalid("PNG dimensions exceed the RGSS bitmap limit.");
                    if (!(_type is 2 or 6 && _depth == 8 || _type == 3 && _depth is 1 or 2 or 4 or 8)
                        || body[10] != 0 || body[11] != 0 || body[12] != 0)
                        throw Invalid("Unsupported PNG format: requires non-interlaced 8-bit RGB/RGBA or indexed PNG.");
                    header = true;
                    break;
                case "PLTE":
                    if (paletteSeen || imageData || transparencySeen || length == 0 || length > 768 || length % 3 != 0
                        || _type == 3 && length / 3 > (1 << _depth))
                        throw Invalid("Invalid PNG palette.");
                    palette = body.ToArray(); paletteSeen = true;
                    break;
                case "tRNS":
                    if (transparencySeen || imageData || _type == 6
                        || _type == 3 && (!paletteSeen || length == 0 || length > palette.Length / 3)
                        || _type == 2 && (length != 6 || body[0] != 0 || body[2] != 0 || body[4] != 0))
                        throw Invalid("Invalid PNG transparency.");
                    transparency = body.ToArray(); transparencySeen = true;
                    break;
                case "IDAT":
                    if (dataClosed || _type == 3 && !paletteSeen) throw Invalid("Invalid PNG IDAT ordering.");
                    compressed.Write(body); imageData = true;
                    break;
                case "IEND":
                    if (!imageData || length != 0 || offset + 12 != data.Length) throw Invalid("Invalid PNG ending.");
                    end = true;
                    break;
                default:
                    if ((name[0] & 32) == 0) throw Invalid("Unsupported critical PNG chunk: " + kind);
                    break;
            }
            offset += length + 12;
        }
        if (!end || compressed.Length < 6) throw Invalid("PNG has no complete image stream.");
        _compressed = compressed.ToArray(); _palette = palette; _transparency = transparency;
        var cmf = _compressed[0]; var flags = _compressed[1];
        if ((cmf & 15) != 8 || (cmf >> 4) > 7 || ((cmf << 8) + flags) % 31 != 0 || (flags & 32) != 0)
            throw Invalid("Unsupported PNG zlib header.");
    }

    internal void DecodeInto(Rm2kPixelBuffer target, CancellationToken cancellation)
    {
        if (target.Width != Width || target.Height != Height) throw Invalid("PNG target dimensions do not match.");
        var channels = _type == 6 ? 4 : _type == 2 ? 3 : 1;
        var stride = (Width * channels * _depth + 7) / 8;
        var bytesPerPixel = _type == 3 ? 1 : channels;
        var row = new byte[stride];
        var previous = new byte[stride];
        uint adlerA = 1, adlerB = 0;
        void Checksum(byte value) { adlerA = (adlerA + value) % 65521; adlerB = (adlerB + adlerA) % 65521; }
        using var input = new MemoryStream(_compressed, false);
        using var inflate = new ZLibStream(input, CompressionMode.Decompress);
        for (var y = 0; y < Height; y++)
        {
            cancellation.ThrowIfCancellationRequested();
            var filter = inflate.ReadByte();
            if (filter is < 0 or > 4) throw Invalid("Invalid or missing PNG scanline filter.");
            Checksum((byte)filter);
            inflate.ReadExactly(row);
            for (var x = 0; x < stride; x++)
            {
                Checksum(row[x]);
                var left = x >= bytesPerPixel ? row[x - bytesPerPixel] : 0;
                var up = previous[x];
                var upperLeft = x >= bytesPerPixel ? previous[x - bytesPerPixel] : 0;
                var prediction = filter switch
                {
                    0 => 0, 1 => left, 2 => up, 3 => (left + up) / 2,
                    _ => Paeth(left, up, upperLeft),
                };
                row[x] = unchecked((byte)(row[x] + prediction));
            }
            for (var x = 0; x < Width; x++)
            {
                var destination = (y * Width + x) * 4;
                if (_type == 3)
                {
                    var bit = x * _depth;
                    var index = (row[bit / 8] >> (8 - _depth - bit % 8)) & ((1 << _depth) - 1);
                    if (index >= _palette.Length / 3) throw Invalid("PNG pixel index exceeds its palette.");
                    Array.Copy(_palette, index * 3, target.Pixels, destination, 3);
                    target.Pixels[destination + 3] = index < _transparency.Length ? _transparency[index] : (byte)255;
                }
                else
                {
                    var source = x * channels;
                    Array.Copy(row, source, target.Pixels, destination, 3);
                    target.Pixels[destination + 3] = _type == 6 ? row[source + 3]
                        : _transparency.Length != 0 && row[source] == _transparency[1]
                            && row[source + 1] == _transparency[3] && row[source + 2] == _transparency[5] ? (byte)0 : (byte)255;
                }
            }
            (row, previous) = (previous, row);
        }
        cancellation.ThrowIfCancellationRequested();
        if (inflate.ReadByte() != -1) throw Invalid("PNG image data exceeds its declared dimensions.");
        var expectedAdler = BinaryPrimitives.ReadUInt32BigEndian(_compressed.AsSpan(_compressed.Length - 4));
        if (((adlerB << 16) | adlerA) != expectedAdler) throw Invalid("PNG zlib checksum mismatch or truncated trailer.");
    }

    private static int Paeth(int left, int up, int upperLeft)
    {
        var prediction = left + up - upperLeft;
        var a = Math.Abs(prediction - left); var b = Math.Abs(prediction - up); var c = Math.Abs(prediction - upperLeft);
        return a <= b && a <= c ? left : b <= c ? up : upperLeft;
    }

    private static InvalidDataException Invalid(string message) => new(message);
    private static uint Crc(ReadOnlySpan<byte> bytes, CancellationToken cancellation)
    {
        uint crc = uint.MaxValue;
        for (var i = 0; i < bytes.Length; i++)
        {
            if ((i & 4095) == 0) cancellation.ThrowIfCancellationRequested();
            crc = CrcTable[(crc ^ bytes[i]) & 255] ^ (crc >> 8);
        }
        return ~crc;
    }
    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            var value = i;
            for (var bit = 0; bit < 8; bit++) value = (value & 1) != 0 ? (value >> 1) ^ 0xedb88320u : value >> 1;
            table[i] = value;
        }
        return table;
    }
}
