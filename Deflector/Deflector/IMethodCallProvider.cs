using System.Reflection;

namespace Deflector
{
    public interface IMethodCallProvider
    {
        IMethodCall GetMethodCallFor(MethodBase method);
    }
}