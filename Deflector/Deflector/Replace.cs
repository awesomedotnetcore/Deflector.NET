using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Deflector
{
    public static class Replace
    {
        public static Func<MethodInfo> Method<TObject>(Expression<Action<TObject>> expression)
        {
            return () => GetMethodByCall(expression);
        }
        public static Func<MethodInfo> Method(Expression<Action> expression)
        {
            return () => GetMethodByCall(expression);
        }

        public static Func<MethodInfo> GetMethod<TObject, T>(Expression<Func<TObject, Action<T>>> expression)
        {
            return () => GetMethodImpl(expression);
        }

        public static Func<MethodInfo> GetMethod<T>(Expression<Func<Action<T>>> expression)
        {
            return () => GetMethodImpl(expression);
        }

        public static void With<T1, T2, T3, T4, T5, T6, T7>(this Func<MethodInfo> methodSelector,
            Action<T1, T2, T3, T4, T5, T6, T7> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, T2, T3, T4, T5, T6>(this Func<MethodInfo> methodSelector,
            Action<T1, T2, T3, T4, T5, T6> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, T2, T3, T4, T5>(this Func<MethodInfo> methodSelector,
            Action<T1, T2, T3, T4, T5> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, T2, T3, T4>(this Func<MethodInfo> methodSelector,
            Action<T1, T2, T3, T4> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, T2, T3>(this Func<MethodInfo> methodSelector,
            Action<T1, T2, T3> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }
        public static void With<T1, T2>(this Func<MethodInfo> methodSelector, Action<T1, T2> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1>(this Func<MethodInfo> methodSelector, Action<T1> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }
        public static void With(this Func<MethodInfo> methodSelector, Action implementation)
        {
            methodSelector.WithDelegate(implementation);
        }
        public static void With<T1, T2, T3, T4, T5, T6, T7, TResult>(this Func<MethodInfo> methodSelector,
            Func<T1, T2, T3, T4, T5, T6, T7, TResult> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, T2, T3, T4, T5, T6, TResult>(this Func<MethodInfo> methodSelector,
            Func<T1, T2, T3, T4, T5, T6, TResult> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, T2, T3, T4, T5, TResult>(this Func<MethodInfo> methodSelector,
            Func<T1, T2, T3, T4, T5, TResult> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, T2, T3, T4, TResult>(this Func<MethodInfo> methodSelector,
            Func<T1, T2, T3, T4, TResult> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, T2, T3, TResult>(this Func<MethodInfo> methodSelector,
            Func<T1, T2, T3, TResult> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }
        public static void With<T1, T2, TResult>(this Func<MethodInfo> methodSelector, Func<T1, T2, TResult> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void With<T1, TResult>(this Func<MethodInfo> methodSelector, Func<T1, TResult> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }
        public static void With<TResult>(this Func<MethodInfo> methodSelector, Func<TResult> implementation)
        {
            methodSelector.WithDelegate(implementation);
        }

        public static void WithDelegate(this Func<MethodInfo> methodSelector, MulticastDelegate implementation)
        {
            var targetMethod = methodSelector();
            // TODO: Verify that the method and the implementation have compatible signatures
            var candidates = new[] { implementation.Method };
            var hasCompatibleMethodSignature = candidates.GetBestMatch(targetMethod) != null;
            if (!hasCompatibleMethodSignature)
                throw new InvalidOperationException(
                    string.Format(
                        "The delegate you provided does not have a compatible signature with the '{0}' method.",
                        targetMethod.Name));

            MethodCallProviderRegistry.AddProvider(new SingleMethodCallProvider(targetMethod, implementation));
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
    }
}
