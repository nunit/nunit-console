//////////////////////////////////////////////////////////////////////
// RUNTIME CONSTANTS AND VARIABLES USED AS CONSTANTS
//////////////////////////////////////////////////////////////////////

// Some values are static so they may be used in property initialization and in
// classes. Initialization is separate to allow use of non-constant expressions.

// Directories
static string PROJECT_DIR; PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath + "/";
static string PACKAGE_DIR; PACKAGE_DIR = Argument("artifact-dir", PROJECT_DIR + "package") + "/";
static string PACKAGE_TEST_DIR; PACKAGE_TEST_DIR = PACKAGE_DIR + "tests/";
static string PACKAGE_RESULT_DIR; PACKAGE_RESULT_DIR = PACKAGE_DIR + "results/";
static string BIN_DIR; BIN_DIR = PROJECT_DIR + "bin/" + Configuration + "/";
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
var SOLUTION_FILE = PROJECT_DIR + "NUnitConsole.sln";
var ENGINE_PROJECT = SOURCE_DIR + "NUnitEngine/nunit.engine/nunit.engine.csproj";
var AGENT_PROJECT = SOURCE_DIR + "NUnitEngine/nunit-agent/nunit-agent.csproj";
var AGENT_X86_PROJECT = SOURCE_DIR + "NUnitEngine/nunit-agent-x86/nunit-agent-x86.csproj";
var ENGINE_API_PROJECT = SOURCE_DIR + "NUnitEngine/nunit.engine.api/nunit.engine.api.csproj";
var ENGINE_CORE_PROJECT = SOURCE_DIR + "NUnitEngine/nunit.engine.core/nunit.engine.core.csproj";
var ENGINE_TESTS_PROJECT = SOURCE_DIR + "NUnitEngine/nunit.engine.tests/nunit.engine.tests.csproj";
var ENGINE_CORE_TESTS_PROJECT = SOURCE_DIR + "NUnitEngine/nunit.engine.core.tests/nunit.engine.core.tests.csproj";
var CONSOLE_PROJECT = SOURCE_DIR + "NUnitConsole/nunit-console/nunit-console.csproj";
var CONSOLE_TESTS_PROJECT = SOURCE_DIR + "NUnitConsole/nunit-console.tests/nunit-console.tests.csproj";
var MOCK_ASSEMBLY_PROJECT = SOURCE_DIR + "TestData/mock-assembly/mock-assembly.csproj";
var MOCK_ASSEMBLY_X86_PROJECT = SOURCE_DIR + "TestData/mock-assembly-x86/mock-assembly-x86.csproj";
var MOCK_ASSEMBLY_NUNIT4_PROJECT = SOURCE_DIR + "TestData/mock-assembly-nunit4/mock-assembly-nunit4.csproj";
var NOTEST_PROJECT = SOURCE_DIR + "TestData/notest-assembly/notest-assembly.csproj";
// Console Runner
var NET20_CONSOLE = BIN_DIR + "net20/nunit-console.exe";
var NET60_CONSOLE = BIN_DIR + "net6.0/nunit-console.dll";
// Unit Tests
var NETFX_ENGINE_CORE_TESTS = "nunit.engine.core.tests.exe";
var NETCORE_ENGINE_CORE_TESTS = "nunit.engine.core.tests.dll";
var NETFX_ENGINE_TESTS = "nunit.engine.tests.exe";
var NETCORE_ENGINE_TESTS = "nunit.engine.tests.dll";
var CONSOLE_TESTS = "nunit-console.tests.dll";

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
