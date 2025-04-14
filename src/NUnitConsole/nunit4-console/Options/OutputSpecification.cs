// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Text;

namespace NUnit.ConsoleRunner.Options
{
    /// <summary>
    /// OutputSpecification encapsulates a file output path and format
    /// for use in saving the results of a run.
    /// </summary>
    public class OutputSpecification
    {
        private static readonly char[] SemicolonSeparator = [';'];
        private static readonly char[] EqualsSeparator = ['='];

        /// <summary>
        /// Construct an OutputSpecification from an option value.
        /// </summary>
        /// <param name="spec">The option value string.</param>
        /// <param name="transformFolder">The folder containing the transform.</param>
        public OutputSpecification(string spec, string? transformFolder)
        {
            Guard.ArgumentNotNull(spec);

            string[] parts = spec.Split(SemicolonSeparator);
            this.OutputPath = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string[] opt = parts[i].Split(EqualsSeparator);

                if (opt.Length != 2)
                    throw new ArgumentException($"Invalid output specification: {spec}");

                switch (opt[0].Trim())
                {
                    case "format":
                        string fmt = opt[1].Trim();

                        if (this.Format is not null && this.Format != fmt)
                            throw new ArgumentException(
                                string.Format("Conflicting format options: {0}", spec));

                        this.Format = fmt;
                        break;

                    case "transform":
                        string val = opt[1].Trim();

                        if (this.Transform is not null && this.Transform != val)
                            throw new ArgumentException(
                                string.Format("Conflicting transform options: {0}", spec));

                        if (this.Format is not null && this.Format != "user")
                            throw new ArgumentException(
                                string.Format("Conflicting format options: {0}", spec));

                        this.Format = "user";
                        this.Transform = Path.Combine(transformFolder ?? string.Empty, val);
                        break;
                }
            }

            if (Format is null)
                Format = "nunit3";
        }

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
        public string? Transform { get; private set; }

        public override string ToString()
        {
            var sb = new StringBuilder($"OutputPath: {OutputPath}");
            if (Format is not null)
                sb.Append($", Format: {Format}");
            if (Transform is not null)
                sb.Append($", Transform: {Transform}");
            return sb.ToString();
        }
    }
}
