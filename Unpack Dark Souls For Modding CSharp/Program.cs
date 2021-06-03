using System;
using System.IO;

namespace Unpack_Dark_Souls_For_Modding_CSharp
{
    class Program
    {

        private static IProgress<(double value, string status)> progress;

        static void Main(string[] args)
        {

            var ini = @"C:\Users\Tor\Desktop\DARK SOULS PREPARE TO DIE EDITION\DATA\DSfix.ini";

            var initext = File.ReadAllText(ini);

            bool contains = initext.Contains("Remastest.ini");

#if DEBUG
            string currentDir = @"C:\Users\Tor\Desktop\DARK SOULS PREPARE TO DIE EDITION TEST\DATA";
#else
            string currentDir = Directory.GetCurrentDirectory();
#endif
            progress = new Progress<(double value, string status)>(ProgressReport);

            //Integrated .dcx unpacking

            string installPath = $@"{currentDir}\DARKSOULS.exe";

            if (!File.Exists(installPath))
            {
                Console.WriteLine("No Dark Souls detected");
                Console.ReadLine();
                return;
            }

            string error = Unpacker.Unpack(installPath, progress);

            if (error != null)
            {
                Console.WriteLine(error);

                return;
            }

            Console.WriteLine("Completed!");

            Console.ReadLine();
        }

        private static void ProgressReport((double value, string message) obj)
        {
            var percent = obj.value * 100;
            Console.WriteLine($"{ (int)percent}: {obj.message}");
        }

    }
}
