using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Deflector
{
    public class SingleMethodCallBinder : IMethodCallBinder
    {
        private readonly MulticastDelegate _implementation;
        private readonly MethodBase _targetMethod;

        public SingleMethodCallBinder(MethodBase targetMethod, MulticastDelegate implementation)
        {
            _targetMethod = targetMethod;
            _implementation = implementation;
        }

        public void AddMethodCalls(object target, MethodBase hostMethod, IEnumerable<MethodBase> interceptedMethods,
            IMethodCallMap methodCallMap,
            StackTrace stackTrace)
        {
            // Map the implementation to the most compatible method signature
            var bestMatch = interceptedMethods.GetBestMatch(_targetMethod);
            if (bestMatch == null)
                return;

            // Verify the delegate signature
            if (!bestMatch.HasCompatibleMethodSignatureWith(_implementation.Method))
                return;

            methodCallMap.Add(method => method == bestMatch, new DelegateMethodCall(_implementation));
        }
    }
}