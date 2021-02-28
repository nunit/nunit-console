// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Engine.Services
{
    public class TestFilterService : Service, ITestFilterService
    {
        public ITestFilterBuilder GetTestFilterBuilder()
        {
            return new TestFilterBuilder();
        }
    }
}
