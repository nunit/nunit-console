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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace NUnit.Engine
{
    [DebuggerDisplay("{ToString(),nq}")]
    public struct Result : IEquatable<Result>
    {
        private readonly string errorMessage;

        private Result(string errorMessage)
        {
            this.errorMessage = errorMessage;
        }

        public bool IsSuccess => errorMessage is null;

        public bool IsError(out string message)
        {
            message = errorMessage;
            return message != null;
        }

        public static Result Success() => default(Result);

        public static ErrorResult Error(string message)
        {
            return new ErrorResult(message);
        }

        public static Result<T> Success<T>(T value) => Result<T>.Success(value);

        public override bool Equals(object obj)
        {
            return obj is Result result && Equals(result);
        }

        public bool Equals(Result other)
        {
            return errorMessage == other.errorMessage;
        }

        public override int GetHashCode()
        {
            return -341426040 + EqualityComparer<string>.Default.GetHashCode(errorMessage);
        }

        public static bool operator ==(Result left, Result right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Result left, Result right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return errorMessage is null
                ? "Success"
                : $"Error({errorMessage})";
        }

        public static implicit operator Result(ErrorResult r) => new Result(r.ErrorMessage);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct ErrorResult
        {
            public string ErrorMessage { get; }

            public ErrorResult(string errorMessage)
            {
                if (string.IsNullOrEmpty(errorMessage))
                    throw new ArgumentException("An error message must be specified.", nameof(errorMessage));

                ErrorMessage = errorMessage;
            }
        }
    }
}
