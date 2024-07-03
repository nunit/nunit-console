// This file contains both real constants and static readonly variables used
// as constants. All values are initialized before any instance variables.

// GitHub owner is the NUnit organization
const string GITHUB_OWNER = "nunit";

// Defaults
const string DEFAULT_CONFIGURATION = "Release";
private static readonly string[] DEFAULT_VALID_CONFIGS = { "Release", "Debug" };
static readonly string[] DEFAULT_STANDARD_HEADER = new[] {
	"// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt" };
const string DEFAULT_TEST_RESULT_FILE = "TestResult.xml";

// Standardized project directory structure - not changeable by user
const string SRC_DIR			= "src/";
const string BIN_DIR			= "bin/";
const string NUGET_DIR			= "nuget/";
const string CHOCO_DIR			= "choco/";
const string ZIP_DIR			= "zip/";
const string PACKAGE_DIR		= "package/";
const string PKG_TEST_DIR		= "package/tests/";
const string NUGET_TEST_DIR		= "package/tests/nuget/";
//const string NUGET_RUNNER_DIR	= "package/tests/nuget/runners/";
const string CHOCO_TEST_DIR		= "package/tests/choco/";
//const string CHOCO_RUNNER_DIR	= "package/tests/choco/runners/";
const string ZIP_TEST_DIR		= "package/tests/zip/";
const string PKG_RSLT_DIR		= "package/results/";
const string NUGET_RSLT_DIR		= "package/results/nuget/";
const string CHOCO_RSLT_DIR		= "package/results/choco/";
const string ZIP_RSLT_DIR		= "package/results/zip/";
const string IMAGE_DIR          = "package/images";
const string ZIP_IMG_DIR		= "package/images/zip/";
const string TOOLS_DIR			= "tools/";

// URLs for uploading packages
private const string MYGET_PUSH_URL = "https://www.myget.org/F/nunit/api/v2";
private const string NUGET_PUSH_URL = "https://api.nuget.org/v3/index.json";
private const string CHOCO_PUSH_URL = "https://push.chocolatey.org/";

// Environment Variable names holding API keys
private const string MYGET_API_KEY = "MYGET_API_KEY";
private const string NUGET_API_KEY = "NUGET_API_KEY";
private const string CHOCO_API_KEY = "CHOCO_API_KEY";
private const string GITHUB_ACCESS_TOKEN = "GITHUB_ACCESS_TOKEN";

// Pre-release labels that we publish
private static readonly string[] LABELS_WE_PUBLISH_ON_MYGET = { "dev" };
private static readonly string[] LABELS_WE_PUBLISH_ON_NUGET = { "alpha", "beta", "rc" };
private static readonly string[] LABELS_WE_PUBLISH_ON_CHOCOLATEY = { "alpha", "beta", "rc" };
private static readonly string[] LABELS_WE_RELEASE_ON_GITHUB = { "alpha", "beta", "rc" };
