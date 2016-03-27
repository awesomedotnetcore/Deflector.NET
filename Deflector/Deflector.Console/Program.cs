using System.IO;
using System.Text;
using CommandLine;
using Mono.Cecil;

namespace Deflector.Console
{
    internal class CommandLineOptions
    {
        [Option('i', "input", Required = true, HelpText = "The input assembly that will be modified.")]
        public string InputFile { get; set; }

        [Option('o', "output", Required = false,
            HelpText =
                "The path of the resulting output assembly. The default output assembly file name will be the same input assembly if this option is not specified."
            )]
        public string OutputFile { get; set; }

        [Option('v', null, HelpText = "Determines whether or not the output should be verbose.")]
        public bool Verbose { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            // this without using CommandLine.Text
            //  or using HelpText.AutoBuild
            var usage = new StringBuilder();
            usage.AppendLine("Deflector Console v1.0");
            usage.AppendLine("Usage: Deflector.Console -i [target assembly] -o [output assembly]");
            return usage.ToString();
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            var options = new CommandLineOptions();
            if (!Parser.Default.ParseArguments(args, options))
                return;

            var targetFile = options.InputFile;
            System.Console.WriteLine("Reading assembly '{0}'", Path.GetFullPath(targetFile));

            var assembly = AssemblyDefinition.ReadAssembly(targetFile);

            // Intercept all method calls
            System.Console.WriteLine("Modifying assembly '{0}", Path.GetFullPath(targetFile));
            var emitter = new MethodCallInterceptionEmitter();
            emitter.Rewrite(assembly);

            // Save the results
            var outputFile = options.OutputFile ?? targetFile;
            var parameters = new WriterParameters {WriteSymbols = true};
            assembly.Write(outputFile, parameters);
        }
    }
}