using Microsoft.Win32;

namespace Rlcm.Util
{
    public static class Settings
    {
        private const string Company = "LorisWit";
        private const string AppName = "RLCM";

        public static string GetValue(string name)
        {
            var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\" + Company + "\\" + AppName);
            return (string) key?.GetValue(name);
        }

        public static void SetValue(string name, string value)
        {
            var key = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);

            key = key?.CreateSubKey(Company)?.CreateSubKey(AppName);
            key?.SetValue(name, value);
        }

        public static void DeleteValue(string name)
        {
            var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\" + Company + "\\" + AppName, true);
            key?.DeleteValue(name);
        }
    }
}
