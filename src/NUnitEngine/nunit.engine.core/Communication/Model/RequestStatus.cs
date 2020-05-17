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
using System.Diagnostics;
using System.IO;

namespace NUnit.Engine.Communication.Model
{
    [DebuggerDisplay("{ToString(),nq}")]
    public struct RequestStatus
    {
        public RequestStatusCode Code { get; }

        public string ErrorMessage { get; }

        private RequestStatus(RequestStatusCode code, string errorMessage)
        {
            Code = code;
            ErrorMessage = errorMessage;
        }

        public static RequestStatus Success => default(RequestStatus);

        public static RequestStatus Error(RequestStatusCode code, string message)
        {
            if (code == RequestStatusCode.Success)
                throw new ArgumentException("The specified code does not indicate an error.", nameof(code));

            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("An error message must be specified.", nameof(message));

            return new RequestStatus(code, message);
        }

        public static Result<RequestStatus> Read(ProtocolReader reader)
        {
            if (!reader.TryReadByte(out var codeByte))
                return Result.Error("Unexpected end of stream.");

            var code = (RequestStatusCode)codeByte;
            if (code == RequestStatusCode.Success)
                return Result.Success(RequestStatus.Success);

            if (!Enum.IsDefined(typeof(RequestStatusCode), code))
                return Result.Error("Unrecognized status code.");

            var messageResult = reader.ReadString();
            if (messageResult.IsError(out var errorReadingMessage))
                return Result.Error(errorReadingMessage);

            return Result.Success(RequestStatus.Error(code, messageResult.Value));
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)Code);

            if (Code != RequestStatusCode.Success)
                writer.Write(ErrorMessage);
        }

        public override string ToString()
        {
            return Code + (string.IsNullOrEmpty(ErrorMessage) ? null : $": {ErrorMessage}");
        }
    }
}
