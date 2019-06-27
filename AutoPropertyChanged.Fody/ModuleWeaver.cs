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
        IEnumerable<TypeDefinition> inpcClasses = ModuleDefinition
            .GetAllTypes()
            .Where(t => ImplementsINPC(t));

        foreach (var inpcClass in inpcClasses)
        {
            var taggedProperties = inpcClass
                .Properties
                .Where(p => ShouldBeWeaved(p));

            foreach (var property in taggedProperties)
                throw new Exception($"{property} should be weaved.");
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

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "netstandard";
        yield return "mscorlib";
    }
}