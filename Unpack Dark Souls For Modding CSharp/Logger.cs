using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unpack_Dark_Souls_For_Modding_CSharp
{
    class Logger
    {
        private static List<string> LogList = new List<string>();

        public static string Log(string message, string logFile)
        {
            LogList.Add(message);
            File.AppendAllLines(logFile, LogList);
            LogList.Clear();
            return message;
        }

    }
}
