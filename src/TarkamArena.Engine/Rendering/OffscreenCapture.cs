using System.Buffers.Binary;
using System.IO.Compression;
using ThreeNet;

namespace TarkamArena.Engine.Rendering;

/// <summary>Renders a scene into an image file without a window: portraits, stage thumbnails, screenshots.</summary>
public static class OffscreenCapture
{
    public static RendererOptions Options(int width, int height, bool high = true)
    {
        var o = RendererOptions.Default;
        o.Width = width;
        o.Height = height;
        o.Shadows = true;
        o.ShadowMapSize = high ? 2048 : 1024;
        o.Bloom = true;
        o.BloomIntensity = 0.35f;
        o.Ssao = high;
        o.Fxaa = true;
        o.Vignette = 0.25f;
        o.ToneMapping = ToneMapping.Aces;
        o.Exposure = 0.85f;
        o.Saturation = 1.12f;
        o.Contrast = 1.08f;
        return o;
    }

    public static byte[] Render(Scene scene, Node camera, int width, int height, int warmupFrames = 2)
    {
        using var renderer = Renderer.CreateOffscreen(Options(width, height));
        scene.FinishStreaming(TimeSpan.FromSeconds(5));
        for (int i = 0; i < warmupFrames; i++) renderer.Render(scene, camera);
        renderer.Render(scene, camera);
        return renderer.ReadPixels();
    }

    public static void RenderToPng(Scene scene, Node camera, int width, int height, string path)
    {
        var rgba = Render(scene, camera, width, height);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllBytes(path, EncodePng(rgba, width, height));
    }

    /// <summary>Minimal PNG encoder (RGBA8, no filtering) so tools need no imaging dependency.</summary>
    public static byte[] EncodePng(ReadOnlySpan<byte> rgba, int width, int height)
    {
        using var raw = new MemoryStream();
        using (var z = new ZLibStream(raw, CompressionLevel.Optimal, leaveOpen: true))
        {
            for (int y = 0; y < height; y++)
            {
                z.WriteByte(0);
                z.Write(rgba.Slice(y * width * 4, width * 4));
            }
        }
        using var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header[4..], height);
        header[8] = 8;
        header[9] = 6;
        WriteChunk(png, "IHDR", header);
        WriteChunk(png, "IDAT", raw.ToArray());
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void WriteChunk(Stream s, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> len = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(len, data.Length);
        s.Write(len);
        Span<byte> typeBytes = stackalloc byte[4];
        for (int i = 0; i < 4; i++) typeBytes[i] = (byte)type[i];
        s.Write(typeBytes);
        s.Write(data);
        uint crc = Crc32(typeBytes, 0xFFFFFFFF);
        crc = Crc32(data, crc) ^ 0xFFFFFFFF;
        BinaryPrimitives.WriteUInt32BigEndian(len, crc);
        s.Write(len);
    }

    private static uint[]? s_table;

    private static uint Crc32(ReadOnlySpan<byte> data, uint crc)
    {
        var table = s_table ??= BuildTable();
        foreach (byte b in data) crc = table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return crc;
    }

    private static uint[] BuildTable()
    {
        var t = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            t[n] = c;
        }
        return t;
    }
}
