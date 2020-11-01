using System.IO;
using System.IO.Compression;
using System.Text;

namespace Ray.BiliBiliTool.Infrastructure.Helpers
{
    /// <summary>
    /// 解压缩Helper
    /// </summary>
    public class ZipHelper
    {
        /// <summary>
        /// 将Gzip的byte数组读取为字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string ReadGzip(byte[] bytes, string encoding = "UTF-8")
        {
            string result = string.Empty;
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                using (GZipStream decompressedStream = new GZipStream(ms, CompressionMode.Decompress))
                {
                    using (StreamReader sr = new StreamReader(decompressedStream, Encoding.GetEncoding(encoding)))
                    {
                        result = sr.ReadToEnd();
                    }
                }
            }
            return result;
        }
    }
}
