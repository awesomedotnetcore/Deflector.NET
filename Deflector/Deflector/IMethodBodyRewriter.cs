using Mono.Cecil;

namespace Deflector
{
    /// <summary>
    ///     Represents a type that can modify method bodies.
    /// </summary>
    public interface IMethodBodyRewriter
    {
        /// <summary>
        ///     Imports references into the target <see cref="ModuleDefinition" /> instance.
        /// </summary>
        /// <param name="module">The module that will hold the modified item.</param>
        void ImportReferences(ModuleDefinition module);

        /// <summary>
        ///     Adds local variables to the <paramref name="hostMethod" />.
        /// </summary>
        /// <param name="hostMethod">The target method.</param>
        void AddLocals(MethodDefinition hostMethod);

        /// <summary>
        ///     Rewrites a target method using the given CilWorker.
        /// </summary>
        /// <param name="method">The host method.</param>
        /// <param name="module">The module that contains the host method.</param>
        void Rewrite(MethodDefinition method, ModuleDefinition module);
    }
}