using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;

namespace RuntimeIcons.Preloader;

public static class CecilHelper
{
    public static bool AddField(this TypeDefinition self, FieldAttributes fieldAttributes, string name, TypeReference type, Action<bool, string> logCallback = default)
    {
        logCallback?.Invoke(false, $"Adding field '{name}' to {self.FullName}");
        if (self.FindField(name) != null)
        {
            logCallback?.Invoke(true, $"Field '{name}' already exists in {self.FullName}");
            return false;
        }
        self.Fields.Add(new FieldDefinition(name, fieldAttributes, type));
        return true;
    }
    
    public static bool AddGetter(this TypeDefinition self, string name, Action<bool, string> logCallback = default)
    {
        var methodName = $"Get{name}";
        logCallback?.Invoke(false, $"Adding getter for field '{name}' to {self.FullName}");
        var field = self.FindField(name);
        if (field == null)
        {
            logCallback?.Invoke(true, $"Field '{name}' does not exists in {self.FullName}");
            return false;
        }

        if (self.FindMethod(methodName) != null)
        {
            logCallback?.Invoke(true, $"Method '{methodName}' already exists in {self.FullName}");
            return false;
        }

        var isStatic = false;
        MethodAttributes methodAttributes = 0;
        if ((field.Attributes & FieldAttributes.Static) != 0)
        {
            methodAttributes |= MethodAttributes.Static;
            isStatic = true;
        }        
        if ((field.Attributes & FieldAttributes.Private) != 0)
        {
            methodAttributes |= MethodAttributes.Private;
        }
        
        var methodDefinition = new MethodDefinition(methodName, methodAttributes, field.FieldType);
        self.Methods.Add(methodDefinition);
        methodDefinition.Body.Instructions.InsertRange(0, [
            Instruction.Create(isStatic ? OpCodes.Nop : OpCodes.Ldarg_0),
            Instruction.Create(isStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field),
            Instruction.Create(OpCodes.Ret)
        ]);
        
        return true;
    }
        
    public static bool AddRaise(this TypeDefinition self, string eventName, Action<bool, string> logCallback = default)
    {
        var methodName = $"Get{eventName}";
        logCallback?.Invoke(false, $"Adding caller for event '{eventName}' to {self.FullName}");
        var eventDefinition = self.FindEvent(eventName);
        if (eventDefinition == null)
        {
            logCallback?.Invoke(true, $"Event '{eventName}' does not exists in {self.FullName}");
            return false;
        }
        
        var field = self.FindField(eventName);
        if (field == null)
        {
            logCallback?.Invoke(true, $"Field '{eventName}' does not exists in {self.FullName}");
            return false;
        }

        if (self.FindMethod(methodName) != null)
        {
            logCallback?.Invoke(true, $"Method '{methodName}' already exists in {self.FullName}");
            return false;
        }

        var fieldInvoker = field!.FieldType.Resolve().FindMethod("Invoke");
        var fieldInvokerReference = self.Module.ImportReference(fieldInvoker);
        
        var isStatic = false;
        MethodAttributes methodAttributes = 0;
        if ((field.Attributes & FieldAttributes.Static) != 0)
        {
            methodAttributes |= MethodAttributes.Static;
            isStatic = true;
        }        
        if ((field.Attributes & FieldAttributes.Private) != 0)
        {
            methodAttributes |= MethodAttributes.Private;
        }
        
        var methodDefinition = new MethodDefinition(methodName, methodAttributes, field.FieldType);
        self.Methods.Add(methodDefinition);
        methodDefinition.Parameters.AddRange(fieldInvokerReference.Parameters);

        var instructions = methodDefinition.Body.Instructions;

        var pop = Instruction.Create(OpCodes.Pop);
        var ret = Instruction.Create(OpCodes.Ret);
        
        instructions.AddRange([
            Instruction.Create(isStatic ? OpCodes.Nop : OpCodes.Ldarg_0),
            Instruction.Create(isStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field),
            Instruction.Create(OpCodes.Dup),
            Instruction.Create(OpCodes.Ldnull),
            Instruction.Create(OpCodes.Cgt_Un),
            Instruction.Create(OpCodes.Brfalse, pop)
        ]);

        foreach (var param in methodDefinition.Parameters)
        {
            instructions.Add(Instruction.Create(OpCodes.Ldarg, param));
        }

        instructions.Add(Instruction.Create(OpCodes.Callvirt, fieldInvokerReference));
        instructions.Add(Instruction.Create(OpCodes.Br, ret));
        
        instructions.AddRange([
            pop,
            ret
        ]);
        return true;
    }
    
    public static bool AddMethod(this TypeDefinition self, string methodName, MethodAttributes attributes = MethodAttributes.Private, TypeReference returnType = null, ParameterDefinition[] parameters = null, Action<bool, string> logCallback = default)
    {
        returnType ??= self.Module.TypeSystem.Void;
        parameters ??= [];
        
        logCallback?.Invoke(false, $"Adding method {returnType.FullName} {methodName}({string.Join(",", parameters.Select(p => p.ParameterType.FullName))}) to {self.FullName}");
        if (self.FindMethod(methodName) != null)
        {
            logCallback?.Invoke(true, $"Method '{methodName}' already exists in {self.FullName}");
            return false;
        }
        
        var methodDefinition = new MethodDefinition(methodName, attributes | MethodAttributes.HideBySig, returnType);
        self.Methods.Add(methodDefinition);
        
        methodDefinition.Parameters.AddRange(parameters);

        var processor = methodDefinition.Body.GetILProcessor();


        var ret = processor.Create(OpCodes.Ret);

        if (returnType.MetadataType != MetadataType.Void)
        {
            var constructorInfo = typeof(NotImplementedException).GetConstructor([typeof(string)]);
            var constructorReference = self.Module.ImportReference(constructorInfo);
            processor.Emit(OpCodes.Ldstr, "This is a Stub");
            processor.Emit(OpCodes.Newobj, constructorReference);
            processor.Emit(OpCodes.Throw);
        }
        
        processor.Append(ret);
        return true;
    }
    
}
