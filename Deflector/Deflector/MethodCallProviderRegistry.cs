using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Deflector
{
    public static class MethodCallProviderRegistry
    {
        private static readonly List<IMethodCallProvider> _providers = new List<IMethodCallProvider>();
        private static readonly object _lock = new object();

        public static void AddProvider(IMethodCallProvider methodCallProvider)
        {
            lock (_lock)
            {
                _providers.Add(methodCallProvider);    
            }            
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _providers.Clear();
            }
        }
        public static IMethodCallProvider GetProvider()
        {
            lock (_lock)
            {
                return new CompositeMethodCallProvider(_providers);
            }
        }

    }
}