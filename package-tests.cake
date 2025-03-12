public static class PackageTests
{
    // Tests run for the Standard runner packages (both nuget and chocolatey)
    public static List<PackageTest> StandardRunnerTests = new List<PackageTest>();

    // Tests run for the NETCORE runner package
    public static List<PackageTest> NetCoreRunnerTests = new List<PackageTest>();

    // Tests Run for the ZIP Package
    public static List<PackageTest> ZipRunnerTests = new List<PackageTest>();

    static PackageTests()
    {
        // Wrapper to add tests to all three Lists
        var AllLists = new ListWrapper(StandardRunnerTests, NetCoreRunnerTests, ZipRunnerTests);

        // Wrapper to add tests to the standard and netcore lists
        var StandardAndNetCoreLists = new ListWrapper(StandardRunnerTests, NetCoreRunnerTests);

        // Wrapper to add tests to the standard and zip Lists
        var StandardAndZipLists = new ListWrapper(StandardRunnerTests, ZipRunnerTests);

        //////////////////////////////////////////////////////////////////////
        // RUN MOCK-ASSEMBLY UNDER EACH RUNTIME
        //////////////////////////////////////////////////////////////////////

        StandardAndZipLists.Add(new PackageTest(1, "Net462Test")
        {
            Description= "Run mock-assembly.dll targeting .NET 4.6.2",
            Arguments="testdata/net462/mock-assembly.dll",
            ExpectedResult=new MockAssemblyExpectedResult("net-4.6.2")
        });

        AllLists.Add(new PackageTest(1, "Net80Test")
        {
            Description = "Run mock-assembly.dll targeting .NET 8.0",
            Arguments = "testdata/net8.0/mock-assembly.dll",
            ExpectedResult = new MockAssemblyExpectedResult("netcore-8.0")
        });

        AllLists.Add(new PackageTest(1, "Net70Test")
        {
            Description = "Run mock-assembly.dll targeting .NET 7.0",
            Arguments = "testdata/net7.0/mock-assembly.dll",
            ExpectedResult = new MockAssemblyExpectedResult("netcore-7.0")
        });

        AllLists.Add(new PackageTest(1, "Net60Test")
        {
            Description = "Run mock-assembly.dll targeting .NET 6.0",
            Arguments = "testdata/net6.0/mock-assembly.dll",
            ExpectedResult = new MockAssemblyExpectedResult("netcore-6.0")
        });

        AllLists.Add(new PackageTest(1, "NetCore31Test")
        {
            Description = "Run mock-assembly.dll targeting .NET Core 3.1",
            Arguments = "testdata/netcoreapp3.1/mock-assembly.dll",
            ExpectedResult = new MockAssemblyExpectedResult("netcore-3.1")
        });

        //////////////////////////////////////////////////////////////////////
        // RUN MOCK-ASSEMBLY-X86 UNDER EACH RUNTIME
        //////////////////////////////////////////////////////////////////////

        const string DOTNET_EXE_X86 = @"C:\Program Files (x86)\dotnet\dotnet.exe";
        // TODO: Remove the limitation to Windows
        bool dotnetX86Available = BuildSettings.IsRunningOnWindows && System.IO.File.Exists(DOTNET_EXE_X86);

        // X86 is always available for .NET Framework
        StandardAndZipLists.Add(new PackageTest(1, "Net462X86Test")
        {
            Description = "Run mock-assembly-x86.dll targeting .NET 4.6.2",
            Arguments = "testdata/net462/mock-assembly-x86.dll",
            ExpectedResult = new MockAssemblyX86ExpectedResult("net-4.6.2")
        });

        if (dotnetX86Available)
        {
            // TODO: Make tests run on all build platforms
            bool onGitHubActions = BuildSettings.IsRunningOnGitHubActions;

            StandardAndZipLists.Add(new PackageTest(1, "Net80X86Test")
            {
                Description = "Run mock-assembly-x86.dll targeting .NET 8.0",
                Arguments = "testdata/net8.0/mock-assembly-x86.dll",
                ExpectedResult = new MockAssemblyX86ExpectedResult("netcore-8.0")
            });

            if (!onGitHubActions)
                StandardAndZipLists.Add(new PackageTest(1, "Net70X86Test")
                {
                    Description = "Run mock-assembly-x86.dll targeting .NET 7.0",
                    Arguments = "testdata/net7.0/mock-assembly-x86.dll",
                    ExpectedResult = new MockAssemblyX86ExpectedResult("netcore-7.0")
                });

            StandardAndZipLists.Add(new PackageTest(1, "Net60X86Test")
            {
                Description = "Run mock-assembly-x86.dll targeting .NET 6.0",
                Arguments = "testdata/net6.0/mock-assembly-x86.dll",
                ExpectedResult = new MockAssemblyX86ExpectedResult("netcore-6.0")
            });

            if (!onGitHubActions)
                StandardAndZipLists.Add(new PackageTest(1, "NetCore31X86Test")
                {
                    Description = "Run mock-assembly-x86.dll targeting .NET Core 3.1",
                    Arguments = "testdata/netcoreapp3.1/mock-assembly-x86.dll",
                    ExpectedResult = new MockAssemblyX86ExpectedResult("netcore-3.1")
                });
        }

        //////////////////////////////////////////////////////////////////////
        // RUN MULTIPLE COPIES OF MOCK-ASSEMBLY
        //////////////////////////////////////////////////////////////////////

        StandardAndZipLists.Add(new PackageTest(1, "Net462PlusNet462Test")
        {
            Description = "Run two copies of mock-assembly together",
            Arguments = "testdata/net462/mock-assembly.dll testdata/net462/mock-assembly.dll",
            ExpectedResult = new MockAssemblyExpectedResult("net-4.6.2", "net-4.6.2")
        });

        StandardAndZipLists.Add(new PackageTest(1, "Net60PlusNet80Test")
        {
            Description = "Run mock-assembly targeting .NET6.0 and 8.0 together",
            Arguments = "testdata/net6.0/mock-assembly.dll testdata/net8.0/mock-assembly.dll",
            ExpectedResult = new MockAssemblyExpectedResult("netcore-6.0", "netcore-8.0")
        });

        StandardAndZipLists.Add(new PackageTest(1, "Net462PlusNet60Test")
        {
            Description = "Run mock-assembly targeting .Net Framework 4.6.2 and .Net 6.0 together",
            Arguments = "testdata/net462/mock-assembly.dll testdata/net6.0/mock-assembly.dll",
            ExpectedResult = new MockAssemblyExpectedResult("net-4.6.2", "netcore-6.0")
        });

        //////////////////////////////////////////////////////////////////////
        // ASP.NETCORE TESTS
        //////////////////////////////////////////////////////////////////////

        AllLists.Add(new PackageTest(1, "Net60AspNetCoreTest")
        {
            Description = "Run test using AspNetCore targeting .NET 6.0",
            Arguments = "testdata/net6.0/aspnetcore-test.dll",
            ExpectedResult = new ExpectedResult("Passed")
            {
                Total = 3,
                Passed = 3,
                Failed = 0,
                Warnings = 0,
                Inconclusive = 0,
                Skipped = 0,
                Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("aspnetcore-test.dll", "netcore-6.0") }
            }
        });

        AllLists.Add(new PackageTest(1, "Net80AspNetCoreTest")
        {
            Description = "Run test using AspNetCore targeting .NET 8.0",
            Arguments = "testdata/net8.0/aspnetcore-test.dll",
            ExpectedResult = new ExpectedResult("Passed")
            {
                Total = 3,
                Passed = 3,
                Failed = 0,
                Warnings = 0,
                Inconclusive = 0,
                Skipped = 0,
                Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("aspnetcore-test.dll", "netcore-8.0") }
            }
        });

        //////////////////////////////////////////////////////////////////////
        // WINDOWS FORMS TESTS
        //////////////////////////////////////////////////////////////////////

        AllLists.Add(new PackageTest(1, "Net60WindowsFormsTest")
        {
            Description = "Run test using windows forms targeting .NET 6.0",
            Arguments = "testdata/net6.0-windows/windows-forms-test.dll",
            ExpectedResult = new ExpectedResult("Passed")
            {
                Total = 2,
                Passed = 2,
                Failed = 0,
                Warnings = 0,
                Inconclusive = 0,
                Skipped = 0,
                Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("windows-forms-test.dll", "netcore-6.0") }
            }
        });

        StandardAndZipLists.Add(new PackageTest(1, "Net80WindowsFormsTest")
        {
            Description = "Run test using windows forms targeting .NET 8.0",
            Arguments = "testdata/net8.0-windows/windows-forms-test.dll",
            ExpectedResult = new ExpectedResult("Passed")
            {
                Total = 2,
                Passed = 2,
                Failed = 0,
                Warnings = 0,
                Inconclusive = 0,
                Skipped = 0,
                Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("windows-forms-test.dll", "netcore-8.0") }
            }
        });

        //////////////////////////////////////////////////////////////////////
        // WPF TESTS
        //////////////////////////////////////////////////////////////////////

        AllLists.Add(new PackageTest(1, "Net60WPFTest")
        {
            Description = "Run test using WPF targeting .NET 6.0",
            Arguments = "testdata/net6.0-windows/WpfTest.dll --trace=Debug",
            ExpectedResult = new ExpectedResult("Passed") { Assemblies = new[] { new ExpectedAssemblyResult("WpfTest.dll", "netcore-6.0") } }
        });

        AllLists.Add(new PackageTest(1, "Net80WPFTest")
        {
            Description = "Run test using WPF targeting .NET 8.0",
            Arguments = "testdata/net8.0-windows/WpfTest.dll --trace=Debug",
            ExpectedResult = new ExpectedResult("Passed") { Assemblies = new[] { new ExpectedAssemblyResult("WpfTest.dll", "netcore-8.0") } }
        });

        //////////////////////////////////////////////////////////////////////
        // TESTS OF EXTENSION LISTING
        //////////////////////////////////////////////////////////////////////

        StandardAndNetCoreLists.Add(new PackageTest(1, "NoExtensionsInstalled")
        {
            Description = "List Extensions shows none installed",
            Arguments = "--list-extensions",
            OutputCheck = new OutputDoesNotContain("Extension:"),
        });

        StandardAndNetCoreLists.Add(new PackageTest(1, "ExtensionsInstalledFromAddedDirectory")
        {
            Description = "List Extensions shows extension from added directory",
            Arguments = "--extensionDirectory ../../src/TestData/FakeExtensions --list-extensions",
            OutputCheck = new OutputContains("Extension:", exactly: 5)
        });

        ZipRunnerTests.Add(new PackageTest(1, "BundledExtensionsInstalled")
        {
            Description = "List Extensions shows bundled extensions",
            Arguments = "--list-extensions",
            OutputCheck = new OutputContains("Extension:", exactly: 5)
        });

        //////////////////////////////////////////////////////////////////////
        // TESTS USING EACH OF OUR EXTENSIONS
        //////////////////////////////////////////////////////////////////////

        // NUnit Project Loader Tests
        StandardAndZipLists.Add(new PackageTest(1, "NUnitProjectTest")
        { 
            Description = "Run NUnit project with mock-assembly.dll targeting .NET 4.6.2 and 6.0",
            Arguments = "../../MixedTests.nunit --config=Release",
            ExpectedResult = new MockAssemblyExpectedResult("net-4.6.2", "net-6.0"),
            ExtensionsNeeded = new [] { KnownExtensions.NUnitProjectLoader }
        });

        NetCoreRunnerTests.Add(new PackageTest(1, "NUnitProjectTest")
        {
            Description= "Run NUnit project with mock-assembly.dll targeting .NET 6.0 and 8.0",
            Arguments="../../NetCoreTests.nunit --config=Release",
            ExpectedResult = new MockAssemblyExpectedResult("netcore-6.0", "netcore-8.0"),
            ExtensionsNeeded = new [] { KnownExtensions.NUnitProjectLoader }
        });

        // V2 Result Writer Test
        AllLists.Add(new PackageTest(1, "V2ResultWriterTest")
        {
            Description = "Run mock-assembly targeting .NET 6.0 and produce V2 output",
            Arguments = "testdata/net6.0/mock-assembly.dll --result=TestResult.xml --result=NUnit2TestResult.xml;format=nunit2",
            ExpectedResult = new MockAssemblyExpectedResult("netcore-6.0"),
            ExtensionsNeeded = new[] { KnownExtensions.NUnitV2ResultWriter }
        });

        // VS Project Loader Tests
        StandardAndZipLists.Add(new PackageTest(1, "VSProjectLoaderTest_Project")
        {
            Description = "Run mock-assembly using the .csproj file",
            Arguments = "../../src/TestData/mock-assembly/mock-assembly.csproj --config=Release",
            ExpectedResult = new MockAssemblyExpectedResult("net462", "netcore-3.1", "netcore-6.0", "netcore-7.0", "netcore-8.0"),
            ExtensionsNeeded = new[] { KnownExtensions.VSProjectLoader }
        });

        StandardAndZipLists.Add(new PackageTest(1, "VSProjectLoaderTest_Solution")
        {
            Description = "Run mock-assembly using the .sln file",
            Arguments = "../../src/TestData/TestData.sln --config=Release --trace=Debug",
            ExpectedResult = new ExpectedResult("Failed")
            {
                Total = 37 * 5,
                Passed = 23 * 5,
                Failed = 5 * 5,
                Warnings = 1 * 5,
                Inconclusive = 1 * 5,
                Skipped = 7 * 5,
                Assemblies = new ExpectedAssemblyResult[]
                {
                    new ExpectedAssemblyResult("mock-assembly.dll", "net-4.6.2"),
                    new ExpectedAssemblyResult("mock-assembly.dll", "netcore-3.1"),
                    new ExpectedAssemblyResult("mock-assembly.dll", "netcore-6.0"),
                    new ExpectedAssemblyResult("mock-assembly.dll", "netcore-7.0"),
                    new ExpectedAssemblyResult("mock-assembly.dll", "netcore-8.0"),
                    new ExpectedAssemblyResult("notest-assembly.dll", "net-4.6.2"),
                    new ExpectedAssemblyResult("notest-assembly.dll", "netcore-3.1"),
                    new ExpectedAssemblyResult("notest-assembly.dll", "netstandard-2.0"),
                    new ExpectedAssemblyResult("WpfApp.exe")
                }
            },
            ExtensionsNeeded = new[] { KnownExtensions.VSProjectLoader }
        });

        // TeamCity Event Listener Tests
        StandardAndZipLists.Add(new PackageTest(1, "Net462TeamCityListenerTest1")
        {
            Description = "Run mock-assembly targeting .NET 4.6.2 with --teamcity option",
            Arguments = "testdata/net462/mock-assembly.dll --teamcity --trace:Debug",
            ExpectedResult = new MockAssemblyExpectedResult("net-4.6.2"),
            ExtensionsNeeded = new[] { KnownExtensions.TeamCityEventListener },
            OutputCheck = new OutputContains("##teamcity")
        });

        // TeamCity Event Listener Test
        StandardAndZipLists.Add(new PackageTest(1, "Net462TeamCityListenerTest2")
        {
            Description = "Run mock-assembly targeting .NET 4.6.2 with --enable teamcity option",
            Arguments = "testdata/net462/mock-assembly.dll --enable:NUnit.Engine.Listeners.TeamCityEventListener --trace:Debug",
            ExpectedResult = new MockAssemblyExpectedResult("net-4.6.2"),
            ExtensionsNeeded = new[] { KnownExtensions.TeamCityEventListener },
            OutputCheck = new OutputContains("##teamcity")
        });

        AllLists.Add(new PackageTest(1, "Net60TeamCityListenerTest1")
        {
            Description = "Run mock-assembly targeting .NET 6.0 with --teamcity option",
            Arguments = "testdata/net6.0/mock-assembly.dll --teamcity --trace:Debug",
            ExpectedResult = new MockAssemblyExpectedResult("net-6.0"),
            ExtensionsNeeded = new[] { KnownExtensions.TeamCityEventListener },
            OutputCheck = new OutputContains("##teamcity")
        });

        // TeamCity Event Listener Test
        AllLists.Add(new PackageTest(1, "Net60TeamCityListenerTest2")
        {
            Description = "Run mock-assembly targeting .NET 6.0 with --enable teamcity option",
            Arguments = "testdata/net6.0/mock-assembly.dll --enable:NUnit.Engine.Listeners.TeamCityEventListener --trace:Debug",
            ExpectedResult = new MockAssemblyExpectedResult("net-6.0"),
            ExtensionsNeeded = new[] { KnownExtensions.TeamCityEventListener },
            OutputCheck = new OutputContains("##teamcity")
        });

        // V2 Framework Driver Tests
        StandardAndZipLists.Add(new PackageTest(1, "V2FrameworkDriverTest")
        {
            Description = "Run mock-assembly-v2 using the V2 Driver in process",
            Arguments = "v2-tests/net462/mock-assembly-v2.dll",
            ExpectedResult = new ExpectedResult("Failed")
            {
                Total = 28,
                Passed = 18,
                Failed = 5,
                Warnings = 0,
                Inconclusive = 1,
                Skipped = 4,
                Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("mock-assembly-v2.dll", "net-4.6.2") }
            },
            ExtensionsNeeded = new[] { KnownExtensions.NUnitV2Driver }
        });

        StandardAndZipLists.Add(new PackageTest(1, "V2FrameworkDriverTest")
        {
            Description = "Run mock-assembly-v2 using the V2 Driver out of process",
            Arguments = "v2-tests/net462/mock-assembly-v2.dll",
            ExpectedResult = new ExpectedResult("Failed")
            {
                Total = 28,
                Passed = 18,
                Failed = 5,
                Warnings = 0,
                Inconclusive = 1,
                Skipped = 4,
                Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("mock-assembly-v2.dll", "net-4.6.2") }
            },
            ExtensionsNeeded = new[] { KnownExtensions.NUnitV2Driver }
        });

        //////////////////////////////////////////////////////////////////////
        // SPECIAL CASES
        //////////////////////////////////////////////////////////////////////

        StandardAndZipLists.Add(new PackageTest(1, "InvalidTestNameTest_Net462")
        {
            Description = "Ensure we handle invalid test names correctly targeting .NET 4.6.2",
            Arguments = "testdata/net462/InvalidTestNames.dll --trace:Debug",
            ExpectedResult = new ExpectedResult("Passed")
            {
                Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("InvalidTestNames.dll", "net-4.6.2") }
            }
        });

        AllLists.Add(new PackageTest(1, "InvalidTestNameTest_Net60")
        {
            Description = "Ensure we handle invalid test names correctly targeting .NET 6.0",
            Arguments = "testdata/net6.0/InvalidTestNames.dll --trace:Debug",
            ExpectedResult = new ExpectedResult("Passed")
            {
                Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("InvalidTestNames.dll", "netcore-6.0") }
            }
        });

        AllLists.Add(new PackageTest(1, "InvalidTestNameTest_Net80")
        {
            Description = "Ensure we handle invalid test names correctly targeting .NET 8.0",
            Arguments = "testdata/net8.0/InvalidTestNames.dll --trace:Debug",
            ExpectedResult = new ExpectedResult("Passed")
            {
                Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("InvalidTestNames.dll", "netcore-8.0") }
            }
        });

        StandardAndZipLists.Add(new PackageTest(1, "AppContextBaseDirectory_NET80")
        {
            Description = "Test Setting the BaseDirectory to match test assembly location targeting .NET 8.0",
            Arguments = "testdata/net8.0/AppContextTest.dll",
            ExpectedResult = new ExpectedResult("Passed")
            {
                Assemblies = new ExpectedAssemblyResult[] { new ExpectedAssemblyResult("AppContextTest.dll", "netcore-8.0") }
            }
        });
    }

    #region Nested Classes

    class ListWrapper
    {
        private List<PackageTest>[] _lists;

        public ListWrapper(params List<PackageTest>[] lists)
        {
            _lists = lists;
        }

        public void Add(PackageTest test)
        {
            foreach (var list in _lists)
                list.Add(test);
        }
    }

    class MockAssemblyExpectedResult : ExpectedResult
    {
        public MockAssemblyExpectedResult(params string[] runtimes) : base("Failed")
        {
            int nCopies = runtimes.Length;
            Total = 37 * nCopies;
            Passed = 23 * nCopies;
            Failed = 5 * nCopies;
            Warnings = 1 * nCopies;
            Inconclusive = 1 * nCopies;
            Skipped = 7 * nCopies;
            Assemblies = new ExpectedAssemblyResult[nCopies];
            for (int i = 0; i < nCopies; i++)
                Assemblies[i] = new ExpectedAssemblyResult("mock-assembly.dll", runtimes[i]);
        }
    }

    class MockAssemblyX86ExpectedResult : MockAssemblyExpectedResult
    {
        public MockAssemblyX86ExpectedResult(params string[] runtimes) : base(runtimes)
        {
            for (int i = 0; i < runtimes.Length; i++)
                Assemblies[i] = new ExpectedAssemblyResult("mock-assembly-x86.dll", runtimes[i]);
        }
    }

    #endregion
}
