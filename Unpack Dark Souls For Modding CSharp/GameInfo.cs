using System.Collections.Generic;

namespace Unpack_Dark_Souls_For_Modding_CSharp
{
    /// <summary>
    /// Copied and modified from UXM
    /// </summary>
    class GameInfo
    {
        public long RequiredGB;
        public List<string> Archives;
        public ArchiveDictionary Dictionary;
        public List<string> BackupDirs;
        public List<string> DeleteDirs;
        public List<string> Replacements;
        public List<string> Replace;

        public GameInfo(string dictionaryStr)
        {
            Dictionary = new ArchiveDictionary(dictionaryStr);

            RequiredGB = 9;
            Archives = new List<string>() { "dvdbnd0", "dvdbnd1", "dvdbnd2", "dvdbnd3" };
            BackupDirs = new List<string>() { "dvdbnd0.bhd5", "dvdbnd0.bdt", "dvdbnd1.bhd5", "dvdbnd1.bdt", "dvdbnd2.bhd5", "dvdbnd2.bdt", "dvdbnd3.bhd5", "dvdbnd3.bdt", "DARKSOULS.exe" };
            DeleteDirs = new List<string>() { "chr", "event", "facegen", "font", "map", "menu", "msg", "mtd", "obj", "other", "param", "paramdef", "parts", "remo", "script", "sfx", "shader", "sound" };
            Replacements = new List<string>() { "dvdbnd0:", "dvdbnd1:", "dvdbnd2:", "dvdbnd3:", "hkxbnd:", "tpfbnd:", @"%stpf", "ⅆ댠球樒栄" };
            Replace = new List<string>() { "dvdroot:", "dvdroot:", "dvdroot:", "dvdroot:", "maphkx:", "map:/tx", "chr\u0000\u0000", "ⅆ댠樒栄"};
        }

        public static GameInfo GetGameInfo()
        {
            string dictionary = string.Join(",", GameData.fileList).Replace(",", "\r\n");

            return new GameInfo(dictionary);
        }
    }
}
