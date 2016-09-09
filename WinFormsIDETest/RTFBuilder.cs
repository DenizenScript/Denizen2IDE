using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace WinFormsIDETest
{
    public class RTFBuilder
    {
        public static RTFBuilder For(string text)
        {
            return new RTFBuilder() { InternalStr = "{" + text.Replace("\\", "\\\\").Replace("{", "\\{").Replace("}", "\\}").Replace("\t", "\\tab").Replace("\r", "").Replace("\n", "\\par") + "}" };
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

        public override string ToString()
        {
            return Internal.ToString();
        }

        public string FinalOutput()
        {
            return "{\\rtf1{\\colortbl ;\\red0\\green0\\blue0;\\red255\\green0\\blue0;\\red0\\green255\\blue0;\\red0\\green0\\blue255;}\\b0\\i0\\cf0" + Internal + "\\par}";
        }
    }

    public enum ColorTable
    {
        BLACK = 1,
        RED = 2,
        GREEN = 3,
        BLUE = 4
    }
}
