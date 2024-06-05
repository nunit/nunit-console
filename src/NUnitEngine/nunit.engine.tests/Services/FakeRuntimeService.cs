// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections.Generic;

namespace NUnit.Engine.Services
{
    public class FakeRuntimeService : FakeService, IRuntimeFrameworkService, IAvailableRuntimes
    {
        public IRuntimeFramework CurrentFramework => throw new System.NotImplementedException();

        bool IRuntimeFrameworkService.IsAvailable(string framework, bool runAsX86)
        {
            return true;
        }

        string IRuntimeFrameworkService.SelectRuntimeFramework(TestPackage package)
        {
            return string.Empty;
        }

        public IList<IRuntimeFramework> AvailableRuntimes => throw new System.NotImplementedException();

        public IList<IRuntimeFramework> AvailableX86Runtimes => throw new System.NotImplementedException();
    }
}
