using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using LinFu.Finders;
using LinFu.Finders.Interfaces;

namespace Deflector
{
    public static class MethodFinderExtensions
    {
        public static TMethod GetBestMatch<TMethod>(this IEnumerable<TMethod> targetMethods, Type[] typeArguments, Type[] parameterTypes, Type returnType, string methodName)
            where TMethod : MethodBase
        {
            var hasTypeArguments = typeArguments != null && typeArguments.Length > 0;
            Func<MethodBase, int, Type, bool> matchesParameterType = (method, index, type) =>
            {
                var result = false;
                try
                {
                    var currentMethod = method;
                    if (hasTypeArguments && method is MethodInfo)
                    {
                        var methodInfo = (MethodInfo)method;
                        currentMethod = methodInfo.MakeGenericMethod(typeArguments ?? new Type[0]);
                    }

                    var parameters = currentMethod.GetParameters();
                    var parameter = index < parameters.Length ? parameters[index] : null;
                    result = parameter != null && type.IsAssignableFrom(parameter.ParameterType);
                }
                catch
                {
                    // Ignore the error
                }

                return result;
            };


            var candidateMethods = targetMethods.AsFuzzyList();
            var expectedArgumentCount = parameterTypes != null ? parameterTypes.Length : 0;

            if (typeof(TMethod) != typeof(ConstructorInfo) && hasTypeArguments)
            {
                // Match the type argument count, if necessary
                candidateMethods.AddCriteria(m => m.IsGenericMethodDefinition &&
                    m.GetGenericArguments().Length == typeArguments.Length, CriteriaType.Critical);
            }

            // Match the method name (optional)
            if (!string.IsNullOrEmpty(methodName))
                candidateMethods.AddCriteria(m => m.Name == methodName, CriteriaType.Optional, 1);

            // Match the argument count
            candidateMethods.AddCriteria(m => m.GetParameters().Length == expectedArgumentCount, CriteriaType.Critical);

            for (var i = 0; parameterTypes != null && i < expectedArgumentCount; i++)
            {
                var currentArgumentType = parameterTypes[i];

                // Match the argument types
                var currentIndex = i;
                if (currentArgumentType != null)
                {
                    candidateMethods.AddCriteria(
                        m => matchesParameterType(m, currentIndex, currentArgumentType),
                        CriteriaType.Critical);
                }
            }

            Func<MethodBase, Type> getReturnType = method =>
            {
                var constructorInfo = method as ConstructorInfo;
                if (constructorInfo != null)
                {
                    return constructorInfo.DeclaringType;
                }

                var methodInfo = (MethodInfo)method;
                return methodInfo.ReturnType;
            };

            // Match the return type
            candidateMethods.AddCriteria(method => returnType.IsAssignableFrom(getReturnType(method)));

            var bestMatch = candidateMethods.BestMatch();
            MethodBase targetMethod = bestMatch != null ? bestMatch.Item : null;

            // Instantiate the generic method if necessary
            if (typeof(TMethod) == typeof(MethodInfo) && hasTypeArguments && bestMatch != null && targetMethod is MethodInfo)
            {
                var targetMethodInfo = targetMethod as MethodInfo;
                targetMethod = targetMethodInfo.MakeGenericMethod(typeArguments);
            }

            return (TMethod)targetMethod;
        }

        public static MethodBase GetBestMatch(this IEnumerable<MethodBase> candidateMethods, MethodBase currentMethod)
        {
            var parameterTypes = currentMethod.GetParameters().Select(p => p.ParameterType).ToArray();
            Type[] typeArguments;
            try
            {
                typeArguments = currentMethod.GetGenericArguments();
            }
            catch (NotSupportedException)
            {
                typeArguments = new Type[0];
            }

            var constructorInfo = currentMethod as ConstructorInfo;
            if (constructorInfo != null)
            {
                return candidateMethods.GetBestMatch(typeArguments,
                    parameterTypes, constructorInfo.DeclaringType, constructorInfo.Name);
            }

            var methodInfo = (MethodInfo)currentMethod;
            return candidateMethods.GetBestMatch(typeArguments, parameterTypes, methodInfo.ReturnType, methodInfo.Name);
        }

        public static bool HasCompatibleMethodSignatureWith(this MethodBase method, MethodBase targetMethod)
        {
            var candidates = new[] { method };
            var hasCompatibleMethodSignature = candidates.GetBestMatch(targetMethod) != null;
            return hasCompatibleMethodSignature;
        }
    }
}
