// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUnit.TextDisplay
{
    public class ResultReporterSettings
    {
        public bool StopOnError { get; init; } = false;
        public bool OmitExplicitTests { get; init; } = false;
        public bool OmitIgnoredTests { get; init; } = false;
        public bool OmitNotRunReport { get; init; } = false;
    }
}
