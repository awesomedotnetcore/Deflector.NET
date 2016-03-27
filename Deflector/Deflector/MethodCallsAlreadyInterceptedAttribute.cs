using System;

namespace Deflector
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class MethodCallsAlreadyInterceptedAttribute : Attribute
    {
    }
}