Deflector.NET
=============

A library for intercepting all method calls at runtime in nearly any .NET application.

# Rationale

## Overview

Instead of having to change legacy code in order to make it easier to test, what if there was a way to test it "in place" without changing the source code at all?

As developers, we've all run into that nightmare scenario where we're given a 1000-line method with no unit tests and hundreds of external dependencies, and somehow, we have to make it work. Somehow, we have to make changes to that method, and isolate every one of its dependencies so that we can test all of the expected behaviour that we need to preserve:

~~~~
public void SomeMassiveLegacyMethodWithoutTests()
{
	// (500 lines of ugly legacy code here)
	SaveSomeData();
	// (499 more lines of even more ugly legacy code here)
}
~~~~

For example, if I have a method that makes a hardcoded database call in the form of a static method call, and that method was a thousand lines long, how would you:

- Test that the method is calling the database, given that it is a static method call.
- Mock out all the other external calls that the method is making, assuming that you only wanted to focus on that one database call.

Now, normally, I would refactor that one static method call into some sort of repository interface that I can swap out using a DI container at runtime:

~~~~
public void SomeMassiveLegacyMethodWithoutTests(IRepository repo)
{
	// (500 lines of ugly legacy code here)
	repo.SaveSomeData();
	// (499 more lines of even more ugly legacy code here)
}
~~~~

...and while refactoring it to use an interface is a sensible approach, it forces you to introduce interface dependencies in order to break some static, hardcoded dependencies. In effect, it forces you to swap one dependency for another, just to make the method's behaviour observable from your tests.

The real problem lies within the unknown state of that 1000-line method itself. How do you know that you haven't introduced an additional bug into that method by swapping out those dependencies?The more untested changes you introduce into that method (in order to test it in the first place), the higher the likelihood that you'll add more bugs to the method, which (in turn), you'll have to test again.

There has to be a way to "observe" a method's behaviour without having to permanently modify it. During testing, as developers, we often need to:

- Nullify/mock any side effects inside of a method so that it doesn't call actual external resources (such as a database, or a long-running process)
- Assert that certain behaviours have been observed, such as making the right calls to the right databases, or making the correct calculations, given a finite set of inputs

Now normally, making those kinds of changes would be impossible (if not difficult) using the C# language by itself. As it turns out, these changes are possible with a bit of IL rewriting (or assembly modification). IL modification allows you to leave the original C# code intact and introduce changes into the compiled assembly at the same time.

## A different testing approach
The idea behind Deflector is that you can leave your C# legacy code untouched, while Deflector adds hooks into your code in IL to make it observable when your tests run. The hooks that it introduces will only exist in-memory while those tests run. Once your legacy code goes into production, those same hooks will not be included in the production releases, leaving your original code unaffected by the testability changes that Deflector has made. 

It's the best of both worlds, and that's why I wrote Deflector.

### Changing the method calls instead of modifying entire third-party dependencies
In practice, it's easier to "fake" a method call inside a method than to have to modify your tests to accommodate a third-party dependency. For example, if you have a hardcoded call to a database or some other data source, you can effectively "trick" a method into thinking that it's calling the same data source if you swap that method call with a mock or a stub that returns a value that simulates the database call. Deflector makes all method calls inside of an assembly easily swappable, which makes removing third-party dependencies (such as databases) trivial to remove.

### "Everything is a mock" versus mock dependency injection
In a typical mocking scenario, most developers would replace a few hardcoded method calls with a call to a mocking framework of their choice:

~~~~
public class MyClass
{
	private IRepository _repo;

	// The IRepository interface can be mocked and injected here
	public MyClass(IRepository repo)
	{
		_repo = repo;
	}

	public void SaveData()
	{
		_repo.SaveSomeData();
	}
}
~~~~

In this case, the repository interface can be mocked using a typical mocking framework that records that the SaveSomeData method was called. In a real-world scenario, you'd have to do much more work to break those dependencies. In the real world, it's very common to be dealing with dozens of interwoven dependencies, and in many cases, it's impractical to change every one of those dependencies just so that you can break all of them and just use mocks.

Deflector flips this concept on its head and effectively makes every method call in your assembly mockable.

### In-place testing without modifying the original source code

The premise behind Deflector is that if you can replace every method call inside a method body/implementation at runtime, then you can replace every one of those method calls with any mock of your choice. It leaves the logical structure of each one of your methods intact. 

