using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using NUnit.Framework;
using SampleLibrary;

namespace Deflector.Tests
{
    [TestFixture]
    public class MethodCallInterceptionTests : BaseAssemblyVerificationTestFixture
    {
        private AssemblyDefinition RewriteAssemblyOf<T>()
        {
            var assemblyLocation = typeof (T).Assembly.Location;
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyLocation);
            var emitter = new MethodCallInterceptionEmitter();
            emitter.Rewrite(assemblyDefinition);

            return assemblyDefinition;
        }

        private static void TestModifiedType(AssemblyDefinition assemblyDefinition, string typeName, ref int callCount)
        {
            var assembly = assemblyDefinition.ToAssembly();
            var targetType = assembly.GetTypes().First(t => t.Name == typeName);
            dynamic instance = Activator.CreateInstance(targetType);
            instance.DoSomething();
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Should_intercept_constructor_call()
        {
            var assemblyDefinition = RewriteAssemblyOf<SampleClassWithConstructorCall>();

            var callCount = 0;
            Func<List<int>> createList = () =>
            {
                callCount++;
                return new List<int>();
            };

            Replace.ConstructorCallOn<List<int>>().With(createList);

            var typeName = "SampleClassWithConstructorCall";
            TestModifiedType(assemblyDefinition, typeName, ref callCount);
        }

        [Test]
        public void Should_intercept_instance_method()
        {
            var assemblyDefinition = RewriteAssemblyOf<SampleClassThatCallsAnInstanceMethod>();

            var callCount = 0;
            Action callCounter = () => callCount++;

            Replace.Method((SampleClassThatCallsAnInstanceMethod c) => c.DoSomethingElse()).With(callCounter);

            var typeName = "SampleClassThatCallsAnInstanceMethod";
            TestModifiedType(assemblyDefinition, typeName, ref callCount);
        }

        [Test]
        public void Should_intercept_property_getter()
        {
            var callCount = 0;

            Func<int> getterMethod = () =>
            {
                callCount++;
                return 42;
            };

            Replace.Property((SampleClassWithProperties c) => c.Value).WithGetter(getterMethod);

            var typeName = "SampleClassThatCallsAProperty";
            var assemblyDefinition = RewriteAssemblyOf<SampleClassWithProperties>();
            TestModifiedType(assemblyDefinition, typeName, ref callCount);
        }

        [Test]
        public void Should_intercept_property_setter()
        {
            var callCount = 0;

            Action<int> setterMethod = value =>
            {
                callCount++;

                // Match the setter value
                Assert.AreEqual(42, value);
            };

            Replace.Property((SampleClassWithProperties c) => c.Value).WithSetter(setterMethod);

            var assemblyDefinition = RewriteAssemblyOf<SampleClassWithProperties>();
            var typeName = "SampleClassThatCallsAProperty";
            TestModifiedType(assemblyDefinition, typeName, ref callCount);
        }

        [Test]
        public void Should_intercept_static_method()
        {
            var assemblyDefinition = RewriteAssemblyOf<SampleClassWithInstanceMethod>();

            var callCount = 0;
            Action<string> incrementCallCount = text =>
            {
                callCount++;

                // Match the parameters passed to the Console.WriteLine() call
                Assert.AreEqual("Hello, World!", text);
            };

            Replace.Method(() => Console.WriteLine("")).With(incrementCallCount);

            var assembly = assemblyDefinition.ToAssembly();
            var targetType = assembly.GetTypes().First(t => t.Name == "SampleClassWithInstanceMethod");

            var targetMethod = targetType.GetMethods().First(m => m.IsStatic && m.Name == "DoSomething");
            targetMethod.Invoke(null, new object[0]);

            Assert.AreEqual(1, callCount);
        }
    }
}