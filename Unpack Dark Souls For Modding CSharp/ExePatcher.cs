using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Unpack_Dark_Souls_For_Modding_CSharp
{
    class ExePatcher
    {
        private static readonly Encoding UTF16 = Encoding.Unicode;
        private static string LogFile;

        public static string Patch(string exePath, IProgress<(double, string)> progress)
        {
            string gameDir = Path.GetDirectoryName(exePath);
            string exeName = Path.GetFileName(exePath);
            LogFile = $@"{gameDir}\UnpackLog.txt";
            progress.Report((0, Logger.Log("Preparing to patch...", LogFile)));
            

            GameInfo gameInfo = GameInfo.GetGameInfo();
            

            if (!File.Exists(gameDir + "\\unpackDS-backup\\" + exeName))
            {
                try
                {
                    Directory.CreateDirectory(gameDir + "\\unpackDS-backup");
                    File.Copy(exePath, gameDir + "\\unpackDS-backup\\" + exeName);
                }
                catch (Exception ex)
                {
                    return Logger.Log($"Failed to backup file:\r\n{exePath}\r\n\r\n{ex}", LogFile);
                }
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(exePath);
            }
            catch (Exception ex)
            {
                return Logger.Log($"Failed to read file:\r\n{exePath}\r\n\r\n{ex}", LogFile);
            }

            try
            {


                for (int i = 0; i < gameInfo.Replacements.Count; i++)
                {

                    string target = gameInfo.Replacements[i];
                    string replacement = gameInfo.Replace[i];

                    // Add 1.0 for preparation step
                    progress.Report(((i + 1.0) / (gameInfo.Replacements.Count + 1.0), Logger.Log($"Patching alias \"{target}\" ({i + 1}/{gameInfo.Replacements.Count})...", LogFile)));

                    replace(bytes, target, replacement);
                }
            }
            catch (Exception ex)
            {
                return Logger.Log($"Failed to patch file:\r\n{exePath}\r\n\r\n{ex}", LogFile);
            }

            try
            {
                File.WriteAllBytes(exePath, bytes);
            }
            catch (Exception ex)
            {
                return Logger.Log($"Failed to write file:\r\n{exePath}\r\n\r\n{ex}", LogFile);
            }

            progress.Report((1, Logger.Log("Patching complete!", LogFile)));
            return null;
        }

        private static void replace(byte[] bytes, string target, string replacement)
        {
            byte[] targetBytes = UTF16.GetBytes(target);
            byte[] replacementBytes = UTF16.GetBytes(replacement);
            if (targetBytes.Length != replacementBytes.Length)
                throw new ArgumentException($"Target length: {targetBytes.Length} | Replacement length: {replacementBytes.Length}");

            List<int> offsets = findBytes(bytes, targetBytes);
            foreach (int offset in offsets)
                Array.Copy(replacementBytes, 0, bytes, offset, replacementBytes.Length);
        }

        private static List<int> findBytes(byte[] bytes, byte[] find)
        {
            List<int> offsets = new List<int>();
            for (int i = 0; i < bytes.Length - find.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < find.Length; j++)
                {
                    if (find[j] != bytes[i + j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                    offsets.Add(i);
            }
            return offsets;
        }
    }
}
