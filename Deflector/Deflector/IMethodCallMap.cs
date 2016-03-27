using System;
using System.Reflection;

namespace Deflector
{
    public interface IMethodCallMap
    {
        bool ContainsMappingFor(MethodBase method);
        void Add(Func<MethodBase, bool> methodFilter, IMethodCall methodCall);
        IMethodCall GetMethodCall(MethodBase method);
    }
}