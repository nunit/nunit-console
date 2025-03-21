// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Engine;

namespace NUnit.Extensibility.Wrappers
{
    /// <summary>
    /// Wrapper class for listeners based on the NUnit3 API
    /// </summary>
    public class TestEventListenerWrapper : ExtensionWrapper, ITestEventListener
    {
        public TestEventListenerWrapper(object listener) : base(listener) { }

        public void OnTestEvent(string report)
        {
            Invoke(nameof(OnTestEvent), report);
        }
    }
}
