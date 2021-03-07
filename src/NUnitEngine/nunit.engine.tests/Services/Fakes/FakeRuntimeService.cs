// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine.Services.Tests.Fakes
{
    public class FakeRuntimeService : FakeService, IRuntimeFrameworkService
    {
        bool IRuntimeFrameworkService.IsAvailable(string framework)
        {
            return true;
        }

        string IRuntimeFrameworkService.SelectRuntimeFramework(TestPackage package)
        {
            return string.Empty;
        }
    }
}
