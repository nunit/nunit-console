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
    /// <summary>
    /// A stream which guarantees that the current frame is consumed before the continuing to read or seek the
    /// underlying stream.
    /// </summary>
    public sealed partial class FrameableStream : Stream
    {
        private readonly Stream _baseStream;
        private FrameStream _currentFrame;

        public FrameableStream(Stream baseStream)
        {
            if (!baseStream.CanRead)
                throw new ArgumentException("The underlying stream must be readable.", nameof(baseStream));

            _baseStream = baseStream;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _baseStream.Dispose();

            base.Dispose(disposing);
        }

        public Stream BeginFrame(long frameLength)
        {
            if (_currentFrame is object && !_currentFrame.IsEnded)
                throw new InvalidOperationException("The current frame must be ended before beginning a new frame.");

            _currentFrame = new FrameStream(_baseStream, frameLength);
            return _currentFrame;
        }

        public void SkipCurrentFrame()
        {
            if (_currentFrame is null) return;
            _currentFrame.SkipToEnd();
            _currentFrame = null;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_currentFrame is object && !_currentFrame.IsEnded)
                throw new InvalidOperationException("The current frame must be ended before continuing to read the underlying stream.");

            return _baseStream.Read(buffer, offset, count);
        }

        public override bool CanRead => true;

        public override bool CanSeek => _baseStream.CanSeek;

        public override long Length => _baseStream.Length;

        public override long Position
        {
            get => _baseStream.Position;
            set
            {
                if (_currentFrame is object && !_currentFrame.IsEnded)
                    throw new InvalidOperationException("The current frame must be ended before seeking the underlying stream.");

                _baseStream.Position = value;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (_currentFrame is object && !_currentFrame.IsEnded)
                throw new InvalidOperationException("The current frame must be ended before seeking the underlying stream.");

            return _baseStream.Seek(offset, origin);
        }

        public override bool CanWrite => false;

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }
    }
}
