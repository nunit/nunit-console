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

using System.IO;

namespace NUnit.Engine.Communication.Model
{
    public struct RequestHeader
    {
        public RequestHeader(AgentWorkerRequestType requestType, uint requestLength)
        {
            RequestType = requestType;
            RequestLength = requestLength;
        }

        public AgentWorkerRequestType RequestType { get; }
        public uint RequestLength { get; }

        public static Result<RequestHeader> Read(ProtocolReader reader)
        {
            if (!reader.TryReadByte(out var requestType))
                return Result.Error("Unexpected end of stream.");

            return reader.Read7BitEncodedUInt32()
                .Select(requestLength => new RequestHeader((AgentWorkerRequestType)requestType, requestLength));
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)RequestType);
            writer.Write7BitEncodedInt(RequestLength);
        }
    }
}
