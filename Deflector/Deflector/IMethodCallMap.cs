using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;

namespace Deflector
{
    public interface IMethodCallMap
    {
        bool ContainsMappingFor(MethodBase method);
        void Add(MethodBase method, IMethodCall methodCall);
        IMethodCall GetMethodCall(MethodBase method);
    }
}