The hypothesis is that if you can: 

- Control all the inputs going into a method
- Control all the the external methods that it calls
- Change/replace the return values of all the functions that it calls (by replacing the functions that it calls)

...it gives you the ability to test any method in perfect isolation.

Deflector rewrites every method call in an assembly so that any one of those calls can be replaced at runtime. 

## The need to test any block of code, no matter how messy it is

At the same time, Deflector's approach works for methods of any arbitrary size. It doesn't matter how messy (or how large) your methods might be, since Deflector only cares about swapping all of your method calls with method call interceptors (or mocks) that replace the original method call.

At its very core, Deflector only cares about this one interface:

~~~~
public interface IMethodCall
{
    object Invoke(IInvocationInfo invocationInfo);
}
~~~~
*(_Note_: By default, if you do not specify that an *IMethodCall* instance should replace a method call, the instrumented method will call the original method)*
If you've ever worked with dynamic proxies, then the *IMethodCall* interface might seem familiar to you. That interface (by definition) is an interceptor interface, for which you can provide your own custom implementation. Deflector itself has lots of its own internal implementations for that interface that make it easy for you to pass in your own lambdas or delegates. Those lambdas/delegates, in turn, will be called in place of any method call that you specify.

For example, here is one test case that shows how Deflector can intercept constructor calls:
~~~~
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
~~~~

The *Replace* class is a helper/facade fluent class that converts your lambdas into *IMethodCall* instances that will be used to replace the method calls that you have chosen to replace. In this case, the *createList* lambda replaced the original call to the generic list constructor, which is why you might have noticed that the delegate returns a list. (As it turns out, a constructor call in the CLR is almost the same as calling a different method that returns the same type as the constructor.)

In addition to intercepting instance method calls, Deflector also supports intercepting static method calls:
~~~~
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
~~~~
Static method calls can be notoriously difficult to mock. In this case, Deflector replaced the static method call to *Console.WriteLine(text)* with a call to the *incrementCallCount* lambda/delegate. Deflector can replace any static method call, provided that you supply it with a lambda delegate with a compatible method signature and return type.

This can be useful if you have a codebase that relies heavily on static helper methods to perform much of the business logic. Deflector gives you a lot of flexibility, but that flexibility is not without its own tradeoffs.

# Limitations

## Not meant for direct production deployments
Needless to say, Deflector adds quite a bit of overhead to any assembly because of all the hooks it has to inject. In theory, you could use it to modify an existing assembly and then push those changes to production--_but I do not recommend it_.

### Built for testing, not performance
Deflector is built to make any legacy .NET assembly malleable for testing, but there is a sizable performance cost for having that level of flexibility. No benchmarks have been performed to date, and it doesn't have to be fast since it shouldn't be used outside of a testing environment.

### Please _do not_ deploy the instrumented assemblies to production unless you absolutely know what you're doing
I take no responsibility for any damages this tool might cause if you push it into a live production environment. With great power, comes great responsibility.

# Getting Started

## The repository
You can clone the repository [here on Github](https://github.com/philiplaureano/Deflector.NET). The solution is easy to build from Visual Studio 2017, and all the method call replacement examples can be found in [these tests](https://github.com/philiplaureano/Deflector.NET/blob/master/Deflector/Deflector.Tests/MethodCallInterceptionTests.cs).
## License
- Deflector is published under the MIT License, which means that it's completely open source. If you find it useful or have some pull requests, drop me a message on twitter and I'd love to hear from you.

## Prerequisites

- Deflector is built for traditional .NET Framework environments, for enterprise users running in a Windows environment. Why Windows? Well, that's where all the untested legacy code is ;)

### Supported Runtimes

- .NET Framework 4.7.2 is supported
- .NET Core and .NET Standard aren't yet supported, because Mono.Cecil v0.10.x doesn't support all the features that Deflector needs. A port [using dnlib](https://github.com/0xd4d/dnlib) is possible, but I'll only consider it if enough people actually ask for it.

# How you can help

The real reason why I'm publishing Deflector is that I want to see what other people do with it. Feel free to clone it, fork it, open a pull request, and test it out in the wild, and drop me a message if there's something interesting you've done with it, or send me a message if you found it useful.

Now go forth, and make awesome things ;)