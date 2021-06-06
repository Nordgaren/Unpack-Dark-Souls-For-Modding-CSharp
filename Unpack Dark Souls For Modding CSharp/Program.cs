using System;
using System.IO;

namespace Unpack_Dark_Souls_For_Modding_CSharp
{
    class Program
    {

        private static IProgress<(double value, string status)> progress;

        static void Main(string[] args)
        {
            string currentDir = Directory.GetCurrentDirectory();

            progress = new Progress<(double value, string status)>(ProgressReport);

            //Integrated .dcx unpacking

            string installPath = $@"{currentDir}\DARKSOULS.exe";

            if (!File.Exists(installPath))
            {
                Console.WriteLine("No Dark Souls detected");
                Console.ReadLine();
                return;
            }
            try
            {
                string error = Unpacker.Unpack(installPath, progress);

                if (error != null)
                {
                    Console.WriteLine(error);
                    Console.ReadLine();
                    return;
                }
                Console.WriteLine("Completed!");
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine($"{e.Message}\r\n\r\nThe folder you are trying to unpack requires additional priviledges. \r\n\r\nRun the unpacker as Admin");
            }
            catch (Exception a)
            {
                Console.WriteLine(a.Message);
                throw;
            }

            Console.ReadLine();
        }

        private static void ProgressReport((double value, string message) obj)
        {
            var percent = obj.value * 100;
            Console.WriteLine($"{ (int)percent}: {obj.message}");
        }

    }
}
