using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace POPHero
{
    public static class CsvFileReader
    {
        public static string[] ReadAllLinesWithRetry(string path, int maxAttempts = 12, int delayMs = 120)
        {
            Exception lastError = null;
            for (var attempt = 1; attempt <= Math.Max(1, maxAttempts); attempt++)
            {
                try
                {
                    return ReadAllLinesShared(path);
                }
                catch (IOException ex)
                {
                    lastError = ex;
                    if (attempt >= maxAttempts)
                        break;

                    Thread.Sleep(Math.Max(1, delayMs));
                }
            }

            throw new IOException(
                $"Failed to read CSV `{path}` because the file is locked by another process. Close the spreadsheet/editor using it and try again.",
                lastError);
        }

        static string[] ReadAllLinesShared(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            var lines = new List<string>();
            while (!reader.EndOfStream)
                lines.Add(reader.ReadLine() ?? string.Empty);
            return lines.ToArray();
        }
    }
}
