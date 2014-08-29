using System;
using System.Diagnostics;

namespace Deflector
{
    public static class MethodCallProviderRegistry
    {
        private static IMethodCallProvider _methodCallProvider;
        private static readonly object _lock = new object();

        public static void SetProvider(IMethodCallProvider provider)
        {
            lock (_lock)
            {
                _methodCallProvider = provider;
            }
        }
        public static IMethodCallProvider GetProvider()
        {
            lock (_lock)
            {
                return _methodCallProvider;
            }
        }

    }
}