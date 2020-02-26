using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using Rlcm.Util;

namespace Rlcm.Game
{
    public class Bundle
    {
        private string _filename;

        public Bundle()
        {
            _filename = Settings.GetValue("Bundle");
            if (NotFound())
                Locate();
        }

        public bool NotFound()
        {
            return _filename == null;
        }

        public void SetLocation(string location)
        {
            if (location == null)
                return;

            _filename = location;
            if (_filename.Last() != '\\')
                _filename += '\\';

            _filename += "Bundle_PC.ipk";

            Settings.SetValue("Bundle", _filename);
        }

        public void InstallTrainingMod(bool install)
        {
            const string paintingPrefix = "world/home/paintings_and_notifications/";
            var assets = new Dictionary<string, long>
            {
                {"world/home/brick/challenge/challenge_endless.isc", 0},
                {paintingPrefix + "painting_challengeendless/animation/painting_challengeendless_a1.tga", 0},
                {paintingPrefix + "painting_levels/textures/challenge/challenge_1.tga", 0},
                {paintingPrefix + "painting_levels/textures/challenge/challenge_2.tga", 0},
                {paintingPrefix + "painting_levels/textures/challenge/challenge_3.tga", 0},
                {paintingPrefix + "painting_levels/textures/challenge/challenge_4.tga", 0},
                {paintingPrefix + "painting_levels/textures/challenge/challenge_5.tga", 0}
            };

            using var file = File.Open(_filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            FindAssetsOffsets(file, ref assets);

            var writer = new BinaryWriter(file);
            foreach (var asset in assets)
            {
                var resourceName = asset.Key.Substring(asset.Key.LastIndexOf('/') + 1);
                resourceName = "Rlcm.Resources." + (install ? "Mod." : "Default.") + resourceName;

                writer.BaseStream.Seek(asset.Value, SeekOrigin.Begin);
                writer.Write(Resource.Get(resourceName));
            }
        }

        public bool IsModInstalled()
        {
            if (NotFound())
                return false;

            var assets = new Dictionary<string, long>
            {
                {"world/home/brick/challenge/challenge_endless.isc", 0}
            };

            try
            {
                using var file = File.Open(_filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                FindAssetsOffsets(file, ref assets);

                var reader = new BinaryReader(file);
                var modData = Resource.Get("Rlcm.Resources.Mod.challenge_endless.isc");
                reader.BaseStream.Seek(assets.First().Value, SeekOrigin.Begin);
                var gameData = reader.ReadBytes(modData.Length);

                return gameData.SequenceEqual(modData);
            }
            catch (IOException)
            {
                return false;
            }
        }

        private void Locate()
        {
            var views = new[] {RegistryView.Registry64, RegistryView.Registry32};
            foreach (var view in views)
            {
                var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");

                var path = (from subKey in key?.GetSubKeyNames()
                    select key?.OpenSubKey(subKey)
                    into program
                    where (string) program?.GetValue("DisplayName") == "Rayman Legends"
                    select (string) program.GetValue("InstallLocation")).FirstOrDefault();

                if (path == null)
                    continue;

                SetLocation(path);
                return;
            }
        }

        private static void FindAssetsOffsets(Stream file, ref Dictionary<string, long> assets)
        {
            var reader = new BinaryReader(file);

            reader.BaseStream.Seek(0xc, SeekOrigin.Begin);
            var baseOffset = ReadInt32(reader);

            reader.BaseStream.Seek(0x2c, SeekOrigin.Begin);
            var fileCount = ReadInt32(reader);

            var assetsLeft = assets.Count;
            for (var i = 0; i < fileCount; ++i)
            {
                reader.BaseStream.Seek(20, SeekOrigin.Current);
                var offset = ReadInt64(reader);
                var filename = ReadString(reader) + ReadString(reader);

                var assetName = filename.Replace("cache/itf_cooked/pc/", "").Replace(".ckd", "");
                if (assets.ContainsKey(assetName))
                {
                    assets[assetName] = baseOffset + offset;
                    if (--assetsLeft == 0)
                        break;
                }

                reader.BaseStream.Seek(8, SeekOrigin.Current);
            }
        }

        private static int ReadInt32(BinaryReader reader)
        {
            var data = reader.ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToInt32(data, 0);
        }

        private static long ReadInt64(BinaryReader reader)
        {
            var data = reader.ReadBytes(8);
            Array.Reverse(data);
            return BitConverter.ToInt64(data, 0);
        }

        private static string ReadString(BinaryReader reader)
        {
            var length = ReadInt32(reader);
            return new string(reader.ReadChars(length));
        }
    }
}
