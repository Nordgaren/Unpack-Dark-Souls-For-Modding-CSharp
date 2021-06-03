using SoulsFormats;
using System.Collections.Generic;
using System.IO;

namespace Unpack_Dark_Souls_For_Modding_CSharp
{
    static class YBinder
    {
        public static void WriteBinderFiles(BinderReader bnd, string targetDir, string sourceName)
        {
            var pathCounts = new Dictionary<string, int>();
            for (int i = 0; i < bnd.Files.Count; i++)
            {
                BinderFileHeader file = bnd.Files[i];

                string root = "";
                string path;
                if (Binder.HasNames(bnd.Format))
                {
                    path = YBUtil.UnrootBNDPath(file.Name, out root);
                }
                else if (Binder.HasIDs(bnd.Format))
                {
                    path = file.ID.ToString();
                }
                else
                {
                    path = i.ToString();
                }

                string suffix = "";
                if (pathCounts.ContainsKey(path))
                {
                    pathCounts[path]++;
                    suffix = $" ({pathCounts[path]})";
                }
                else
                {
                    pathCounts[path] = 1;
                }
                byte[] bytes = bnd.ReadFile(file);

                if (path.Contains(".dcx"))
                {
                    bytes = DCX.Decompress(bytes, out DCX.Type compression);
                    path = path.Replace(".dcx", "");
                }
                string outPath = $@"{targetDir}\{Path.GetFileNameWithoutExtension(path)}{suffix}{Path.GetExtension(path)}";
                Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                if (sourceName.Contains(".chrbnd"))
                {
                    if (outPath.Contains(".chrtpfbhd"))
                    {
                        File.WriteAllBytes(outPath, bytes);
                    }
                    continue;
                }
                File.WriteAllBytes(outPath, bytes);
            }
        }

    }
}
