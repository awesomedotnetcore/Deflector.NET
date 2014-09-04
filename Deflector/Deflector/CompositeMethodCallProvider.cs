using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Deflector
{
    public class CompositeMethodCallProvider : IMethodCallProvider
    {
        private readonly IEnumerable<IMethodCallProvider> _methodCallProviders;

        public CompositeMethodCallProvider(params IMethodCallProvider[] methodCallProviders)
            : this((IEnumerable<IMethodCallProvider>)methodCallProviders)
        {
        }

        public CompositeMethodCallProvider(IEnumerable<IMethodCallProvider> methodCallProviders)
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
