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

using System;
using System.IO;

namespace NUnit.Engine.Communication
{
    public struct ProtocolReader : IDisposable
    {
        private readonly BinaryReader _reader;

        public ProtocolReader(Stream stream)
        {
            _reader = new BinaryReader(stream);
        }

        public Stream BaseStream => _reader.BaseStream;

        public void Dispose()
        {
#if NET20
            _reader.Close();
#else
            _reader.Dispose();
#endif
        }

        public bool TryReadByte(out byte value)
        {
            var result = _reader.BaseStream.ReadByte();
            if (result != -1)
            {
                value = (byte)result;
                return true;
            }

            value = 0;
            return false;
        }

        public Result<int> Read7BitEncodedInt32()
        {
            return Read7BitEncodedUInt32().Select(unsigned => (int)unsigned);
        }

        public Result<uint> Read7BitEncodedUInt32()
        {
            var result = 0U;
            var shift = 0;

            while (true)
            {
                if (!TryReadByte(out var nextByte))
                    return Result.Error("The stream ended unexpectedly.");

                if (shift == 28 && nextByte > 0b1111u)
                    return Result.Error("The value is too large to be represented in 32 bits.");

                result |= (nextByte & 0b0_1111111u) << shift;

                if ((nextByte & 0b1_0000000u) == 0)
                    return Result.Success(result);

                shift += 7;
            }
        }

        public Result<string> ReadString()
        {
            try
            {
                return Result.Success(_reader.ReadString());
            }
            catch (Exception ex) when (ex is FormatException || ex is IOException)
            {
                return Result.Error("Error parsing length-prefixed string: " + ex.Message);
            }
        }

        public Result<byte[]> ReadBytes(int count)
        {
            var result = new byte[count];
            var position = 0;

            while (position < count)
            {
                var byteCount = _reader.BaseStream.Read(result, position, count - position);
                if (byteCount == 0)
                    return Result.Error("Unexpected end of stream");

                position += byteCount;
            }

            return Result.Success(result);
        }

        public int ReadInt32() => _reader.ReadInt32();
    }
}
