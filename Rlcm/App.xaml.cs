using System;
using System.Linq;
using System.Windows;
using Rlcm.Util;
using Rlcm.Windows;
using Version = Rlcm.Util.Version;

namespace Rlcm
{
    public partial class App
    {
        public static Version Version;

        private void AppStartup(object sender, StartupEventArgs startupEventArgs)
        {
            Version.Number = 300;
            Version.Name = "3.0.0";

            var args = Environment.GetCommandLineArgs();
            if (args.Contains("--updated") || args.Contains("-u"))
                Updater.OnUpdated();

            if (!args.Contains("--no-update") && !args.Contains("-n"))
                Updater.Update();

            new MainWindow().Show();
        }
    }
}
