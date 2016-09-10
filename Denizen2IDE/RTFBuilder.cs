using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Denizen2IDE
{
    public class RTFBuilder
    {
        public static RTFBuilder For(string text)
        {
            return new RTFBuilder() { InternalStr = "{" + text.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}").Replace("\t", "\\tab").Replace("\r", "").Replace("\n", "\\par").Replace(" ", "\\~") + "}" };
        }

        public static RTFBuilder Bold(RTFBuilder text)
        {
            return new RTFBuilder() { InternalStr = "{\\b " + text.ToString() + "\\b0}" };
        }

        public static RTFBuilder Italic(RTFBuilder text)
        {
            return new RTFBuilder() { InternalStr = "{\\i " + text.ToString() + "\\i0}" };
        }

        public static RTFBuilder WavyUnderline(RTFBuilder text)
        {
            return new RTFBuilder() { InternalStr = "{\\ulwave " + text.ToString() + "\\ulwave0}" };
        }

        public static RTFBuilder Strike(RTFBuilder text)
        {
            return new RTFBuilder() { InternalStr = "{\\strike " + text.ToString() + "\\strike0}" };
        }

        public static RTFBuilder Underline(RTFBuilder text)
        {
            return new RTFBuilder() { InternalStr = "{\\ul " + text.ToString() + "\\ul0}" };
        }

        public static RTFBuilder Colored(RTFBuilder text, ColorTable color)
        {
            return new RTFBuilder() { InternalStr = "{\\cf" + ((int)color).ToString() + " " + text.ToString() + "\\cf0}" };
        }

        public StringBuilder Internal = new StringBuilder();

        string InternalStr
        {
            set
            {
                Internal.Append(value);
            }
        }

        public Dictionary<int, string> colors;

        public RTFBuilder Append(RTFBuilder builder)
        {
            Internal.Append(builder.ToString());
            return this;
        }

        public RTFBuilder AppendLine()
        {
            Internal.Append("\\par");
            return this;
        }

        public override string ToString()
        {
            return Internal.ToString();
        }

        public string CT()
        {
            return "\\red0\\green0\\blue0;"  // BLACK = 1
                + "\\red128\\green0\\blue0;" // RED = 2
                + "\\red0\\green128\\blue0;" // GREEN = 3
                + "\\red0\\green0\\blue128;" // BLUE = 4
                + "\\red255\\green0\\blue128;"; // PINK = 5
        }

        public string FinalOutput()
        {
            return "{\\rtf1{\\colortbl ;" + CT() + "}\\b0\\i0\\cf0" + Internal + "\\par}";
        }
    }

    public enum ColorTable
    {
        BLACK = 1,
        RED = 2,
        GREEN = 3,
        BLUE = 4,
        PINK = 5
    }
}
