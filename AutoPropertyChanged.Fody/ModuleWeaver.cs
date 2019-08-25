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
        var notifyChangedProps = t
            .Properties
            .Where(p => HasAttribute(p, "NotifyChangedAttribute"));

        foreach (var property in notifyChangedProps)
            dependencyMap
                .GetOrAdd(property)
                .Add(property.Name);

        // Process properties marked with [DependsOn]
        var propertiesByName = t
            .Properties
            .ToDictionary(p => p.Name);

        var dependsOnProps = t
            .Properties
            .Where(p => HasAttribute(p, "DependsOnAttribute"));

        foreach (var property in dependsOnProps)
        {
            // Get all the properties that this guy depends on
            // TODO: Right now it's only just one.  Extend it to
            // accept multiple params.
            var dependsOnAttr = property
                .CustomAttributes
                .Where(a => a.AttributeType.Name == "DependsOnAttribute")
                .Single();

            // The second argument, if it exists, is a params array
            // with the rest of them.
            // Why not JUST use a params array as the only argument?
            // Beats me, I just know it doesn't work if you do.
            foreach (string dependencyName in GetDependsOnArguments(dependsOnAttr))
                dependencyMap
                    .GetOrAdd(propertiesByName[dependencyName])
                    .Add(property.Name);
        }

        // Convert the values from HashSet to IEnumerable, because
        // apparently C# doesn't do that automatically.
        return dependencyMap
            .Keys
            .Select(k => (k, dependencyMap[k]))
            .ToDictionary(pair => pair.k, pair => (IEnumerable<string>)pair.Item2);
    }

    /// <summary>
    /// Enumerates the arguments of the given DependsOnAttribute.
    /// </summary>
    /// <param name="dependsOnAttr">
    ///     A CustomAttribute corresponding to a DependsOnAttribute.
    ///     I wish I could express that in the type system.
    /// </param>
    /// <returns></returns>
    private IEnumerable<string> GetDependsOnArguments(CustomAttribute dependsOnAttr)
    {
        var attrArgs = dependsOnAttr
                .ConstructorArguments
                .ToList();

        // The first argument is a single string(not a params array).
        yield return (string)(attrArgs[0].Value);

        // The second argument, if it exists, is a params array
        // with the rest of them.
        // Why not JUST use a params array as the only argument?
        // Beats me, I just know it doesn't work if you do.
        if (attrArgs.Count == 1)
            yield break;

        var remainingArgs = (CustomAttributeArgument[])attrArgs[1].Value;
        IEnumerable<string> remainingDependencies = remainingArgs
            .Select(arg => (string)arg.Value);

        foreach (string dependencyName in remainingDependencies)
            yield return dependencyName;
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