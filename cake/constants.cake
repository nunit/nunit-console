//////////////////////////////////////////////////////////////////////
// RUNTIME CONSTANTS AND VARIABLES USED AS CONSTANTS
//////////////////////////////////////////////////////////////////////

// Values are static so they may be used in property initialization and in
// classes. Initialization is separate for any that depend upon PROJECT_DIR
// or Configuration being initialized before them.

// Directories
static string PROJECT_DIR; PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
static string PACKAGE_DIR; PACKAGE_DIR = Argument("artifact-dir", PROJECT_DIR + "package") + "/";
static string PACKAGE_TEST_DIR; PACKAGE_TEST_DIR = PACKAGE_DIR + "tests/";
static string PACKAGE_RESULT_DIR; PACKAGE_RESULT_DIR = PACKAGE_DIR + "results/";
static string NUGET_DIR; NUGET_DIR = PROJECT_DIR + "nuget/";
static string CHOCO_DIR; CHOCO_DIR = PROJECT_DIR + "choco/";
static string MSI_DIR; MSI_DIR = PROJECT_DIR + "msi/";
static string ZIP_DIR; ZIP_DIR = PROJECT_DIR + "zip/";
static string TOOLS_DIR; TOOLS_DIR = PROJECT_DIR + "tools/";
static string IMAGE_DIR; IMAGE_DIR = PROJECT_DIR + "images/";
static string MSI_IMG_DIR; MSI_IMG_DIR = IMAGE_DIR + "msi/";
static string ZIP_IMG_DIR; ZIP_IMG_DIR = IMAGE_DIR + "zip/";
static string SOURCE_DIR; SOURCE_DIR = PROJECT_DIR + "src/";
static string EXTENSIONS_DIR; EXTENSIONS_DIR = PROJECT_DIR + "bundled-extensions";

// Solution and Projects
static string SOLUTION_FILE; SOLUTION_FILE = PROJECT_DIR + "NUnitConsole.sln";
static string NETFX_CONSOLE_PROJECT; NETFX_CONSOLE_PROJECT = SOURCE_DIR + "NUnitConsole/nunit4-console/nunit4-console.csproj";
static string NETCORE_CONSOLE_PROJECT; NETCORE_CONSOLE_PROJECT = SOURCE_DIR + "NUnitConsole/nunit4-netcore-console/nunit4-netcore-console.csproj";
static string ENGINE_PROJECT; ENGINE_PROJECT = SOURCE_DIR + "NUnitEngine/nunit.engine/nunit.engine.csproj";
static string ENGINE_CORE_PROJECT; ENGINE_CORE_PROJECT = SOURCE_DIR + "NUnitEngine/nunit.engine.core/nunit.engine.core.csproj";
static string ENGINE_API_PROJECT; ENGINE_API_PROJECT = SOURCE_DIR + "NUnitEngine/nunit.engine.api/nunit.engine.api.csproj";
static string NET20_AGENT_PROJECT; NET20_AGENT_PROJECT = SOURCE_DIR + "NUnitEngine/agents/nunit-agent-net20-x86/nunit-agent-net20.csproj";
static string NET20_AGENT_X86_PROJECT; NET20_AGENT_X86_PROJECT = SOURCE_DIR + "NUnitEngine/agents/nunit-agent-net20-x86/nunit-agent-net20-x86.csproj";
static string NET462_AGENT_PROJECT; NET462_AGENT_PROJECT = SOURCE_DIR + "NUnitEngine/agents/nunit-agent-net462/nunit-agent-net462.csproj";
static string NET462_AGENT_X86_PROJECT; NET462_AGENT_X86_PROJECT = SOURCE_DIR + "NUnitEngine/agents/nunit-agent-net462-x86/nunit-agent-net462-x86.csproj";
static string NET50_AGENT_PROJECT; NET50_AGENT_PROJECT = SOURCE_DIR + "NUnitEngine/agents/nunit-agent-net50/nunit-agent-net50.csproj";
static string NET60_AGENT_PROJECT; NET60_AGENT_PROJECT = SOURCE_DIR + "NUnitEngine/agents/nunit-agent-net60/nunit-agent-net60.csproj";
static string NETCORE31_AGENT_PROJECT; NETCORE31_AGENT_PROJECT = SOURCE_DIR + "NUnitEngine/agents/nunit-agent-netcore31/nunit-agent-netcore31.csproj";
static string CONSOLE_TESTS_PROJECT; CONSOLE_TESTS_PROJECT = SOURCE_DIR + "NUnitConsole/nunit4-console.tests/nunit4-console.tests.csproj";
static string ENGINE_TESTS_PROJECT; ENGINE_TESTS_PROJECT = SOURCE_DIR + "NUnitEngine/nunit.engine.tests/nunit.engine.tests.csproj";
static string ENGINE_CORE_TESTS_PROJECT; ENGINE_CORE_TESTS_PROJECT = SOURCE_DIR + "NUnitEngine/nunit.engine.core.tests/nunit.engine.core.tests.csproj";
static string MOCK_ASSEMBLY_PROJECT; MOCK_ASSEMBLY_PROJECT = SOURCE_DIR + "NUnitEngine/mock-assembly/mock-assembly.csproj";
static string MOCK_ASSEMBLY_X86_PROJECT; MOCK_ASSEMBLY_X86_PROJECT = SOURCE_DIR + "NUnitEngine/mock-assembly-x86/mock-assembly-x86.csproj";
static string NOTEST_PROJECT; NOTEST_PROJECT = SOURCE_DIR + "NUnitEngine/notest-assembly/notest-assembly.csproj";// Console Runner

