using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Deflector
{
    public class MethodCallMap : IMethodCallMap
    {
        private readonly ConcurrentDictionary<Func<MethodBase, bool>, IMethodCall> _callMap =
            new ConcurrentDictionary<Func<MethodBase, bool>, IMethodCall>();

        public bool ContainsMappingFor(MethodBase method)
        {
            var keys = _callMap.Keys;
            var result = keys.Any(filter => filter(method));

            return result;
        }

        public void Add(Func<MethodBase, bool> methodFilter, IMethodCall methodCall)
        {
            _callMap[methodFilter] = methodCall;
        }

        public IMethodCall GetMethodCall(MethodBase method)
        {
            var keys = _callMap.Keys;
            var closestMatch = keys.First(filter => filter(method));

            return closestMatch != null ? _callMap[closestMatch] : null;
        }
    }
}