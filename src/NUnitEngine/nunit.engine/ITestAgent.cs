﻿// ***********************************************************************
// Copyright (c) 2011 Charlie Poole
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

namespace NUnit.Engine
{
    /// <summary>
    /// The ITestAgent interface is implemented by remote test agents.
    /// </summary>
    public interface ITestAgent
    {
        /// <summary>
        /// Gets the agency with which this agent is associated.
        /// </summary>
        ITestAgency Agency { get; }

        /// <summary>
        /// Gets a Guid that uniquely identifies this agent.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Starts the agent, performing any required initialization
        /// </summary>
        /// <returns>True if successful, otherwise false</returns>
        bool Start();

        /// <summary>
        /// Stops the agent, releasing any resources
        /// </summary>
        void Stop();

        /// <summary>
        /// Pings the agent to check that it is still alive.
        /// </summary>
        void Ping();

        /// <summary>
        ///  Creates a test runner
        /// </summary>
        ITestEngineRunner CreateRunner(TestPackage package);
    }
}
