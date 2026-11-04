using System.IO;
using System.Text;

namespace Ux.Editor
{
    /// <summary>
    /// 代码生成的统一出口：UTF-8 无 BOM、LF 换行、内容没变就不落盘（避免无意义的重编译）。
    /// 所有生成器都走这里，别各自 File.WriteAllText——编码和换行不一致会让 git diff 整文件翻红。
    /// </summary>
    public static class CodeFileWriter
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        /// <summary>写入文件，返回是否真的改变了内容（false = 原样跳过）。</summary>
        public static bool Write(string path, string content)
        {
            if (string.IsNullOrEmpty(path)) return false;

            path = path.Replace('\\', '/');
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            content = ToLf(content);
            if (File.Exists(path) && ToLf(File.ReadAllText(path, Utf8NoBom)) == content)
                return false;

            File.WriteAllText(path, content, Utf8NoBom);
            return true;
        }

        /// <summary>生成 .cs 文件：pathWithoutExtension 不带扩展名。</summary>
        public static bool WriteCs(string pathWithoutExtension, string content)
            => Write(pathWithoutExtension + ".cs", content);

        private static string ToLf(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('\r') < 0) return text;
            return text.Replace("\r\n", "\n").Replace('\r', '\n');
        }
    }
}
