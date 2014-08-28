using System;

namespace Deflector
{
    public static class MethodCallProviderRegistry
    {
        private static Func<object, Type, IMethodCallProvider> _resolver;
        private static readonly object _lock = new object();
        public static void SetResolver(Func<object, Type, IMethodCallProvider> resolver)
        {
            _resolver = resolver;
        }

        public static IMethodCallProvider GetProvider(object instance, Type declaringType)
        {
            lock (_lock)
            {
                if (_resolver != null)
                    return _resolver(instance, declaringType);
            }

            return null;
        }

    }
}