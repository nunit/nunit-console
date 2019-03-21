// ***********************************************************************
// Copyright (c) 2019 Charlie Poole, Rob Prouse
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

#if !NETSTANDARD1_6
using NUnit.Agent;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace NUnit.Engine.AgentProtocol
{
    internal sealed class AgentClient : IDisposable
    {
        private readonly Stream _stream;
        private readonly BinaryReader _reader;
        private readonly BinaryWriter _writer;
        private readonly BinaryFormatter _formatter = new BinaryFormatter();
        private volatile StopRunState _stopRun;

        private enum StopRunState
        {
            None = 0,
            Stop,
            Force
        }

        public AgentClient(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _reader = new BinaryReader(stream);
            _writer = new BinaryWriter(stream);
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        public void Connect(TestPackage package)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));

            const byte version = 1;
            _writer.Write(version);
            _formatter.Serialize(_stream, package);
            _stream.Flush();
        }

        public TestEngineResult Load()
        {
            WriteCommandType(AgentCommandType.Load);
            _stream.Flush();

            return ReadTestEngineResult();
        }

        public TestEngineResult Reload()
        {
            WriteCommandType(AgentCommandType.Reload);
            _stream.Flush();

            return ReadTestEngineResult();
        }

        public void Unload()
        {
            WriteCommandType(AgentCommandType.Unload);
            _stream.Flush();
        }

        public int CountTestCases(TestFilter filter)
        {
            WriteCommandType(AgentCommandType.CountTestCases);
            WriteTestFilter(filter);
            _stream.Flush();

            return _reader.ReadInt32();
        }

        public TestEngineResult Explore(TestFilter filter)
        {
            WriteCommandType(AgentCommandType.Explore);
            WriteTestFilter(filter);
            _stream.Flush();

            return ReadTestEngineResult();
        }

        public TestEngineResult Run(ITestEventListener listener, TestFilter filter)
        {
            _stopRun = StopRunState.None;

            WriteCommandType(AgentCommandType.Run);
            WriteTestFilter(filter);
            _stream.Flush();

            while (true)
            {
                var stop = _stopRun;
                if (stop != StopRunState.None)
                {
                    WriteCommandType(AgentCommandType.StopRun);
                    var force = stop == StopRunState.Force;
                    _writer.Write(force);
                    _stream.Flush();

                    _stopRun = StopRunState.None;
                }

                var isEvent = _reader.ReadBoolean();
                if (!isEvent) break;

                listener?.OnTestEvent(_reader.ReadString());
            }

            return ReadTestEngineResult();
        }

        public AsyncTestEngineResult RunAsync(ITestEventListener listener, TestFilter filter)
        {
            var result = new AsyncTestEngineResult();

            // This isn't super important since the console never uses RunAsync,
            // but a proper implementation would not hold any threads while waiting.
            ThreadPool.QueueUserWorkItem(_ =>
            {
                result.SetResult(Run(listener, filter));
            }, null);

            return result;
        }

        public void StopRun(bool force)
        {
            _stopRun = force ? StopRunState.Force : StopRunState.Stop;
        }

        private void WriteCommandType(AgentCommandType commandType)
        {
            _writer.Write((byte)commandType);
        }

        private void WriteTestFilter(TestFilter filter)
        {
            _writer.Write(filter?.Text ?? string.Empty);
        }

        private TestEngineResult ReadTestEngineResult()
        {
            return TestEngineResult.Deserialize(_reader);
        }
    }
}
#endif
