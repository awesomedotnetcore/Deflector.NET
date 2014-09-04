using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Mono.Cecil;
using NUnit.Framework;
using SampleLibrary;

namespace Deflector.Tests
{
    [TestFixture]
    public class MethodCallInterceptionTests : BaseAssemblyVerificationTestFixture
    {
        [Test]
        public void Should_intercept_static_method()
        {
            var assemblyLocation = typeof(SampleClassWithInstanceMethod).Assembly.Location;
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyLocation);
            var emitter = new MethodCallInterceptionEmitter();
            emitter.Rewrite(assemblyDefinition);

            var callCount = 0;
            Action<string> incrementCallCount = text =>
            {
                callCount++;
            };

            Replace.Method(() => Console.WriteLine("")).With(incrementCallCount);

            var assembly = assemblyDefinition.ToAssembly();
            var targetType = assembly.GetTypes().First(t => t.Name == "SampleClassWithInstanceMethod");

            var targetMethod = targetType.GetMethods().First(m=>m.IsStatic && m.Name == "DoSomething");
            targetMethod.Invoke(null, new object[0]);

            Assert.AreEqual(1, callCount);
        }
    }
}
