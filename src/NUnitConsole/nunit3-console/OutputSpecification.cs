﻿// ***********************************************************************
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
using System.IO;
using System.Text;

namespace NUnit.Common
{
    /// <summary>
    /// OutputSpecification encapsulates a file output path and format
    /// for use in saving the results of a run.
    /// </summary>
    public class OutputSpecification
    {
        #region Constructor

        /// <summary>
        /// Construct an OutputSpecification from an option value.
        /// </summary>
        /// <param name="spec">The option value string.</param>
        /// <param name="transformFolder">The folder containing the transform.</param>
        public OutputSpecification(string spec, string transformFolder)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec), "Output spec may not be null");

            string[] parts = spec.Split(';');
            this.OutputPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string[] opt = parts[i].Split('=');

                if (opt.Length != 2)
                    throw new ArgumentException($"Invalid output specification: {spec}");

                switch (opt[0].Trim())
                {
                    case "format":
                        string fmt = opt[1].Trim();

                        if (this.Format != null && this.Format != fmt)
                            throw new ArgumentException(
                                string.Format("Conflicting format options: {0}", spec));

                        this.Format = fmt;
                        break;

                    case "transform":
                        string val = opt[1].Trim();

                        if (this.Transform != null && this.Transform != val)
                            throw new ArgumentException(
                                string.Format("Conflicting transform options: {0}", spec));

                        if (this.Format != null && this.Format != "user")
                            throw new ArgumentException(
                                string.Format("Conflicting format options: {0}", spec));

                        this.Format = "user";
                        this.Transform = Path.Combine(transformFolder ?? "", val);
                        break;
                }
            }

            if (Format == null)
                Format = "nunit3";
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the path to which output will be written
        /// </summary>
        public string OutputPath { get; private set; }

        /// <summary>
        /// Gets the name of the format to be used
        /// </summary>
        public string Format { get; private set; }

        /// <summary>
        /// Gets the file name of a transform to be applied
        /// </summary>
        public string Transform { get; private set; }

        #endregion

        public override string ToString()
        {
            var sb = new StringBuilder($"OutputPath: {OutputPath}");
            if (Format != null) sb.Append($", Format: {Format}");
            if (Transform != null) sb.Append($", Transform: {Transform}");
            return sb.ToString();
        }
    }
}
