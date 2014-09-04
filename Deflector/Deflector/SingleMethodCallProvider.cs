using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Deflector
{
    public class SingleMethodCallProvider : IMethodCallProvider
    {
        private readonly MethodBase _targetMethod;
        private readonly MulticastDelegate _implementation;

        public SingleMethodCallProvider(MethodBase targetMethod, MulticastDelegate implementation)
        {
            _targetMethod = targetMethod;
            _implementation = implementation;
        }

        public void AddMethodCalls(object target, MethodBase hostMethod, IEnumerable<MethodBase> interceptedMethods, IDictionary<MethodBase, IMethodCall> methodCallMap,
            StackTrace stackTrace)
        {
            // Map the implementation to the most compatible method signature
            var bestMatch = interceptedMethods.GetBestMatch(_targetMethod);
            if (bestMatch == null)
                return;

            // Verify the delegate signature
            if (!bestMatch.HasCompatibleMethodSignatureWith(_implementation.Method))
                return;

            methodCallMap[bestMatch] = new DelegateMethodCall(_implementation);
        }
    }
}
