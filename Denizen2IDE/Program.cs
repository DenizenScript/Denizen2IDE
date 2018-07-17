using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Globalization;

namespace Denizen2IDE
{
    static class Program
    {
        /// <summary>
        /// Program version.
        /// </summary>
        public static readonly string VERSION = Assembly.GetCallingAssembly().GetName().Version.ToString();

        public static string[] START_ARGS { get; private set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            START_ARGS = args;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DenizenIDEForm());
        }
    }
}
