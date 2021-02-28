// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

#if NETFRAMEWORK
using System;
using System.Web.UI;

namespace NUnit.Engine
{
    public class CallbackHandler : MarshalByRefObject, ICallbackEventHandler
    {
        public string Result { get; private set; }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public string GetCallbackResult()
        {
            throw new NotImplementedException();
        }

        public void RaiseCallbackEvent(string eventArgument)
        {
            Result = eventArgument;
        }
    }
}
#endif