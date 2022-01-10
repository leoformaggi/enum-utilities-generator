using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace EnumUtilitiesGenerator
{
    internal class ServicesReceiver : ISyntaxReceiver
    {
        public List<EnumDeclarationSyntax> EnumsToGenerate { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is EnumDeclarationSyntax eds)
                EnumsToGenerate.Add(eds);
        }
    }
}
