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

using NUnit.Engine;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace NUnit.Agent.AgentProtocol
{
    internal sealed partial class AgentServer : IDisposable
    {
        private readonly Stream _stream;
        private readonly BinaryReader _reader;
        private readonly BinaryWriter _writer;
        private readonly BinaryFormatter _formatter = new BinaryFormatter();

        private ITestEngineRunner _runner;

        public AgentServer(Stream stream)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _reader = new BinaryReader(stream);
            _writer = new BinaryWriter(stream);
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        public void Connect(ITestRunnerFactory runnerFactory)
        {
            if (runnerFactory == null) throw new ArgumentNullException(nameof(runnerFactory));

            var version = new BinaryReader(_stream).ReadByte();
            if (version != 1) throw new InvalidDataException("Incompatible protocol version.");

            _runner = runnerFactory.MakeTestRunner(ReadTestPackage());
        }

        public bool Read()
        {
            var nextByte = _stream.ReadByte();
            if (nextByte == -1) return false; // Read side was closed.

            switch ((AgentCommandType)nextByte)
            {
                case AgentCommandType.Load:
                    WriteTestEngineResult(_runner.Load());
                    _stream.Flush();
                    break;

                case AgentCommandType.Reload:
                    WriteTestEngineResult(_runner.Reload());
                    _stream.Flush();
                    break;

                case AgentCommandType.Unload:
                    _runner.Unload();
                    break;

                case AgentCommandType.CountTestCases:
                    _writer.Write(_runner.CountTestCases(ReadTestFilter()));
                    _stream.Flush();
                    break;

                case AgentCommandType.Explore:
                    WriteTestEngineResult(_runner.Explore(ReadTestFilter()));
                    _stream.Flush();
                    break;

                case AgentCommandType.Run:
                    // TODO: run on threadpool and keep the read loop going in case the next byte is StopRun
                    var result = _runner.Run(new SerializedWritingEventListener(_writer), ReadTestFilter());

                    const bool isEvent = false;
                    _writer.Write(isEvent);
                    WriteTestEngineResult(result);
                    _stream.Flush();
                    break;

                case AgentCommandType.StopRun:
                    _runner.StopRun(force: _reader.ReadBoolean());
                    break;

                default:
                    throw new InvalidDataException("Unrecognized message type " + (AgentCommandType)nextByte);
            }

            return true;
        }

        private TestFilter ReadTestFilter()
        {
            var text = _reader.ReadString();
            return text != string.Empty ? new TestFilter(text) : TestFilter.Empty;
        }

        private TestPackage ReadTestPackage()
        {
            return (TestPackage)_formatter.Deserialize(_stream);
        }

        private void WriteTestEngineResult(TestEngineResult result)
        {
            TestEngineResult.Serialize(result, _writer);
        }
    }
}
