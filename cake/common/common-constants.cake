////////////////////////////////////////////////////////////////////
// COMMON CONSTANTS
//////////////////////////////////////////////////////////////////////

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
private const string GITHUB_ACCESS_TOKEN = "GITHUB_ACCESS_TOKEN";

// Pre-release labels that we publish
private static readonly string[] LABELS_WE_PUBLISH_ON_MYGET = { "dev" };
private static readonly string[] LABELS_WE_PUBLISH_ON_NUGET = { "alpha", "beta", "rc" };
private static readonly string[] LABELS_WE_PUBLISH_ON_CHOCOLATEY = { "alpha", "beta", "rc" };
private static readonly string[] LABELS_WE_RELEASE_ON_GITHUB = { "alpha", "beta", "rc" };
