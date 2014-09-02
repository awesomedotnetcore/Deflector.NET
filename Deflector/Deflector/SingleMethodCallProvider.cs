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
            if (!interceptedMethods.Contains(_targetMethod))
                return;

            methodCallMap[_targetMethod] = new DelegateMethodCall(_implementation);
        }
    }
}
