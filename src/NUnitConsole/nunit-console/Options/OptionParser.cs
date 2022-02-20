// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;

namespace NUnit.ConsoleRunner.Options
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

            if (equalsIndex > 0 && equalsIndex < testParameterSpecification.Length - 1)
            {
                string name = testParameterSpecification.Substring(0, equalsIndex).Trim();
                if (name != string.Empty)
                {
                    string value = testParameterSpecification.Substring(equalsIndex + 1).Trim();
                    if (IsQuotedString(value))
                        value = value.Substring(1, value.Length - 2);

                    if (value != string.Empty)
                        return new KeyValuePair<string, string>(name, value);
                }
            }

            _logError("Invalid format for test parameter. Use NAME=VALUE.");
            return null;
        }

        private bool IsQuotedString(string value)
        {
            var length = value.Length;
            if (length > 2)
            {
                var quoteChar = value[0];
                if (quoteChar == '"' || quoteChar == '\'')
                    return value[length - 1] == quoteChar;
            }

            return false;
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
