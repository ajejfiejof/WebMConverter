using System;
using System.Net;
using System.Windows.Forms;

namespace WebMConverter.Updater
{
    static partial class Program
    {
        public const string GitHubApiUrl = @"https://api.github.com/repos/ajejfiejof/WebMConverter/releases/latest";
        public const string UserAgent = @"WebMConverter-Updater";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch { }

            if (args.Length == 0)
            {
                MessageBox.Show("No mode specified. You shouldn't be running this directly!");
                return 1;
            }

            var mode = args[0];
            switch (mode)
            {
                case "check":
                    var current = args[1];
                    string output;

                    var success = CheckUpdate(current, out output);
                    Console.Write(output);

                    return success ? 0 : 1;

                case "update":
                    Application.Run(new UpdateUI());
                    return 0;
            }

            Console.WriteLine("Invalid mode");
            return 1;
        }
    }
}
