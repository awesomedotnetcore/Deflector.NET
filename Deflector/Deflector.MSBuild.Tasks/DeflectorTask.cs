using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;

namespace Deflector.MSBuild.Tasks
{
    public class DeflectorTask : Task
    {
        [Required]
        public ITaskItem[] TargetFiles { get; set; }

        public override bool Execute()
        {
            bool result = false;

            foreach (var item in TargetFiles)
            {
                var targetFile = item.ItemSpec;
                result = Rewrite(targetFile, result);
                if (!result)
                    return false;
            }
            

            return result;
        }

        private bool Rewrite(string targetFile, bool result)
        {
            var outputFile = targetFile;

            try
            {
                Log.LogMessage(MessageImportance.Normal,
                    "{0}: Adding method call interception to assembly '{1}' (Output File: {2})", GetType().Name, targetFile,
                    outputFile);
                var assembly = AssemblyDefinition.ReadAssembly(targetFile);

                var emitter = new MethodCallInterceptionEmitter();
                emitter.Rewrite(assembly);

                var parameters = new WriterParameters() {WriteSymbols = true};
                assembly.Write(outputFile, parameters);

                result = true;
            }
            catch (Exception exception)
            {
                Log.LogError("Unknown error while trying to modify assembly '{0}'", targetFile);
                Log.LogErrorFromException(exception);
            }
            return result;
        }
    }
}
