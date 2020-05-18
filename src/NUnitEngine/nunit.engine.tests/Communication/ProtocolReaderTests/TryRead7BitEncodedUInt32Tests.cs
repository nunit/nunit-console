// ***********************************************************************
// Copyright (c) 2020 Charlie Poole, Rob Prouse
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

using NUnit.Engine.Communication;
using NUnit.Framework;
using System.IO;

namespace NUnit.Engine.Tests.Communication.ProtocolReaderTests
{
    public static class TryRead7BitEncodedUInt32Tests
    {
        [Test]
        public static void Smallest_number_requiring_one_byte()
        {
            using (var reader = new ProtocolReader(new MemoryStream(new byte[] { 0b0_0000000 })))
            {
                Assert.That(reader.Read7BitEncodedUInt32(), Is.EqualTo(Result.Success(0u)));
            }
        }

        [Test]
        public static void Largest_number_requiring_one_byte()
        {
            using (var reader = new ProtocolReader(new MemoryStream(new byte[] { 0b0_1111111 })))
            {
                Assert.That(reader.Read7BitEncodedUInt32(), Is.EqualTo(Result.Success(0b1111111u)));
            }
        }

        [Test]
        public static void Smallest_number_requiring_two_bytes()
        {
            using (var reader = new ProtocolReader(new MemoryStream(new byte[] { 0b1_0000000, 0b0_0000001 })))
            {
                Assert.That(reader.Read7BitEncodedUInt32(), Is.EqualTo(Result.Success(0b1_0000000u)));
            }
        }

        [Test]
        public static void Largest_number_requiring_two_bytes()
        {
            using (var reader = new ProtocolReader(new MemoryStream(new byte[] { 0b1_1111111, 0b0_1111111 })))
            {
                Assert.That(reader.Read7BitEncodedUInt32(), Is.EqualTo(Result.Success(0b1111111_1111111u)));
            }
        }

        [Test]
        public static void Smallest_number_requiring_three_bytes()
        {
            using (var reader = new ProtocolReader(new MemoryStream(new byte[] { 0b1_0000000, 0b1_0000000, 0b0_0000001 })))
            {
                Assert.That(reader.Read7BitEncodedUInt32(), Is.EqualTo(Result.Success(0b1_0000000_0000000u)));
            }
        }

        [Test]
        public static void Largest_number_requiring_three_bytes()
        {
            using (var reader = new ProtocolReader(new MemoryStream(new byte[] { 0b1_1111111, 0b1_1111111, 0b0_1111111 })))
            {
                Assert.That(reader.Read7BitEncodedUInt32(), Is.EqualTo(Result.Success(0b1111111_1111111_1111111u)));
            }
        }

        [Test]
        public static void Smallest_number_requiring_four_bytes()
        {
            using (var reader = new ProtocolReader(new MemoryStream(new byte[] { 0b1_0000000, 0b1_0000000, 0b1_0000000, 0b0_0000001 })))
            {
                Assert.That(reader.Read7BitEncodedUInt32(), Is.EqualTo(Result.Success(0b1_0000000_0000000_0000000u)));
            }
        }

        [Test]
        public static void Largest_number_requiring_four_bytes()
        {
            using (var reader = new ProtocolReader(new MemoryStream(new byte[] { 0b1_1111111, 0b1_1111111, 0b1_1111111, 0b0_1111111 })))
            {
                Assert.That(reader.Read7BitEncodedUInt32(), Is.EqualTo(Result.Success(0b1111111_1111111_1111111_1111111u)));
            }
        }

        [Test]
        public static void Smallest_number_requiring_five_bytes()
        {
            using (var reader = new ProtocolReader(new MemoryStream(new byte[] { 0b1_0000000, 0b1_0000000, 0b1_0000000, 0b1_0000000, 0b0_0000001 })))
            {
                Assert.That(reader.Read7BitEncodedUInt32(), Is.EqualTo(Result.Success(0b1_0000000_0000000_0000000_0000000u)));
            }
        }

        [Test]
        public static void Largest_representable_UInt32_value()
        {
            using (var reader = new ProtocolReader(new MemoryStream(new byte[] { 0b1_1111111, 0b1_1111111, 0b1_1111111, 0b1_1111111, 0b0_0001111 })))
            {
                Assert.That(reader.Read7BitEncodedUInt32(), Is.EqualTo(Result.Success(uint.MaxValue)));
            }
        }

        [Test]
        public static void Smallest_unrepresentable_UInt32_value()
        {
            using (var reader = new ProtocolReader(new MemoryStream(new byte[] { 0b1_0000000, 0b1_0000000, 0b1_0000000, 0b1_0000000, 0b0_0010000 })))
            {
                Assert.That(reader.Read7BitEncodedUInt32(), Is.EqualTo(Result<uint>.Error("The value is too large to be represented in 32 bits.")));
            }
        }

        [Test]
        public static void Maximum_number_of_leading_zeros()
        {
            using (var reader = new ProtocolReader(new MemoryStream(new byte[] { 0b1_0000000, 0b1_0000000, 0b1_0000000, 0b1_0000000, 0b0_0000000 })))
            {
                Assert.That(reader.Read7BitEncodedUInt32(), Is.EqualTo(Result.Success(0u)));
            }
        }

        [Test]
        public static void Too_many_leading_zeros()
        {
            using (var reader = new ProtocolReader(new MemoryStream(new byte[] { 0b1_0000000, 0b1_0000000, 0b1_0000000, 0b1_0000000, 0b1_0000000, 0b0_0000000 })))
            {
                Assert.That(reader.Read7BitEncodedUInt32(), Is.EqualTo(Result<uint>.Error("The value is too large to be represented in 32 bits.")));
            }
        }

        [Test]
        public static void End_of_stream()
        {
            using (var reader = new ProtocolReader(new MemoryStream(new byte[] { 0b1_0000000, 0b1_0000000, 0b1_0000000, 0b1_0000000 })))
            {
                Assert.That(reader.Read7BitEncodedUInt32(), Is.EqualTo(Result<uint>.Error("The stream ended unexpectedly.")));
            }
        }
    }
}
