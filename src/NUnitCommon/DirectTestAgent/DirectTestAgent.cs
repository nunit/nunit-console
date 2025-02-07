// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Agents
{
    public class DirectTestAgent : NUnitAgent<DirectTestAgent>
    {
        public static void Main(string[] args)
        {
            Execute(args);
        }
    }
}
