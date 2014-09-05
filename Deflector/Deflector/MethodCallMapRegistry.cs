using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Deflector
{
    public static class MethodCallMapRegistry
    {
        private static readonly ConcurrentDictionary<MethodBase, IMethodCallMap> _entries = new ConcurrentDictionary<MethodBase, IMethodCallMap>();

        public static IMethodCallMap GetMap(MethodBase method)
        {
            // Reuse the existing map
            if (_entries.ContainsKey(method))
                return _entries[method];

            var newMap = new MethodCallMap();
            _entries[method] = newMap;

            return newMap;
        }

        public static void Store(MethodBase method, IMethodCallMap map)
        {
            _entries[method] = map;
        }

        public static bool ContainsMapFor(MethodBase method)
        {
            return _entries.ContainsKey(method);
        }
    }
}
