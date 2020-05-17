#if NET20

namespace System
{
    // This would cause conflicts if it was public in the API assembly. This is an engine implementation detail since
    // even though it's public because this assembly should not be compiled against by anyone that references the
    // engine.
    public delegate TResult Func<TResult>();
    public delegate TResult Func<T, TResult>(T arg);
    public delegate void Action<T1, T2>(T1 arg1, T2 arg2);
}

#endif
