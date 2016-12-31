using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Denizen2IDE
{
    static class Program
    {
        public static string VERSION = "0.0.6";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
