using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace Phinix.LegacyTalentTradeExtension.Client
{
    internal static class TalentTradeInputLimits
    {
        internal const int MaxResponseBytes = 16 * 1024 * 1024;
        internal const int MaxCompressedBytes = 8 * 1024 * 1024;
        internal const int MaxExpandedBytes = 32 * 1024 * 1024;
        internal const int MaxProtocolCharacters = 8 * 1024 * 1024;
        internal const int MaxQueuedCharacters = 16 * 1024 * 1024;
        internal const int MaxPendingBlobs = 32;
        internal const int MaxBlobParts = 2048;
        internal const int MaxIdCharacters = 256;
        internal const int MaxProcessedKeys = 8192;
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);

        internal static byte[] ReadBounded(Stream stream, int maximum)
        {
            if (stream == null || maximum < 0) throw new ArgumentException("TalentInputInvalid");
            using (var output = new MemoryStream())
            {
                byte[] block = new byte[16384];
                int count;
                while ((count = stream.Read(block, 0, (int)Math.Min(block.Length, maximum - output.Length + 1))) != 0)
                {
                    if (output.Length + count > maximum) throw new InvalidDataException("TalentInputLimit");
                    output.Write(block, 0, count);
                }
                return output.ToArray();
            }
        }

        internal static byte[] DecodeBase64(string encoded, int maximum)
        {
            if (encoded == null || encoded.Length > 4L * ((maximum + 2L) / 3L))
                throw new InvalidDataException("TalentEncodedLimit");
            byte[] bytes = Convert.FromBase64String(encoded);
            if (bytes.Length > maximum) throw new InvalidDataException("TalentDecodedLimit");
            return bytes;
        }

        internal static string ReadResponse(Stream stream, long contentLength)
        {
            if (contentLength > MaxResponseBytes) throw new InvalidDataException("TalentResponseLimit");
            return StrictUtf8.GetString(ReadBounded(stream, MaxResponseBytes));
        }

        internal static string DecodeProtocol(string encoded)
        {
            string message = StrictUtf8.GetString(DecodeBase64(encoded, MaxProtocolCharacters));
            if (message.Length > MaxProtocolCharacters) throw new InvalidDataException("TalentProtocolLimit");
            return message;
        }

        internal static string Decompress(string encoded)
        {
            byte[] bytes = DecodeBase64(encoded, MaxCompressedBytes);
            using (var input = new MemoryStream(bytes, false))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
                return StrictUtf8.GetString(ReadBounded(gzip, MaxExpandedBytes));
        }

        internal static XmlDocument ReadPawnXml(string xml)
        {
            if (xml == null || xml.Length > MaxExpandedBytes) throw new InvalidDataException("TalentXmlLimit");
            var settings = new XmlReaderSettings {
                DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null,
                MaxCharactersInDocument = MaxExpandedBytes
            };
            var document = new XmlDocument { XmlResolver = null };
            using (var input = new StringReader(xml))
            using (var reader = XmlReader.Create(input, settings)) document.Load(reader);
            return document;
        }
    }
}
