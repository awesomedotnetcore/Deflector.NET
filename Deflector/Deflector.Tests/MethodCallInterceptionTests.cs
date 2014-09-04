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
            var assemblyDefinition = RewriteAssemblyOf<SampleClassWithInstanceMethod>();

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

        [Test]
        public void Should_intercept_instance_method()
        {
            var assemblyDefinition = RewriteAssemblyOf<SampleClassThatCallsAnInstanceMethod>();

            var callCount = 0;
            Action callCounter = () => callCount++;

            Replace.Method((SampleClassThatCallsAnInstanceMethod c) => c.DoSomethingElse()).With(callCounter);

            var assembly = assemblyDefinition.ToAssembly();
            var targetType = assembly.GetTypes().First(t => t.Name == "SampleClassThatCallsAnInstanceMethod");
            dynamic instance = Activator.CreateInstance(targetType);
            instance.DoSomething();
            Assert.AreEqual(1, callCount);
        }

        private AssemblyDefinition RewriteAssemblyOf<T>()
        {
            var assemblyLocation = typeof(T).Assembly.Location;
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyLocation);
            var emitter = new MethodCallInterceptionEmitter();
            emitter.Rewrite(assemblyDefinition);

            return assemblyDefinition;
        }
    }
}
