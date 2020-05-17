#if NET20

namespace System
{
    // This would cause conflicts if it was public in the API assembly. This is an engine implementation detail since
    // even though it's public because this assembly should not be compiled against by anyone that references the
    // engine.
    public delegate TResult Func<TResult>();
}

#endif
