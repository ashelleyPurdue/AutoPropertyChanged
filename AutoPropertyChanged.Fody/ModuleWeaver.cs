using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Cecil.Cil;
using Fody;
using System.ComponentModel;
using System.Reflection;

public class ModuleWeaver : BaseModuleWeaver
{
    public override bool ShouldCleanReference => true;

    public override void Execute()
    {
        IEnumerable<TypeDefinition> inpcClasses = ModuleDefinition
            .GetAllTypes()
            .Where(t => ImplementsINPC(t));

        foreach (var inpcClass in inpcClasses)
        {
            var taggedProperties = inpcClass
                .Properties
                .Where(p => ShouldBeWeaved(p));

            if (!taggedProperties.Any())
                continue;

            inpcClass.Methods.Add(CreateInvokePropertyChanged(inpcClass));

            foreach (var property in taggedProperties)
                WeaveProperty(property);
        }
    }

    private bool ImplementsINPC(TypeDefinition t) => t
        .Interfaces
        .Where(i => i.InterfaceType.Name == "INotifyPropertyChanged")
        .Any();

    private bool ShouldBeWeaved(PropertyDefinition p) => p
        .CustomAttributes
        .Where(c => c.AttributeType.Name == "NotifyChangedAttribute")
        .Any();

    private void WeaveProperty(PropertyDefinition p)
    {

    }

    private MethodDefinition CreateInvokePropertyChanged(TypeDefinition type)
    {
        ConstructorInfo argsConstructor = typeof(PropertyChangedEventArgs)
            .GetConstructor(new[] { typeof(string) });

        FieldReference propertyChanged = type
            .Fields
            .Where(f => f.Name == "PropertyChanged")
            .Single();

        MethodInfo invoke = typeof(PropertyChangedEventHandler)
            .GetMethod("Invoke");

        var method = new MethodDefinition("InvokePropertyChanged", Mono.Cecil.MethodAttributes.Private, TypeRef(typeof(void)));
        method.Parameters.Add(new ParameterDefinition("propertyName", Mono.Cecil.ParameterAttributes.None, TypeRef<string>()));
        var processor = method.Body.GetILProcessor();

        // Equivalent to:
        //      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        // Bail out if it has no subscribers.
        Instruction ret = processor.Create(OpCodes.Ret);

        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Ldfld, propertyChanged);
        processor.Emit(OpCodes.Ldnull);
        processor.Emit(OpCodes.Beq, ret);

        // Call the Invoke method.
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Ldfld, propertyChanged);
        processor.Emit(OpCodes.Ldarg_0);
        processor.Emit(OpCodes.Ldarg_1);
        processor.Emit(OpCodes.Newobj, ModuleDefinition.ImportReference(argsConstructor));
        processor.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(invoke));

        processor.Append(ret);

        return method;
    }

    private TypeReference TypeRef(Type t) => ModuleDefinition.ImportReference(t);
    private TypeReference TypeRef<T>() => ModuleDefinition.ImportReference(typeof(T));

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "netstandard";
        yield return "mscorlib";
    }
}