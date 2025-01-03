// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine.Extensibility;

// Extension points supported by the engine

[assembly: ExtensionPoint("/NUnit/Engine/NUnitV2Driver", typeof(IFrameworkDriver),
    Description="Driver for NUnit tests using the V2 framework.")]
