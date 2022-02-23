// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using NUnit.Framework;

namespace NUnit.Engine.Internal.Backports
{
    [TestFixture]
    public sealed class TupleTests
    {
        [Test]
        public void Init()
        {
            var tuple = new Tuple<string, int>("foo", 4711);

            Assert.That(tuple.Item1, Is.EqualTo("foo"));
            Assert.That(tuple.Item2, Is.EqualTo(4711));
        }
    }
}
