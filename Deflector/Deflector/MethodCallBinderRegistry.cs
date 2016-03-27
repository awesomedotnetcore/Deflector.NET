using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Deflector
{
    public static class MethodCallBinderRegistry
    {
        private static readonly List<IMethodCallBinder> _providers = new List<IMethodCallBinder>();
        private static readonly object _lock = new object();

        public static void AddProvider(IMethodCallBinder methodCallBinder)
        {
            lock (_lock)
            {
                _providers.Add(methodCallBinder);    
            }            
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _providers.Clear();
            }
        }
        public static IMethodCallBinder GetProvider()
        {
            lock (_lock)
            {
                return new CompositeMethodCallBinder(_providers);
            }
        }

    }
}