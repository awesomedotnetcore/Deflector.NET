using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;
using NUnit.Framework;
using SampleLibrary;

namespace Deflector.Tests
{
    [TestFixture]
    public class AssemblyVerificationTests : BaseAssemblyVerificationTestFixture
    {
        [Test]
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
