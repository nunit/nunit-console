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

using NUnit.Engine.Communication.Model;
using System;
using System.IO;

#if !NETSTANDARD1_6
using System.Runtime.Serialization.Formatters.Binary;
#endif

namespace NUnit.Engine.Communication
{
    public static partial class CommunicationUtils
    {
        /// <summary>
        /// Temporary remoting go-between.
        /// </summary>
        public static ProtocolReader SendMessage(ITestAgent agent, Action<BinaryWriter> writeMessage)
        {
            return new ProtocolReader(new MemoryStream(
                agent.SendMessage(CreateMessage(writeMessage)), writable: false));
        }

        /// <summary>
        /// Temporary remoting go-between.
        /// </summary>
        public static T HandleMessageResponse<T>(ProtocolReader reader, Func<ProtocolReader, Result<T>> readResponseBody)
        {
            using (reader)
            {
                var statusResult = RequestStatus.Read(reader);
                if (statusResult.IsError(out var message))
                    throw new NUnitEngineException("Error reading request status: " + message);

                if (statusResult.Value.ErrorMessage is object)
                    throw new NUnitEngineException("Error message from agent: " + statusResult.Value.ErrorMessage);

                var messageResult = readResponseBody.Invoke(reader);
                if (messageResult.IsError(out message))
                    throw new NUnitEngineException("Error reading request body: " + message);

                return messageResult.Value;
            }
        }

        /// <summary>
        /// Temporary remoting go-between.
        /// </summary>
        public static byte[] CreateMessage(Action<BinaryWriter> writeMessage)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writeMessage.Invoke(writer);
                writer.Flush();
                return stream.ToArray();
            }
        }

#if !NETSTANDARD1_6
        /// <summary>
        /// Use of BinaryFormatter is not good, but this is what remoting has already been doing. BinaryFormatter should be
        /// tackled separately.
        /// </summary>
        public static Stream CreateStream(TestPackage package)
        {
            var stream = new MemoryStream();

            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, package);

            stream.Position = 0;
            return stream;
        }

        /// <summary>
        /// Use of BinaryFormatter is not good, but this is what remoting has already been doing. BinaryFormatter should be
        /// tackled separately.
        /// </summary>
        public static TestPackage ReadTestPackage(Stream stream)
        {
            var formatter = new BinaryFormatter();
            return (TestPackage)formatter.Deserialize(stream);
        }
#endif
    }
}
