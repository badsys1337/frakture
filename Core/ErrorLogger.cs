using System;
using System.IO;

namespace TriggerLAG.Core
{
    internal static class ErrorLogger
    {
        private static readonly string LogFilePath = Path.Combine(AppContext.BaseDirectory, "frakture_errors.log");
        private static readonly object _lock = new object();

        public static void Log(string message, Exception? ex = null)
        {
            try
            {
                
            }
            catch
            {
                
            }
        }
    }
}
