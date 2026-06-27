using System.IO;
using System.Text;

namespace Musoq.Plugins;

public partial class LibraryBase
{
    private static string DecompressToString(byte[] data, Encoding encoding, Func<Stream, Stream> createDecompressionStream)
    {
        using var inputStream = new MemoryStream(data);
        using var decompressionStream = createDecompressionStream(inputStream);
        using var outputStream = new MemoryStream();

        decompressionStream.CopyTo(outputStream);
        return encoding.GetString(outputStream.ToArray());
    }

    private static byte[] DecompressToBytesCore(byte[] data, Func<Stream, Stream> createDecompressionStream)
    {
        using var inputStream = new MemoryStream(data);
        using var decompressionStream = createDecompressionStream(inputStream);
        using var outputStream = new MemoryStream();

        decompressionStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }

    private static byte[] CompressCore(byte[] data, Func<Stream, Stream> createCompressionStream)
    {
        using var outputStream = new MemoryStream();
        using (var compressionStream = createCompressionStream(outputStream))
        {
            compressionStream.Write(data, 0, data.Length);
        }

        return outputStream.ToArray();
    }
}
