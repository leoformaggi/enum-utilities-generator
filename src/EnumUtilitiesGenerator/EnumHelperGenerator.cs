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

                    var methodBuilder = new StringBuilder();

                    GenerateMethods(methodBuilder, membersSymbols, generationBehavior);
                    methodBuilder.Replace("{enumType}", symbol.Name);

                    string methodsGenerated = methodBuilder.ToString();

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

        private void GenerateMethods(StringBuilder methodBuilder, ISymbol[] membersSymbols, GenerateExtensionOption option)
        {
            var descList = new HashSet<string>();
            var getDescFastBodyBuilder = new SwitchesBuilder("{0} => {1}");

            const string switchFormat = "_ when string.Equals({0}, description, stringComparison) => {1}";
            SwitchesBuilder getEnumFromDescFastBodyBuilder = option switch
            {
                GenerateExtensionOption.UseItselfWhenNoDescription => new SwitchesBuilder(switchFormat, "_ => null"),
                GenerateExtensionOption.IgnoreEnumWithoutDescription => new SwitchesBuilder(switchFormat, "_ => null"),
                GenerateExtensionOption.ThrowForEnumWithoutDescription => new SwitchesBuilder(switchFormat, "_ => throw new System.InvalidOperationException($\"Enum for description '{description}' was not found.\")"),
            };

            foreach (var item in membersSymbols)
            {
                AttributeData? descriptionAttribute = item.GetAttributes().FirstOrDefault(at => at.AttributeClass!.ToString() == "System.ComponentModel.DescriptionAttribute");

                IterateGetAvailableDescriptions(descList, descriptionAttribute);
                IterateGetDescriptionFast(getDescFastBodyBuilder, descriptionAttribute, item, option);
                IterateGetEnumFromDescriptionFast(getEnumFromDescFastBodyBuilder, descriptionAttribute, item, option);
            }

            BuildGetAvailableDescriptionsMethod(methodBuilder, descList);
            BuildGetDescriptionFastMethod(methodBuilder, getDescFastBodyBuilder);
            BuildGetEnumFromDescriptionFastMethod(methodBuilder, option, getEnumFromDescFastBodyBuilder);
        }

        private static void BuildGetAvailableDescriptionsMethod(StringBuilder methodBuilder, HashSet<string> descList)
        {
            var methodTemplate = @"        private static readonly string[] _descriptions = new string[]
        {
{listTemplate}
        };

        /// <summary>
        /// Get an array of all the descriptions available. Members without or with empty <see cref=""System.ComponentModel.DescriptionAttribute""/>
        /// will not be included, not even as themselves.
        /// </summary>
        public static string[] GetAvailableDescriptions()
        {
            return _descriptions;
        }
";

            methodBuilder.AppendLine(methodTemplate.Replace("{listTemplate}", string.Join("\r\n", descList)));
        }

        private static void BuildGetDescriptionFastMethod(StringBuilder methodBuilder, SwitchesBuilder switchesBuilder)
        {
            var switchesBodyGetDescFast = switchesBuilder.Build(Indent(4));
            methodBuilder.AppendLine(@"        public static string GetDescriptionFast(this {enumType} @enum)
        {
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            return @enum switch
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            {
{switchTemplate}
            };
        }
".Replace("{switchTemplate}", switchesBodyGetDescFast));
        }

        private static void BuildGetEnumFromDescriptionFastMethod(StringBuilder methodBuilder, GenerateExtensionOption option, SwitchesBuilder switchesBuilder)
        {
            var switchesBodyGetEnumFromDescFast = switchesBuilder.Build(Indent(4));

            GetGetEnumFromDescriptionDoc(methodBuilder, option, true);
            methodBuilder.AppendLine(@"        public static {enumType}? GetEnumFromDescriptionFast(string description)
        {
            return GetEnumFromDescriptionFast(description, StringComparison.InvariantCultureIgnoreCase);
        }
");

            GetGetEnumFromDescriptionDoc(methodBuilder, option, false);
            methodBuilder.AppendLine(@"        public static {enumType}? GetEnumFromDescriptionFast(string description, StringComparison stringComparison)
        {
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            return description switch
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            {
{switchTemplate}
            };
        }".Replace("{switchTemplate}", switchesBodyGetEnumFromDescFast));
        }

        private static void IterateGetAvailableDescriptions(HashSet<string> descsList, AttributeData? descriptionAttribute)
        {
            if (descriptionAttribute is null || descriptionAttribute.ConstructorArguments.IsEmpty)
                return;

            string value = ((string)descriptionAttribute!.ConstructorArguments[0].Value!).Quote();

            descsList.Add(Indent(3) + value + ",");
        }

        private static void IterateGetDescriptionFast(SwitchesBuilder builder, AttributeData? descriptionAttribute, ISymbol enumMember, GenerateExtensionOption option)
        {
            Func<string> getAttrValue = () => (string)descriptionAttribute!.ConstructorArguments[0].Value!;

            string exceptionTemplate = $"throw new System.InvalidOperationException(\"Description for member {enumMember.Name} was not found.\")";

            (string enumValue, string description) = (descriptionAttribute, option) switch
            {
                (null, GenerateExtensionOption.IgnoreEnumWithoutDescription) => ("_", "null"),
                (null, GenerateExtensionOption.ThrowForEnumWithoutDescription) => ($"{{enumType}}.{enumMember.Name}", exceptionTemplate),
                (null, GenerateExtensionOption.UseItselfWhenNoDescription) => ($"{{enumType}}.{enumMember.Name}", $"nameof({{enumType}}.{enumMember.Name})"),

                (not null, GenerateExtensionOption.IgnoreEnumWithoutDescription) when descriptionAttribute.ConstructorArguments.Length == 0 => ("_", "null"),
                (not null, GenerateExtensionOption.ThrowForEnumWithoutDescription) when descriptionAttribute.ConstructorArguments.Length == 0 => ($"{{enumType}}.{enumMember.Name}", exceptionTemplate),
                (not null, GenerateExtensionOption.UseItselfWhenNoDescription) when descriptionAttribute.ConstructorArguments.Length == 0 => ($"{{enumType}}.{enumMember.Name}", $"nameof({{enumType}}.{enumMember.Name})"),

                (not null, _) => ($"{{enumType}}.{enumMember.Name}", getAttrValue().Quote())
            };

            builder.Add(enumValue, description);
        }

        private static void IterateGetEnumFromDescriptionFast(SwitchesBuilder builder, AttributeData? descriptionAttribute, ISymbol enumMember, GenerateExtensionOption option)
        {
            Func<string> getAttrValue = () => (string)descriptionAttribute!.ConstructorArguments[0].Value!;
            const string exceptionTemplate = "throw new System.InvalidOperationException($\"Enum for description '{description}' was not found.\")";

            (string description, string value) = (descriptionAttribute, option) switch
            {
                (null, GenerateExtensionOption.IgnoreEnumWithoutDescription) => ("_", "null"),
                (null, GenerateExtensionOption.ThrowForEnumWithoutDescription) => ("_", exceptionTemplate),
                (null, GenerateExtensionOption.UseItselfWhenNoDescription) => (enumMember.Name.Quote(), $"{{enumType}}.{enumMember.Name}"),

                (not null, GenerateExtensionOption.IgnoreEnumWithoutDescription) when descriptionAttribute.ConstructorArguments.Length == 0 => ("_", "null"),
                (not null, GenerateExtensionOption.ThrowForEnumWithoutDescription) when descriptionAttribute.ConstructorArguments.Length == 0 => ("_", exceptionTemplate),
                (not null, GenerateExtensionOption.UseItselfWhenNoDescription) when descriptionAttribute.ConstructorArguments.Length == 0 => (enumMember.Name.Quote(), $"{{enumType}}.{enumMember.Name}"),

                (not null, _) => (getAttrValue().Quote(), $"{{enumType}}.{enumMember.Name}"),
            };

            builder.Add(description, value);
        }

        private static void GetGetEnumFromDescriptionDoc(StringBuilder methodBuilder, GenerateExtensionOption generateExtensionOption, bool isDefault)
        {
            const int indentLevel = 2;
            methodBuilder.AppendLine(Indent(indentLevel) + "/// <summary>");

            var doc = isDefault ? $@"/// Returns the enum that has the given description. Compares using <see cref=""System.StringComparison.InvariantCultureIgnoreCase""/>."
                              : $@"/// Returns the enum that has the given description using any <see cref=""System.StringComparison""/>.";

            methodBuilder.AppendLine(Indent(indentLevel) + doc);

            if (generateExtensionOption == GenerateExtensionOption.IgnoreEnumWithoutDescription)
                methodBuilder.AppendLine(Indent(indentLevel) + "/// Returns null if no enum with given description was found.");
            else if (generateExtensionOption == GenerateExtensionOption.ThrowForEnumWithoutDescription)
                methodBuilder.AppendLine(Indent(indentLevel) + @"/// Throws <see cref=""System.InvalidOperationException""/> if no enum with given description was found.");

            methodBuilder.AppendLine(Indent(indentLevel) + "/// </summary>");
            methodBuilder.AppendLine(Indent(indentLevel) + @"/// <param name=""description"">The <see cref=""System.ComponentModel.DescriptionAttribute""/> value</param>");
        }

        private static string Indent(int n)
        {
            return new string(' ', 4 * n);
        }
    }
}
