// ***********************************************************************
// Copyright (c) 2015 Charlie Poole, Rob Prouse
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
using NUnit.Common;

namespace NUnit.ConsoleRunner.OptionsUtils
{
    internal class OptionParser
    {
        private readonly Action<string> _logError;

        public OptionParser(Action<string> logError)
        {
            _logError = logError;
        }

        /// <summary>
        /// Case is ignored when val is compared to validValues. When a match is found, the
        /// returned value will be in the canonical case from validValues.
        /// </summary>
        public string RequiredValue(string val, string option, params string[] validValues)
        {
            if (string.IsNullOrEmpty(val))
                _logError("Missing required value for option '" + option + "'.");

            bool isValid = true;

            if (validValues != null && validValues.Length > 0)
            {
                isValid = false;

                foreach (string valid in validValues)
                    if (string.Compare(valid, val, StringComparison.OrdinalIgnoreCase) == 0)
                        return valid;

            }

            if (!isValid)
                _logError($"The value '{val}' is not valid for option '{option}'.");

            return val;
        }

        public int RequiredInt(string val, string option)
        {
            if (string.IsNullOrEmpty(val))
            {
                _logError("Missing required value for option '" + option + "'.");
                return -1;
            }
            else
            {
                var success = int.TryParse(val, out var result);
                if (!success)
                {
                    _logError($"An int value was expected for option '{option}' but a value of '{val}' was used");
                    return -1;
                }
                return result;
            }
        }

        public KeyValuePair<string, string>? RequiredKeyValue(string testParameterSpecification)
        {
            var equalsIndex = testParameterSpecification.IndexOf("=");

            if (equalsIndex <= 0 || equalsIndex == testParameterSpecification.Length - 1)
            {
                _logError("Invalid format for test parameter. Use NAME=VALUE.");
                return null;
            }
            else
            {
                string name = testParameterSpecification.Substring(0, equalsIndex);
                string value = testParameterSpecification.Substring(equalsIndex + 1);

                return new KeyValuePair<string, string>(name, value);
            }
        }

        public OutputSpecification ResolveOutputSpecification(string value, IList<OutputSpecification> outputSpecifications, IFileSystem fileSystem, string currentDir)
        {
            if (value == null)
                return null;

            OutputSpecification spec;

            try
            {
                spec = new OutputSpecification(value, currentDir);
            }
            catch (ArgumentException e)
            {
                _logError(e.Message);
                return null;
            }

            if (spec.Transform != null)
            {
                if (!fileSystem.FileExists(spec.Transform))
                {
                    _logError($"Transform {spec.Transform} could not be found.");
                    return null;
                }
            }

            return spec;
        }
    }
}
