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
        // TODO: Dragging files onto the form

        Size Rel;
        Size TabRel;

        public static Encoding ENCODING = new UTF8Encoding(false);

        public Form1()
        {
            InitializeComponent();
            Timer t = new Timer()
            {
                Interval = 250
            };
            t.Tick += T_Tick;
            t.Start();
            Configure(richTextBox1);
            Rel = this.Size - richTextBox1.Size;
            TabRel = this.Size - tabControl1.Size;
            Scripts.Add(new LoadedScript() { FilePath = null, UnsavedName = tabPage1.Text });
            Configure(tabPage1, 0);
            tabControl1.MouseClick += TabControl1_MouseClick;
            tabControl1.MouseClick += TabControl1_MouseClick2;
            this.MouseWheel += Form1_MouseWheel;
            this.FormClosing += Form1_FormClosing;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            for (int i = 0; i < Scripts.Count; i++)
            {
                if ((!Scripts[i].Saved && Scripts[i].FilePath != null)
                    || Scripts[i].FilePath == null && GetText(i).Length > 0)
                {
                    DialogResult res = MessageBox.Show("One or more scripts are not saved!"
                        + Environment.NewLine + "Are you sure you want to close them?",
                        "Are You Sure", MessageBoxButtons.YesNoCancel);
                    if (res != DialogResult.Yes)
                    {
                        e.Cancel = true;
                    }
                    return;
                }
            }
        }

        float zoom = 1;

        private void Form1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (!ModifierKeys.HasFlag(Keys.Control))
            {
                return;
            }
            if (e.Delta > 0)
            {
                zoom *= 1.5f * (e.Delta / 120f);
            }
            else if (e.Delta < 0)
            {
                zoom /= 1.5f * (-e.Delta / 120f);
            }
            if (zoom < 1f / 10f)
            {
                zoom = 1f / 10f;
            }
            else if (zoom > 10f)
            {
                zoom = 10f;
            }
            textChanged = true;
            T_Tick(null, null);
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
            int c = tabControl1.TabPages.Count;
            for (int i = tab + 2; i < c; i++)
            {
                CloseTab(tab + 1);
            }
        }

        public void CloseAllToLeft(int tab)
        {
            for (int i = 0; i < tab; i++)
            {
                CloseTab(0);
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
                return (RichTextBox)tabControl1.SelectedTab.Controls[1];
            }
        }

        public RichTextBox LinesRTFBox
        {
            get
            {
                return (RichTextBox)tabControl1.SelectedTab.Controls[0];
            }
        }

        public RichTextBox LineReferenceBox
        {
            get
            {
                return (RichTextBox)tabControl1.TabPages[0].Controls[0];
            }
        }

        public RichTextBox ReferenceBox
        {
            get
            {
                return (RichTextBox)tabControl1.TabPages[0].Controls[1];
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

        // TODO: Linux-y version of this WINDOWS locked code!
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
        
        public RTFBuilder ColorArg(string arg)
        {
            if (arg.StartsWith("\"") || arg.StartsWith("\'"))
            {
                bool colon = arg.EndsWith(arg[0].ToString() + ":");
                if (arg.Length == 1 || (!arg.EndsWith(arg[0].ToString()) && !colon))
                {
                    return RTFBuilder.Colored(RTFBuilder.WavyUnderline(RTFBuilder.For(arg)), ColorTable.PINK);
                }
                RTFBuilder res = new RTFBuilder();
                res.Append(RTFBuilder.Colored(RTFBuilder.For(arg[0].ToString()), ColorTable.DARK_CYAN));
                res.Append(RTFBuilder.Colored(ColorArg(arg.Substring(1, arg.Length - (colon ? 3 : 2))), ColorTable.DARK_CYAN));
                res.Append(RTFBuilder.Colored(RTFBuilder.For(arg[0].ToString()), ColorTable.DARK_CYAN));
                if (colon)
                {
                    res.Append(RTFBuilder.For(":"));
                }
                return res;
            }
            RTFBuilder built = new RTFBuilder();
            StringBuilder sb = new StringBuilder();
            int deep = 0;
            for (int i = 0; i < arg.Length; i++)
            {
                if (arg[i] == '<')
                {
                    if (deep == 0)
                    {
                        built.Append(RTFBuilder.For(sb.ToString()));
                        sb.Clear();
                    }
                    deep++;
                }
                else if (deep > 0 && arg[i] == '>')
                {
                    deep--;
                    if (deep == 0)
                    {
                        built.Append(RTFBuilder.BackColored(RTFBuilder.Colored(RTFBuilder.For("<"), ColorTable.DARK_GRAY), ColorTable.LIGHT_GRAY));
                        if (sb.Length > 1)
                        {
                            built.Append(RTFBuilder.BackColored(RTFBuilder.Colored(ColorInsideTag(sb.ToString().Substring(1, sb.Length - 1)), ColorTable.DARK_GRAY), ColorTable.LIGHT_GRAY));
                        }
                        built.Append(RTFBuilder.BackColored(RTFBuilder.Colored(RTFBuilder.For(">"), ColorTable.DARK_GRAY), ColorTable.LIGHT_GRAY));
                        sb.Clear();
                        continue;
                    }
                }
                sb.Append(arg[i]);
            }
            built.Append(RTFBuilder.For(sb.ToString()));
            return built;
        }

        public RTFBuilder ColorInsideTag(string arg)
        {
            RTFBuilder built = new RTFBuilder();
            StringBuilder sb = new StringBuilder();
            int deep = 0;
            for (int i = 0; i < arg.Length; i++)
            {
                if (arg[i] == '[')
                {
                    if (deep == 0)
                    {
                        built.Append(RTFBuilder.For(sb.ToString()));
                        sb.Clear();
                    }
                    deep++;
                }
                else if (deep > 0 && arg[i] == ']')
                {
                    deep--;
                    if (deep == 0)
                    {
                        built.Append(RTFBuilder.BackColored(RTFBuilder.Colored(RTFBuilder.For("["), ColorTable.LIGHT_GRAY), ColorTable.DARK_GRAY));
                        if (sb.Length > 1)
                        {
                            built.Append(RTFBuilder.BackColored(RTFBuilder.Colored(ColorArg(sb.ToString().Substring(1, sb.Length - 1)), ColorTable.LIGHT_GRAY), ColorTable.DARK_GRAY));
                        }
                        built.Append(RTFBuilder.BackColored(RTFBuilder.Colored(RTFBuilder.For("]"), ColorTable.LIGHT_GRAY), ColorTable.DARK_GRAY));
                        sb.Clear();
                        continue;
                    }
                }
                sb.Append(arg[i]);
            }
            built.Append(RTFBuilder.For(sb.ToString()));
            return built;
        }

        public List<string> CleverSplit(string input)
        {
            List<string> res = new List<string>();
            StringBuilder built = new StringBuilder();
            char quoted = '\0';
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '\0')
                {
                    continue;
                }
                if (quoted == '\0' && input[i] == ' ')
                {
                    res.Add(built.ToString());
                    built.Clear();
                    continue;
                }
                else if (quoted == '\0' && (input[i] == '\"' || input[i] == '\''))
                {
                    quoted = input[i];
                }
                else if (quoted == input[i] && i + 1 < input.Length && input[i + 1] == ' ')
                {
                    quoted = '\0';
                }
                built.Append(input[i]);
            }
            res.Add(built.ToString());
            return res;
        }

        public RTFBuilder ColorArgs(string args)
        {
            RTFBuilder res = new RTFBuilder();
            List<string> dat = CleverSplit(args);
            for (int i = 0; i < dat.Count; i++)
            {
                res.Append(ColorArg(dat[i]));
                if (i + 1 < dat.Count)
                {
                    res.Append(RTFBuilder.For(" "));
                }
            }
            return res;
        }

        bool NoDup = false;
        
        // TODO: Accelerate this method as much as possible using async and whatever else we can do!
        // Maybe also replace RTFBuilder with lower-level calls somehow.
        private void T_Tick(object sender, EventArgs e)
        {
            if (!textChanged || NoDup)
            {
                return;
            }
            NoDup = true;
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
                        int dash = line.IndexOf('-');
                        rtf.Append(RTFBuilder.Colored(RTFBuilder.For(line.Substring(0, dash + 1)), ColorTable.BLACK));
                        string fullcmd = line.Substring(dash + 1);
                        if (fullcmd.Length <= 1)
                        {
                            rtf.Append(RTFBuilder.Colored(RTFBuilder.For(fullcmd), ColorTable.BLACK));
                        }
                        else
                        {
                            if (fullcmd[0] != ' ')
                            {
                                rtf.Append(RTFBuilder.Colored(RTFBuilder.WavyUnderline(RTFBuilder.For(fullcmd)), ColorTable.PINK));
                            }
                            else
                            {
                                rtf.Append(RTFBuilder.For(" "));
                                string basecmd = fullcmd.Substring(1);
                                if (basecmd.Length > 0)
                                {
                                    int space = basecmd.IndexOf(' ');
                                    if (space == -1)
                                    {
                                        rtf.Append(RTFBuilder.Colored(RTFBuilder.For(basecmd), ColorTable.PURPLE));
                                    }
                                    else
                                    {
                                        rtf.Append(RTFBuilder.Colored(RTFBuilder.For(basecmd.Substring(0, space)), ColorTable.PURPLE));
                                        rtf.Append(RTFBuilder.For(" "));
                                        rtf.Append(RTFBuilder.Colored(ColorArgs(basecmd.Substring(space + 1)), ColorTable.BLACK));
                                    }
                                }
                            }
                        }
                    }
                    else if (trim.EndsWith(":"))
                    {
                        RTFBuilder coloredcolon = RTFBuilder.Colored(RTFBuilder.For(":"), ColorTable.GRAY);
                        rtf.Append(RTFBuilder.Colored(RTFBuilder.For(line).Replace(":", coloredcolon), ColorTable.BLUE));
                    }
                    else if (trim.Contains(": "))
                    {
                        int ind = line.IndexOf(": ");
                        RTFBuilder coloredcolon = RTFBuilder.Colored(RTFBuilder.For(":"), ColorTable.GRAY);
                        rtf.Append(RTFBuilder.Colored(RTFBuilder.For(line.Substring(0, ind)).Replace(":", coloredcolon), ColorTable.BLUE));
                        rtf.Append(coloredcolon);
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
            RTFBox.Rtf = rtf.FinalOutput((int)(zoom * 24));
            // Fix line numbers
            int lineCount = RTFBox.GetLineFromCharIndex(RTFBox.Text.Length) + 1;
            RTFBuilder bLines = new RTFBuilder();
            for (int i = 0; i < lineCount; i++)
            {
                bLines.Append(RTFBuilder.For((i + 1).ToString()));
                bLines.AppendLine();
            }
            LinesRTFBox.Rtf = bLines.FinalOutput((int)(zoom * 24));
            LinesRTFBox.SelectAll();
            LinesRTFBox.SelectionAlignment = HorizontalAlignment.Right;
            LinesRTFBox.ShowSelectionMargin = false;
            LinesRTFBox.ScrollBars = RichTextBoxScrollBars.None;
            // Set back to normal
            Resume(this);
            RTFBox.Select(start, len); // TODO: Mess with selecting fresh text a bit less often.
            SetScrollPos(RTFBox, scroll);
            RTFBox.Invalidate();
            LinesRTFBox.Invalidate();
            textChanged = false;
            Scripts[tabControl1.SelectedIndex].Saved = Scripts[tabControl1.SelectedIndex].IgnoreOneSaveNotif;
            Scripts[tabControl1.SelectedIndex].IgnoreOneSaveNotif = false;
            FixTabName(tabControl1.SelectedIndex);
            z = zoom;
            NoDup = false;
        }

        float z = -1;

        bool textChanged = true;
        
        private void RichTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (!NoDup)
            {
                textChanged = true;
            }
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            tabControl1.Size = this.Size - TabRel;
            //RTFBox.Size = this.Size - Rel;
        }

        AboutBox AB = new AboutBox();

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (AB == null || AB.IsDisposed)
            {
                AB = new AboutBox();
                AB.FormClosing += (one, two) => { AB = null; };
            }
            AB.Show();
            AB.Focus();
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        int CPage = 2;

        public void NewTab()
        {
            TabPage tp = new TabPage("New Script " + CPage++);
            Configure(tp, tabControl1.TabCount - 1);
            tabControl1.TabPages.Insert(tabControl1.TabCount - 1, tp);
            RichTextBox linertfb = new RichTextBox()
            {
                Location = LineReferenceBox.Location,
                Size = LineReferenceBox.Size,
                Font = LineReferenceBox.Font,
                ReadOnly = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };
            tp.Controls.Add(linertfb);
            RichTextBox rtfb = new RichTextBox()
            {
                Location = ReferenceBox.Location,
                Size = ReferenceBox.Size,
                Font = ReferenceBox.Font
            };
            Configure(rtfb);
            tp.Controls.Add(rtfb);
            tabControl1.SelectTab(tabControl1.TabCount - 2);
            Scripts.Add(new LoadedScript() { FilePath = null, UnsavedName = tp.Text });
        }

        public void CloseTab(int index)
        {
            if ((!Scripts[index].Saved && Scripts[index].FilePath != null)
                || Scripts[index].FilePath == null && GetText(index).Length > 0)
            {
                DialogResult res = MessageBox.Show("Script " + Scripts[index].FilePath + " is not saved!"
                    + Environment.NewLine + "Are you sure you want to close it?",
                    "Are You Sure", MessageBoxButtons.YesNoCancel);
                if (res != DialogResult.Yes)
                {
                    return;
                }
            }
            if (Scripts.Count == 1)
            {
                NewTab();
            }
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
            rtfb.TextChanged += RichTextBox1_TextChanged;
            rtfb.MouseWheel += Form1_MouseWheel;
            rtfb.KeyDown += Rtfb_KeyDown;
            rtfb.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            rtfb.FontChanged += Rtfb_FontChanged;
            rtfb.VScroll += Rtfb_VScroll;
        }

        private void Rtfb_VScroll(object sender, EventArgs e)
        {
            Point pt = GetScrollPos(RTFBox);
            SetScrollPos(LinesRTFBox, pt);
        }

        private void Rtfb_FontChanged(object sender, EventArgs e)
        {
            LinesRTFBox.Font = RTFBox.Font;
        }

        private void Rtfb_KeyDown(object sender, KeyEventArgs e)
        {
            if (!e.Shift && e.KeyData == Keys.Enter)
            {
                Point pt = GetScrollPos(RTFBox);
                e.Handled = true;
                int s = RTFBox.SelectionStart;
                int spaces = 0;
                string t = RTFBox.Text;
                for (int i = s - 1; i >= 0; i--)
                {
                    if (t[i] == '\n')
                    {
                        for (int x = i + 1; x < s; x++)
                        {
                            if (t[x] == ' ')
                            {
                                spaces++;
                            }
                            else
                            {
                                break;
                            }
                        }
                        break;
                    }
                }
                RTFBox.Text = RTFBox.Text.Substring(0, s) + Environment.NewLine + new string(' ', spaces) + RTFBox.Text.Substring(s + RTFBox.SelectionLength);
                RTFBox.Select(s + Environment.NewLine.Length + spaces - 1, 0);
                SetScrollPos(RTFBox, pt);
                T_Tick(null, null);
            }
        }

        private void Rtfb_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\t')
            {
                e.Handled = true;
                int s = RTFBox.SelectionStart;
                RTFBox.Text = RTFBox.Text.Substring(0, s) + "    " + RTFBox.Text.Substring(s + RTFBox.SelectionLength);
                RTFBox.Select(s + 4, 0);
            }
        }

        private void TabControl1_Selected(object sender, TabControlEventArgs e)
        {
            if (e.TabPage == plusButton)
            {
                NewTab();
            }
            textChanged = true;
        }

        public List<LoadedScript> Scripts = new List<LoadedScript>();

        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewTab();
        }

        private void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Undo!
        }

        private void RedoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Redo!
        }

        private void CutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Cut!
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Copy!
        }

        private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: Paste!
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Open();
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save(tabControl1.SelectedIndex);
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAs(tabControl1.SelectedIndex);
        }

        private void SaveAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < Scripts.Count; i++)
            {
                Save(i);
            }
        }

        public void Open()
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                AddExtension = true,
                DefaultExt = "yml",
                Filter = "Script Files (*.yml)|*.yml",
                Multiselect = true
            };
            DialogResult dr = ofd.ShowDialog(this);
            if (dr == DialogResult.OK || dr == DialogResult.Yes)
            {
                try
                {
                    foreach (string fn in ofd.FileNames)
                    {
                        string data = File.ReadAllText(fn);
                        NewTab();
                        int tab = Scripts.Count - 1;
                        Scripts[tab].FilePath = fn;
                        SetText(tab, data);
                        Scripts[tab].Saved = true;
                        Scripts[tab].IgnoreOneSaveNotif = true;
                        FixTabName(tab);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Internal Exception");
                }
            }
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

        public void SetText(int tab, string txt)
        {
            ((RichTextBox)tabControl1.TabPages[tab].Controls[0]).Text = txt;
        }

        public void SaveAs(int tab)
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                AddExtension = true,
                DefaultExt = "yml"
            };
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

        private void ZoomInToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form1_MouseWheel(null, new MouseEventArgs(MouseButtons.None, 0, 0, 0, 120));
        }

        private void ZoomOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form1_MouseWheel(null, new MouseEventArgs(MouseButtons.None, 0, 0, 0, -120));
        }

        private void ResetZoomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            zoom = 1f;
            Form1_MouseWheel(null, new MouseEventArgs(MouseButtons.None, 0, 0, 0, 0));
        }

        private void RichTextBox2_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
