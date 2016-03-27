using System.Collections.Concurrent;

namespace Deflector
{
    public static class MethodCallBinderRegistry
    {
        private static readonly ConcurrentQueue<IMethodCallBinder> _providers = new ConcurrentQueue<IMethodCallBinder>();

        public static void AddProvider(IMethodCallBinder methodCallBinder)
        {
            _providers.Enqueue(methodCallBinder);
        }

        public static void Clear()
        {
            while (!_providers.IsEmpty)
            {
                IMethodCallBinder result;
                _providers.TryDequeue(out result);
            }
        }

        public static IMethodCallBinder GetProvider()
        {
            return new CompositeMethodCallBinder(_providers);
        }
    }
}