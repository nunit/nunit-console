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
using System.Diagnostics;

namespace NUnit.Engine
{
    [DebuggerDisplay("{ToString(),nq}")]
    public struct Result<T> : IEquatable<Result<T>>
    {
        private readonly T value;
        private readonly string errorMessage;

        private Result(T value, string errorMessage)
        {
            this.value = value;
            this.errorMessage = errorMessage;
        }

        public static Result<T> Success(T value)
        {
            return new Result<T>(value, errorMessage: null);
        }

        public static Result<T> Error(string message)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Error message must be specified.", nameof(message));

            return new Result<T>(value: default(T), errorMessage: message);
        }

        public bool IsSuccess(out T value)
        {
            value = this.value;
            return errorMessage is null;
        }

        public bool IsError(out string message)
        {
            message = errorMessage;
            return message != null;
        }

        public override bool Equals(object obj)
        {
            return obj is Result<T> result && Equals(result);
        }

        public bool Equals(Result<T> other)
        {
            return EqualityComparer<T>.Default.Equals(value, other.value) &&
                   errorMessage == other.errorMessage;
        }

        public override int GetHashCode()
        {
            var hashCode = 700220066;
            hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(value);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(errorMessage);
            return hashCode;
        }

        public T Value
        {
            get
            {
                if (errorMessage != null) throw new InvalidOperationException("The result is not success.");
                return value;
            }
        }

        public static implicit operator Result<T>(Result.ErrorResult error) => Error(error.ErrorMessage);

        public override string ToString()
        {
            return errorMessage is null
                ? $"Success({value})"
                : $"Error({errorMessage})";
        }

        public Result<TResult> Select<TResult>(Func<T, TResult> selector)
        {
            return errorMessage is null
                ? Result.Success(selector.Invoke(value))
                : Result.Error(errorMessage);
        }
    }
}
