using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Deflector
{
    public class MethodBag : IMethodCallProvider
    {
        private readonly IDictionary<string, IList<MulticastDelegate>> _instanceMethods = new ConcurrentDictionary<string, IList<MulticastDelegate>>();
        private readonly IDictionary<string, IList<MulticastDelegate>> _staticMethods = new ConcurrentDictionary<string, IList<MulticastDelegate>>();
        private readonly IList<MulticastDelegate> _constructors = new List<MulticastDelegate>();
        private static readonly object _syncLock = new object();

        public void AddConstructor(MulticastDelegate implementation)
        {
            lock (_syncLock)
            {
                _constructors.Add(implementation);
            }
        }

        public void AddMethod(string methodName, MulticastDelegate implementation)
        {
            if (!_instanceMethods.ContainsKey(methodName))
                _instanceMethods[methodName] = new List<MulticastDelegate>();

            _instanceMethods[methodName].Add(implementation);
        }

        public void AddStaticMethod(string methodName, MulticastDelegate implementation)
        {
            if (!_staticMethods.ContainsKey(methodName))
                _staticMethods[methodName] = new List<MulticastDelegate>();

            _staticMethods[methodName].Add(implementation);
        }

        public void AddMethodCalls(object target, MethodBase hostMethod, IEnumerable<MethodBase> interceptedMethods, IDictionary<MethodBase, IMethodCall> methodCallMap,
            StackTrace stackTrace)
        {
            var calledMethods = GetInterceptedMethods(interceptedMethods).ToArray();

            MapInstanceMethods(methodCallMap, calledMethods);
            MapStaticMethods(methodCallMap, calledMethods);
            MapConstructors(methodCallMap, calledMethods);

            AddAdditionalMethodCalls(target, hostMethod, calledMethods, methodCallMap, stackTrace);
        }

        protected virtual void AddAdditionalMethodCalls(object target, MethodBase hostMethod,
            IEnumerable<MethodBase> interceptedMethods, IDictionary<MethodBase, IMethodCall> methodCallMap,
            StackTrace stackTrace)
        {
        }
        protected virtual IEnumerable<MethodBase> GetInterceptedMethods(IEnumerable<MethodBase> interceptedMethods)
        {
            return interceptedMethods;
        }

        private void MapConstructors(IDictionary<MethodBase, IMethodCall> methodCallMap, IEnumerable<MethodBase> mockedMethods)
        {
            var mockedConstructors = mockedMethods.Where(m => m.Name == ".ctor" && m.IsSpecialName);
            var constructorMap = new ConcurrentDictionary<string, IList<MulticastDelegate>>();
            constructorMap[".ctor"] = _constructors;
            MapMethodCalls(methodCallMap, new[] { ".ctor" }, mockedConstructors, constructorMap);
        }

        private void MapStaticMethods(IDictionary<MethodBase, IMethodCall> methodCallMap, MethodBase[] mockedMethods)
        {
            var mockedStaticMethodNames = mockedMethods.Where(m => _staticMethods.ContainsKey(m.Name)).Select(m => m.Name);
            var mockedStaticMethods = mockedMethods.Where(m => m.IsStatic).ToArray();
            MapMethodCalls(methodCallMap, mockedStaticMethodNames, mockedStaticMethods, _staticMethods);
        }

        private void MapInstanceMethods(IDictionary<MethodBase, IMethodCall> methodCallMap, MethodBase[] mockedMethods)
        {
            var mockedInstanceMethods = mockedMethods.Where(m => !m.IsStatic);
            var mockedInstanceMethodNames = mockedMethods.Where(m => _instanceMethods.ContainsKey(m.Name)).Select(m => m.Name);
            MapMethodCalls(methodCallMap, mockedInstanceMethodNames, mockedInstanceMethods, _instanceMethods);
        }

        private void MapMethodCalls(IDictionary<MethodBase, IMethodCall> methodCallMap, IEnumerable<string> mockedMethodNames,
            IEnumerable<MethodBase> mockedMethods, IDictionary<string, IList<MulticastDelegate>> methods)
        {
            var allMockedMethods = mockedMethods.ToArray();
            foreach (var methodName in mockedMethodNames)
            {
                var candidates = methods[methodName];
                var candidateMethods = candidates.Select(m => m.Method).ToArray();
                var delegateMap = candidates.ToDictionary(currentDelegate => (MethodBase)currentDelegate.Method);

                foreach (var currentMethod in allMockedMethods)
                {
                    var bestMatch = candidateMethods.GetBestMatch(currentMethod);
                    if (bestMatch == null || !delegateMap.ContainsKey(bestMatch))
                        continue;

                    var targetDelegate = delegateMap[bestMatch];
                    var methodCall = new DelegateMethodCall(targetDelegate);
                    methodCallMap[currentMethod] = methodCall;
                }
            }
        }
    }
}
