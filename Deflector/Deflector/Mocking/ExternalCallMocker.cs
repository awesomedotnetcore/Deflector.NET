using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Deflector.Mocking
{
    public class ExternalCallMocker : MethodBag
    {
        private readonly Type _targetTypeUnderTest;

        public ExternalCallMocker(Type targetTypeUnderTest)
        {
            _targetTypeUnderTest = targetTypeUnderTest;
        }

        protected override void AddAdditionalMethodCalls(object target, MethodBase hostMethod, IEnumerable<MethodBase> interceptedMethods,
            IDictionary<MethodBase, IMethodCall> methodCallMap, StackTrace stackTrace)
        {
            // Verify that all methods have been mocked
            var calledMethods = GetInterceptedMethods(interceptedMethods);
            foreach (var calledMethod in calledMethods)
            {
                if (methodCallMap.ContainsKey(calledMethod) && methodCallMap[calledMethod] != null)
                    continue;

                var declaringType = calledMethod.DeclaringType;
                var typeName = declaringType != null ? declaringType.FullName : "(Unknown Type)";
                throw new NotImplementedException(string.Format("Method '{0}.{1}' is missing a mock implementation", typeName, calledMethod.Name));
            }
        }

        protected override IEnumerable<MethodBase> GetInterceptedMethods(IEnumerable<MethodBase> interceptedMethods)
        {
            return interceptedMethods.Where(m => m.DeclaringType != _targetTypeUnderTest && m.DeclaringType != typeof(object));
        }
    }
}