// Bin directories for projects
static string NETFX_CONSOLE_PROJECT_BIN_DIR; NETFX_CONSOLE_PROJECT_BIN_DIR = SOURCE_DIR + $"NUnitConsole/nunit4-console/bin/{Configuration}/";
static string NETCORE_CONSOLE_PROJECT_BIN_DIR; NETCORE_CONSOLE_PROJECT_BIN_DIR = SOURCE_DIR + $"NUnitConsole/nunit4-netcore-console/bin/{Configuration}/";
static string ENGINE_PROJECT_BIN_DIR; ENGINE_PROJECT_BIN_DIR = SOURCE_DIR + $"NUnitEngine/nunit.engine/bin/{Configuration}/";
static string ENGINE_CORE_PROJECT_BIN_DIR; ENGINE_CORE_PROJECT_BIN_DIR = SOURCE_DIR + $"NUnitEngine/nunit.engine.core/bin/{Configuration}/";
static string ENGINE_API_PROJECT_BIN_DIR; ENGINE_API_PROJECT_BIN_DIR = SOURCE_DIR + $"NUnitEngine/nunit.engine.api/bin/{Configuration}/";
static string NET20_AGENT_PROJECT_BIN_DIR; NET20_AGENT_PROJECT_BIN_DIR = SOURCE_DIR + $"NUnitEngine/agents/nunit-agent-net20/bin/{Configuration}/net20/";
static string NET20_AGENT_X86_PROJECT_BIN_DIR; NET20_AGENT_X86_PROJECT_BIN_DIR = SOURCE_DIR + $"NUnitEngine/agents/nunit-agent-net20-x86/bin/{Configuration}/net20/";
static string NET462_AGENT_PROJECT_BIN_DIR; NET462_AGENT_PROJECT_BIN_DIR = SOURCE_DIR + $"NUnitEngine/agents/nunit-agent-net462/bin/{Configuration}/net462/";
static string NET462_AGENT_X86_PROJECT_BIN_DIR; NET462_AGENT_X86_PROJECT_BIN_DIR = SOURCE_DIR + $"NUnitEngine/agents/nunit-agent-net462-x86/bin/{Configuration}/net462/";
static string NET50_AGENT_PROJECT_BIN_DIR; NET50_AGENT_PROJECT_BIN_DIR = SOURCE_DIR + $"NUnitEngine/agents/nunit-agent-net50/bin/{Configuration}/net5.0/";
static string NET60_AGENT_PROJECT_BIN_DIR; NET60_AGENT_PROJECT_BIN_DIR = SOURCE_DIR + $"NUnitEngine/agents/nunit-agent-net60/bin/{Configuration}/net6.0/";
static string NET70_AGENT_PROJECT_BIN_DIR; NET70_AGENT_PROJECT_BIN_DIR = SOURCE_DIR + $"NUnitEngine/agents/nunit-agent-net70/bin/{Configuration}/net7.0/";
static string NETCORE31_AGENT_PROJECT_BIN_DIR; NETCORE31_AGENT_PROJECT_BIN_DIR = SOURCE_DIR + $"NUnitEngine/agents/nunit-agent-netcore31/bin/{Configuration}/netcoreapp3.1/";

