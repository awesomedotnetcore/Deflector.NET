using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Deflector
{
    public class MethodCallInterceptionEmitter : IMethodBodyRewriter
    {
        private VariableDefinition _invocationInfo;
        private VariableDefinition _currentArgument;
        private VariableDefinition _currentArguments;
        private VariableDefinition _target;
        private VariableDefinition _parameterTypes;
        private VariableDefinition _typeArguments;
        private VariableDefinition _callMap;
        private VariableDefinition _currentMethodCall;

        private MethodReference _pushMethod;
        private MethodReference _toArray;
        private MethodReference _invocationInfoCtor;
        private MethodReference _getMethodCall;
        private MethodReference _invokeMethod;
        private MethodReference _stackCtor;

        public void ImportReferences(ModuleDefinition module)
        {
            _pushMethod = module.ImportMethod<Stack<object>>("Push");
            _toArray = module.ImportMethod<Stack<object>>("ToArray");
            _getMethodCall = module.ImportMethod<IDictionary<MethodBase, IMethodCall>>("get_Item");
            _invokeMethod = module.ImportMethod<IMethodCall>("Invoke");
            _stackCtor = module.ImportConstructor<Stack<object>>(new Type[0]);

            var types = new[]
            {
                typeof (object),
                typeof (MethodBase),
                typeof (Type[]),
                typeof (Type[]),
                typeof (Type),
                typeof (object[])
            };

            _invocationInfoCtor = module.ImportConstructor<InvocationInfo>(types);
        }

        public void AddLocals(MethodDefinition hostMethod)
        {
            _currentArguments = hostMethod.AddLocal<Stack<object>>("__arguments");
            _currentArgument = hostMethod.AddLocal<object>("__currentArgument");
            _invocationInfo = hostMethod.AddLocal<IInvocationInfo>("___invocationInfo");
            _currentMethodCall = hostMethod.AddLocal<IMethodCall>("___currentMethodCall");

            _target = hostMethod.AddLocal<object>("__target");
            _parameterTypes = hostMethod.AddLocal<Type[]>("__parameterTypes");
            _typeArguments = hostMethod.AddLocal<Type[]>("__typeArguments");
            _callMap = hostMethod.AddLocal<IDictionary<MethodBase, IMethodCall>>();
        }

        public void Rewrite(MethodDefinition method, ModuleDefinition module)
        {
            var body = method.Body;
            body.InitLocals = true;

            var oldInstructions = body.Instructions.ToArray();
            body.Instructions.Clear();

            var callInstructions = oldInstructions.Where(instruction => instruction.OpCode == OpCodes.Call ||
                                                                        instruction.OpCode == OpCodes.Callvirt);

            var il = body.GetILProcessor();
            var targetMethods = callInstructions.Select(instruction => instruction.Operand as MethodReference);

            // Precalculate all method call interceptors            
            var addMethod = module.ImportMethod<IDictionary<MethodBase, IMethodCall>>("Add", typeof(MethodBase),
                typeof(IMethodCall));

            var provider = method.AddLocal<IMethodCallProvider>();
            var getProvider = module.ImportMethod("GetProvider", typeof(MethodCallProviderRegistry));

            // Create the stack that will hold the method arguments
            il.Emit(OpCodes.Newobj, _stackCtor);
            il.Emit(OpCodes.Stloc, _currentArguments);

            // Obtain the method call provider instance
            il.Emit(method.HasThis ? OpCodes.Ldarg_0 : OpCodes.Ldnull);
            il.PushType(method.DeclaringType, module);
            il.Emit(OpCodes.Call, getProvider);
            il.Emit(OpCodes.Stloc, provider);

            // Instantiate the map
            var mapCtor = module.ImportConstructor<ConcurrentDictionary<MethodBase, IMethodCall>>(new Type[0]);
            il.Emit(OpCodes.Newobj, mapCtor);
            il.Emit(OpCodes.Stloc, _callMap);

            var getMethodCallFor = module.ImportMethod<IMethodCallProvider>("GetMethodCallFor");

            var skipCallMapConstruction = il.Create(OpCodes.Nop);

            il.Emit(OpCodes.Ldloc, provider);
            il.Emit(OpCodes.Brfalse, skipCallMapConstruction);

            // if (provider != null) {
            foreach (var targetMethod in targetMethods)
            {
                // callMap.Add(targetMethod, provider.GetMethodCallFor(targetMethod))
                il.Emit(OpCodes.Ldloc, _callMap);
                il.PushMethod(targetMethod, module);

                il.Emit(OpCodes.Ldloc, provider);
                il.PushMethod(targetMethod, module);
                il.Emit(OpCodes.Callvirt, getMethodCallFor);

                il.Emit(OpCodes.Callvirt, addMethod);
            }

            // }

            il.Append(skipCallMapConstruction);

            foreach (var instruction in oldInstructions)
            {
                var opCode = instruction.OpCode;
                if (opCode != OpCodes.Call && opCode != OpCodes.Callvirt)
                {
                    il.Append(instruction);
                    continue;
                }

                ReplaceMethodCallInstruction(instruction, method, il);
            }
        }

        private void ReplaceMethodCallInstruction(Instruction oldInstruction, MethodDefinition hostMethod,
            ILProcessor il)
        {
            var opCode = oldInstruction.OpCode;
            if (opCode != OpCodes.Call && opCode != OpCodes.Callvirt)
            {
                il.Append(oldInstruction);
                return;
            }

            var targetMethod = (MethodReference)oldInstruction.Operand;
            var module = hostMethod.Module;

            // Grab the method call instance
            il.Emit(OpCodes.Ldloc, _callMap);


            il.PushMethod(targetMethod, module);

            il.Emit(OpCodes.Callvirt, _getMethodCall);


            il.Emit(OpCodes.Stloc, _currentMethodCall);

            var skipInterception = il.Create(OpCodes.Nop);

            il.Emit(OpCodes.Ldloc, _currentMethodCall);
            il.Emit(OpCodes.Brfalse, skipInterception);

            // if (currentMethodCall != null) {

            // var returnValue = currentMethodCall.Invoke(methodCallInvocationInfo);
            var returnType = targetMethod.ReturnType;
            SaveMethodCallInvocationInfo(il, targetMethod, module, returnType);


            il.Emit(OpCodes.Ldloc, _currentMethodCall);
            il.Emit(OpCodes.Ldloc, _invocationInfo);
            il.Emit(OpCodes.Callvirt, _invokeMethod);

            il.PackageReturnValue(module, returnType);


            var skipMethodCall = il.Create(OpCodes.Nop);

            if (returnType.FullName != "System.Void")
                il.Emit(OpCodes.Ldnull);

            il.Emit(OpCodes.Br, skipMethodCall);

            // } else {
            il.Append(skipInterception);

            // Call the original method
            il.Emit(oldInstruction.OpCode, targetMethod);

            il.Append(skipMethodCall);
            // }
        }

        private void SaveMethodCallInvocationInfo(ILProcessor il, MethodReference targetMethod, ModuleDefinition module,
            TypeReference returnType)
        {
            // If the target method is an instance method, then the remaining item on the stack
            // will be the target object instance

            // Put all the method arguments into the argument stack
            foreach (var param in targetMethod.Parameters)
            {
                // Save the current argument
                var parameterType = param.ParameterType;
                if (parameterType.IsValueType || parameterType is GenericParameter)
                    il.Emit(OpCodes.Box, parameterType);


                il.EmitWriteLineIfNull("Current Arguments is null!", _currentArguments);
                il.Emit(OpCodes.Stloc, _currentArgument);
                il.Emit(OpCodes.Ldloc, _currentArguments);
                il.Emit(OpCodes.Ldloc, _currentArgument);

                il.Emit(OpCodes.Callvirt, _pushMethod);
            }

            // Static methods will always have a null reference as the target
            if (!targetMethod.HasThis)
                il.Emit(OpCodes.Ldnull);

            // Box the target, if necessary
            TypeReference declaringType = targetMethod.GetDeclaringType();
            if (targetMethod.HasThis && (declaringType.IsValueType || declaringType is GenericParameter))
                il.Emit(OpCodes.Box, declaringType);


            if (targetMethod.HasThis)
            {
                il.Emit(OpCodes.Stloc, _target);
                il.Emit(OpCodes.Ldloc, _target);
            }

            // Push the current method
            il.PushMethod(targetMethod, module);


            var systemType = module.Import(typeof(Type));

            // Save the parameter types
            var parameterCount = targetMethod.Parameters.Count;
            il.Emit(OpCodes.Ldc_I4, parameterCount);
            il.Emit(OpCodes.Newarr, systemType);
            il.Emit(OpCodes.Stloc, _parameterTypes);

            il.SaveParameterTypes(targetMethod, module, _parameterTypes);
            il.Emit(OpCodes.Ldloc, _parameterTypes);

            // Save the type arguments
            var genericParameterCount = targetMethod.GenericParameters.Count;
            il.Emit(OpCodes.Ldc_I4, genericParameterCount);
            il.Emit(OpCodes.Newarr, systemType);
            il.Emit(OpCodes.Stloc, _typeArguments);
            il.PushGenericArguments(targetMethod, module, _typeArguments);
            il.Emit(OpCodes.Ldloc, _typeArguments);

            // Push the return type
            il.PushType(returnType, module);

            // Save the method arguments
            il.Emit(OpCodes.Ldloc, _currentArguments);
            il.Emit(OpCodes.Callvirt, _toArray);

            il.Emit(OpCodes.Newobj, _invocationInfoCtor);
            il.Emit(OpCodes.Stloc, _invocationInfo);
        }
    }
}