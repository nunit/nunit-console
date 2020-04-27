// ***********************************************************************
// Copyright (c) 2011 Charlie Poole, Rob Prouse
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
using System.Text;

namespace NUnit.Engine
{
    /// <summary>
    /// The IProjectService interface exposes selected information about Project files publicly
    /// </summary>
    public interface IProjectService
    {
        /// <summary>
        /// Determines whether a file represents a supported project type.
        /// </summary>
        /// <param name="path">Path to the file being examined.</param>
        /// <returns></returns>
        bool IsSupportedProject(string path);

        /// <summary>
        /// Get the name of the currently active configuration for this project
        /// </summary>
        /// <param name="projectPath">Path to the project</param>
        /// <returns>True if this is a supported project type, otherwise false.</returns>
        string GetActiveConfig(string projectPath);

        /// <summary>
        /// Get a list of the available configs for a project
        /// </summary>
        /// <param name="projectPath">Path to the project</param>
        /// <returns>A list of available project configurations</returns>
        IList<string> GetConfigNames(string projectPath);
    }
}
