using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
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

            try
            {
                foreach (var @enum in receiver.EnumsToGenerate)
                {
                    var semanticModel = context.Compilation.GetSemanticModel(@enum.SyntaxTree);
                    if (semanticModel == null)
                        continue;

                    ISymbol? symbol = semanticModel.GetDeclaredSymbol(@enum);
                    if (symbol == null)
                        return;

                    AttributeData? generateAttribute = symbol.GetAttributes()
                        .FirstOrDefault(at => at.AttributeClass?.Name == Constants.GENERATEHELPER_FULL_NAME || at.AttributeClass?.Name == Constants.GENERATEHELPER_NAME);

                    if (generateAttribute is null || generateAttribute.ConstructorArguments.IsEmpty)
                        continue;

                    var args = generateAttribute.ConstructorArguments[0];
                    var generationBehavior = (GenerateExtensionOption)(int)args.Value!;

                    ISymbol[] membersSymbols = new ISymbol[@enum.Members.Count];
                    for (int i = 0; i < @enum.Members.Count; i++)
                        membersSymbols[i] = semanticModel.GetDeclaredSymbol(@enum.Members[i])!;

                    StringBuilder methodSB = new();

                    GenerateGetDescriptionFastMethod(methodSB, membersSymbols, generationBehavior);
                    GenerateGetEnumFromDescriptionFastMethod(methodSB, membersSymbols, generationBehavior);
                    methodSB.Replace("{enumType}", symbol.Name);

                    string methodsGenerated = methodSB.ToString();

                    string generatedClass = Constants.CLASS_TEMPLATE
                        .Replace("{enumName}", symbol.Name)
                        .Replace("{methodTemplate}", methodsGenerated);

                    string source = Constants.NAMESPACE_TEMPLATE
                        .Replace("{namespaceValue}", symbol.ContainingNamespace.ToDisplayString())
                        .Replace("{classTemplate}", generatedClass);

                    context.AddSource($"{symbol.Name}Helper.g.cs", SourceText.From(source, Encoding.UTF8));
                }
            }
            catch (Exception)
            {
                // do not generate
            }
        }

        private static void GenerateGetDescriptionFastMethod(StringBuilder methodBuilder, ISymbol[] membersSymbols, GenerateExtensionOption option)
        {
            SwitchesBuilder builder = new("{0} => {1}");
            foreach (var item in membersSymbols)
            {
                AttributeData? descriptionAttribute = item.GetAttributes().FirstOrDefault(at => at.AttributeClass!.ToString() == "System.ComponentModel.DescriptionAttribute");

                Func<string> getAttrValue = () => (string)descriptionAttribute!.ConstructorArguments[0].Value!;

                string exceptionTemplate = $"throw new System.InvalidOperationException(\"Description for member {item.Name} was not found.\")";

                (string enumValue, string description) = (descriptionAttribute, option) switch
                {
                    (null, GenerateExtensionOption.IgnoreEnumWithoutDescription) => ("_", "null"),
                    (null, GenerateExtensionOption.ThrowForEnumWithoutDescription) => ($"{{enumType}}.{item.Name}", exceptionTemplate),
                    (null, GenerateExtensionOption.UseItselfWhenNoDescription) => ($"{{enumType}}.{item.Name}", Quote(item.Name)),

                    (not null, GenerateExtensionOption.IgnoreEnumWithoutDescription) when descriptionAttribute.ConstructorArguments.Length == 0 => ("_", "null"),
                    (not null, GenerateExtensionOption.ThrowForEnumWithoutDescription) when descriptionAttribute.ConstructorArguments.Length == 0 => ($"{{enumType}}.{item.Name}", exceptionTemplate),
                    (not null, GenerateExtensionOption.UseItselfWhenNoDescription) when descriptionAttribute.ConstructorArguments.Length == 0 => ($"{{enumType}}.{item.Name}", Quote(item.Name)),

                    (not null, _) => ($"{{enumType}}.{item.Name}", Quote(getAttrValue()))
                };

                builder.Add(enumValue, description);
            }

            var switchesBody = builder.Build(Indent(4));

            const string methodTemplate = @"        public static string GetDescriptionFast(this {enumType} @enum)
        {
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            return @enum switch
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            {
{switchTemplate}
            };
        }";

            methodBuilder.AppendLine(methodTemplate.Replace("{switchTemplate}", switchesBody));
        }

        private static void GenerateGetEnumFromDescriptionFastMethod(StringBuilder methodBuilder, ISymbol[] membersSymbols, GenerateExtensionOption option)
        {
            SwitchesBuilder builder = new("_ when string.Equals({0}, description, StringComparison.InvariantCultureIgnoreCase) => {1}");

            foreach (var item in membersSymbols)
            {
                AttributeData? descriptionAttribute = item.GetAttributes().FirstOrDefault(at => at.AttributeClass!.ToString() == "System.ComponentModel.DescriptionAttribute");

                Func<string> getAttrValue = () => (string)descriptionAttribute!.ConstructorArguments[0].Value!;
                const string exceptionTemplate = "throw new System.InvalidOperationException($\"Enum for description '{description}' was not found.\")";

                (string description, string value) = (descriptionAttribute, option) switch
                {
                    (null, GenerateExtensionOption.IgnoreEnumWithoutDescription) => ("_", "null"),
                    (null, GenerateExtensionOption.ThrowForEnumWithoutDescription) => ("_", exceptionTemplate),
                    (null, GenerateExtensionOption.UseItselfWhenNoDescription) => (Quote(item.Name), $"{{enumType}}.{item.Name}"),

                    (not null, GenerateExtensionOption.IgnoreEnumWithoutDescription) when descriptionAttribute.ConstructorArguments.Length == 0 => ("_", "null"),
                    (not null, GenerateExtensionOption.ThrowForEnumWithoutDescription) when descriptionAttribute.ConstructorArguments.Length == 0 => ("_", exceptionTemplate),
                    (not null, GenerateExtensionOption.UseItselfWhenNoDescription) when descriptionAttribute.ConstructorArguments.Length == 0 => (Quote(item.Name), $"{{enumType}}.{item.Name}"),

                    (not null, _) => (Quote(getAttrValue()), $"{{enumType}}.{item.Name}"),
                };

                builder.Add(description, value);
            }

            var switchesBody = builder.Build(Indent(4));

            const string methodTemplate = @"        public static {enumType}? GetEnumFromDescriptionFast(string description)
        {
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            return description switch
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            {
{switchTemplate}
            };
        }";

            methodBuilder.AppendLine(methodTemplate.Replace("{switchTemplate}", switchesBody));
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
