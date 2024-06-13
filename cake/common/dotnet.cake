public class DotnetInfo
{
    // Experimenting with finding dotnet installs for X64 vs x86
    // This code will end up moved into the engine as well.

    private ICakeContext _context;
    private bool _onWindows;

    public DotnetInfo(ICakeContext context)
    {
        _context = context;
        _onWindows = context.IsRunningOnWindows();

        InstallPath = GetDotnetInstallDirectory(false);
        X86InstallPath = GetDotnetInstallDirectory(true);
    }

    // NOTES:
    // * We don't need an IsInstalled property because our scripts all run under dotnet.

    public bool IsX86Installed => System.IO.Directory.Exists(X86InstallPath) && System.IO.File.Exists(X86Executable);

    public string InstallPath { get; }
    public string Executable => InstallPath + "dotnet.exe";
    public List<string> Runtimes { get; }

    public string X86InstallPath { get; }
    public string X86Executable => X86InstallPath + "dotnet.exe";
    public List<string> X86Runtimes { get; }

    public void Display()
    {
        _context.Information($"Install Path:      {InstallPath}");
        _context.Information($"Executable:        {Executable}");
        _context.Information("Runtimes:");
        foreach (string dir in System.IO.Directory.GetDirectories(System.IO.Path.Combine(InstallPath, "shared")))
        {
            string runtime = System.IO.Path.GetFileName(dir);
            foreach (string dir2 in System.IO.Directory.GetDirectories(dir))
            {
                string version = System.IO.Path.GetFileName(dir2);
                _context.Information($"  {runtime} {version}");
            }
        }

        if (IsX86Installed)
        {
            _context.Information($"\nX86 Install Path:  {X86InstallPath}");
            _context.Information($"X86 Executable:    {X86Executable}");
            _context.Information("Runtimes:");
            foreach (var dir in System.IO.Directory.GetDirectories(System.IO.Path.Combine(X86InstallPath, "shared")))
            {
                string runtime = System.IO.Path.GetFileName(dir);
                foreach (string dir2 in System.IO.Directory.GetDirectories(dir))
                {
                    string version = System.IO.Path.GetFileName(dir2);
                    _context.Information($"  {runtime} {version}");
                }
            }
        }
        else
            _context.Information("\nDotnet X86 is not installed");
    }

    private string GetDotnetInstallDirectory(bool forX86 = false)
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

// Use this task to verify that the script understands the dotnet environment
Task("DotnetInfo").Does(() => { new DotnetInfo(Context).Display(); });
