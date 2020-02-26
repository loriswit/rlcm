using System.Reflection;

namespace Rlcm.Util
{
    public static class Resource
    {
        public static byte[] Get(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream == null)
                return null;

            var data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);
            return data;
        }
    }
}
