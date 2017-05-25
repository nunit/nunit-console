using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace NUnit.Engine.Tests.Helpers
{
    public static class StackEnumerator
    {
        public static StackEnumerator<T> Create<T>(params T[] initial) => new StackEnumerator<T>(initial);
        public static StackEnumerator<T> Create<T>(IEnumerable<T> initial) => new StackEnumerator<T>(initial);
        public static StackEnumerator<T> Create<T>(IEnumerator<T> initial) => new StackEnumerator<T>(initial);
    }

    public sealed class StackEnumerator<T> : IDisposable
    {
        private readonly Stack<IEnumerator<T>> stack = new Stack<IEnumerator<T>>();
        private IEnumerator<T> current;

        public bool MoveNext()
        {
            while (!current.MoveNext())
            {
                current.Dispose();
                if (stack.Count == 0) return false;
                current = stack.Pop();
            }

            return true;
        }

        public T Current => current.Current;

        public void Recurse(IEnumerator<T> newCurrent)
        {
            if (newCurrent == null) return;
            stack.Push(current);
            current = newCurrent;
        }
        public void Recurse(IEnumerable<T> newCurrent)
        {
            if (newCurrent == null) return;
            Recurse(newCurrent.GetEnumerator());
        }
        public void Recurse(params T[] newCurrent)
        {
            Recurse((IEnumerable<T>)newCurrent);
        }

        public StackEnumerator(IEnumerator<T> initial)
        {
            current = initial ?? Enumerable.Empty<T>().GetEnumerator();
        }
        public StackEnumerator(IEnumerable<T> initial) : this(initial?.GetEnumerator())
        {
        }
        public StackEnumerator(params T[] initial) : this((IEnumerable<T>)initial)
        {
        }

        // Foreach support
        [EditorBrowsable(EditorBrowsableState.Never)]
        public StackEnumerator<T> GetEnumerator()
        {
            return this;
        }

        public void Dispose()
        {
            current.Dispose();
            foreach (var item in stack)
                item.Dispose();
            stack.Clear();
        }
    }
}
