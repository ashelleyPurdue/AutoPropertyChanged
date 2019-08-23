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
            .Where(t => Implements(t, "INotifyPropertyChanged"));

        foreach (var inpcClass in inpcClasses)
        {
            var dependencies = FindPropertyDependencies(inpcClass);

            foreach (var property in dependencies.Keys)
                AddPropertyChangedInvokations(property, dependencies[property]);
        }
    }

    private bool Implements(TypeDefinition t, string interfaceName) => t
        .Interfaces
        .Where(i => i.InterfaceType.Name == interfaceName)
        .Any();

    private bool HasAttribute(PropertyDefinition p, string attrName) => p
        .CustomAttributes
        .Where(c => c.AttributeType.Name == attrName)
        .Any();

    /// <summary>
    /// Scans through type t and produces a dictionary
    /// detailing which properties depend on each key.
    /// </summary>
    /// <param name="t"></param>
    /// <returns>
    /// Each key is a property marked with [NotifyChanged].
    /// Each value is a list of properties that should be
    /// updated when the key changes.
    /// </returns>
    private Dictionary<PropertyDefinition, IEnumerable<string>> FindPropertyDependencies(TypeDefinition t)
    {
        var dependencyMap = new Dictionary<PropertyDefinition, HashSet<string>>();

        // Every property marked with [NotifyChanged] should
        // fire a PropertyChanged event for itself.
        var taggedProperties = t
            .Properties
            .Where(p => HasAttribute(p, "NotifyChangedAttribute"));

        foreach (var property in taggedProperties)
            dependencyMap
                .GetOrAdd(property)
                .Add(property.Name);

        // TODO: Process properties marked with [DependsOn]


        // Convert the values from HashSet to IEnumerable, because
        // apparently C# doesn't do that automatically.
        return dependencyMap
            .Keys
            .Select(k => (k, dependencyMap[k]))
            .ToDictionary(pair => pair.k, pair => (IEnumerable<string>)pair.Item2);
    }

    /// <summary>
    /// Weaves property p's setter so that it invokes PropertyChanged
    /// with each of the given items
    /// </summary>
    /// <param name="p"></param>
    /// <param name="propertiesToInvokeChangesFor"></param>
    private void AddPropertyChangedInvokations(PropertyDefinition p, IEnumerable<string> propertiesToInvokeChangesFor)
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

        // Invoke PropertyChanged for each of the properties
        foreach (string propertyName in propertiesToInvokeChangesFor)
        {
            proc.Emit(OpCodes.Ldarg_0);
            proc.Emit(OpCodes.Ldfld, propertyChanged);

            proc.Emit(OpCodes.Ldarg_0);
            proc.Emit(OpCodes.Ldstr, propertyName);
            proc.Emit(OpCodes.Newobj, ModuleDefinition.ImportReference(argsConstructor));
            proc.Emit(OpCodes.Callvirt, ModuleDefinition.ImportReference(invoke));
        }

        // Add the final "ret" back on.
        proc.Append(ret);
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "netstandard";
        yield return "mscorlib";
    }
}