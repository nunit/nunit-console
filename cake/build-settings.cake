public class BuildSettings
{
    public BuildSettings(ISetupContext context)
    {
        if (context == null)
            throw new System.ArgumentNullException(nameof(context));

        Target = context.TargetTask.Name;
        Configuration = context.Argument("configuration", "Release");
    }

    public string Target { get; }
    public string Configuration { get; }
}

Task("DumpSettings")
    .Does(() =>
    {
        Console.WriteLine("Top Level Directories");
        Console.WriteLine($"  Project:         {PROJECT_DIR}");
        Console.WriteLine($"  Package:         {PACKAGE_DIR}");
        Console.WriteLine($"  Package Test:    {PACKAGE_TEST_DIR}");
        Console.WriteLine($"  Package Results: {PACKAGE_RESULT_DIR}");
        Console.WriteLine($"  Zip Image:       {ZIP_IMG_DIR}");
        Console.WriteLine($"  Extensions:      {EXTENSIONS_DIR}");
        Console.WriteLine();
        Console.WriteLine("Solution and Projects");
        Console.WriteLine($"  Solution:        {SOLUTION_FILE}");
        Console.WriteLine($"  NetFx Runner:    {NETFX_CONSOLE_PROJECT}");
        Console.WriteLine($"    Bin Dir:       {NETFX_CONSOLE_PROJECT_BIN_DIR}");
        Console.WriteLine($"  NetCore Runner:  {NETCORE_CONSOLE_PROJECT}");
        Console.WriteLine($"    Bin Dir:       {NETCORE_CONSOLE_PROJECT_BIN_DIR}");
    });