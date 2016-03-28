using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Deflector
{
    public class MethodCallBinder : IMethodCallBinder
    {
        private readonly Func<MethodBase, bool> _methodFilter;
        private readonly IMethodCall _methodCall;

        public MethodCallBinder(Func<MethodBase, bool> methodFilter, IMethodCall methodCall)
        {
            _methodFilter = methodFilter;
            _methodCall = methodCall;
        }

        public void AddMethodCalls(object target, MethodBase hostMethod, IEnumerable<MethodBase> interceptedMethods, IMethodCallMap methodCallMap,
            StackTrace stackTrace)
        {
            var targetMethods = interceptedMethods.Where(method => _methodFilter(method)).ToArray();
            foreach (var method in targetMethods)
            {
                methodCallMap.Add(_methodFilter, _methodCall);
            }
        }
    }
}