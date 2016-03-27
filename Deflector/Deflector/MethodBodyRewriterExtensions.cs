using System;
using System.Linq;
using Mono.Cecil;

namespace Deflector
{
    public static class MethodBodyRewriterExtensions
    {
        public static void Rewrite(this IMethodBodyRewriter rewriter, AssemblyDefinition assembly,
            Func<MethodDefinition, bool> methodFilter = null)
        {
            var mainModule = assembly.MainModule;

            var allTypes = mainModule.Types.Where(t => t.Name != "<Module>");
            var allClasses = allTypes.Where(t => t.IsClass && !t.IsInterface).ToArray();

            var allMethods = allClasses.SelectMany(c => c.Methods)
                .Where(m => m.HasBody && !m.IsAbstract && m.Name != ".cctor");

            if (methodFilter != null)
                allMethods = allMethods.Where(methodFilter);

            rewriter.ImportReferences(mainModule);

            foreach (var method in allMethods)
            {
                rewriter.AddLocals(method);
                rewriter.Rewrite(method, mainModule);
            }
        }
    }
}