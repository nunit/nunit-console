//////////////////////////////////////////////////////////////////////
// RUNTIME CONSTANTS AND VARIABLES USED AS CONSTANTS
//////////////////////////////////////////////////////////////////////

// This really belongs in utilities.cake, but it's needed in this file,
// which is loaded first.
string CombinePaths(string path1, string path2)
{
    return System.IO.Path.Combine(path1, path2);
}

// Consts and static values are available for use within classes.
// Static initialization is done as a separate statement for values that
// depend upon PROJECT_DIR or Configuration being initialized.

// Useful Constants
static readonly char SEPARATOR = System.IO.Path.DirectorySeparatorChar;

// Directories
// NOTE: All paths use '/' as a separator because some usage requires it and
// most usage allows it. In a few cases, like installing an msi, we adjust the
// path at the point of usage to use the correct separator for the operating system.
static string PROJECT_DIR; PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
static string PACKAGE_DIR; PACKAGE_DIR = Argument("artifact-dir", PROJECT_DIR + "package") + "/";
static string PACKAGE_TEST_DIR; PACKAGE_TEST_DIR = PACKAGE_DIR + "tests/";
static string PACKAGE_RESULT_DIR; PACKAGE_RESULT_DIR = PACKAGE_DIR + "results/";
static string ZIP_IMG_DIR; ZIP_IMG_DIR = PACKAGE_DIR + "zip-image/";
static string EXTENSIONS_DIR; EXTENSIONS_DIR = PROJECT_DIR + "bundled-extensions";

// Solution and Projects
static string SOLUTION_FILE;
SOLUTION_FILE = IsRunningOnWindows()
    ? PROJECT_DIR + "NUnitConsole.sln"
    : PROJECT_DIR + "NUnitConsole_Linux.sln";
static string NETFX_CONSOLE_PROJECT; NETFX_CONSOLE_PROJECT = PROJECT_DIR + "src/NUnitConsole/nunit3-console/nunit3-console.csproj";
static string NETCORE_CONSOLE_PROJECT; NETCORE_CONSOLE_PROJECT = PROJECT_DIR + "src/NUnitConsole/nunit3-netcore-console/nunit3-netcore-console.csproj";
static string ENGINE_PROJECT; ENGINE_PROJECT = PROJECT_DIR + "src/NUnitEngine/nunit.engine/nunit.engine.csproj";
static string ENGINE_CORE_PROJECT; ENGINE_CORE_PROJECT = PROJECT_DIR + "src/NUnitEngine/nunit.engine.core/nunit.engine.core.csproj";
static string ENGINE_API_PROJECT; ENGINE_API_PROJECT = PROJECT_DIR + "src/NUnitEngine/nunit.engine.api/nunit.engine.api.csproj";
static string AGENT_PROJECT; AGENT_PROJECT = PROJECT_DIR + "src/NUnitEngine/nunit-agent/nunit-agent.csproj";
static string AGENT_X86_PROJECT; AGENT_X86_PROJECT = PROJECT_DIR + "src/NUnitEngine/nunit-agent-x86/nunit-agent-x86.csproj";
static string CONSOLE_TESTS_PROJECT; CONSOLE_TESTS_PROJECT = PROJECT_DIR + "src/NUnitConsole/nunit3-console.tests/nunit3-console.tests.csproj";
static string ENGINE_TESTS_PROJECT; ENGINE_TESTS_PROJECT = PROJECT_DIR + "src/NUnitEngine/nunit.engine.tests/nunit.engine.tests.csproj";
static string ENGINE_CORE_TESTS_PROJECT; ENGINE_CORE_TESTS_PROJECT = PROJECT_DIR + "src/NUnitEngine/nunit.engine.core.tests/nunit.engine.core.tests.csproj";
static string MOCK_ASSEMBLY_PROJECT; MOCK_ASSEMBLY_PROJECT = PROJECT_DIR + "src/NUnitEngine/mock-assembly/mock-assembly.csproj";
static string MOCK_ASSEMBLY_X86_PROJECT; MOCK_ASSEMBLY_X86_PROJECT = PROJECT_DIR + "src/NUnitEngine/mock-assembly-x86/mock-assembly-x86.csproj";
static string NOTEST_PROJECT; NOTEST_PROJECT = PROJECT_DIR + "src/NUnitEngine/notest-assembly/notest-assembly.csproj";
static string WINDOWS_TEST_PROJECT; WINDOWS_TEST_PROJECT = PROJECT_DIR + "src/NUnitEngine/windows-test/windows-test.csproj";
static string ASPNETCORE_TEST_PROJECT; ASPNETCORE_TEST_PROJECT = PROJECT_DIR + "src/NUnitEngine/aspnetcore-test/aspnetcore-test.csproj";

