using SoulsFormats;
using System.IO;

namespace Unpack_Dark_Souls_For_Modding_CSharp
{
    static class YBXF3
    {
        public static void Unpack(this BXF3Reader bxf, string bhdName, string bdtName, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            YBinder.WriteBinderFiles(bxf, targetDir, bhdName);
        }

    }
}
