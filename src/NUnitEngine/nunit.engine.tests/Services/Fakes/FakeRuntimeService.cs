// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine.Services.Tests.Fakes
{
    public class FakeRuntimeService : FakeService, IRuntimeFrameworkService
    {
        bool IRuntimeFrameworkService.IsAvailable(string framework, bool x86)
        {
            return true;
        }

        void IRuntimeFrameworkService.SelectRuntimeFramework(TestPackage package)
        {
        }
    }
}
