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
    public sealed partial class FrameableStream
    {
        private sealed class FrameStream : Stream
        {
            private readonly Stream _baseStream;
            private long _remainingLength;

            public FrameStream(Stream baseStream, long frameLength)
            {
                if (frameLength < 0)
                    throw new ArgumentOutOfRangeException(nameof(frameLength), frameLength, "The frame length must not be negative.");

                if (baseStream.CanSeek && baseStream.Length - baseStream.Position < frameLength)
                    throw new ArgumentOutOfRangeException(nameof(frameLength), frameLength, "The frame length must not extend past the end of the underlying stream.");

                _baseStream = baseStream;
                _remainingLength = frameLength;
            }

            public bool IsEnded => _remainingLength == 0;

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_remainingLength == 0) return 0;

                if (_remainingLength < int.MaxValue)
                    count = Math.Min(count, (int)_remainingLength);

                var byteCount = _baseStream.Read(buffer, offset, count);
                if (byteCount == 0)
                    throw CreateEndOfStreamException();

                _remainingLength -= byteCount;
                return byteCount;
            }

            public void SkipToEnd()
            {
                if (_remainingLength == 0) return;

                if (_baseStream.CanSeek)
                {
                    if (_baseStream.Length - _baseStream.Position < _remainingLength)
                        throw CreateEndOfStreamException();

                    _baseStream.Seek(_remainingLength, SeekOrigin.Current);
                    _remainingLength = 0;
                }
                else
                {
                    var skipBuffer = new byte[Math.Min(_remainingLength, 81920)];

                    while (_remainingLength > 0)
                    {
                        var bytesRead = (uint)_baseStream.Read(skipBuffer, 0, (int)Math.Min(_remainingLength, skipBuffer.Length));
                        if (bytesRead == 0)
                            throw CreateEndOfStreamException();

                        _remainingLength -= bytesRead;
                    }
                }
            }

            private static Exception CreateEndOfStreamException()
            {
                return new EndOfStreamException("The frame extends past the end of the underlying stream.");
            }

            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => false;

            public override long Length => throw new NotSupportedException();

            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            public override void Flush()
            {
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }
        }
    }
}
