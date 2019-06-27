using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Cecil.Cil;
using Fody;

public class ModuleWeaver : BaseModuleWeaver
{
    public override bool ShouldCleanReference => true;

    public override void Execute()
    {
        IEnumerable<TypeDefinition> inpClasses = ModuleDefinition
            .GetAllTypes()
            .Where(t => ImplementsINPC(t));

        foreach (var inpClass in inpClasses)
            throw new Exception($"{inpClass.Name} implements INotifyPropertyChanged");
    }

    private bool ImplementsINPC(TypeDefinition t) => t
        .Interfaces
        .Where(i => i.InterfaceType.Name == "INotifyPropertyChanged")
        .Any();

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "netstandard";
        yield return "mscorlib";
    }
}