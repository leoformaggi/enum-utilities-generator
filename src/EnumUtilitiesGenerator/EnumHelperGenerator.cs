using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnumUtilitiesGenerator
{
    [Generator]
    public class EnumHelperGenerator : ISourceGenerator
    {
        private enum GenerateExtensionOption
        {
            IgnoreEnumWithoutDescription = 1,
            ThrowForEnumWithoutDescription = 2,
            UseItselfWhenNoDescription = 3
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization(c => c.AddSource("GenerateHelper.g.cs", SourceText.From(Constants.GENERATEHELPER_ATTRIBUTE, Encoding.UTF8)));
            context.RegisterForSyntaxNotifications(() => new ServicesReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var receiver = (ServicesReceiver?)context.SyntaxReceiver;
            if (receiver == null || !receiver.EnumsToGenerate.Any())
                return;

            foreach (var @enum in receiver.EnumsToGenerate)
            {
                var semanticModel = context.Compilation.GetSemanticModel(@enum.SyntaxTree);
                if (semanticModel == null)
                    continue;

                ISymbol? symbol = semanticModel.GetDeclaredSymbol(@enum);
                if (symbol == null)
                    return;

                AttributeData? generateAttribute = symbol.GetAttributes()
                    .FirstOrDefault(at => at.AttributeClass!.ToString() == Constants.GENERATEHELPER_FULL_NAME);

                if (generateAttribute is null)
                    continue;

                var args = generateAttribute.ConstructorArguments[0];
                var generationBehavior = (GenerateExtensionOption)(int)args.Value!;

                ISymbol[] membersSymbols = new ISymbol[@enum.Members.Count];
                for (int i = 0; i < @enum.Members.Count; i++)
                    membersSymbols[i] = semanticModel.GetDeclaredSymbol(@enum.Members[i])!;

                string generatedMethod1 = GenerateGetDescriptionFastMethod(symbol.Name, membersSymbols, generationBehavior);
                string generatedMethod2 = GenerateGetEnumFromDescriptionFastMethod(symbol.Name, membersSymbols, generationBehavior);

                string methodsGenerated = generatedMethod1 + "\r\n\r\n" + generatedMethod2;

                string generatedClass = Constants.CLASS_TEMPLATE
                    .Replace("{enumName}", symbol.Name)
                    .Replace("{methodTemplate}", methodsGenerated);

                string source = Constants.NAMESPACE_TEMPLATE
                    .Replace("{namespaceValue}", symbol.ContainingNamespace.ToDisplayString())
                    .Replace("{classTemplate}", generatedClass);

                context.AddSource($"{symbol.Name}Helper.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        }

        private static string GenerateGetDescriptionFastMethod(string enumType, ISymbol[] membersSymbols, GenerateExtensionOption option)
        {
            HashSet<string> switches = new();
            foreach (var item in membersSymbols)
            {
                AttributeData? descriptionAttribute = item.GetAttributes().FirstOrDefault(at => at.AttributeClass!.ToString() == "System.ComponentModel.DescriptionAttribute");

                Func<string> getAttrValue = () => (string)descriptionAttribute!.ConstructorArguments[0].Value!;

                string exceptionTemplate = $"throw new System.InvalidOperationException(\"Description for member {item.Name} was not found.\")";

                (string enumValue, string description) = (descriptionAttribute, option) switch
                {
                    (null, GenerateExtensionOption.IgnoreEnumWithoutDescription) => ("_", "null"),
                    (null, GenerateExtensionOption.ThrowForEnumWithoutDescription) => ($"{enumType}.{item.Name}", exceptionTemplate),
                    (null, GenerateExtensionOption.UseItselfWhenNoDescription) => ($"{enumType}.{item.Name}", Quote(item.Name)),

                    (not null, GenerateExtensionOption.IgnoreEnumWithoutDescription) when descriptionAttribute.ConstructorArguments.Length == 0 => ("_", "null"),
                    (not null, GenerateExtensionOption.ThrowForEnumWithoutDescription) when descriptionAttribute.ConstructorArguments.Length == 0 => ($"{enumType}.{item.Name}", exceptionTemplate),
                    (not null, GenerateExtensionOption.UseItselfWhenNoDescription) when descriptionAttribute.ConstructorArguments.Length == 0 => ($"{enumType}.{item.Name}", Quote(item.Name)),

                    (not null, _) => ($"{enumType}.{item.Name}", Quote(getAttrValue()))
                };

                switches.Add($"{enumValue} => {description}");
            }

            List<string> switchesList = MoveDefaultToLast(switches);

            var switchesBody = string.Join($",\r\n{Indent(4)}", switchesList);

            const string methodTemplate = @"        public static string GetDescriptionFast(this {enumType} @enum)
        {
            return @enum switch
            {
                {switchTemplate}
            };
        }";

            return methodTemplate
                .Replace("{enumType}", enumType)
                .Replace("{switchTemplate}", switchesBody);
        }

        private static string GenerateGetEnumFromDescriptionFastMethod(string enumType, ISymbol[] membersSymbols, GenerateExtensionOption option)
        {
            HashSet<string> switches = new();
            foreach (var item in membersSymbols)
            {
                AttributeData? descriptionAttribute = item.GetAttributes().FirstOrDefault(at => at.AttributeClass!.ToString() == "System.ComponentModel.DescriptionAttribute");

                Func<string> getAttrValue = () => ((string)descriptionAttribute!.ConstructorArguments[0].Value!).ToLower();
                const string exceptionTemplate = "throw new System.InvalidOperationException($\"Enum for description '{description}' was not found.\")";

                (string description, string value) = (descriptionAttribute, option) switch
                {
                    (null, GenerateExtensionOption.IgnoreEnumWithoutDescription) => ("_", "null"),
                    (null, GenerateExtensionOption.ThrowForEnumWithoutDescription) => ("_", exceptionTemplate),
                    (null, GenerateExtensionOption.UseItselfWhenNoDescription) => (Quote(item.Name.ToLower()), $"{enumType}.{item.Name}"),

                    (not null, GenerateExtensionOption.IgnoreEnumWithoutDescription) when descriptionAttribute.ConstructorArguments.Length == 0 => ("_", "null"),
                    (not null, GenerateExtensionOption.ThrowForEnumWithoutDescription) when descriptionAttribute.ConstructorArguments.Length == 0 => ("_", exceptionTemplate),
                    (not null, GenerateExtensionOption.UseItselfWhenNoDescription) when descriptionAttribute.ConstructorArguments.Length == 0 => (Quote(item.Name.ToLower()), $"{enumType}.{item.Name}"),

                    (not null, _) => (Quote(getAttrValue()), $"{enumType}.{item.Name}"),
                };

                switches.Add($"{description} => {value}");
            }

            List<string> switchesList = MoveDefaultToLast(switches);

            var switchesBody = string.Join($",\r\n{Indent(4)}", switchesList);

            const string methodTemplate = @"        public static {enumType}? GetEnumFromDescriptionFast(string description)
        {
            return description.ToLower() switch
            {
                {switchTemplate}
            };
        }";

            return methodTemplate
                .Replace("{enumType}", enumType)
                .Replace("{switchTemplate}", switchesBody);
        }

        private static List<string> MoveDefaultToLast(HashSet<string> switches)
        {
            List<string> switchesList = switches.ToList();
            int indexOfDefault = switchesList.FindIndex(x => x.StartsWith("_ =>"));
            if (indexOfDefault > -1)
            {
                string lastMember = switchesList[indexOfDefault];
                switchesList.RemoveAt(indexOfDefault);
                switchesList.Add(lastMember);
            }

            return switchesList;
        }

        private static string Quote(string s)
        {
            return $"\"{s}\"";
        }

        private static string Indent(int n)
        {
            return new string(' ', 4 * n);
        }
    }

}