// Bin directories for selected projects
static string NETFX_CONSOLE_PROJECT_BIN_DIR; NETFX_CONSOLE_PROJECT_BIN_DIR = PROJECT_DIR + $"src/NUnitConsole/nunit3-console/bin/{Configuration}/";
static string NETCORE_CONSOLE_PROJECT_BIN_DIR; NETCORE_CONSOLE_PROJECT_BIN_DIR = PROJECT_DIR + $"src/NUnitConsole/nunit3-netcore-console/bin/{Configuration}/";
static string ENGINE_PROJECT_BIN_DIR; ENGINE_PROJECT_BIN_DIR = PROJECT_DIR + $"src/NUnitEngine/nunit.engine/bin/{Configuration}/";
static string ENGINE_API_PROJECT_BIN_DIR; ENGINE_API_PROJECT_BIN_DIR = PROJECT_DIR + $"src/NUnitEngine/nunit.engine.api/bin/{Configuration}/";

// Console Runner
// We build two console runners. If version of either is upgraded, change it here
const string NETFX_CONSOLE_TARGET = "net462";
const string NETCORE_CONSOLE_TARGET = "net6.0";
static string NETFX_CONSOLE_DIR; NETFX_CONSOLE_DIR = $"{NETFX_CONSOLE_PROJECT_BIN_DIR}{NETFX_CONSOLE_TARGET}/";
static string NETFX_CONSOLE; NETFX_CONSOLE = NETFX_CONSOLE_DIR + "nunit3-console.exe";
static string NETCORE_CONSOLE_DIR; NETCORE_CONSOLE_DIR = $"{NETCORE_CONSOLE_PROJECT_BIN_DIR}{NETCORE_CONSOLE_TARGET}/";
static string NETCORE_CONSOLE; NETCORE_CONSOLE = NETCORE_CONSOLE_DIR + "nunit3-netcore-console.dll";

// Package sources for nuget restore
var PACKAGE_SOURCE = new string[]
{
    "https://www.nuget.org/api/v2",
    "https://www.myget.org/F/nunit/api/v2"
};

// Extensions we bundle
var BUNDLED_EXTENSIONS = new[]
{
  "NUnit.Extension.VSProjectLoader",
  "NUnit.Extension.NUnitProjectLoader",
  "NUnit.Extension.NUnitV2Driver",
  "NUnit.Extension.NUnitV2ResultWriter",
  "NUnit.Extension.TeamCityEventListener"
};

// URLs for uploading packages
private const string MYGET_PUSH_URL = "https://www.myget.org/F/nunit/api/v2";
private const string NUGET_PUSH_URL = "https://api.nuget.org/v3/index.json";
private const string CHOCO_PUSH_URL = "https://push.chocolatey.org/";

// Environment Variable names holding API keys
private const string MYGET_API_KEY = "MYGET_API_KEY";
private const string NUGET_API_KEY = "NUGET_API_KEY";
private const string CHOCO_API_KEY = "CHOCO_API_KEY";

// GitHub Information
private const string GITHUB_OWNER = "nunit";
private const string GITHUB_REPO = "nunit-console";
private const string GITHUB_ACCESS_TOKEN = "GITHUB_ACCESS_TOKEN";

// Pre-release labels that we publish
private static readonly string[] LABELS_WE_PUBLISH_ON_MYGET = { "dev" };
private static readonly string[] LABELS_WE_PUBLISH_ON_NUGET = { "alpha", "beta", "rc" };
private static readonly string[] LABELS_WE_PUBLISH_ON_CHOCOLATEY = { "alpha", "beta", "rc" };
private static readonly string[] LABELS_WE_RELEASE_ON_GITHUB = { "alpha", "beta", "rc" };
