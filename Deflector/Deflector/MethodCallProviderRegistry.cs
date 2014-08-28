using System;
using System.Diagnostics;

namespace Deflector
{
    public static class MethodCallProviderRegistry
    {
        private static Func<object, Type, StackTrace, IMethodCallProvider> _resolver;
        private static readonly object _lock = new object();
        public static void SetResolver(Func<object, Type, StackTrace, IMethodCallProvider> resolver)
        {
            _resolver = resolver;
        }

        public static IMethodCallProvider GetProvider(object instance, Type declaringType, StackTrace stackTrace)
        {
            lock (_lock)
            {
                if (_resolver != null)
                    return _resolver(instance, declaringType, stackTrace);
            }

            return null;
        }

    }
}