using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Deflector
{
    /// <summary>
    /// Represents a class that emits the method call information and pushes it onto the stack.
    /// </summary>
    public class InvocationInfoEmitter 
    {
        private static readonly ConstructorInfo InvocationInfoConstructor;
        private static readonly MethodInfo GetTypeFromHandle;

        static InvocationInfoEmitter()
        {
            var types = new[]
            {
                typeof (object),
                typeof (MethodBase),
                typeof (Type[]),
                typeof (Type[]),
                typeof (Type),
                typeof (object[])
            };

            InvocationInfoConstructor = typeof(InvocationInfo).GetConstructor(types);

            GetTypeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle",
                BindingFlags.Static | BindingFlags.Public);
        }

        /// <summary>
        /// Emits the IL to save information about
        /// the method currently being executed.
        /// </summary>
        /// <seealso cref="IInvocationInfo"/>
        /// <param name="targetMethod">The target method currently being executed.</param>
        /// <param name="interceptedMethod">The method that will be passed to the <paramref name="invocationInfo"/> as the currently executing method.</param>
        /// <param name="invocationInfo">The local variable that will store the resulting <see cref="IInvocationInfo"/> instance.</param>
        public void Emit(MethodDefinition targetMethod, MethodReference interceptedMethod,
            VariableDefinition invocationInfo)
        {
            var module = targetMethod.DeclaringType.Module;
            var currentMethod = targetMethod.AddLocal(typeof(MethodBase));
            var parameterTypes = targetMethod.AddLocal(typeof(Type[]));
            var arguments = targetMethod.AddLocal(typeof(object[]));
            var typeArguments = targetMethod.AddLocal(typeof(Type[]));
            var systemType = module.ImportType(typeof(Type));

            var IL = targetMethod.GetILGenerator();


            // Type[] typeArguments = new Type[genericTypeCount];
            var genericParameterCount = targetMethod.GenericParameters.Count;
            IL.Emit(OpCodes.Ldc_I4, genericParameterCount);
            IL.Emit(OpCodes.Newarr, systemType);
            IL.Emit(OpCodes.Stloc, typeArguments);

            // object[] arguments = new object[argumentCount];            
            IL.PushArguments(targetMethod, module, arguments);

            // object target = this;
            IL.Emit(targetMethod.HasThis ? OpCodes.Ldarg_0 : OpCodes.Ldnull);

            IL.PushMethod(interceptedMethod, module);

            IL.Emit(OpCodes.Stloc, currentMethod);

            // MethodBase targetMethod = currentMethod as MethodBase;            
            IL.Emit(OpCodes.Ldloc, currentMethod);

            // Push the generic type arguments onto the stack
            if (genericParameterCount > 0)
                IL.PushGenericArguments(targetMethod, module, typeArguments);

            // Make sure that the generic methodinfo is instantiated with the
            // proper type arguments
            if (targetMethod.GenericParameters.Count > 0)
            {
                var methodInfoType = module.Import(typeof(MethodInfo));
                IL.Emit(OpCodes.Isinst, methodInfoType);

                var getIsGenericMethodDef = module.ImportMethod<MethodInfo>("get_IsGenericMethodDefinition");
                IL.Emit(OpCodes.Dup);
                IL.Emit(OpCodes.Callvirt, getIsGenericMethodDef);

                // Determine if the current method is a generic method
                // definition
                var skipMakeGenericMethod = IL.Create(OpCodes.Nop);
                IL.Emit(OpCodes.Brfalse, skipMakeGenericMethod);

                // Instantiate the specific generic method instance
                var makeGenericMethod = module.ImportMethod<MethodInfo>("MakeGenericMethod", typeof(Type[]));
                IL.Emit(OpCodes.Ldloc, typeArguments);
                IL.Emit(OpCodes.Callvirt, makeGenericMethod);
                IL.Append(skipMakeGenericMethod);
            }


            // Save the parameter types
            IL.Emit(OpCodes.Ldc_I4, targetMethod.Parameters.Count);
            IL.Emit(OpCodes.Newarr, systemType);
            IL.Emit(OpCodes.Stloc, parameterTypes);

            IL.SaveParameterTypes(targetMethod, module, parameterTypes);
            IL.Emit(OpCodes.Ldloc, parameterTypes);

            // Push the type arguments back onto the stack
            IL.Emit(OpCodes.Ldloc, typeArguments);

            // Save the return type
            var getTypeFromHandle = module.Import(GetTypeFromHandle);

            var returnType = targetMethod.ReturnType;
            IL.Emit(OpCodes.Ldtoken, returnType);
            IL.Emit(OpCodes.Call, getTypeFromHandle);

            // Push the arguments back onto the stack
            IL.Emit(OpCodes.Ldloc, arguments);


            // InvocationInfo info = new InvocationInfo(...);
            var infoConstructor = module.Import(InvocationInfoConstructor);
            IL.Emit(OpCodes.Newobj, infoConstructor);
            IL.Emit(OpCodes.Stloc, invocationInfo);
            IL.Emit(OpCodes.Ldloc, invocationInfo);
        }
    }
}