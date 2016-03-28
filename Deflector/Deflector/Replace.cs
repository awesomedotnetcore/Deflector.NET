using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Deflector
{
    public static class Replace
    {
        public static Func<Tuple<IEnumerable<MethodBase>, T>> ConstructorCallOn<T>()
        {
            return () =>
            {
                var constructors =
                    typeof(T).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                return new Tuple<IEnumerable<MethodBase>, T>(constructors, default(T));
            };
        }

        public static void With<T1, T2, T3, T4, T5, T6, T7, TResult>(
            this Func<Tuple<IEnumerable<MethodBase>, TResult>> getConstructorData,
            Func<T1, T2, T3, T4, T5, T6, T7, TResult> implementation)
        {
            WithConstructorImpl(getConstructorData, implementation);
        }

        public static void With<T1, T2, T3, T4, T5, T6, TResult>(
            this Func<Tuple<IEnumerable<MethodBase>, TResult>> getConstructorData,
            Func<T1, T2, T3, T4, T5, T6, TResult> implementation)
        {
            WithConstructorImpl(getConstructorData, implementation);
        }

        public static void With<T1, T2, T3, T4, T5, TResult>(
            this Func<Tuple<IEnumerable<MethodBase>, TResult>> getConstructorData,
            Func<T1, T2, T3, T4, T5, TResult> implementation)
        {
            WithConstructorImpl(getConstructorData, implementation);
        }

        public static void With<T1, T2, T3, T4, TResult>(
            this Func<Tuple<IEnumerable<MethodBase>, TResult>> getConstructorData,
            Func<T1, T2, T3, T4, TResult> implementation)
        {
            WithConstructorImpl(getConstructorData, implementation);
        }

        public static void With<T1, T2, T3, TResult>(
            this Func<Tuple<IEnumerable<MethodBase>, TResult>> getConstructorData,
            Func<T1, T2, T3, TResult> implementation)
        {
            WithConstructorImpl(getConstructorData, implementation);
        }

        public static void With<T1, T2, TResult>(
            this Func<Tuple<IEnumerable<MethodBase>, TResult>> getConstructorData,
            Func<T1, T2, TResult> implementation)
        {
            WithConstructorImpl(getConstructorData, implementation);
        }

        public static void With<T1, TResult>(
            this Func<Tuple<IEnumerable<MethodBase>, TResult>> getConstructorData,
            Func<T1, TResult> implementation)
        {
            WithConstructorImpl(getConstructorData, implementation);
        }

        public static void With<TResult>(this Func<Tuple<IEnumerable<MethodBase>, TResult>> getConstructorData,
            Func<TResult> implementation)
        {
            WithConstructorImpl(getConstructorData, implementation);
        }

        private static void WithConstructorImpl<TResult>(
            Func<Tuple<IEnumerable<MethodBase>, TResult>> getConstructorData,
            MulticastDelegate implementation)
        {
            var constructorData = getConstructorData();
            var constructors = constructorData.Item1;
            var implementationMethod = implementation.Method;
            var matchingConstructor = constructors.GetBestMatch(implementationMethod);
            if (matchingConstructor == null)
                throw new InvalidOperationException(
                    string.Format(
                        "Unable to find a compatible constructor from type '{0}' that matches the current implementation delegate.",
                        typeof(TResult).FullName));

            AddMethodCall(matchingConstructor, implementation);
        }

        public static Func<Tuple<PropertyInfo, T>> Property<TObject, T>(Expression<Func<TObject, T>> p)
        {
            return () =>
            {
                var propertyInfo = GetPropertyImpl(p);
                return Tuple.Create(propertyInfo, default(T));
            };
        }

        public static void With<T>(this Func<Tuple<PropertyInfo, T>> getPropertyData, Func<T> getterImplementation,
            Action<T> setterImplementation)
        {
            getPropertyData.WithGetter(getterImplementation);
            getPropertyData.WithSetter(setterImplementation);
        }

        public static void WithSetter<T>(this Func<Tuple<PropertyInfo, T>> getPropertyData,
            Action<T> setterImplementation)
        {
            var metaData = getPropertyData();
            var propertyInfo = metaData.Item1;
            var targetMethod = propertyInfo.GetSetMethod();
            if (targetMethod == null)
                throw new InvalidOperationException($"The property '{propertyInfo.Name}' has no setter method.");

            AddMethodCall(targetMethod, setterImplementation);
        }

        public static void WithGetter<T>(this Func<Tuple<PropertyInfo, T>> getPropertyData, Func<T> getterImplementation)
        {
            var metaData = getPropertyData();
            var propertyInfo = metaData.Item1;
            var targetMethod = propertyInfo.GetGetMethod();
            if (targetMethod == null)
                throw new InvalidOperationException($"The property '{propertyInfo.Name}' has no getter method.");

            AddMethodCall(targetMethod, getterImplementation);
        }

        public static Func<MethodBase> Method<TObject>(Expression<Action<TObject>> expression)
        {
            return () => GetMethodByCall(expression);
        }

        public static Func<MethodBase> Method(Expression<Action> expression)
        {
            return () => GetMethodByCall(expression);
        }

        public static Func<MethodBase> Method<TObject, T>(Expression<Func<TObject, Action<T>>> expression)
        {
            return () => GetMethodImpl(expression);
        }

        public static Func<MethodBase> Method<T>(Expression<Func<Action<T>>> expression)
        {
            return () => GetMethodImpl(expression);
        }

        public static Func<MethodBase, bool> Methods(Func<MethodBase, bool> filter)
        {
            return filter;
        }

        public static Func<MethodBase, bool> Methods<T>(Func<MethodBase, bool> filter)
        {
            return method => filter(method) && method.DeclaringType == typeof (T);
        }

        public static void With(this Func<MethodBase, bool> methodSelector, IMethodCall methodCall)
        {
            var binder = new MethodCallBinder(methodSelector, methodCall);
            MethodCallBinderRegistry.AddBinder(binder);
        }

        public static void With(this Func<MethodBase> methodSelector, IMethodCall methodCall)
        {
            var targetMethod = methodSelector();

            Func<MethodBase, bool> selector = method =>
            {
                // Match the methods by declaring type name and signature
                // Note: The CLR doesn't see the two methods as the same type
                // since one of the other types will be modified by the Deflector library
                var result = method?.DeclaringType?.FullName == targetMethod?.DeclaringType?.FullName && 
                    method.HasCompatibleMethodSignatureWith(targetMethod);

                return result;
            };

            var binder = new MethodCallBinder(selector, methodCall);
            MethodCallBinderRegistry.AddBinder(binder);
        }

        public static void With<T1, T2, T3, T4, T5, T6, T7>(this Func<MethodBase> methodSelector,
            Action<T1, T2, T3, T4, T5, T6, T7> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, T2, T3, T4, T5, T6>(this Func<MethodBase> methodSelector,
            Action<T1, T2, T3, T4, T5, T6> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, T2, T3, T4, T5>(this Func<MethodBase> methodSelector,
            Action<T1, T2, T3, T4, T5> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, T2, T3, T4>(this Func<MethodBase> methodSelector,
            Action<T1, T2, T3, T4> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, T2, T3>(this Func<MethodBase> methodSelector,
            Action<T1, T2, T3> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, T2>(this Func<MethodBase> methodSelector, Action<T1, T2> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1>(this Func<MethodBase> methodSelector, Action<T1> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With(this Func<MethodBase> methodSelector, Action implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, T2, T3, T4, T5, T6, T7, TResult>(this Func<MethodBase> methodSelector,
            Func<T1, T2, T3, T4, T5, T6, T7, TResult> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, T2, T3, T4, T5, T6, TResult>(this Func<MethodBase> methodSelector,
            Func<T1, T2, T3, T4, T5, T6, TResult> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, T2, T3, T4, T5, TResult>(this Func<MethodBase> methodSelector,
            Func<T1, T2, T3, T4, T5, TResult> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, T2, T3, T4, TResult>(this Func<MethodBase> methodSelector,
            Func<T1, T2, T3, T4, TResult> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, T2, T3, TResult>(this Func<MethodBase> methodSelector,
            Func<T1, T2, T3, TResult> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, T2, TResult>(this Func<MethodBase> methodSelector,
            Func<T1, T2, TResult> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, TResult>(this Func<MethodBase> methodSelector, Func<T1, TResult> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<TResult>(this Func<MethodBase> methodSelector, Func<TResult> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void WithDelegate(this Func<MethodBase> methodSelector, MulticastDelegate implementation)
        {
            var targetMethod = methodSelector();

            AddMethodCall(targetMethod, implementation);
        }

        private static void AddMethodCall(MethodBase targetMethod, MulticastDelegate implementation)
        {
            // Verify that the method and the implementation have compatible signatures
            var method = implementation.Method;
            var hasCompatibleMethodSignature = method.HasCompatibleMethodSignatureWith(targetMethod);
            if (!hasCompatibleMethodSignature)
                throw new InvalidOperationException(
                    string.Format(
                        "The delegate you provided does not have a compatible signature with the '{0}' method.",
                        targetMethod.Name));

            MethodCallBinderRegistry.AddBinder(new DelegateBinder(targetMethod, implementation));
        }

        private static MethodInfo GetMethodImpl(LambdaExpression expression)
        {
            var body = expression.Body;
            var unaryExpression = (UnaryExpression)body;
            var operand = (MethodCallExpression)unaryExpression.Operand;
            var methodCallExpression = operand;

            var arguments = methodCallExpression.Arguments;
            var constantExpression = (ConstantExpression)arguments.Last();

            return (MethodInfo)constantExpression.Value;
        }

        private static MethodInfo GetMethodByCall(LambdaExpression expression)
        {
            return ((MethodCallExpression)expression.Body).Method;
        }

        private static PropertyInfo GetPropertyImpl(LambdaExpression p)
        {
            return (PropertyInfo)((MemberExpression)p.Body).Member;
        }
    }
}