static string CONSOLE_TESTS_PROJECT_BIN_DIR; CONSOLE_TESTS_PROJECT_BIN_DIR = SOURCE_DIR + $"NUnitConsole/nunit4-console.tests/bin/{Configuration}/";
static string ENGINE_TESTS_PROJECT_BIN_DIR; ENGINE_TESTS_PROJECT_BIN_DIR = SOURCE_DIR + $"NUnitEngine/nunit.engine.tests/bin/{Configuration}/";
static string ENGINE_CORE_TESTS_PROJECT_BIN_DIR; ENGINE_CORE_TESTS_PROJECT_BIN_DIR = SOURCE_DIR + $"NUnitEngine/nunit.engine.core/bin/{Configuration}/";
static string MOCK_ASSEMBLY_PROJECT_BIN_DIR; MOCK_ASSEMBLY_PROJECT_BIN_DIR = SOURCE_DIR + $"TestData/mock-assembly/bin/{Configuration}/";
static string MOCK_ASSEMBLY_X86_PROJECT_BIN_DIR; MOCK_ASSEMBLY_X86_PROJECT_BIN_DIR = SOURCE_DIR + $"TestData/mock-assembly-x86/bin/{Configuration}/";
static string MOCK_ASSEMBLY_NUNIT4_PROJECT_BIN_DIR; MOCK_ASSEMBLY_NUNIT4_PROJECT_BIN_DIR = SOURCE_DIR + $"TestData/mock-assembly-nunit4/bin/{Configuration}/";
static string NOTEST_ASSEMBLY_PROJECT_BIN_DIR; NOTEST_ASSEMBLY_PROJECT_BIN_DIR = SOURCE_DIR + $"TestData/notest-assembly/bin/{Configuration}/";
static string WINDOWS_TEST_PROJECT_BIN_DIR; WINDOWS_TEST_PROJECT_BIN_DIR = SOURCE_DIR + $"TestData/windows-test/bin/{Configuration}/";
static string ASP_NET_CORE_TEST_PROJECT_BIN_DIR; ASP_NET_CORE_TEST_PROJECT_BIN_DIR = SOURCE_DIR + $"TestData/aspnetcore-test/bin/{Configuration}/";

// We build two console runners. If version of either is upgraded, change it here
const string NETFX_CONSOLE_TARGET = "net462";
const string NETCORE_CONSOLE_TARGET = "net6.0";
static string[] ENGINE_TARGETS = new [] { "net462", "netstandard2.0", "netcoreapp3.1" };
static string[] ENGINE_API_TARGETS = new [] { "net20", "netstandard2.0" };

// Console Runner
static string NETFX_CONSOLE_DIR; NETFX_CONSOLE_DIR = $"{NETFX_CONSOLE_PROJECT_BIN_DIR}{NETFX_CONSOLE_TARGET}/";
static string NETFX_CONSOLE; NETFX_CONSOLE = NETFX_CONSOLE_DIR + "nunit4-console.exe";
static string NETCORE_CONSOLE_DIR; NETCORE_CONSOLE_DIR = $"{NETCORE_CONSOLE_PROJECT_BIN_DIR}{NETCORE_CONSOLE_TARGET}/";
static string NETCORE_CONSOLE; NETCORE_CONSOLE = NETCORE_CONSOLE_DIR + "nunit4-netcore-console.dll";

// Currently, the engine uses the same versions as the console runner but this may not always be true
const string NETFX_ENGINE_TARGET = NETFX_CONSOLE_TARGET;
const string NETCORE_ENGINE_TARGET = NETCORE_CONSOLE_TARGET;

// Unit Tests
const string NETFX_ENGINE_CORE_TESTS = "src/NUnitEngine/nunit.engine.core.tests/nunit.engine.core.tests.exe";
const string NETCORE_ENGINE_CORE_TESTS = "nunit.engine.core.tests.dll";
const string NETFX_ENGINE_TESTS = "nunit.engine.tests.exe";
const string NETCORE_ENGINE_TESTS = "nunit.engine.tests.dll";
const string CONSOLE_TESTS = "nunit4-console.tests.dll";

// Package sources for nuget restore
var PACKAGE_SOURCE = new string[]
{
    "https://www.nuget.org/api/v2",
    "https://www.myget.org/F/nunit/api/v2"
};

// Extensions we bundle
var BUNDLED_EXTENSIONS = new[]
{
  // TODO: Not yet available for 4.0 API
  //"NUnit.Extension.VSProjectLoader",
  "NUnit.Extension.NUnitProjectLoader"
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
