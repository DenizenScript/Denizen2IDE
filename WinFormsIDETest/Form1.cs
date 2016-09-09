using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace WinFormsIDETest
{
    public partial class Form1 : Form
    {
        Size Rel;

        public Form1()
        {
            InitializeComponent();
            Timer t = new Timer();
            t.Interval = 1000;
            t.Tick += T_Tick;
            t.Start();
            richTextBox1.AcceptsTab = true;
            richTextBox1.HScroll += RichTextBox1_HScroll;
            Rel = this.Size - richTextBox1.Size;
        }

        private void RichTextBox1_HScroll(object sender, EventArgs e)
        {
            textChanged = true;
        }

        private const int WM_SETREDRAW = 0x000B;

        public static void Suspend(Control control)
        {
            Message msgSuspendUpdate = Message.Create(control.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            NativeWindow.FromHandle(control.Handle).DefWndProc(ref msgSuspendUpdate);
        }

        public static void Resume(Control control)
        {
            Message msgResumeUpdate = Message.Create(control.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
            NativeWindow.FromHandle(control.Handle).DefWndProc(ref msgResumeUpdate);
            control.Invalidate();
        }

        // TODO: Accelerate this method as much as possible using async and whatever else we can do!
        private void T_Tick(object sender, EventArgs e)
        {
            if (!textChanged)
            {
                return;
            }
            // Setup
            Suspend(this);
            int start = richTextBox1.SelectionStart;
            int len = richTextBox1.SelectionLength;
            // Update RTF
            string text = richTextBox1.Text;
            int py = richTextBox1.GetPositionFromCharIndex(0).Y;
            int cLoc = 0;// (richTextBox1.GetCharIndexFromPosition(new Point(0, -Math.Max(py - 1, 0))) / 6) * 6;
            RTFBuilder rtf = new RTFBuilder();
            //rtf.Append(RTFBuilder.For(text.Substring(0, cLoc)));
            int maxlen = (text.Length / 6) * 6;
            for (int i = cLoc; i < maxlen; i += 6)
            {
                rtf.Append(RTFBuilder.Bold(RTFBuilder.For(text[i].ToString())));
                rtf.Append(RTFBuilder.Italic(RTFBuilder.For(text[i + 1].ToString())));
                rtf.Append(RTFBuilder.Colored(RTFBuilder.For(text[i + 2].ToString()), ColorTable.BLUE));
                rtf.Append(RTFBuilder.Strike(RTFBuilder.For(text[i + 3].ToString())));
                rtf.Append(RTFBuilder.WavyUnderline(RTFBuilder.For(text[i + 4].ToString())));
                rtf.Append(RTFBuilder.Underline(RTFBuilder.For(text[i + 5].ToString())));
            }
            for (int i = maxlen; i < text.Length; i++)
            {
                rtf.Append(RTFBuilder.For(text[i].ToString()));
            }
            richTextBox1.Rtf = rtf.FinalOutput();
            // Set back to normal
            Resume(this);
            richTextBox1.Select(start, len);
            richTextBox1.Invalidate();
            textChanged = false;
        }

        bool textChanged = false;

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            textChanged = true;
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            richTextBox1.Size = this.Size - Rel;
        }
    }
}
