using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using Rlcm.Windows;

namespace Rlcm.Util
{
    public static class Updater
    {
        public static void Update()
        {
            var webClient = new WebClient();
            UpdateWindow updateWindow = null;

            try
            {
                var json = webClient.DownloadString("https://loriswit.com/rlcm/latest.json");
                var latestVersion = new JavaScriptSerializer().Deserialize<Version>(json);

                if (App.Version.Number >= latestVersion.Number)
                    return;

                updateWindow = new UpdateWindow(latestVersion);
                updateWindow.Show();

                var path = Environment.GetCommandLineArgs().First();
                var oldPath = path + "_tmp.exe";
                var newPath = path + "_" + latestVersion.Number + ".exe";

                // delete temporary files in case they still exist
                File.Delete(newPath);
                File.Delete(oldPath);

                webClient.DownloadFile(latestVersion.Url, newPath);

                // if download succeeded, rename the current program
                File.Move(path, oldPath);
                File.Move(newPath, path);

                // start the new version
                var process = new Process {StartInfo = {FileName = path, Arguments = "--updated"}};
                process.Start();

                Application.Current.Shutdown();
            }
            catch (WebException)
            {
                // ignore failed update
            }
            finally
            {
                updateWindow?.Close();
            }
        }

        public static void OnUpdated()
        {
            var path = Environment.GetCommandLineArgs().First();

            // try deleting the old version a few times (it may still be running)
            for (var i = 0; i < 10; ++i)
                try
                {
                    File.Delete(path + "_tmp.exe");
                    break;
                }
                catch (UnauthorizedAccessException)
                {
                    Thread.Sleep(500);
                }

            MessageBox.Show("Successfully updated to version " + App.Version.Name,
                "RLCM Updated", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
