﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace Denizen2IDE
{
    public partial class AboutBox : Form
    {
        public AboutBox()
        {
            InitializeComponent();
            versionLabel.Text = "Version: " + Program.VERSION;
        }

        private void LinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://denizenscript.com/");
        }

        private void AboutBox_Load(object sender, EventArgs e)
        {

        }
    }
}
