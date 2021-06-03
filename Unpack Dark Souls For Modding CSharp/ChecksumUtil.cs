using System;
using System.IO;
using System.Security.Cryptography;

namespace Unpack_Dark_Souls_For_Modding_CSharp
{
    public static class ChecksumUtil
    {

        //Modified https://makolyte.com/csharp-get-a-files-checksum-using-any-hashing-algorithm-md5-sha256/
        public static string GetChecksum(string filename)
        {
            using (var hasher = HashAlgorithm.Create("SHA256"))
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = hasher.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }

}
