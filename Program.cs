using System.Diagnostics;
using System.Collections.Concurrent;

namespace ClocSharp;

public class Program
{
    public static async Task Main(string[] args)
    {
        bool verbose = false;
        bool help = false;
        int threads = 4;
        string pattern = "*";
        List<string> excludedDirs = new();
        foreach (string arg in args)
        {
            if (arg == "-verbose") verbose = true;
            else if (arg == "-help") help = true;
            else if (arg.StartsWith("-threads=")) threads = int.Parse(arg[9..]);
            else if (arg.StartsWith("-pattern=")) pattern = arg[9..];
            else if (arg.StartsWith("-exclude=")) excludedDirs.Add(arg[9..]);
        }

        if (help)
        {
            Console.WriteLine("(C)ount (L)ines (O)f (C)ode (Sharp)\n");
            Console.WriteLine($"Usage: clocsharp [-help] [-verbose] [-threads=<n>] [-pattern=<p>] [-exclude=<d>] <directory>");
            Console.WriteLine("           -help: Shows this help message and quits the program.");
            Console.WriteLine("        -verbose: Log files to the output as they are being processed.");
            Console.WriteLine("    -threads=<n>: Make the program use <n> threads.");
            Console.WriteLine("    -pattern=<p>: Search and count only files matching the pattern <p>.");
            Console.WriteLine("    -exclude=<d>: Exclude files in the directory <d> from being processed.");
            Console.WriteLine("     <directory>: The directory to search in.");
            Environment.Exit(0);
        }
        
        if (args.Length < 1)
        {
            Console.Error.WriteLine("ERROR: You must specify a directory.");
            Environment.Exit(1);
        }

        string directory = args[^1];
        if (!Directory.Exists(directory))
        {
            Console.Error.WriteLine("ERROR: The specified directory does not exist.");
            Environment.Exit(1);
        }

        string[] files = Directory.GetFiles(directory, pattern, SearchOption.AllDirectories);
        long fileCount = files.Length;
        Console.WriteLine($"{fileCount} files found.");
        Console.WriteLine($"Will use {threads} threads.");
        Console.WriteLine($"Will count files matching pattern `{pattern}`.");
        if (excludedDirs.Count > 0)
            Console.WriteLine($"Will exclude files in directories: {excludedDirs.PrettyPrint()}");
        Console.WriteLine("Counting...");

        string[][] partitioned = files.Partition(threads);

        ConcurrentDictionary<FileType, long> lineCounts = new();
        List<Task> tasks = new();

        Stopwatch sw = new();
        sw.Start();

        for (int i = 0; i < threads; i++)
        {
            var arr = partitioned[i];
            tasks.Add(new Task(() =>
            {
                foreach (string filename in arr)
                {
                    foreach (string excludedDir in excludedDirs)
                    {
                        if (filename.StartsWith($"{directory}{Path.DirectorySeparatorChar}{excludedDir}"))
                        {
                            if (verbose)
                                Console.WriteLine($"Skipping {filename}...");
                            goto skip;
                        }
                    }
                    int dotIndex = filename.LastIndexOf('.');
                    string extension = new FileInfo(filename).Extension;
                    string[] lines = File.ReadAllLines(filename);
                    if (verbose)
                        Console.WriteLine($"Processing {filename}...");
                    FileType type = GetFileType(extension);
                    lineCounts.AddOrUpdate(type, lines.Length, (t, total) =>
                    {
                        return total + lines.Length;
                    });
                    skip: continue;
                }
            }));
        }
        foreach (Task t in tasks) t.Start();
        await Task.WhenAll(tasks);

        sw.Stop();
        long totalLineCount = lineCounts.Sum(e => e.Value);

        Console.WriteLine($"\nTotal files: {fileCount}. Total lines: {totalLineCount}. Calculated in {sw.Elapsed:h'h 'm'min 'ss's'}");

        var sorted = lineCounts.OrderByDescending((a) => lineCounts[a.Key]).ToDictionary();
        foreach (var typeLines in sorted)
        {
            Console.WriteLine($"{typeLines.Key,16} => {typeLines.Value}");
        }
    }

    private static FileType GetFileType(string extension)
    {
        return extension switch
        {
            ".c" or ".h" => FileType.C,
            ".cc" or ".cpp" or ".cxx" or ".c++" or ".hh" or ".hpp" or ".hxx" or ".h++" or ".cppm" or ".ixx" => FileType.CPlusPlus,
            ".cs" or ".csx" => FileType.CSharp,
            ".rs" => FileType.Rust,
            ".rst" => FileType.ReStructuredText,
            ".txt" => FileType.Text,
            ".x" => FileType.DirectXFile,
            ".html" => FileType.Html,
            ".js" => FileType.JavaScript,
            ".css" => FileType.Css,
            ".sh" => FileType.Bash,
            ".yml" or ".yaml" => FileType.Yaml,
            ".litmus" => FileType.Yaml,
            ".svg" => FileType.Svg,
            ".csv" => FileType.Csv,
            ".asm" or ".s" or ".S" or ".inc" or ".wla" or ".SRC" => FileType.Assembly,
            ".gitignore" => FileType.Gitignore,
            ".json" => FileType.Json,
            ".py" or ".pyi" => FileType.Python,
            ".xml" => FileType.Xml,
            ".xaml" => FileType.Xaml,
            ".y" => FileType.Yacc,
            ".config" or ".cfg" => FileType.ConfigFile,
            ".awk" => FileType.Awk,
            ".md" => FileType.Markdown,
            ".toml" => FileType.Toml,
            ".vim" => FileType.VimScript,
            ".vimrc" => FileType.VimConfig,
            ".lua" => FileType.Lua,
            ".rb" or ".ru" => FileType.Ruby,
            _ => FileType.Unknown,
        };
    }
}

enum FileType
{
    Unknown,
    C,
    CPlusPlus,
    CSharp,
    Rust,
    ReStructuredText,
    Text,
    DirectXFile,
    Html,
    JavaScript,
    Css,
    Bash,
    Yaml,
    Litmus,
    Svg,
    Csv,
    Assembly,
    Gitignore,
    Json,
    Python,
    Xml,
    Xaml,
    Yacc,
    ConfigFile,
    Awk,
    Markdown,
    Toml,
    VimScript,
    VimConfig,
    Lua,
    Ruby,
}