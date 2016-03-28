using System.Reflection;
using Mono.Cecil;

namespace Deflector
{
    public static class MethodCallInterceptionExtensions
    {
        public static Assembly AddInterceptionHooks(this Assembly assembly)
        {
            var assemblyLocation = assembly.Location;
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyLocation);
            var modifiedAssembly = assemblyDefinition.AddInterceptionHooks();

            return modifiedAssembly.ToAssembly();
        }

        public static AssemblyDefinition AddInterceptionHooks(this AssemblyDefinition assemblyDefinition)
        {
            var emitter = new MethodCallInterceptionEmitter();
            emitter.Rewrite(assemblyDefinition);

            return assemblyDefinition;
        }
    }
}