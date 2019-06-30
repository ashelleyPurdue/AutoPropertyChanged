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
        ConstructorInfo argsConstructor = typeof(PropertyChangedEventArgs)
            .GetConstructor(new[] { typeof(string) });

        FieldReference propertyChanged = p
            .DeclaringType
            .Fields
            .Where(f => f.Name == "PropertyChanged")
            .Single();

        MethodInfo invoke = typeof(PropertyChangedEventHandler)
            .GetMethod("Invoke");

        ILProcessor proc = p
            .SetMethod
            .Body
            .GetILProcessor();

        // Remove the ending "ret", so we can add stuff to the end.
        Instruction ret = proc
            .Body
            .Instructions
            .Last();
        proc.Remove(ret);

        // Bail out if there are no subscribers.
        proc.Emit(OpCodes.Ldarg_0);
        proc.Emit(OpCodes.Ldfld, propertyChanged);

        proc.Emit(OpCodes.Ldnull);
        proc.Emit(OpCodes.Beq, ret);

        // Invoke PropertyChanged
        proc.Emit(OpCodes.Ldarg_0);
        proc.Emit(OpCodes.Ldfld, propertyChanged);

        proc.Emit(OpCodes.Ldarg_0);
        proc.Emit(OpCodes.Ldstr, p.Name);
        proc.Emit(OpCodes.Newobj, ModuleDefinition.ImportReference(argsConstructor));
        proc.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(invoke));

        // Add the final "ret" back on.
        proc.Append(ret);
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "netstandard";
        yield return "mscorlib";
    }
}