using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AStar.Dev.Source.Generators.Tests.Unit.Utilitites;

internal static class CompilationHelpers
{
    private const string AttributeSource = @"using System;
namespace AStar.Dev.Source.Generators.Attributes {
    public sealed class StrongIdAttribute(Type? idType) : Attribute
    {
        /// <summary>
        /// The type of the ID property (e.g., typeof(Guid), typeof(int)).
        /// </summary>
        public Type IdType { get; } = idType ?? typeof(Guid);
    }
}";

    public static CSharpCompilation CreateCompilation(string input)
        => CSharpCompilation.Create("TestAssembly",
            [
                CSharpSyntaxTree.ParseText(AttributeSource),
                CSharpSyntaxTree.ParseText(input)
            ],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location)
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
}
