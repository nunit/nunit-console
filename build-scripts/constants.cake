// This file contains constants as well as some readonly values, which
// are used as constants. The latter must not depend in any way on the
// contents of other cake files, which are loaded after this one.

// NOTE: The default version should be updated to the 
// next version after each release.
const string DEFAULT_PRODUCT_VERSION = "3.13.0";
const string DEFAULT_CONFIGURATION = "Release";

//Production code targets net20, tests target nets35
static readonly string[] NETFX_FRAMEWORKS = new[] { "net20", "net35" };

readonly List<string> INSTALLED_NET_CORE_RUNTIMES = GetInstalledNetCoreRuntimes();

const string SOLUTION_FILE = "NUnitConsole.sln";

const string CONSOLE_EXE = "nunit3-console.exe";

const string ENGINE_TESTS = "nunit.engine.tests.dll";
const string CONSOLE_TESTS = "nunit3-console.tests.dll";

const string DEFAULT_TEST_RESULT_FILE = "TestResult.xml";

// Package sources for nuget restore
static readonly string[] PACKAGE_SOURCE = new string[]
{
    "https://www.nuget.org/api/v2",
    "https://www.myget.org/F/nunit/api/v2"
};

static readonly string[] EXTENSION_PACKAGES = new[]
{
  "NUnit.Extension.VSProjectLoader",
  "NUnit.Extension.NUnitProjectLoader",
  "NUnit.Extension.NUnitV2Driver",
  "NUnit.Extension.NUnitV2ResultWriter",
  "NUnit.Extension.TeamCityEventListener"
};

//// URLs for uploading packages
//private const string MYGET_PUSH_URL = "https://www.myget.org/F/nunit/api/v2";
//private const string NUGET_PUSH_URL = "https://api.nuget.org/v3/index.json";
//private const string CHOCO_PUSH_URL = "https://push.chocolatey.org/";

//// Environment Variable names holding API keys
//private const string MYGET_API_KEY = "MYGET_API_KEY";
//private const string NUGET_API_KEY = "NUGET_API_KEY";
//private const string CHOCO_API_KEY = "CHOCO_API_KEY";

//// Environment Variable names holding GitHub identity of user
//private const string GITHUB_OWNER = "TestCentric";
//private const string GITHUB_REPO = "testcentric-gui";	
//// Access token is used by GitReleaseManager
//private const string GITHUB_ACCESS_TOKEN = "GITHUB_ACCESS_TOKEN";

//// Pre-release labels that we publish
//private static readonly string[] LABELS_WE_PUBLISH_ON_MYGET = { "dev", "pre" };
//private static readonly string[] LABELS_WE_PUBLISH_ON_NUGET = { "alpha", "beta", "rc" };
//private static readonly string[] LABELS_WE_PUBLISH_ON_CHOCOLATEY = { "alpha", "beta", "rc" };
