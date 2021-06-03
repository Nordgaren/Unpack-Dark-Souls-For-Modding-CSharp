using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Unpack_Dark_Souls_For_Modding_CSharp
{
    /// <summary>
    /// Copied and modified from UXM and Yabber
    /// </summary>
    public class Unpacker
    {
        private const int WRITE_LIMIT = 1024 * 1024 * 100;
        private static string LogFile;

        public static string Unpack(string exePath, IProgress<(double, string)> progress)
        {
            string gameDir = Path.GetDirectoryName(exePath);

            LogFile = $@"{gameDir}\UnpackLog.txt";

            if (File.Exists(LogFile))
                File.Delete(LogFile);

            GameInfo gameInfo = GameInfo.GetGameInfo();

            string drive = Path.GetPathRoot(Path.GetFullPath(gameDir));
            DriveInfo driveInfo = new DriveInfo(drive);

            bool unpacked = CheckDIR(gameDir, gameInfo);
            bool patched = CheckEXE(exePath);

            if (unpacked && patched)
                return Logger.Log("Dark Souls is already unpacked!", LogFile);

            progress.Report((0, "Preparing to unpack..."));
            if (driveInfo.AvailableFreeSpace < gameInfo.RequiredGB * 1024 * 1024 * 1024)
            {
                return Logger.Log($"You are out of Disk space. You need at least {gameInfo.RequiredGB}GB. Please free up some space and try to unpack, again.", LogFile);
            }

            Restore(exePath, progress);

            var archivesExist = CheckArchives(gameDir, gameInfo);

            if (!archivesExist)
            {
                if (patched)
                    return Logger.Log("EXE already patched!", LogFile);

                ExePatcher.Patch(exePath, progress);
                return Logger.Log("EXE successfully patched", LogFile);
            }

            try
            {
                progress.Report((0, Logger.Log("Attempting to backup...", LogFile)));
                BackupDirs(gameDir, gameInfo, progress);
            }
            catch (Exception ex)
            {
                return Logger.Log($"Failed to back up directories.\r\n\r\n{ex}", LogFile);
            }

            for (int i = 0; i < gameInfo.Archives.Count; i++)
            {
                string archive = gameInfo.Archives[i];
                string error = UnpackArchive(gameDir, archive, i,
                    gameInfo.Archives.Count, BHD5.Game.DarkSouls1, gameInfo.Dictionary, progress).Result;
                if (error != null)
                    return Logger.Log(error, LogFile);
            }

            progress.Report((0, Logger.Log(@"Grabbing missing", LogFile)));
            GetBHD(gameDir, progress);

            progress.Report((0, Logger.Log("Creating c4110 file", LogFile)));
            CreateC4110(gameDir);

            progress.Report((0, Logger.Log(@"Moving map tpf files", LogFile)));
            MoveTPFs(gameDir);

            progress.Report((0, Logger.Log(@"Extracting bhd/bdt pairs", LogFile)));
            ExtractBHD(gameDir, progress);

            //await UnDCX(gameDir);
            progress.Report((1, Logger.Log("Unpacking complete!", LogFile)));

            if(!patched)
                ExePatcher.Patch(exePath, progress);

            progress.Report((1, Logger.Log("Cleaning Archives", LogFile)));
            CleanupArchives(exePath);

            return null;
        }

        private static void BackupDirs(string gameDir, GameInfo gameInfo, IProgress<(double, string)> progress)
        {
            for (int i = 0; i < gameInfo.BackupDirs.Count; i++)
            {
                string backup = gameInfo.BackupDirs[i];
                

                string backupSource = $@"{gameDir}\{backup}";
                string backupTarget = $@"{gameDir}\unpackDS-backup\{backup}";

                if (!Directory.Exists($@"{gameDir}\unpackDS-backup\"))
                    Directory.CreateDirectory($@"{gameDir}\unpackDS-backup\");

                if (File.Exists(backupSource) && !File.Exists(backupTarget))
                {
                    progress.Report(((1.0 + (double)i / gameInfo.BackupDirs.Count) / (gameInfo.Archives.Count + 2.0),
                    $"Backing up directory \"{backup}\" ({i + 1}/{gameInfo.BackupDirs.Count})..."));
                    File.Copy(backupSource, backupTarget);
                }
            }
        }

        private static void CleanupArchives(string installPath)
        {
            var archives = Directory.GetFiles(Path.GetDirectoryName(installPath), "dvdbnd*", SearchOption.TopDirectoryOnly);

            foreach (var file in archives)
            {
                File.Delete(file);
            }
        }

        private static bool CheckEXE(string exePath)
        {
            bool patched = true;
            if (!GameData.Checksum.ContainsKey(ChecksumUtil.GetChecksum(exePath)))
            {
                patched = false;
            }

            return patched;
        }

        private static bool CheckDIR(string gameDir, GameInfo gameInfo)
        {
            bool unpacked = true;
            foreach (var directory in gameInfo.DeleteDirs)
            {
                if (!Directory.Exists($@"{gameDir}\{directory}"))
                    unpacked = false;
            }

            return unpacked;
        }

        private static bool CheckArchives(string gameDir, GameInfo gameInfo)
        {
            bool result = true;
            foreach (var file in gameInfo.BackupDirs)
            {
                string dvd = $@"{gameDir}\{file}";

                if (!File.Exists(dvd))
                    result = false;
            }

            return result;
        }

        private static async Task<string> UnpackArchive(string gameDir, string archive, int index, int total,
            BHD5.Game gameVersion, ArchiveDictionary archiveDictionary, IProgress<(double, string)> progress)
        {
            progress.Report(((index + 2.0) / (total + 2.0), Logger.Log($"Loading {archive}...", LogFile)));
            
            string bhdPath = $@"{gameDir}\{archive}.bhd";
            string bdtPath = $@"{gameDir}\{archive}.bdt";

            if (gameVersion == BHD5.Game.DarkSouls1)
                bhdPath = $@"{gameDir}\{archive}.bhd5";

            if (File.Exists(bhdPath) && File.Exists(bdtPath))
            {
                BHD5 bhd;

                try
                {
                    using (FileStream bhdStream = File.OpenRead(bhdPath))
                    {
                        bhd = BHD5.Read(bhdStream, gameVersion);
                    }
                }
                catch (OverflowException ex)
                {
                    return Logger.Log($"Failed to open BHD:\n{bhdPath}\n\n{ex}", LogFile);
                }

                int fileCount = bhd.Buckets.Sum(b => b.Count);

                try
                {
                    var asyncFileWriters = new List<Task<long>>();
                    using (FileStream bdtStream = File.OpenRead(bdtPath))
                    {
                        int currentFile = -1;
                        long writingSize = 0;

                        foreach (BHD5.Bucket bucket in bhd.Buckets)
                        {

                            foreach (BHD5.FileHeader header in bucket)
                            {

                                currentFile++;

                                string path;
                                if (archiveDictionary.GetPath(header.FileNameHash, out path))
                                {
                                    path = gameDir + path.Replace('/', '\\');
                                    if (File.Exists(path))
                                        continue;
                                }
                                else
                                {
                                    return Logger.Log($"Failed to read file in:\r\n{archive}\r\n\r\n", LogFile);
                                }

                                progress.Report(((index + 2.0 + currentFile / (double)fileCount) / (total + 2.0),
                                    $"Unpacking {archive} ({currentFile + 1}/{fileCount})..."));

                                while (asyncFileWriters.Count > 0 && writingSize + header.PaddedFileSize > WRITE_LIMIT)
                                {
                                    for (int i = 0; i < asyncFileWriters.Count; i++)
                                    {
                                        if (asyncFileWriters[i].IsCompleted)
                                        {
                                            writingSize -= await asyncFileWriters[i];
                                            asyncFileWriters.RemoveAt(i);
                                        }
                                    }

                                    if (asyncFileWriters.Count > 0 && writingSize + header.PaddedFileSize > WRITE_LIMIT)
                                        Thread.Sleep(10);
                                }

                                byte[] bytes;
                                try
                                {
                                    bytes = header.ReadFile(bdtStream);

                                    if (path.Contains(".dcx"))
                                    {
                                        bytes = DCX.Decompress(bytes, out DCX.Type compression);
                                        path = path.Replace(".dcx", "");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return Logger.Log($"Failed to read file:\r\n{path}\r\n\r\n{ex}", LogFile);
                                }

                                try
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                                    writingSize += bytes.Length;
                                    asyncFileWriters.Add(WriteFileAsync(path, bytes));
                                }
                                catch (Exception ex)
                                {
                                    return Logger.Log($"Failed to write file:\r\n{path}\r\n\r\n{ex}", LogFile);
                                }
                            }
                        }
                    }

                    foreach (Task<long> task in asyncFileWriters)
                        await task;
                }
                catch (Exception ex)
                {
                    return Logger.Log($"Failed to unpack BDT:\r\n{bdtPath}\r\n\r\n{ex}", LogFile);
                }
            }
            return null;
        }

        private static async Task<long> WriteFileAsync(string path, byte[] bytes)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                await fs.WriteAsync(bytes, 0, bytes.Length);
            }

            return bytes.Length;
        }

        private static void GetBHD(string gameDir, IProgress<(double, string)> progress)
        {
            var bdt = Directory.GetFiles(gameDir, "*.chrtpfbdt", SearchOption.AllDirectories);

            var position = 0;

            foreach (var path in bdt)
            {
                position++;
                var target = Path.GetDirectoryName(path) + @"\" + Path.GetFileNameWithoutExtension(path) + ".chrbnd";
                var percent = (double)position/bdt.Length;
                progress.Report((percent ,$"Unpacking BND3 ({position}/{bdt.Length}): {target.Replace(gameDir, "")}..."));
                UnpackBND(target);
            }
        }

        public static void UnpackBND(string sourceFile)
        {
            string sourceDir = Path.GetDirectoryName(sourceFile);
            string filename = Path.GetFileName(sourceFile);
            string targetDir = $"{sourceDir}";

            using (var bnd = new BND3Reader(sourceFile))
            {
                bnd.Unpack(filename, targetDir);
            }

        }

        private static void CreateC4110(string gameDir)
        {
            string path = $@"{gameDir}\chr\c4110.chrtpfbhd";

            File.WriteAllBytes(path, GameData.c4110);
        }

        private static void MoveTPFs(string gameDir)
        {
            Directory.CreateDirectory($@"{gameDir}\map\tx");
            var tpfbdt = Directory.GetFiles(gameDir, "*.tpfbdt", SearchOption.AllDirectories);
            var tpfbhd = Directory.GetFiles(gameDir, "*.tpfbhd", SearchOption.AllDirectories);
            foreach (var file in tpfbdt)
            {
                File.Move(file, $@"{gameDir}\map\tx\{Path.GetFileName(file)}");
            }

            foreach (var file in tpfbhd)
            {
                File.Move(file, $@"{gameDir}\map\tx\{Path.GetFileName(file)}");
            }
        }

        private static void ExtractBHD(string gameDir, IProgress<(double, string)> progress)
        {
            var bhd = Directory.GetFiles(gameDir, "*bhd", SearchOption.AllDirectories).Where(x => !x.Contains("bhd5")).ToArray();

            var position = 0;

            foreach (var filePath in bhd)
            {
                position++;
                var percent = (double)position / bhd.Length;
                progress.Report((percent, $"Unpacking BXF3 ({position}/{bhd.Length}): {filePath.Replace(gameDir, "")}..."));
                UnpackBHD(filePath);
                var bdt = filePath.Replace("bhd", "bdt");
                File.Delete(filePath);
                File.Delete(bdt);
            }
            
        }

        public static void UnpackBHD(string sourceFile)
        {
            string sourceDir = Path.GetDirectoryName(sourceFile);
            string filename = Path.GetFileName(sourceFile);
            string targetDir = $"{sourceDir}";
            if (filename.Contains(".chrtpfbhd"))
                targetDir += $@"\{Path.GetFileNameWithoutExtension(filename)}";

            string bdtExtension = Path.GetExtension(filename).Replace("bhd", "bdt");
            string bdtFilename = $"{Path.GetFileNameWithoutExtension(filename)}{bdtExtension}";
            string bdtPath = $"{sourceDir}\\{bdtFilename}";
            if (File.Exists(bdtPath))
            {
                using (var bxf = new BXF3Reader(sourceFile, bdtPath))
                {
                    bxf.Unpack(filename, bdtFilename, targetDir);
                }
            }
            else
            {
                //progress.Report($"BDT not found for BHD: {filename}");
            }

        }

        public static string Restore(string exePath, IProgress<(double, string)> progress)
        {
            string gameDir = Path.GetDirectoryName(exePath);
            string exeName = Path.GetFileName(exePath);

            GameInfo gameInfo = GameInfo.GetGameInfo();

            if (File.Exists(gameDir + "\\unpackDS-backup\\" + exeName))
            {
                progress.Report((0, Logger.Log("Restoring executable...", LogFile)));
                try
                {
                    File.Copy(gameDir + "\\unpackDS-backup\\" + exeName, exePath, true);
                }
                catch (Exception ex)
                {
                    return Logger.Log($"Failed to restore executable.\r\n\r\n{ex}", LogFile);
                }
            }

            double totalSteps = gameInfo.BackupDirs.Count + gameInfo.DeleteDirs.Count + 1;

            for (int i = 0; i < gameInfo.BackupDirs.Count; i++)
            {
                string restore = gameInfo.BackupDirs[i];

                string restoreSource = gameDir + "\\unpackDS-backup\\" + restore;
                string restoreTarget = gameDir + "\\" + restore;

                if (File.Exists(restoreSource) && !File.Exists(restoreTarget))
                {
                    progress.Report(((i + 1.0) / totalSteps, Logger.Log($"Restoring file \"{restore}\" ({i + 1}/{gameInfo.BackupDirs.Count})...", LogFile)));
                    try
                    {
                        File.Copy(restoreSource, restoreTarget, true);
                    }
                    catch (Exception ex)
                    {
                        return Logger.Log($"Failed to restore files.\r\n\r\n{ex}", LogFile);
                    }
                }
            }

            try
            {
                for (int i = 0; i < gameInfo.DeleteDirs.Count; i++)
                {
                    string dir = gameInfo.DeleteDirs[i];


                    if (Directory.Exists(gameDir + "\\" + dir))
                    {
                        progress.Report(((i + 1.0 + gameInfo.BackupDirs.Count) / totalSteps, Logger.Log($"Deleting directory \"{dir}\" ({i + 1}/{gameInfo.DeleteDirs.Count})...", LogFile)));

                        Directory.Delete(gameDir + "\\" + dir, true);
                    }
                }
            }
            catch (Exception ex)
            {
                return Logger.Log($"Failed to delete directory.\r\n\r\n{ex}", LogFile);
            }

            return null;
        }
    }
}
