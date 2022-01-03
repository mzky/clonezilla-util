﻿using libCommon;
using System.Runtime.InteropServices;

namespace lib7Zip
{
    public class SevenZipUtility
    {
        public static string SevenZipExe()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return @"ext\7-Zip\win-x64\7z.exe";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return @"ext/7-Zip/linux-x64/7zz";

            throw new Exception("OS not supported yet.");
        }

        public static void ExtractFile(string inputFilename, string outputFolder, bool verbose)
        {
            var sevenZipOutput = ProcessUtility.RunCommand(SevenZipExe(), new[] { $"x \"{inputFilename}\" -p\"blah\" -r -y -o\"{outputFolder}\"" }, verbose);

            //iteration will finish when the program has exited
            _ = sevenZipOutput.ToList();
        }

        public static IEnumerable<ArchiveEntry> GetArchiveEntries(string archiveFilename, bool verbose)
        {
            var sevenZipOutput = ProcessUtility.RunCommand(SevenZipExe(), new[] { $"l -slt \"{archiveFilename}\"" }, verbose);

            ArchiveEntry? currentEntry = null;
            foreach (var line in sevenZipOutput)
            {
                if (line.StartsWith($"Path ="))
                {
                    currentEntry = new ArchiveEntry
                    {
                        Name = line.Replace("Path = ", "")
                    };
                }

                if (string.IsNullOrEmpty(line))
                {
                    if (currentEntry != null)
                    {
                        yield return currentEntry;
                    }

                    continue;
                }

                if (currentEntry == null) continue;

                if (line.Equals($"Folder = +")) currentEntry.IsFolder = true;

                if (!currentEntry.IsFolder)
                {
                    if (line.StartsWith($"Size =")) currentEntry.Size = long.Parse(line.Replace("Size = ", ""));
                }

                if (line.StartsWith($"Modified =")) currentEntry.Modified = DateTime.Parse(line.Replace("Modified = ", ""));
                if (line.StartsWith($"Created =")) currentEntry.Created = DateTime.Parse(line.Replace("Created = ", ""));
                if (line.StartsWith($"Accessed =")) currentEntry.Accessed = DateTime.Parse(line.Replace("Accessed = ", ""));
            }
        }

        public static IEnumerable<string> GetArchivesInFolder(string inputFolder, bool verbose)
        {
            var sevenZipOutput = ProcessUtility.RunCommand(SevenZipExe(), new[] { $"l \"{inputFolder.EnsureEndsInPathSeparator()}\"" }, verbose);

            foreach (var line in sevenZipOutput)
            {
                if (line.StartsWith($"Path ="))
                {
                    var archiveFilename = line.Replace("Path = ", "");
                    if (File.Exists(archiveFilename))
                    {
                        if (verbose)
                        {
                            Console.WriteLine($"Found archive: {archiveFilename}");
                        }
                        yield return archiveFilename;
                    }
                }
            }
        }
    }
}