//////////////////////////////////////////////////////////////////////
// RUNTIME CONSTANTS AND VARIABLES USED AS CONSTANTS
//////////////////////////////////////////////////////////////////////

// Values are const or static so they may be used in property initialization
// and in classes. Initialization is separate for any that depend upon PROJECT_DIR
// or Configuration being initialized before them.

// We build two console runners. If version of either is upgraded, change it here
const string NETFX_CONSOLE_TARGET = "net462";
const string NETCORE_CONSOLE_TARGET = "net6.0";

// Directories
// NOTE: All paths use '/' as a separator because some usage requires it and
// most usage allows it. In a few cases, like installing an msi, we adjust the
// path at the point of usage to use the correct separator for the operating system.
static string PROJECT_DIR; PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
static string PACKAGE_DIR; PACKAGE_DIR = Argument("artifact-dir", PROJECT_DIR + "package") + "/";
static string PACKAGE_TEST_DIR; PACKAGE_TEST_DIR = PACKAGE_DIR + "tests/";
static string PACKAGE_RESULT_DIR; PACKAGE_RESULT_DIR = PACKAGE_DIR + "results/";
static string EXTENSIONS_DIR; EXTENSIONS_DIR = PROJECT_DIR + "bundled-extensions";

// Solution and Projects
static string SOLUTION_FILE;
SOLUTION_FILE = IsRunningOnWindows()
    ? PROJECT_DIR + "NUnitConsole.sln"
    : PROJECT_DIR + "NUnitConsole_Linux.sln";
static string NETFX_CONSOLE_DIR; NETFX_CONSOLE_DIR = PROJECT_DIR + "src/NUnitConsole/nunit4-console/";
static string NETFX_CONSOLE_PROJECT; NETFX_CONSOLE_PROJECT = NETFX_CONSOLE_DIR + "nunit4-console.csproj";
static string NETCORE_CONSOLE_DIR; NETCORE_CONSOLE_DIR = PROJECT_DIR + "src/NUnitConsole/nunit4-netcore-console/";
static string NETCORE_CONSOLE_PROJECT; NETCORE_CONSOLE_PROJECT = NETCORE_CONSOLE_DIR + "nunit4-netcore-console.csproj";
static string CONSOLE_TESTS_PROJECT; CONSOLE_TESTS_PROJECT = PROJECT_DIR + "src/NUnitConsole/nunit4-console.tests/nunit4-console.tests.csproj";

static string ENGINE_PROJECT; ENGINE_PROJECT = PROJECT_DIR + "src/NUnitEngine/nunit.engine/nunit.engine.csproj";
static string ENGINE_CORE_PROJECT; ENGINE_CORE_PROJECT = PROJECT_DIR + "src/NUnitEngine/nunit.engine.core/nunit.engine.core.csproj";
static string ENGINE_API_PROJECT; ENGINE_API_PROJECT = PROJECT_DIR + "src/NUnitEngine/nunit.engine.api/nunit.engine.api.csproj";
static string NET20_AGENT_PROJECT; NET20_AGENT_PROJECT = PROJECT_DIR + "src/NUnitEngine/agents/nunit-agent-net20-x86/nunit-agent-net20.csproj";
static string NET20_AGENT_X86_PROJECT; NET20_AGENT_X86_PROJECT = PROJECT_DIR + "src/NUnitEngine/agents/nunit-agent-net20-x86/nunit-agent-net20-x86.csproj";
static string NET462_AGENT_PROJECT; NET462_AGENT_PROJECT = PROJECT_DIR + "src/NUnitEngine/agents/nunit-agent-net462/nunit-agent-net462.csproj";
static string NET462_AGENT_X86_PROJECT; NET462_AGENT_X86_PROJECT = PROJECT_DIR + "src/NUnitEngine/agents/nunit-agent-net462-x86/nunit-agent-net462-x86.csproj";
static string NET50_AGENT_PROJECT; NET50_AGENT_PROJECT = PROJECT_DIR + "src/NUnitEngine/agents/nunit-agent-net50/nunit-agent-net50.csproj";
static string NET60_AGENT_PROJECT; NET60_AGENT_PROJECT = PROJECT_DIR + "src/NUnitEngine/agents/nunit-agent-net60/nunit-agent-net60.csproj";
static string NETCORE31_AGENT_PROJECT; NETCORE31_AGENT_PROJECT = PROJECT_DIR + "src/NUnitEngine/agents/nunit-agent-netcore31/nunit-agent-netcore31.csproj";
static string ENGINE_TESTS_PROJECT; ENGINE_TESTS_PROJECT = PROJECT_DIR + "src/NUnitEngine/nunit.engine.tests/nunit.engine.tests.csproj";
static string ENGINE_CORE_TESTS_PROJECT; ENGINE_CORE_TESTS_PROJECT = PROJECT_DIR + "src/NUnitEngine/nunit.engine.core.tests/nunit.engine.core.tests.csproj";

static string MOCK_ASSEMBLY_PROJECT; MOCK_ASSEMBLY_PROJECT = PROJECT_DIR + "src/TestData/mock-assembly/mock-assembly.csproj";
static string MOCK_ASSEMBLY_X86_PROJECT; MOCK_ASSEMBLY_X86_PROJECT = PROJECT_DIR + "src/TestData/mock-assembly-x86/mock-assembly-x86.csproj";
static string NOTEST_PROJECT; NOTEST_PROJECT = PROJECT_DIR + "src/TestData/notest-assembly/notest-assembly.csproj";// Console Runner
static string WINDOWS_FORMS_TEST_PROJECT; WINDOWS_FORMS_TEST_PROJECT = PROJECT_DIR + "src/TestData/windows-test/windows-test.csproj";
static string ASP_NET_CORE_TEST_PROJECT; ASP_NET_CORE_TEST_PROJECT = PROJECT_DIR + "src/TestData/aspnetcore-test/aspnetcore-test.csproj";

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
private const string MYGET_API_KEY = "NUNIT_MYGET_API_KEY";
private const string NUGET_API_KEY = "NUNIT_NUGET_API_KEY";
private const string CHOCO_API_KEY = "NUNIT_CHOCO_API_KEY";
private const string FALLBACK_MYGET_API_KEY = "MYGET_API_KEY";
private const string FALLBACK_NUGET_API_KEY = "NUGET_API_KEY";
private const string FALLBACK_CHOCO_API_KEY = "CHOCO_API_KEY";

// GitHub Information
private const string GITHUB_OWNER = "nunit";
private const string GITHUB_REPO = "nunit-console";
private const string GITHUB_ACCESS_TOKEN = "GITHUB_ACCESS_TOKEN";

// Pre-release labels that we publish
private static readonly string[] LABELS_WE_PUBLISH_ON_MYGET = { "dev" };
private static readonly string[] LABELS_WE_PUBLISH_ON_NUGET = { "alpha", "beta", "rc" };
private static readonly string[] LABELS_WE_PUBLISH_ON_CHOCOLATEY = { "alpha", "beta", "rc" };
private static readonly string[] LABELS_WE_RELEASE_ON_GITHUB = { "alpha", "beta", "rc" };
