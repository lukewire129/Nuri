using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nuri.UI.Dsl;

namespace Nuri.PreviewHost;

public static class ComponentDiscovery
{
    public static IReadOnlyList<ComponentDescriptor> Discover(Assembly assembly)
    {
        return GetLoadableTypes(assembly)
            .Where(IsPreviewableComponent)
            .Select(type => new ComponentDescriptor(type))
            .OrderBy(component => component.FullName, StringComparer.Ordinal)
            .ToArray();
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type != null)!;
        }
    }

    public static bool IsPreviewableComponent(Type type)
    {
        return typeof(Component).IsAssignableFrom(type)
            && type is { IsAbstract: false, IsGenericTypeDefinition: false }
            && type.GetConstructor(Type.EmptyTypes) != null;
    }
}

public sealed class ComponentDescriptor
{
    public ComponentDescriptor(Type componentType)
    {
        ComponentType = componentType ?? throw new ArgumentNullException(nameof(componentType));
    }

    public Type ComponentType { get; }

    public string FullName => ComponentType.FullName ?? ComponentType.Name;

    public string DisplayName => ComponentType.Name;

    public override string ToString()
    {
        return FullName;
    }
}
