using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

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
        private VariableDefinition _interceptedMethods;
        private VariableDefinition _hasMethodCall;
        private VariableDefinition _currentMethod;
        private VariableDefinition _stackTrace;
        private VariableDefinition _currentArgsAsArray;

        private MethodReference _pushMethod;
        private MethodReference _toArray;
        private MethodReference _invocationInfoCtor;
        private MethodReference _getMethodCall;
        private MethodReference _invokeMethod;
        private MethodReference _stackCtor;
        private MethodReference _markerAttributeCtor;
        private MethodReference _addMethodCalls;
        private MethodReference _containsKey;

        private TypeReference _markerAttributeType;

        public void ImportReferences(ModuleDefinition module)
        {
            _pushMethod = module.ImportMethod<Stack<object>>("Push");
            _toArray = module.ImportMethod<Stack<object>>("ToArray");
            _getMethodCall = module.ImportMethod<IMethodCallMap>("GetMethodCall");
            _containsKey = module.ImportMethod<IMethodCallMap>("ContainsMappingFor");

            _invokeMethod = module.ImportMethod<IMethodCall>("Invoke");
            _stackCtor = module.ImportConstructor<Stack<object>>(new Type[0]);
            _markerAttributeCtor = module.ImportConstructor<MethodCallsAlreadyInterceptedAttribute>(new Type[0]);
            _markerAttributeType = module.ImportType<MethodCallsAlreadyInterceptedAttribute>();
            _addMethodCalls = module.ImportMethod<IMethodCallProvider>("AddMethodCalls");

            var types = new[]
            {
                typeof (object),
                typeof (MethodBase),
                typeof(StackTrace),
                typeof (Type[]),
                typeof (Type[]),
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
            _interceptedMethods = hostMethod.AddLocal<MethodBase[]>("___interceptedMethods");

            _callMap = hostMethod.AddLocal<IMethodCallMap>();
            _hasMethodCall = hostMethod.AddLocal<bool>();
            _currentMethod = hostMethod.AddLocal<MethodBase>();
            _stackTrace = hostMethod.AddLocal<StackTrace>();
            _currentArgsAsArray = hostMethod.AddLocal<object[]>();
        }

        public void Rewrite(MethodDefinition method, ModuleDefinition module)
        {
            // Ignore methods that have already been modified
            var customAttributes = method.CustomAttributes;
            if (customAttributes.Any(c => c.AttributeType == _markerAttributeType))
                return;

            var body = method.Body;
            body.InitLocals = true;

            var oldInstructions = body.Instructions.ToArray();

            var callInstructions = oldInstructions.Where(instruction => instruction.OpCode == OpCodes.Call ||
                                                                        instruction.OpCode == OpCodes.Callvirt).ToArray();

            var constructorCalls = oldInstructions.Where(instruction => instruction.OpCode == OpCodes.Newobj).ToArray();

            // Skip the method if there are no calls to intercept
            if (callInstructions.Any() || constructorCalls.Any())
            {
                // Clear the method body if and only if there are methods 
                // that need to be intercepted
                body.Instructions.Clear();

                var objectType = module.ImportType<object>();
                var il = body.GetILProcessor();
                var targetMethods = callInstructions.Select(instruction => instruction.Operand as MethodReference).ToArray();
                var constructors =
                    constructorCalls.Select(instruction => instruction.Operand as MethodReference).Where(c => c.DeclaringType != objectType).ToArray();

                // Precalculate all method call interceptors            

                var provider = method.AddLocal<IMethodCallProvider>();
                var getProvider = module.ImportMethod("GetProvider", typeof(MethodCallProviderRegistry));

                // Obtain the method call provider instance
                il.Emit(OpCodes.Call, getProvider);

                il.Emit(OpCodes.Stloc, provider);

                // Instantiate the map
                var createMap = module.ImportMethod("CreateMap", typeof(MethodCallMapRegistry),
                    BindingFlags.Public | BindingFlags.Static);

                il.PushMethod(method, module);
                il.Emit(OpCodes.Call, createMap);
                il.Emit(OpCodes.Stloc, _callMap);

                var skipCallMapConstruction = il.Create(OpCodes.Nop);

                il.Emit(OpCodes.Ldloc, _callMap);
                il.Emit(OpCodes.Brfalse, skipCallMapConstruction);

                il.Emit(OpCodes.Ldloc, provider);
                il.Emit(OpCodes.Brfalse, skipCallMapConstruction);

                // if (provider != null) {
                var interceptedMethods = new List<MethodReference>();
                interceptedMethods.AddRange(targetMethods);
                interceptedMethods.AddRange(constructors);

                // Store the list of intercepted methods
                var targetMethodCount = interceptedMethods.Count;
                il.Emit(OpCodes.Ldc_I4, targetMethodCount);
                il.Emit(OpCodes.Newarr, module.ImportType<MethodBase>());
                il.Emit(OpCodes.Stloc, _interceptedMethods);

                // Populate the array of intercepted methods

                for (var i = 0; i < targetMethodCount; i++)
                {
                    var currentMethod = interceptedMethods[i];
                    il.Emit(OpCodes.Ldloc, _interceptedMethods);
                    il.Emit(OpCodes.Ldc_I4, i);
                    il.PushMethod(currentMethod, module);
                    il.Emit(OpCodes.Stelem_Ref);
                }

                // Build the list of intercepted methods
                il.Emit(OpCodes.Ldloc, provider);
                il.Emit(OpCodes.Ldloc, _target);
                il.PushMethod(method, module);
                il.Emit(OpCodes.Ldloc, _interceptedMethods);
                il.Emit(OpCodes.Ldloc, _callMap);
                il.PushStackTrace(module);
                il.Emit(OpCodes.Callvirt, _addMethodCalls);

                il.Append(skipCallMapConstruction);

                foreach (var instruction in oldInstructions)
                {
                    var opCode = instruction.OpCode;
                    if (opCode == OpCodes.Newobj)
                    {
                        il.Emit(OpCodes.Newobj, _stackCtor);
                        il.Emit(OpCodes.Stloc, _currentArguments);
                        ReplaceConstructorCall(instruction, method, il);
                        continue;
                    }


                    if (opCode != OpCodes.Call && opCode != OpCodes.Callvirt)
                    {
                        il.Append(instruction);
                        continue;
                    }

                    // Create the stack that will hold the method arguments
                    il.Emit(OpCodes.Newobj, _stackCtor);
                    il.Emit(OpCodes.Stloc, _currentArguments);

                    if (opCode == OpCodes.Call || opCode == OpCodes.Callvirt)
                        ReplaceMethodCallInstruction(instruction, method, il);
                }
            }

            // Add the attribute marker so that this method is only modified once
            customAttributes.Add(new CustomAttribute(_markerAttributeCtor));
        }

        private void ReplaceConstructorCall(Instruction oldInstruction, MethodDefinition hostMethod,
            ILProcessor il)
        {
            var constructor = (MethodReference)oldInstruction.Operand;
            var module = hostMethod.Module;

            // Skip the System.Object ctor call
            var objectType = module.ImportType<object>();

            if (constructor.DeclaringType == objectType)
            {
                il.Append(oldInstruction);
                return;
            }


            //AddMethodInterceptionHooks(oldInstruction, il, constructor, module);
            il.Emit(OpCodes.Ldloc, _callMap);
            il.PushMethod(constructor, module);
            il.Emit(OpCodes.Callvirt, _containsKey);
            il.Emit(OpCodes.Stloc, _hasMethodCall);

            var skipInterception = il.Create(OpCodes.Nop);
            var endLabel = il.Create(OpCodes.Nop);
            il.Emit(OpCodes.Ldloc, _hasMethodCall);
            il.Emit(OpCodes.Brfalse, skipInterception);

            // TODO: Insert the interception code here
            il.Emit(OpCodes.Ldloc, _callMap);

            il.PushMethod(constructor, module);

            il.Emit(OpCodes.Callvirt, _getMethodCall);
            il.Emit(OpCodes.Stloc, _currentMethodCall);
            il.Emit(OpCodes.Ldloc, _currentMethodCall);
            il.Emit(OpCodes.Brfalse, skipInterception);

            SaveMethodCallArguments(il, constructor);
            var systemType = module.Import(typeof(Type));

            // Note: There is no 'this' pointer when the constructor isn't called yet
            il.Emit(OpCodes.Ldnull);
            SaveParameterTypes(il, constructor, module, systemType);
            SaveTypeArguments(il, constructor, module, systemType);
            SaveCurrentMethod(il, constructor, module);
            SaveStackTrace(il, module);
            SaveMethodArgumentsAsArray(il);
            SaveCurrentInvocationInfo(il);

            il.Emit(OpCodes.Ldloc, _currentMethodCall);
            il.Emit(OpCodes.Ldloc, _invocationInfo);
            il.Emit(OpCodes.Callvirt, _invokeMethod);
            il.Emit(OpCodes.Unbox_Any, constructor.DeclaringType);

            il.Emit(OpCodes.Br, endLabel);
            il.Append(skipInterception);
            il.Append(oldInstruction);

            il.Append(endLabel);
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

            // Replacing constructor calls with the OpCodes.Call instruction is not supported
            if (targetMethod.Name == ".ctor" || targetMethod.Name == ".cctor" && opCode == OpCodes.Call)
            {
                il.Append(oldInstruction);
                return;
            }

            AddMethodInterceptionHooks(oldInstruction, il, targetMethod, module);
            // }
        }

        private void AddMethodInterceptionHooks(Instruction oldInstruction, ILProcessor il, MethodReference targetMethod,
            ModuleDefinition module)
        {
            // Grab the method call instance
            il.Emit(OpCodes.Ldloc, _callMap);
            il.PushMethod(targetMethod, module);
            il.Emit(OpCodes.Callvirt, _containsKey);


            var skipInterception = il.Create(OpCodes.Nop);
            il.Emit(OpCodes.Brfalse, skipInterception);


            EmitMethodCallInterception(il, targetMethod, module, skipInterception);

            var skipMethodCall = il.Create(OpCodes.Nop);

            il.Emit(OpCodes.Br, skipMethodCall);

            // } else {
            il.Append(skipInterception);

            // Call the original method
            il.Emit(oldInstruction.OpCode, targetMethod);

            il.Append(skipMethodCall);
        }

        private void EmitMethodCallInterception(ILProcessor il, MethodReference targetMethod, ModuleDefinition module,
            Instruction skipInterception)
        {
            il.Emit(OpCodes.Ldloc, _callMap);

            il.PushMethod(targetMethod, module);

            il.Emit(OpCodes.Callvirt, _getMethodCall);


            il.Emit(OpCodes.Stloc, _currentMethodCall);

            il.Emit(OpCodes.Ldloc, _currentMethodCall);
            il.Emit(OpCodes.Brfalse, skipInterception);

            // if (currentMethodCall != null) {

            // var returnValue = currentMethodCall.Invoke(methodCallInvocationInfo);

            // Resolve the return type if the target method's declaring type is a generic type
            var returnType = targetMethod.GetReturnType();

            SaveMethodCallInvocationInfo(il, targetMethod, module, returnType);

            il.Emit(OpCodes.Ldloc, _currentMethodCall);
            il.Emit(OpCodes.Ldloc, _invocationInfo);
            il.Emit(OpCodes.Callvirt, _invokeMethod);

            il.PackageReturnValue(module, returnType);
        }

        private void SaveMethodCallInvocationInfo(ILProcessor il, MethodReference targetMethod, ModuleDefinition module,
            TypeReference returnType)
        {
            PushThisPointer(il, targetMethod);

            var systemType = module.Import(typeof(Type));
            // Push the current method
            SaveMethodCallArguments(il, targetMethod);
            SaveCurrentMethod(il, targetMethod, module);
            SaveStackTrace(il, module);
            SaveParameterTypes(il, targetMethod, module, systemType);
            SaveTypeArguments(il, targetMethod, module, systemType);
            SaveMethodArgumentsAsArray(il);

            SaveCurrentInvocationInfo(il);
        }

        private void SaveCurrentInvocationInfo(ILProcessor il)
        {
            il.Emit(OpCodes.Ldloc, _currentMethod);
            il.Emit(OpCodes.Ldloc, _stackTrace);
            il.Emit(OpCodes.Ldloc, _parameterTypes);
            il.Emit(OpCodes.Ldloc, _typeArguments);
            il.Emit(OpCodes.Ldloc, _currentArgsAsArray);
            il.Emit(OpCodes.Newobj, _invocationInfoCtor);
            il.Emit(OpCodes.Stloc, _invocationInfo);
        }

        private void SaveMethodArgumentsAsArray(ILProcessor il)
        {
            // Save the method arguments
            il.Emit(OpCodes.Ldloc, _currentArguments);
            il.Emit(OpCodes.Callvirt, _toArray);
            il.Emit(OpCodes.Stloc, _currentArgsAsArray);
        }

        private void SaveStackTrace(ILProcessor il, ModuleDefinition module)
        {
            il.PushStackTrace(module);
            il.Emit(OpCodes.Stloc, _stackTrace);
        }

        private void SaveCurrentMethod(ILProcessor il, MethodReference targetMethod, ModuleDefinition module)
        {
            il.PushMethod(targetMethod, module);
            il.Emit(OpCodes.Stloc, _currentMethod);
        }

        private void SaveTypeArguments(ILProcessor il, MethodReference targetMethod, ModuleDefinition module,
            TypeReference systemType)
        {
            // Save the type arguments
            var genericParameterCount = targetMethod.GenericParameters.Count;
            il.Emit(OpCodes.Ldc_I4, genericParameterCount);
            il.Emit(OpCodes.Newarr, systemType);
            il.Emit(OpCodes.Stloc, _typeArguments);
            il.PushGenericArguments(targetMethod, module, _typeArguments);
        }

        private void SaveParameterTypes(ILProcessor il, MethodReference targetMethod, ModuleDefinition module,
            TypeReference systemType)
        {
            // Save the parameter types
            var parameterCount = targetMethod.Parameters.Count;
            il.Emit(OpCodes.Ldc_I4, parameterCount);
            il.Emit(OpCodes.Newarr, systemType);
            il.Emit(OpCodes.Stloc, _parameterTypes);

            il.SaveParameterTypes(targetMethod, module, _parameterTypes);
        }

        private void PushThisPointer(ILProcessor il, MethodReference targetMethod)
        {
            // Static methods will always have a null reference as the target
            if (!targetMethod.HasThis)
                il.Emit(OpCodes.Ldnull);

            // Box the target, if necessary
            TypeReference declaringType = targetMethod.GetDeclaringType();
            if (targetMethod.HasThis && (declaringType.IsValueType || declaringType is GenericParameter))
                il.Emit(OpCodes.Box, declaringType);
        }

        private void SaveMethodCallArguments(ILProcessor il, MethodReference targetMethod)
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

                il.Emit(OpCodes.Stloc, _currentArgument);
                il.Emit(OpCodes.Ldloc, _currentArguments);
                il.Emit(OpCodes.Ldloc, _currentArgument);

                il.Emit(OpCodes.Callvirt, _pushMethod);
            }
        }
    }
}