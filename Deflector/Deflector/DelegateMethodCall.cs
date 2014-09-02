using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Deflector
{
    public class DelegateMethodCall : IMethodCall
    {
        private readonly MulticastDelegate _targetDelegate;

        public DelegateMethodCall(MulticastDelegate targetDelegate)
        {
            _targetDelegate = targetDelegate;
        }

        public object Invoke(IInvocationInfo invocationInfo)
        {
            var arguments = invocationInfo.Arguments;
            return _targetDelegate.DynamicInvoke(arguments);
        }
    }
}
