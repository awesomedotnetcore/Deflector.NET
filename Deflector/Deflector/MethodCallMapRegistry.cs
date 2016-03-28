using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Deflector
{
    public static class MethodCallMapRegistry
    {
        private static readonly ConcurrentBag<IMethodCallMap> _entries = new ConcurrentBag<IMethodCallMap>();

        public static IMethodCallMap GetMap(MethodBase method)
        {
            // Reuse the existing map
            var existingMap= _entries.FirstOrDefault(m => m.ContainsMappingFor(method));
            if (existingMap != null)
                return existingMap;

            var newMap = new MethodCallMap();
            Store(newMap);

            return newMap;
        }

        public static void Store(IMethodCallMap map)
        {
            _entries.Add(map);
        }

        public static bool ContainsMapFor(MethodBase method)
        {
            return _entries.Any(m => m.ContainsMappingFor(method));
        }
    }
}