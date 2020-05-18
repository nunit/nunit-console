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
using System.Text;

namespace NUnit.Engine.Communication.Model
{
    public struct CountTestCasesRequest
    {
        private static readonly Encoding Encoding = new UTF8Encoding(false);

        public CountTestCasesRequest(TestFilter filter)
        {
            Filter = filter;
        }

        public TestFilter Filter { get; }

        public static Result<CountTestCasesRequest> ReadBody(uint requestLength, ProtocolReader reader)
        {
            return reader.ReadBytes(checked((int)requestLength))
                .Select(byteArray => new CountTestCasesRequest(new TestFilter(Encoding.GetString(byteArray))));
        }

        public void Write(BinaryWriter writer)
        {
            var bytes = Encoding.GetBytes(Filter.Text);

            new RequestHeader(AgentWorkerRequestType.CountTestCases, (uint)bytes.Length)
                .Write(writer);

            writer.Write(bytes);
        }
    }
}
