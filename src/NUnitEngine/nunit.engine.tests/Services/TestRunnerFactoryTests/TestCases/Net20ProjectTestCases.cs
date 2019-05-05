// ***********************************************************************
// Copyright (c) 2019 Charlie Poole, Rob Prouse
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Engine.Internal;
using NUnit.Engine.Tests.Services.TestRunnerFactoryTests.Results;
using NUnit.Framework;

namespace NUnit.Engine.Tests.Services.TestRunnerFactoryTests.TestCases
{
#if !NETCOREAPP
    internal static class Net20ProjectTestCases
    {
        public static IEnumerable<TestCaseData> TestCases
        {
            get
            {
                foreach (var processModel in Enum.GetValues(typeof(ProcessModel)).Cast<ProcessModel>())
                {
                    foreach (var domainUsage in Enum.GetValues(typeof(DomainUsage)).Cast<DomainUsage>())
                    {
                        var testName = "Single project (list ctor) - " +
                                       $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel} " +
                                       $"{nameof(EnginePackageSettings.DomainUsage)}:{domainUsage}";

                        var package = TestPackageFactory.OneProjectListCtor();
                        package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());
                        package.AddSetting(EnginePackageSettings.DomainUsage, domainUsage.ToString());

                        var expected = Net20SingleProjectListCtorExpectedRunnerResults.ResultFor(processModel, domainUsage);
                        yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");

                        testName = "Single project (string ctor) - " +
                                       $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel} " +
                                       $"{nameof(EnginePackageSettings.DomainUsage)}:{domainUsage}";

                        package = TestPackageFactory.OneProjectStringCtor();
                        package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());
                        package.AddSetting(EnginePackageSettings.DomainUsage, domainUsage.ToString());

                        expected = Net20SingleProjectStringCtorExpectedRunnerResults.ResultFor(processModel, domainUsage);
                        yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");

                        testName = "Two projects - " +
                                       $"{nameof(EnginePackageSettings.ProcessModel)}:{processModel} " +
                                       $"{nameof(EnginePackageSettings.DomainUsage)}:{domainUsage}";

                        package = TestPackageFactory.TwoProjects();
                        package.AddSetting(EnginePackageSettings.ProcessModel, processModel.ToString());
                        package.AddSetting(EnginePackageSettings.DomainUsage, domainUsage.ToString());

                        expected = Net20TwoProjectExpectedRunnerResults.ResultFor(processModel, domainUsage);
                        yield return new TestCaseData(package, expected).SetName($"{{m}}({testName})");
                    }
                }
            }
        }
    }
#endif
}
