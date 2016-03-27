using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Deflector
{
    public class CompositeMethodCallBinder : IMethodCallBinder
    {
        private readonly IEnumerable<IMethodCallBinder> _methodCallProviders;

        public CompositeMethodCallBinder(params IMethodCallBinder[] methodCallBinders)
            : this((IEnumerable<IMethodCallBinder>)methodCallBinders)
        {
        }

        public CompositeMethodCallBinder(IEnumerable<IMethodCallBinder> methodCallProviders)
        {
            _methodCallProviders = methodCallProviders;
        }

        public void AddMethodCalls(object target, MethodBase hostMethod, IEnumerable<MethodBase> interceptedMethods, IMethodCallMap methodCallMap,
            StackTrace stackTrace)
        {
            foreach (var provider in _methodCallProviders)
            {
                provider.AddMethodCalls(target, hostMethod, interceptedMethods, methodCallMap, stackTrace);
            }
        }
    }
}
