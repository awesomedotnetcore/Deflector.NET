namespace Deflector
{
    public interface IMethodCall
    {
        object Invoke(IInvocationInfo invocationInfo);
    }
}