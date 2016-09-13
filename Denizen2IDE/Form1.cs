using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;

namespace Denizen2IDE
{
    public partial class Form1 : Form
    {
        Size Rel;
        Size TabRel;

        public static Encoding ENCODING = new UTF8Encoding(false);

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
            Scripts.Add(new LoadedScript() { FilePath = null, UnsavedName = tabPage1.Text });
            Configure(tabPage1, 0);
            tabControl1.MouseClick += TabControl1_MouseClick;
            tabControl1.MouseClick += TabControl1_MouseClick2;
        }

        private void TabControl1_MouseClick2(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }
            Point mp = new Point(e.X, e.Y);
            for (int i = 0; i < tabControl1.TabPages.Count - 1; i++)
            {
                if (tabControl1.GetTabRect(i).Contains(mp))
                {
                    TabOptions(e.X, e.Y, i);
                    return;
                }
            }
        }

        public void TabOptions(int x, int y, int tab)
        {
            ContextMenu cm = new ContextMenu();
            cm.MenuItems.Add("Close", (o, e) => CloseTab(tab));
            cm.MenuItems.Add("Close Others", (o, e) => CloseAllBut(tab));
            cm.MenuItems.Add("Close All To Left", (o, e) => CloseAllToLeft(tab));
            cm.MenuItems.Add("Close All To Right", (o, e) => CloseAllToRight(tab));
            cm.MenuItems.Add("Switch To", (o, e) => tabControl1.SelectTab(tab));
            cm.MenuItems.Add("Save", (o, e) => Save(tab));
            cm.MenuItems.Add("Save As...", (o, e) => SaveAs(tab));
            cm.Show(tabControl1, new Point(x, y));
        }

        public void CloseAllToRight(int tab)
        {
            for (int i = 0; i < tab; i++)
            {
                CloseTab(0);
            }
        }

        public void CloseAllToLeft(int tab)
        {
            int c = tabControl1.TabPages.Count;
            for (int i = tab + 2; i < c; i++)
            {
                CloseTab(tab + 1);
            }
        }

        public void CloseAllBut(int tab)
        {
            CloseAllToLeft(tab);
            CloseAllToRight(tab);
        }

        private void TabControl1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Middle)
            {
                return;
            }
            Point mp = new Point(e.X, e.Y);
            for (int i = 0; i < tabControl1.TabPages.Count - 1; i++)
            {
                if (tabControl1.GetTabRect(i).Contains(mp))
                {
                    CloseTab(i);
                    return;
                }
            }
        }

        public RichTextBox RTFBox
        {
            get
            {
                return (RichTextBox)tabControl1.SelectedTab.Controls[0];
            }
        }

        public RichTextBox ReferenceBox
        {
            get
            {
                return (RichTextBox)tabControl1.TabPages[0].Controls[0];
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
            Scripts[tabControl1.SelectedIndex].Saved = false;
            FixTabName(tabControl1.SelectedIndex);
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

        AboutBox AB = new AboutBox();

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AB.Show();
            AB.Focus();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        int CPage = 2;

        public void NewTab()
        {
            TabPage tp = new TabPage("New Script " + CPage++);
            Configure(tp, tabControl1.TabCount - 1);
            tabControl1.TabPages.Insert(tabControl1.TabCount - 1, tp);
            RichTextBox rtfb = new RichTextBox();
            rtfb.Location = ReferenceBox.Location;
            rtfb.Size = ReferenceBox.Size;
            Configure(rtfb);
            tp.Controls.Add(rtfb);
            tabControl1.SelectTab(tabControl1.TabCount - 2);
            Scripts.Add(new LoadedScript() { FilePath = null, UnsavedName = tp.Text });
        }

        public void CloseTab(int index)
        {
            if (Scripts.Count == 1)
            {
                NewTab();
            }
            // TODO: if (unsaved) { Are you sure(); }
            tabControl1.TabPages.RemoveAt(index);
            Scripts.RemoveAt(index);
        }

        public void Configure(TabPage tab, int ind)
        {
            // Any Configuration Needed
        }
        
        public void Configure(RichTextBox rtfb)
        {
            rtfb.AcceptsTab = true;
            rtfb.KeyPress += Rtfb_KeyPress;
            rtfb.AutoWordSelection = false;
            rtfb.ShowSelectionMargin = false;
            rtfb.TextChanged += richTextBox1_TextChanged;
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

        public List<LoadedScript> Scripts = new List<LoadedScript>();

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewTab();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Undo!
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Redo!
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Cut!
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Copy!
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Paste!
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Open!
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save(tabControl1.SelectedIndex);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAs(tabControl1.SelectedIndex);
        }

        private void saveAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Save all!
        }

        public void FixTabName(int tab)
        {
            // TODO: Maybe saved/unsaved images?
            if (Scripts[tab].FilePath != null)
            {
                if (Scripts[tab].Saved)
                {
                    tabControl1.TabPages[tab].Text = Path.GetFileName(Scripts[tab].FilePath);
                }
                else
                {
                    tabControl1.TabPages[tab].Text = Path.GetFileName(Scripts[tab].FilePath) + "*";
                }
            }
            else
            {
                if (GetText(tab).Length == 0)
                {
                    tabControl1.TabPages[tab].Text = Scripts[tab].UnsavedName;
                }
                else
                {
                    tabControl1.TabPages[tab].Text = Scripts[tab].UnsavedName + "*";
                }
            }
        }

        public void Save(int tab)
        {
            if (Scripts[tab].FilePath != null)
            {
                try
                {
                    File.WriteAllBytes(Scripts[tab].FilePath, ENCODING.GetBytes(GetText(tab)));
                    Scripts[tab].Saved = true;
                    FixTabName(tab);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Internal Exception");
                }
            }
            else
            {
                SaveAs(tab);
            }
        }

        public string GetText(int tab)
        {
            return ((RichTextBox)tabControl1.TabPages[tab].Controls[0]).Text;
        }

        public void SaveAs(int tab)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.AddExtension = true;
            sfd.DefaultExt = "yml";
            if (Scripts[tab].FilePath != null)
            {
                sfd.InitialDirectory = Path.GetDirectoryName(Scripts[tab].FilePath);
            }
            sfd.Filter = "Script Files (*.yml)|*.yml";
            DialogResult dr = sfd.ShowDialog(this);
            if (dr == DialogResult.OK || dr == DialogResult.Yes)
            {
                string fn = sfd.FileName;
                Scripts[tab].FilePath = fn;
                try
                {
                    File.WriteAllBytes(fn, ENCODING.GetBytes(GetText(tab)));
                    Scripts[tab].Saved = true;
                    FixTabName(tab);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Internal Exception");
                }
            }
        }
    }
}
