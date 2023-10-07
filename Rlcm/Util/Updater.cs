using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using Rlcm.Game;
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

            // if the old patch is installed, update it
            var patchUpdated = false;
            var trainingRoom = new TrainingRoom();
            if (trainingRoom.CheckPatch(new byte[]
                {
                    0x86, 0xa, 0xb4, 0xcc, 0xf1, 0x9f, 0x94, 0xf1,
                    0xe7, 0xb8, 0xe7, 0xf3, 0x6b, 0x72, 0x82, 0x7e
                }))
            {
                trainingRoom.UninstallMod(skipWarning: true, skipRestore: true);
                trainingRoom.InstallMod(skipWarning: true);
                patchUpdated = true;
            }

            MessageBox.Show("Successfully updated to version " + App.Version.Name +
                            (patchUpdated ? "\n\nThe training room mod has been automatically updated." : "") +
                            "\n\nWhat's new?\n" +
                            "• The challenge menus are now displayed correctly in the training room.",
                "RLCM Updated", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
