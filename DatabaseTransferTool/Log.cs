using System;
using System.Linq;
using System.IO;

namespace DatabaseTransferTool {

    /// <summary>
    /// A logger for the application.
    /// </summary>
    internal class Logger {

        public static string LogFile = null; // not safe but doesn't matter for now

        /// <summary>
        /// a lock object for the log file
        /// </summary>
        private static object LogLock = new object();

        /// <summary>
        /// An event for responding to log entry additions
        /// </summary>
        /// <param name="text"></param>
        public delegate void LogEntryAdded(string text);

        public static LogEntryAdded EntryAdded = null;

        /// <summary>
        /// Log a message composed of the message and stack trace of the exception.
        /// </summary>
        /// <param name="e"></param>
        public static void Log(Exception e) {
            Log(e.Message, e.StackTrace);
        }

        /// <summary>
        /// Write an arbitrary number of messages to the log file.
        /// </summary>
        /// <param name="text"></param>
        public static void Log(params string[] text) {

            if (!string.IsNullOrWhiteSpace(LogFile)) {

                if (!Directory.Exists(Path.GetDirectoryName(LogFile))) {
                    Directory.CreateDirectory(Path.GetDirectoryName(LogFile));
                }

                text[0] = "[ " + DateTime.Now.ToString() + " ] " + text[0];

                lock (LogLock) {
                    if (File.Exists(LogFile)) {
                        File.AppendAllLines(LogFile, text);
                    }
                    else {
                        File.WriteAllLines(LogFile, text);
                    }

                }

                if (EntryAdded != null) {
                    EntryAdded(string.Join("\n", text));
                }
            }
        }

    }
}
