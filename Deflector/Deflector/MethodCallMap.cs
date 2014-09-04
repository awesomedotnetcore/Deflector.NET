using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Deflector
{
    public class MethodCallMap : IMethodCallMap
    {
        private ConcurrentDictionary<MethodBase, IMethodCall> _callMap =
            new ConcurrentDictionary<MethodBase, IMethodCall>();
        public bool ContainsMappingFor(MethodBase method)
        {
            var keys = _callMap.Keys;
            var closestMatch = keys.GetBestMatch(method);
            
            return closestMatch != null;
        }

        public void Add(MethodBase method, IMethodCall methodCall)
        {
            _callMap[method] = methodCall;
        }

        public IMethodCall GetMethodCall(MethodBase method)
        {
            var keys = _callMap.Keys;
            var closestMatch = keys.GetBestMatch(method);

            return closestMatch != null ? _callMap[closestMatch] : null;
        }
    }
}
