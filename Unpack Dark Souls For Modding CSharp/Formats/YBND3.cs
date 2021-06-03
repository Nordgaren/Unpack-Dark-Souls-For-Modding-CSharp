using SoulsFormats;
using System.IO;

namespace Unpack_Dark_Souls_For_Modding_CSharp
{
    static class YBND3
    {
        public static void Unpack(this BND3Reader bnd, string sourceName, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            YBinder.WriteBinderFiles(bnd, targetDir, sourceName);
        }

    }
}
