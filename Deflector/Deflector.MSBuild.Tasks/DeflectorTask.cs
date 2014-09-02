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
        /// <summary>
        /// Gets or sets the value indicating the full path and filename of the target assembly.
        /// </summary>
        /// <value>The target assembly filename.</value>
        [Required]
        public string TargetFile { get; set; }

        /// <summary>
        /// Gets or sets the value indicating the full path and filename of the output assembly.
        /// </summary>
        /// <value>The output assembly filename.</value>
        /// <remarks>This field is optional; if blank, the default value will be the same value as the <see cref="TargetFile"/> property.</remarks>
        public string OutputFile { get; set; }

        public override bool Execute()
        {
            bool result = false;

            // The output file name will be the same as the target
            // file by default 
            var outputFile = OutputFile;
            if (string.IsNullOrEmpty(outputFile))
                outputFile = TargetFile;

            try
            {
                Log.LogMessage(MessageImportance.Normal, "{0}: Adding method call interception to assembly '{1}' (Output File: {2})", GetType().Name, TargetFile, OutputFile);
                var assembly = AssemblyDefinition.ReadAssembly(TargetFile);

                var emitter = new MethodCallInterceptionEmitter();
                emitter.Rewrite(assembly);

                var parameters = new WriterParameters() { WriteSymbols = true };
                assembly.Write(outputFile, parameters);

                result = true;
            }
            catch (Exception exception)
            {
                Log.LogError("Unknown error while trying to modify assembly '{0}'", TargetFile);
                Log.LogErrorFromException(exception);
            }

            return result;
        }
    }
}
