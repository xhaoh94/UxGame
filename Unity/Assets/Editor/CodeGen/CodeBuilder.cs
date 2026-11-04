using System.Text;

namespace Ux.Editor
{
    /// <summary>
    /// 生成代码的缩进写入器：缩进只由 Open/Close 维护，写行内容时不用手打空格。
    /// 和 CodeFileWriter 配套使用——一个管排版，一个管落盘。
    /// </summary>
    public sealed class CodeBuilder
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly string _indentUnit;
        private int _level;
        private bool _lastIsBlank = true;

        public CodeBuilder(int indentSize = 4)
        {
            _indentUnit = new string(' ', indentSize);
        }

        public int Level => _level;

        /// <summary>写一行（按当前缩进）；text 为空时等价于 Blank。</summary>
        public CodeBuilder Line(string text = null)
        {
            if (string.IsNullOrEmpty(text))
                return Blank();

            for (int i = 0; i < _level; i++)
                _sb.Append(_indentUnit);
            _sb.Append(text).Append('\n');
            _lastIsBlank = false;
            return this;
        }

        /// <summary>写一行声明并开块：header 非空时先写它，再写 {，缩进 +1。</summary>
        public CodeBuilder Open(string header = null)
        {
            if (!string.IsNullOrEmpty(header)) Line(header);
            return Line("{").Indent();
        }

        /// <summary>闭块：缩进 -1 后写 }，可带后缀（如 ");"）。</summary>
        public CodeBuilder Close(string suffix = null)
        {
            Unindent();
            return Line("}" + suffix);
        }

        /// <summary>空行；连续空行和文件开头的空行会被压掉。</summary>
        public CodeBuilder Blank()
        {
            if (_lastIsBlank) return this;
            _sb.Append('\n');
            _lastIsBlank = true;
            return this;
        }

        public CodeBuilder Indent()
        {
            _level++;
            return this;
        }

        public CodeBuilder Unindent()
        {
            if (_level > 0) _level--;
            return this;
        }

        public override string ToString() => _sb.ToString();
    }
}
