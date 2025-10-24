// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Extensibility;

// ExtensionPoints supported by the engine
[assembly: ExtensionPoint("/NUnit/Engine/AgentLaunchers", typeof(NUnit.Engine.Extensibility.IAgentLauncher),
    Description = "Launches an Agent Process for supported target runtimes")]
[assembly: ExtensionPoint("/NUnit/Engine/ProjectLoaders", typeof(NUnit.Engine.Extensibility.IProjectLoader),
    Description = "Recognizes and loads assemblies from various types of project formats.")]
[assembly: ExtensionPoint("/NUnit/Engine/ResultWriters", typeof(NUnit.Engine.Extensibility.IResultWriter),
    Description = "Supplies a writer to write the result of a test to a file using a specific format.")]
[assembly: ExtensionPoint("/NUnit/Engine/TestEventListeners", typeof(NUnit.Engine.ITestEventListener),
    Description = "Allows an extension to process progress reports and other events from the test.")]
[assembly: ExtensionPoint("/NUnit/Engine/FrameworkDrivers", typeof(NUnit.Engine.Extensibility.IDriverFactory),
    Description = "Supplies a driver to run tests that use a specific test framework.")]
[assembly: ExtensionPoint("/NUnit/Engine/Services", typeof(NUnit.Engine.IService),
    Description = "Provides a service within the engine and possibly externally as well.")]
