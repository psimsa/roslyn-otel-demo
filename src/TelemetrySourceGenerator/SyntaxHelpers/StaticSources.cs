namespace TelemetrySourceGenerator.SyntaxHelpers;

internal static class StaticSources
{
    internal static string DecoratorExtensionsSource(string @namespace)
        // lang=csharp
        => $$"""
             // <auto-generated />
             #nullable enable
             namespace {{@namespace}}
             {
                 using Microsoft.Extensions.DependencyInjection;
                 
                 internal static class DecoratorExtensions
                 {
                     public static void Decorate(this IServiceCollection services, Type decorated, Type decorator)
                     {
                         var indexedDescriptors = services
                             .Select((descriptor, index) => (descriptor, index))
                             .Where(x => x.descriptor.ServiceType == decorated)
                             .ToList();
                 
                         for (var i = 0; i < indexedDescriptors.Count; i++)
                         {
                             var (originalDescriptor, descriptorIndex) = indexedDescriptors[i];
                 
                             var decoratorFactory = ActivatorUtilities.CreateFactory(
                                 decorator,
                                 new[] { decorated });
                             var serviceDescriptor = ServiceDescriptor.Describe(
                                 decorated,
                                 serviceProvider =>
                                 {
                                     var target = serviceProvider.CreateInstance(originalDescriptor);
                                     return decoratorFactory(
                                         serviceProvider,
                                         new[] { target }
                                     );
                                 },
                                 originalDescriptor.Lifetime);
                 
                             services.RemoveAt(descriptorIndex);
                             services.Insert(descriptorIndex, serviceDescriptor);
                         }
                     }
                     
                     private static object CreateInstance(this IServiceProvider services, ServiceDescriptor descriptor)
                     {
                         if (descriptor.ImplementationInstance != null)
                             return descriptor.ImplementationInstance;
                 
                         if (descriptor.ImplementationFactory != null)
                             return descriptor.ImplementationFactory(services);
                 
                         if (descriptor.ImplementationType == null)
                         {
                             throw new ArgumentException($"Cannot decorate a service that does not specify implementation type");
                         }
                 
                         var instance = ActivatorUtilities.GetServiceOrCreateInstance(services, descriptor.ImplementationType!);
                 
                         if (instance == null)
                         {
                             throw new ArgumentException($"Service of type {descriptor.ImplementationType.Name} cannot be instantiated");
                         }
                 
                         return instance;
                     }
                 }
             }
             #nullable restore
             """;

    internal static string BuildServiceProviderSource(string @namespace)
        // lang=csharp
        => $$"""
             // <auto-generated />
             #nullable enable
             namespace {{@namespace}}
             {
                 using Microsoft.Extensions.DependencyInjection;
                 
                 internal static partial class TelemetryDecoratorServiceProviderRegistrationExtensions
                 {
                     public static ServiceProvider BuildServiceProvider(this IServiceCollection services, bool enableAutoInstrumentation)
                     {
                         if (!enableAutoInstrumentation)
                         {
                             return services.BuildServiceProvider();
                         }
                         
                         foreach (var decoratorForType in TelemetryDecoratorServiceProviderRegistrationExtensions.Decorators)
                         {
                             services.Decorate(decoratorForType.interfaceType, decoratorForType.decoratorType);
                         }
                         
                         return services.BuildServiceProvider();
                     }
                 }
             }
             #nullable restore
             """;

    internal static string BuildWebHostBuilderSource(string @namespace)
        // lang=csharp
        => $$"""
             // <auto-generated />
             #nullable enable
             namespace {{@namespace}}
             {
                 internal static partial class TelemetryDecoratorWebApplicationRegistrationExtensions
                 {
                     public static Microsoft.AspNetCore.Builder.WebApplication Build(this Microsoft.AspNetCore.Builder.WebApplicationBuilder builder, bool enableAutoInstrumentation)
                     {
                         if (!enableAutoInstrumentation)
                         {
                             return builder.Build();
                         }
                         
                         foreach (var decoratorForType in TelemetryDecoratorServiceProviderRegistrationExtensions.Decorators)
                         {
                             builder.Services.Decorate(decoratorForType.interfaceType, decoratorForType.decoratorType);
                         }
                                          
                         return builder.Build();
                     }
                 }
             }
             #nullable restore
             """;

    internal static string DecoratedTypesMapSource(string @namespace, IEnumerable<(string fullDecoratedTypeName, string fullDecoratorTypeName)> namePairs)
        // lang=csharp
        => $$"""
             // <auto-generated />
             #nullable enable
             namespace {{@namespace}}
             {
                 internal static partial class TelemetryDecoratorServiceProviderRegistrationExtensions
                 {
                     public static readonly (Type interfaceType, Type decoratorType)[] Decorators = new []
                     {
                         {{
                             string.Join(",\n", (namePairs ?? Enumerable.Empty<(string, string)>())
                                 .Select(pair
                                     => $"(typeof({pair.fullDecoratedTypeName}), typeof({pair.fullDecoratorTypeName}))"))
                         }}
                     };
                 }
             }
             #nullable restore
             """;

    public static string DefaultActivitySourceSource(string @namespace, string sourceName, string sourceVersion)
        // lang=csharp
        => $$"""
             // <auto-generated />
             namespace {{@namespace}}
             {
                 public static class DefaultActivitySource
                 {
                     public static readonly global::System.Diagnostics.ActivitySource ActivitySource = new("{{sourceName}}", "{{sourceVersion}}");
                 }
             }
             """;

    public static string InterceptsLocationAttributeSource
        // lang=csharp
        => """
           namespace System.Runtime.CompilerServices
           {
               [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
               file sealed class InterceptsLocationAttribute(string filePath, int line, int column) : Attribute { }
           }
           """;

    public static string InterceptsLocationAttributeUsageSource(string path, int line, int column)
        // lang=csharp
        => $"""
            [System.Runtime.CompilerServices.InterceptsLocationAttribute(filePath: @"{path}", line: {line}, column: {column})]
            """;
}
