using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using ThreadingTask = System.Threading.Tasks.Task;

namespace raindesk.Util
{
    public static class LogUtil
    {
        private readonly record struct LogEntry(string FileName, string Text);

        private static readonly ConcurrentQueue<LogEntry> WriteQueue = new();
        private static readonly SemaphoreSlim WriteSignal = new(0);
        private static readonly Lazy<string[]> LogDirectories = new(BuildLogDirectories);
        private static int logPumpStarted;

        public static void WriteErrorLog(Exception ex, string msg = "")
        {
            Debug.WriteLine($"[Error] {msg}: {ex.Message}");
            WriteFile("error.log", $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}\r\n{ex}\r\n");
        }

        public static void WriteLog(string msg)
        {
            Debug.WriteLine($"[Log] {msg}");
            WriteFile("log.log", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}\r\n");
        }

        private static void WriteFile(string fileName, string text)
        {
            WriteQueue.Enqueue(new LogEntry(fileName, text));
            WriteSignal.Release();
            EnsureLogPumpStarted();
        }

        private static void EnsureLogPumpStarted()
        {
            if (Interlocked.Exchange(ref logPumpStarted, 1) == 1)
            {
                return;
            }

            _ = ThreadingTask.Run(ProcessLogQueueAsync);
        }

        private static async ThreadingTask ProcessLogQueueAsync()
        {
            while (true)
            {
                await WriteSignal.WaitAsync().ConfigureAwait(false);

                while (WriteQueue.TryDequeue(out LogEntry entry))
                {
                    WriteEntry(entry);
                }
            }
        }

        private static void WriteEntry(LogEntry entry)
        {
            foreach (string dir in LogDirectories.Value)
            {
                try
                {
                    Directory.CreateDirectory(dir);
                    File.AppendAllText(Path.Combine(dir, entry.FileName), entry.Text);
                }
                catch
                {
                }
            }
        }

        private static string[] BuildLogDirectories()
        {
            var dirs = new List<string>();
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                dirs.Add(Path.Combine(localAppData, "raindesk", "logs"));
            }
            catch
            {
            }

            // G1: 移除 APP_DIR\logs 回退路径，日志只写入 %LOCALAPPDATA%\raindesk\logs

            return dirs
                .Where(dir => !string.IsNullOrWhiteSpace(dir))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
