using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using Microsoft.Win32;
using Rlcm.Util;

namespace Rlcm.Game
{
    public class TrainingRoom
    {
        private string _location;
        private const string PatchName = "patch_PC.ipk";

        public TrainingRoom()
        {
            _location = Settings.GetValue("GameLocation");
            if (GameNotFound())
                LocateGame();
        }

        public bool GameNotFound()
        {
            return _location == null;
        }

        public void SetGameLocation(string location)
        {
            if (location == null)
                return;

            _location = location;
            if (_location.Last() != '\\')
                _location += '\\';

            Settings.SetValue("GameLocation", _location);
        }

        public void InstallMod()
        {
            var patchFilename = _location + PatchName;
            if (File.Exists(patchFilename))
            {
                MessageBox.Show(
                    "There is already a patch in the game folder.\n\n"
                    + "Installing the training room will replace the existing patch, "
                    + "which will be restored when uninstalling the training room.",
                    "Existing patch found", MessageBoxButton.OK, MessageBoxImage.Information);

                var uuid = Guid.NewGuid();
                File.Move(patchFilename, _location + uuid + ".ipk");
                Settings.SetValue("PreviousPatch", uuid.ToString());
            }

            var modData = Resource.Get("Rlcm.Resources.Mod.patch_PC.ipk");

            using var file = File.Open(patchFilename, FileMode.CreateNew, FileAccess.Write);
            new BinaryWriter(file).Write(modData);
        }

        public void UninstallMod()
        {
            var patchFilename = _location + PatchName;
            File.Delete(patchFilename);

            // restore the previous saved patch, if any
            var uuid = Settings.GetValue("PreviousPatch");
            if (uuid != null)
            {
                try
                {
                    File.Move(_location + uuid + ".ipk", patchFilename);
                }
                catch (IOException e)
                {
                    MessageBox.Show(e.Message, "Failed to restore previous patch",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                Settings.DeleteValue("PreviousPatch");
            }

            // show warning only once
            if (Settings.GetValue("UninstallWarning") == null)
            {
                MessageBox.Show(
                    "If you installed the mod with a previous version of RLCM, "
                    + "you may still see the training room painting in the game. "
                    + "To fix this, you need to reinstall the game.",
                    "Uninstalling the training room", MessageBoxButton.OK, MessageBoxImage.Warning);

                var date = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                Settings.SetValue("UninstallWarning", date.ToString());
            }
        }

        public bool IsModInstalled()
        {
            var patchFilename = _location + PatchName;

            if (GameNotFound() || !File.Exists(patchFilename))
                return false;

            byte[] checksum =
            {
                0x86, 0xa, 0xb4, 0xcc, 0xf1, 0x9f, 0x94, 0xf1,
                0xe7, 0xb8, 0xe7, 0xf3, 0x6b, 0x72, 0x82, 0x7e
            };

            using var file = File.OpenRead(patchFilename);
            var hash = MD5.Create().ComputeHash(file);
            return checksum.SequenceEqual(hash);
        }

        private void LocateGame()
        {
            // get game location from bundle path used in previous versions
            var bundlePath = Settings.GetValue("Bundle");
            if (bundlePath != null)
            {
                SetGameLocation(bundlePath.Replace("Bundle_PC.ipk", ""));
                Settings.DeleteValue("Bundle");
                return;
            }

            // get game location from registry
            var views = new[] {RegistryView.Registry64, RegistryView.Registry32};
            foreach (var view in views)
            {
                var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");

                var path = (from subKey in key?.GetSubKeyNames()
                    select key?.OpenSubKey(subKey)
                    into program
                    where (string) program?.GetValue("DisplayName") == "Rayman Legends" &&
                          !string.IsNullOrEmpty((string) program.GetValue("InstallLocation"))
                    select (string) program.GetValue("InstallLocation")).FirstOrDefault();

                if (path == null)
                    continue;

                SetGameLocation(path);
                return;
            }
        }
    }
}
