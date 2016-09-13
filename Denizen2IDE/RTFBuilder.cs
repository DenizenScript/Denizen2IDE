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

        public static RTFBuilder BackColored(RTFBuilder text, ColorTable color)
        {
            return new RTFBuilder() { InternalStr = "{\\chcbpat" + ((int)color).ToString()
                + "\\cb" + ((int)color).ToString()
                + "\\highlight" + ((int)color).ToString()
                + " " + text.ToString() + "\\chcbpat0\\cb0\\hightlight0}" };
        }

        public RTFBuilder Replace(string text, RTFBuilder res)
        {
            return new RTFBuilder() { Internal = new StringBuilder(Internal.ToString().Replace(text, res.ToString())) };
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
                + "\\red255\\green0\\blue128;" // PINK = 5
                + "\\red128\\green128\\blue128;" // GRAY = 6
                + "\\red128\\green0\\blue255;" // PURPLE = 7
                + "\\red64\\green64\\blue64;" // DARK_GRAY = 8
                + "\\red0\\green128\\blue128;" // DARK_CYAN = 9
                + "\\red200\\green200\\blue200;" // LIGHT_GRAY = 10
                ;
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
        PINK = 5,
        GRAY = 6,
        PURPLE = 7,
        DARK_GRAY = 8,
        DARK_CYAN = 9,
        LIGHT_GRAY = 10
    }
}
