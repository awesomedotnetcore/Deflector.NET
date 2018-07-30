using Mono.Cecil;
using SampleLibrary;
using Xunit;

namespace Deflector.Tests
{
    public class AssemblyVerificationTests : BaseAssemblyVerificationTestFixture
    {
        [Fact]
        public void Should_emit_valid_assembly()
        {
            var assemblyLocation = typeof (SampleClassWithInstanceMethod).Assembly.Location;
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyLocation);
            var emitter = new MethodCallInterceptionEmitter();
            emitter.Rewrite(assemblyDefinition);

            var outputFile = "output.dll";
            assemblyDefinition.Write(outputFile);

            PEVerify(outputFile);
        }
    }
}