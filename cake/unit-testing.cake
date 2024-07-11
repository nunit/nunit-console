//////////////////////////////////////////////////////////////////////
// UNIT TEST RUNNER
//////////////////////////////////////////////////////////////////////

public static class UnitTesting
{
    static ICakeContext _context;

    static UnitTesting()
    {
        _context = BuildSettings.Context;
    }

    public static void RunAllTests()
    {
		var unitTests = FindUnitTestFiles(BuildSettings.UnitTests);

        _context.Information($"Located {unitTests.Count} unit test assemblies.");
        var errors = new List<string>();
    
        foreach (var testPath in unitTests)
        {
            var testFile = testPath.GetFilename();
            var containingDir = testPath.GetDirectory().GetDirectoryName();
            var runtime = IsValidRuntime(containingDir) ? containingDir : null;
            
            Banner.Display(runtime != null
                ? $"Running {testFile} under {runtime}"
                : $"Running {testFile}");

            var runner = BuildSettings.UnitTestRunner ?? new NUnitLiteRunner();
            int rc = runner.Run(testPath);

            var name = runtime != null
                ? $"{testFile}({runtime})"
                : testFile;
            if (rc > 0)
                errors.Add($"{name}: {rc} tests failed");
            else if (rc < 0)
                errors.Add($"{name} returned rc = {rc}");
        }

        if (unitTests.Count == 0)
            _context.Warning("No unit tests were found");
        else if (errors.Count > 0)
            throw new Exception(
                "One or more unit tests failed, breaking the build.\r\n"
                + errors.Aggregate((x, y) => x + "\r\n" + y) );

        static bool IsValidRuntime(string text)
        {
            string[] VALID_RUNTIMES = {
                "net20", "net30", "net35", "net40", "net45", "net451", "net451",
                "net46", "net461", "net462", "net47", "net471", "net472", "net48", "net481",
                "netcoreapp1.1", "netcoreapp2.1", "netcoreapp3.1",
                "net5.0", "net6.0", "net7.0", "net8.0"
            };

            return VALID_RUNTIMES.Contains(text);
        }
    }

    private static List<FilePath> FindUnitTestFiles(string patternSet)
    {
        var result = new List<FilePath>();

        if (!string.IsNullOrEmpty(patternSet))
        { 
            // User supplied a set of patterns for the unit tests
            foreach (string filePattern in patternSet.Split('|'))
                foreach (var testPath in _context.GetFiles(BuildSettings.OutputDirectory + filePattern))
                    result.Add(testPath);
        }
        else
        {
            // Use default patterns to find unit tests - case insensitive because
            // we don't know how the user may have named test assemblies.
            var defaultPatterns = new [] { "**/*.tests.dll", "**/*.tests.exe" };
            var globberSettings = new GlobberSettings { IsCaseSensitive = false };
            foreach (string filePattern in defaultPatterns)
                foreach (var testPath in _context.GetFiles(BuildSettings.OutputDirectory + filePattern, globberSettings))
                    result.Add(testPath);
        }

        result.Sort(ComparePathsByFileName);

        return result;

        static int ComparePathsByFileName(FilePath x, FilePath y)
        {
            return x.GetFilename().ToString().CompareTo(y.GetFilename().ToString());
        }
    }
}
