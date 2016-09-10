using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Denizen2IDE
{
    public partial class Form1 : Form
    {
        Size Rel;
        Size TabRel;

        public Form1()
        {
            InitializeComponent();
            Timer t = new Timer();
            t.Interval = 250;
            t.Tick += T_Tick;
            t.Start();
            Configure(richTextBox1);
            Rel = this.Size - richTextBox1.Size;
            TabRel = this.Size - tabControl1.Size;
        }

        public RichTextBox RTFBox
        {
            get
            {
                return (RichTextBox)tabControl1.SelectedTab.Controls[0];
            }
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
            int start = RTFBox.SelectionStart;
            int len = RTFBox.SelectionLength;
            Point scroll = GetScrollPos(RTFBox);
            // Update RTF
            string[] lines = RTFBox.Text.Replace("\r", "").Split('\n');
            RTFBuilder rtf = new RTFBuilder();
            RTFBox.Rtf = "";
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trim = lines[i].Trim();
                if (trim.Length != 0)
                {
                    if (trim.StartsWith("#"))
                    {
                        string nospace = trim.Replace(" ", "");
                        if (nospace.StartsWith("#-") || nospace.StartsWith("#|") || nospace.StartsWith("#+"))
                        {
                            rtf.Append(RTFBuilder.Colored(RTFBuilder.For(line), ColorTable.RED));
                        }
                        else
                        {
                            rtf.Append(RTFBuilder.Colored(RTFBuilder.For(line), ColorTable.GREEN));
                        }
                    }
                    else if (trim.StartsWith("-"))
                    {
                        // TODO: Sub-colors for command lines
                        rtf.Append(RTFBuilder.Colored(RTFBuilder.For(line), ColorTable.BLACK));
                    }
                    else if (trim.EndsWith(":"))
                    {
                        rtf.Append(RTFBuilder.Colored(RTFBuilder.For(line), ColorTable.BLUE));
                    }
                    else if (trim.Contains(": "))
                    {
                        int ind = line.IndexOf(": ");
                        rtf.Append(RTFBuilder.Colored(RTFBuilder.For(line.Substring(0, ind + 1)), ColorTable.BLUE));
                        rtf.Append(RTFBuilder.Colored(RTFBuilder.Italic(RTFBuilder.For(line.Substring(ind + 1))), ColorTable.BLACK));
                    }
                    else
                    {
                        rtf.Append(RTFBuilder.Colored(RTFBuilder.WavyUnderline(RTFBuilder.For(line)), ColorTable.PINK));
                    }
                }
                else
                {
                    rtf.Append(RTFBuilder.For(line));
                }
                if (i < lines.Length - 1)
                {
                    rtf.AppendLine();
                }
            }
            RTFBox.Rtf = rtf.FinalOutput();
            // Set back to normal
            Resume(this);
            RTFBox.Select(start, len); // TODO: Mess with selecting fresh text a bit less often.
            SetScrollPos(RTFBox, scroll);
            RTFBox.Invalidate();
            textChanged = false;
        }

        bool textChanged = false;
        
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            textChanged = true;
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            tabControl1.Size = this.Size - TabRel;
            RTFBox.Size = this.Size - Rel;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Denizen 2 IDE." + Environment.NewLine + Environment.NewLine + "Created by the DenizenScript team, for DenizenScript users.", "Denizen 2 IDE");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        int i = 2;

        public void NewTab()
        {
            TabPage tp = new TabPage("New Script " + i++);
            tabControl1.TabPages.Insert(tabControl1.TabCount - 1, tp);
            RichTextBox rtfb = new RichTextBox();
            rtfb.Location = RTFBox.Location;
            rtfb.Size = RTFBox.Size;
            Configure(rtfb);
            tp.Controls.Add(rtfb);
            tabControl1.SelectTab(tabControl1.TabCount - 2);
        }

        public void Configure(RichTextBox rtfb)
        {
            rtfb.AcceptsTab = true;
            rtfb.KeyPress += Rtfb_KeyPress;
            rtfb.AutoWordSelection = false;
            rtfb.ShowSelectionMargin = false;
        }

        private void Rtfb_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\t')
            {
                e.Handled = true;
                RTFBox.AppendText("    ");
            }
        }

        private void tabControl1_Selected(object sender, TabControlEventArgs e)
        {
            if (e.TabPage == plusButton)
            {
                NewTab();
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewTab();
        }
    }
}
