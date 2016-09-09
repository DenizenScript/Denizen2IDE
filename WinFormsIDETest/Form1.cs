using System;
using System.Collections.Generic;
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

        private const int WM_HSCROLL = 0x0114;

        private const int WM_VSCROLL = 0x0115;

        private const int SB_THUMBPOSITION = 4;

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

        // TODO: Linux-y version!
#if WINDOWS
        [DllImport( "User32.dll" )]
        public extern static int GetScrollPos(IntPtr hWnd, int nBar);

        [DllImport("User32.dll")]
        public extern static int SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
#endif

        public static Point GetScrollPos(RichTextBox box)
        {
#if WINDOWS
            return new Point(GetScrollPos(box.Handle, 0), GetScrollPos(box.Handle, 1));
#else
            return Point.Empty;
#endif
        }

        public static void SetScrollPos(RichTextBox box, Point point)
        {
#if WINDOWS
            int x = point.X;
            int y = point.Y;
            x <<= 16;
            uint wParam = (uint)SB_THUMBPOSITION | (uint)x;
            SendMessage(box.Handle, WM_HSCROLL, new IntPtr(wParam), IntPtr.Zero);
            y <<= 16;
            uint wParamY = (uint)SB_THUMBPOSITION | (uint)y;
            SendMessage(box.Handle, WM_VSCROLL, new IntPtr(wParamY), IntPtr.Zero);
#endif
        }

        // TODO: Accelerate this method as much as possible using async and whatever else we can do!
        // Maybe also replace RTFBuilder with lower-level calls somehow.
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
            Point scroll = GetScrollPos(richTextBox1);
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
            SetScrollPos(richTextBox1, scroll);
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
