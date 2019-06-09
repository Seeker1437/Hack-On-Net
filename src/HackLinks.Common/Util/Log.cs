using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HackLinks.Common.Util
{
    public static class Log
    {
        public static event Action<string> Logged;
        private static string _logFile;
        private static readonly object LockObject = new object();

        public static string Archive { get; set; }

        public static string LogFile
        {
            get => _logFile;
            set
            {
                if (value != null)
                {
                    var directoryName = Path.GetDirectoryName(value);
                    if (directoryName != null && !Directory.Exists(directoryName))
                        Directory.CreateDirectory(directoryName);
                    if (File.Exists(value))
                    {
                        if (Archive != null)
                        {
                            if (!Directory.Exists(Archive))
                                Directory.CreateDirectory(Archive);
                            var str1 = Path.Combine(Archive, File.GetCreationTime(value).ToString("yyyy-MM-dd_hh-mm"));
                            var str2 = Path.Combine(str1, Path.GetFileName(value));
                            if (!Directory.Exists(str1))
                                Directory.CreateDirectory(str1);
                            if (File.Exists(str2))
                                File.Delete(str2);
                            File.Move(value, str2);
                        }

                        File.Delete(value);
                    }
                }

                _logFile = value;
            }
        }

        internal static void OnLogged(string message)
        {
            Logged?.Raise(message);
        }

        internal static void OnLogged(string format, params object[] args)
        {
            if (args == null || args.Length == 0)
                format = format.Replace('{', '[').Replace('}', ']');

            OnLogged(string.Format(format, args));
        }

        public static void Info(string format, params object[] args)
        {
            WriteLine(LogLevel.Info, format, args);
        }

        public static void Warning(string format, params object[] args)
        {
            WriteLine(LogLevel.Warning, format, args);
        }

        public static void Error(string format, params object[] args)
        {
            WriteLine(LogLevel.Error, format, args);
        }

        public static void Debug(string format, params object[] args)
        {
            WriteLine(LogLevel.Debug, format, args);
        }

        public static void Debug(object obj)
        {
            WriteLine(LogLevel.Debug, obj.ToString());
        }

        public static void Status(string format, params object[] args)
        {
            WriteLine(LogLevel.Status, format, args);
        }

        public static void Exception(Exception ex, string description = null, params object[] args)
        {
            if (description != null)
                WriteLine(LogLevel.Error, description, args);
            WriteLine(LogLevel.Exception, ex.ToString());
        }

        public static void Progress(int current, int max)
        {
            var donePerc = (100f / max * current);
            var done = (int)Math.Min(20, Math.Ceiling(20f / max * current));

            Write(LogLevel.Info, false, string.Format("[" + ("".PadRight(done, '#') + "".PadLeft(20 - done, '.')) + "] {0,5:0.0}%\r", donePerc));
        }

        public static void WriteLine(LogLevel level, string format, params object[] args)
        {
            OnLogged(format, args);

            var newLineFormat = format + Environment.NewLine;

            Write(level, newLineFormat, args);
        }

        public static void Write(LogLevel level, string format)
        {
            Write(level, true, format);
        }

        public static void Write(LogLevel level, string format, params object[] args)
        {
            if (args.Length == 0)
                format = format.Replace('{', '[').Replace('}', ']');

            Write(level, string.Format(format, args));
        }

        private static void Write(LogLevel level, bool toFile, string format)
        {
            lock (LockObject)
            {
                switch (level)
                {
                    case LogLevel.Info: Console.ForegroundColor = ConsoleColor.White; break;
                    case LogLevel.Warning: Console.ForegroundColor = ConsoleColor.Yellow; break;
                    case LogLevel.Error: Console.ForegroundColor = ConsoleColor.Red; break;
                    case LogLevel.Debug: Console.ForegroundColor = ConsoleColor.Cyan; break;
                    case LogLevel.Status: Console.ForegroundColor = ConsoleColor.Green; break;
                    case LogLevel.Exception: Console.ForegroundColor = ConsoleColor.DarkRed; break;
                }

                if (level != LogLevel.None)
                    Console.Write("[{0}]", level);

                Console.ForegroundColor = ConsoleColor.Gray;

                if (level != LogLevel.None)
                    Console.Write(" - ");

                Console.Write(format);

                if (_logFile == null || !toFile)
                    return;

                using (var file = new StreamWriter(_logFile, true))
                {
                    file.Write("{0:yyyy-MM-dd HH:mm} ", DateTime.Now);
                    if (level != LogLevel.None)
                        file.Write("[{0}] - ", level);
                    file.Write(format);
                    file.Flush();
                }
            }
        }
    }
}
