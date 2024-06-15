using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor
{
    public class CodeGenWrite
    {
        string content;
        bool newLine;
        public CodeGenWrite(string _content)
        {
            content = _content;
        }
        public CodeGenWrite()
        {
            content = string.Empty;
        }
        public void Writeln()
        {
            content += "\n";
            newLine = true;
        }
        public void Writeln(string add, bool isBlock = true)
        {
            if (isBlock)
            {
                foreach (var b in block)
                {
                    content += b;
                }
            }
            content += add + "\n";
            newLine = true;
        }
        public void Write(string add, bool isBlock = true)
        {
            if (isBlock)
            {
                foreach (var b in block)
                {
                    content += b;
                }
            }
            content += add;
        }

        List<string> block = new List<string>();
        public void StartBlock()
        {
            Writeln("{", newLine);
            block.Add("\t");
        }
        public void EndBlock(bool isLn = true)
        {
            block.RemoveAt(0);
            if (isLn)
                Writeln("}");
            else
                Write("}");
        }

        public void Export(string path, string fileName)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            path += $"{fileName}.cs";
            if (!File.Exists(path))
            {
                File.CreateText(path).Dispose();
            }
            File.WriteAllText(path, content, System.Text.Encoding.UTF8);
        }
    }
}