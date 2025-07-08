// Load all tools used by the recipe
#tool NuGet.CommandLine&version=6.9.1
#tool dotnet:?package=GitVersion.Tool&version=5.12.0
#tool dotnet:?package=GitReleaseManager.Tool&version=0.18.0
#addin nuget:?package=Cake.Git&version=5.0.1

// Using statements needed in the scripts
using Cake.Git;
using System.Text.RegularExpressions;
using System.Xml;
using SIO = System.IO;

// Banner class displays banners similar to those created by Cake
// We use this to display intermediate steps within a task
public static class Banner
{
    public static void Display(string message, char barChar = '-', int length = 0)
    {
        if (length == 0) length = MaxLineLength(message);
        var bar = new string(barChar, length);

        Console.WriteLine();
        Console.WriteLine(bar);
        Console.WriteLine(message);
        Console.WriteLine(bar);

        int MaxLineLength(string message)
        {
            int length = 0;
            foreach (string line in message.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
                length = Math.Max(length, line.Length);
            return length;
        }
    }
}

public static class Dotnet
{
    // Experimenting with finding dotnet installs for X64 vs x86
    // This code will end up moved into the engine as well.

    private static ICakeContext _context = BuildSettings.Context;
    private static bool _onWindows = SIO.Path.DirectorySeparatorChar == '\\';

    // NOTES:
    // * We don't need an IsInstalled property because our scripts all run under dotnet.

    public static string InstallPath { get; } = GetDotnetInstallDirectory(false);
    public static string X86InstallPath { get; } = GetDotnetInstallDirectory(true);

    // These three properties use => to avoid issues with initialization order!
    public static string Executable => InstallPath + "dotnet.exe";
    public static string X86Executable => X86InstallPath + "dotnet.exe";
    public static bool IsX86Installed => SIO.Directory.Exists(X86InstallPath) && SIO.File.Exists(X86Executable);

    public static void Display()
    {
        _context.Information($"Install Path:      {InstallPath}");
        _context.Information($"Executable:        {Executable}");
        _context.Information("Runtimes:");
        foreach (string dir in SIO.Directory.GetDirectories(SIO.Path.Combine(InstallPath, "shared")))
        {
            string runtime = SIO.Path.GetFileName(dir);
            foreach (string dir2 in SIO.Directory.GetDirectories(dir))
            {
                string version = SIO.Path.GetFileName(dir2);
                _context.Information($"  {runtime} {version}");
            }
        }

        if (IsX86Installed)
        {
            _context.Information($"\nX86 Install Path:  {X86InstallPath}");
            _context.Information($"X86 Executable:    {X86Executable}");
            _context.Information("Runtimes:");
            foreach (var dir in SIO.Directory.GetDirectories(SIO.Path.Combine(X86InstallPath, "shared")))
            {
                string runtime = SIO.Path.GetFileName(dir);
                foreach (string dir2 in SIO.Directory.GetDirectories(dir))
                {
                    string version = SIO.Path.GetFileName(dir2);
                    _context.Information($"  {runtime} {version}");
                }
            }
        }
        else
            _context.Information("\nDotnet X86 is not installed");
    }

    private static string GetDotnetInstallDirectory(bool forX86 = false)
    {
        if (_onWindows)
        {
            if (forX86)
            {
                Microsoft.Win32.RegistryKey key =
                    Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\dotnet\SetUp\InstalledVersions\x86\");
                return (string)key?.GetValue("InstallLocation");
            }
            else
            {
                Microsoft.Win32.RegistryKey key =
                    Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\dotnet\SetUp\InstalledVersions\x64\sharedHost\");
                return (string)key?.GetValue("Path");
            }
        }
        else // Assuming linux for now
            return "/usr/shared/dotnet/";
    }
}
