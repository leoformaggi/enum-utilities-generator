using EnumUtilitiesGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace SourceGeneratorTest.Tests
{
    public class EnumUtilitiesGeneratorTest
    {
        private readonly ITestOutputHelper _output;

        public EnumUtilitiesGeneratorTest(ITestOutputHelper output) => this._output = output ?? throw new ArgumentNullException(nameof(output));

        [Fact]
        public void Generator_Should_Generate_Attribute()
        {
            //in this case with no source, the attribute comes second
            var (extensionCode, attributeCode) = GetGeneratedOutput("");
            attributeCode.ShouldNotBeNull();
            extensionCode.ShouldBeEmpty();

            const string expectedAttributeCode = @"// <auto-generated />
using System;

[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
public sealed class GenerateHelperAttribute : Attribute
{
    public GenerateHelperAttribute(GenerateHelperOption generationOption)
    {
        GenerationOption = generationOption;
    }

    public GenerateHelperOption GenerationOption { get; }
}

/// <summary>
/// Define the behaviour of the generated Helper class. All members with a not empty <see cref=""System.ComponentModel.DescriptionAttribute""/> 
/// will be mapped 1:1 as long as each member has an unique description. Each option will treat members without a valid 
/// <see cref=""System.ComponentModel.DescriptionAttribute""/> differently.
/// </summary>
public enum GenerateHelperOption
{
    /// <summary>
    /// Members without description will return null.
    /// </summary>
    IgnoreEnumWithoutDescription = 1,

    /// <summary>
    /// Members without description will throw an exception when requested.
    /// </summary>
    ThrowForEnumWithoutDescription = 2,

    /// <summary>
    /// Members without description will be mapped as themselves, equivalent to using nameof() or .ToString().
    /// </summary>
    UseItselfWhenNoDescription = 3
}";
            
            attributeCode.ShouldBe(expectedAttributeCode);
        }

        [Fact]
        public void Enum_With_IgnoreEnumWithoutDescription_Should_Generate_Helper()
        {
            var source = @"
using System.ComponentModel;

namespace SourceGeneratorTest.Console
{
    [GenerateHelper(GenerateHelperOption.IgnoreEnumWithoutDescription)]
    public enum PaymentMethod
    {
        [Description(""Cart�o de cr�dito"")]
        Credit,
        [Description(""Pix"")]
            Pix,

        [Description]
            Debit,

        Boleto,
        Dinheiro
    }
}";

            var (attributeCode, extensionCode) = GetGeneratedOutput(source);
            attributeCode.ShouldNotBeNullOrEmpty();
            extensionCode.ShouldNotBeNull();

            const string expectedGeneratedHelper = @"// <auto-generated />
using System;

namespace SourceGeneratorTest.Console
{
    public static class PaymentMethodHelper
    {
        private static readonly string[] _descriptions = new string[]
        {
            ""Cart�o de cr�dito"",
            ""Pix"",
        };

        /// <summary>
        /// Get an array of all the descriptions available. Members without or with empty <see cref=""System.ComponentModel.DescriptionAttribute""/>
        /// will not be included, not even as themselves.
        /// </summary>
        public static string[] GetAvailableDescriptions()
        {
            return _descriptions;
        }

        public static string GetDescriptionFast(this PaymentMethod @enum)
        {
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            return @enum switch
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            {
                PaymentMethod.Credit => ""Cart�o de cr�dito"",
                PaymentMethod.Pix => ""Pix"",
                _ => null
            };
        }

        /// <summary>
        /// Returns the enum that has the given description. Compares using <see cref=""System.StringComparison.InvariantCultureIgnoreCase""/>.
        /// Returns null if no enum with given description was found.
        /// </summary>
        /// <param name=""description"">The <see cref=""System.ComponentModel.DescriptionAttribute""/> value</param>
        public static PaymentMethod? GetEnumFromDescriptionFast(string description)
        {
            return GetEnumFromDescriptionFast(description, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Returns the enum that has the given description using any <see cref=""System.StringComparison""/>.
        /// Returns null if no enum with given description was found.
        /// </summary>
        /// <param name=""description"">The <see cref=""System.ComponentModel.DescriptionAttribute""/> value</param>
        public static PaymentMethod? GetEnumFromDescriptionFast(string description, StringComparison stringComparison)
        {
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            return description switch
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            {
                _ when string.Equals(""Cart�o de cr�dito"", description, stringComparison) => PaymentMethod.Credit,
                _ when string.Equals(""Pix"", description, stringComparison) => PaymentMethod.Pix,
                _ => null
            };
        }

    }
}";
            extensionCode.ShouldBe(expectedGeneratedHelper);
        }

        [Fact]
        public void Enum_With_ThrowForEnumWithoutDescription_Should_Generate_Helper()
        {
            var source = @"
using System.ComponentModel;

namespace SourceGeneratorTest.Console
{
    [GenerateHelper(GenerateHelperOption.ThrowForEnumWithoutDescription)]
    public enum PaymentMethod
    {
        [Description(""Cart�o de cr�dito"")]
        Credit,
        [Description(""Pix"")]
            Pix,

        [Description]
            Debit,

        Boleto,
        Dinheiro
    }
}";

            var (attributeCode, extensionCode) = GetGeneratedOutput(source);
            attributeCode.ShouldNotBeNullOrEmpty();
            extensionCode.ShouldNotBeNull();
            
            const string expectedGeneratedHelper = @"// <auto-generated />
using System;

namespace SourceGeneratorTest.Console
{
    public static class PaymentMethodHelper
    {
        private static readonly string[] _descriptions = new string[]
        {
            ""Cart�o de cr�dito"",
            ""Pix"",
        };

        /// <summary>
        /// Get an array of all the descriptions available. Members without or with empty <see cref=""System.ComponentModel.DescriptionAttribute""/>
        /// will not be included, not even as themselves.
        /// </summary>
        public static string[] GetAvailableDescriptions()
        {
            return _descriptions;
        }

        public static string GetDescriptionFast(this PaymentMethod @enum)
        {
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            return @enum switch
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            {
                PaymentMethod.Credit => ""Cart�o de cr�dito"",
                PaymentMethod.Pix => ""Pix"",
                PaymentMethod.Debit => throw new System.InvalidOperationException(""Description for member Debit was not found.""),
                PaymentMethod.Boleto => throw new System.InvalidOperationException(""Description for member Boleto was not found.""),
                PaymentMethod.Dinheiro => throw new System.InvalidOperationException(""Description for member Dinheiro was not found."")
            };
        }

        /// <summary>
        /// Returns the enum that has the given description. Compares using <see cref=""System.StringComparison.InvariantCultureIgnoreCase""/>.
        /// Throws <see cref=""System.InvalidOperationException""/> if no enum with given description was found.
        /// </summary>
        /// <param name=""description"">The <see cref=""System.ComponentModel.DescriptionAttribute""/> value</param>
        public static PaymentMethod? GetEnumFromDescriptionFast(string description)
        {
            return GetEnumFromDescriptionFast(description, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Returns the enum that has the given description using any <see cref=""System.StringComparison""/>.
        /// Throws <see cref=""System.InvalidOperationException""/> if no enum with given description was found.
        /// </summary>
        /// <param name=""description"">The <see cref=""System.ComponentModel.DescriptionAttribute""/> value</param>
        public static PaymentMethod? GetEnumFromDescriptionFast(string description, StringComparison stringComparison)
        {
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            return description switch
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            {
                _ when string.Equals(""Cart�o de cr�dito"", description, stringComparison) => PaymentMethod.Credit,
                _ when string.Equals(""Pix"", description, stringComparison) => PaymentMethod.Pix,
                _ => throw new System.InvalidOperationException($""Enum for description '{description}' was not found."")
            };
        }

    }
}";

            extensionCode.ShouldBe(expectedGeneratedHelper);
        }

        [Fact]
        public void Enum_With_UseItselfWhenNoDescription_Should_Generate_Helper()
        {
            var source = @"
using System.ComponentModel;

namespace SourceGeneratorTest.Console
{
    [GenerateHelper(GenerateHelperOption.UseItselfWhenNoDescription)]
    public enum PaymentMethod
    {
        [Description(""Cart�o de cr�dito"")]
        Credit,
        [Description(""Pix"")]
            Pix,

        [Description]
            Debit,

        Boleto,
        Dinheiro
    }
}";

            var (attributeCode, extensionCode) = GetGeneratedOutput(source);
            attributeCode.ShouldNotBeNullOrEmpty();
            extensionCode.ShouldNotBeNull();

            const string expectedGeneratedHelper = @"// <auto-generated />
using System;

namespace SourceGeneratorTest.Console
{
    public static class PaymentMethodHelper
    {
        private static readonly string[] _descriptions = new string[]
        {
            ""Cart�o de cr�dito"",
            ""Pix"",
        };

        /// <summary>
        /// Get an array of all the descriptions available. Members without or with empty <see cref=""System.ComponentModel.DescriptionAttribute""/>
        /// will not be included, not even as themselves.
        /// </summary>
        public static string[] GetAvailableDescriptions()
        {
            return _descriptions;
        }

        public static string GetDescriptionFast(this PaymentMethod @enum)
        {
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            return @enum switch
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            {
                PaymentMethod.Credit => ""Cart�o de cr�dito"",
                PaymentMethod.Pix => ""Pix"",
                PaymentMethod.Debit => nameof(PaymentMethod.Debit),
                PaymentMethod.Boleto => nameof(PaymentMethod.Boleto),
                PaymentMethod.Dinheiro => nameof(PaymentMethod.Dinheiro)
            };
        }

        /// <summary>
        /// Returns the enum that has the given description. Compares using <see cref=""System.StringComparison.InvariantCultureIgnoreCase""/>.
        /// </summary>
        /// <param name=""description"">The <see cref=""System.ComponentModel.DescriptionAttribute""/> value</param>
        public static PaymentMethod? GetEnumFromDescriptionFast(string description)
        {
            return GetEnumFromDescriptionFast(description, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Returns the enum that has the given description using any <see cref=""System.StringComparison""/>.
        /// </summary>
        /// <param name=""description"">The <see cref=""System.ComponentModel.DescriptionAttribute""/> value</param>
        public static PaymentMethod? GetEnumFromDescriptionFast(string description, StringComparison stringComparison)
        {
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            return description switch
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            {
                _ when string.Equals(""Cart�o de cr�dito"", description, stringComparison) => PaymentMethod.Credit,
                _ when string.Equals(""Pix"", description, stringComparison) => PaymentMethod.Pix,
                _ when string.Equals(""Debit"", description, stringComparison) => PaymentMethod.Debit,
                _ when string.Equals(""Boleto"", description, stringComparison) => PaymentMethod.Boleto,
                _ when string.Equals(""Dinheiro"", description, stringComparison) => PaymentMethod.Dinheiro,
                _ => null
            };
        }

    }
}";

            extensionCode.ShouldBe(expectedGeneratedHelper);
        }

        [Fact]
        public void Enum_With_IgnoreEnumWithoutDescription_With_All_Members_Descripted_Should_Generate_Helper_With_Default()
        {
            var source = @"
using System.ComponentModel;

namespace SourceGeneratorTest.Console
{
    [GenerateHelper(GenerateHelperOption.IgnoreEnumWithoutDescription)]
    public enum PaymentMethod
    {
        [Description(""Credit Card"")]
        Credit,
        [Description(""Debit Card"")]
        Debit
    }
}";

            var (attributeCode, extensionCode) = GetGeneratedOutput(source);
            attributeCode.ShouldNotBeNullOrEmpty();
            extensionCode.ShouldNotBeNull();

            const string expectedGeneratedHelper = @"// <auto-generated />
using System;

namespace SourceGeneratorTest.Console
{
    public static class PaymentMethodHelper
    {
        private static readonly string[] _descriptions = new string[]
        {
            ""Credit Card"",
            ""Debit Card"",
        };

        /// <summary>
        /// Get an array of all the descriptions available. Members without or with empty <see cref=""System.ComponentModel.DescriptionAttribute""/>
        /// will not be included, not even as themselves.
        /// </summary>
        public static string[] GetAvailableDescriptions()
        {
            return _descriptions;
        }

        public static string GetDescriptionFast(this PaymentMethod @enum)
        {
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            return @enum switch
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            {
                PaymentMethod.Credit => ""Credit Card"",
                PaymentMethod.Debit => ""Debit Card""
            };
        }

        /// <summary>
        /// Returns the enum that has the given description. Compares using <see cref=""System.StringComparison.InvariantCultureIgnoreCase""/>.
        /// Returns null if no enum with given description was found.
        /// </summary>
        /// <param name=""description"">The <see cref=""System.ComponentModel.DescriptionAttribute""/> value</param>
        public static PaymentMethod? GetEnumFromDescriptionFast(string description)
        {
            return GetEnumFromDescriptionFast(description, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Returns the enum that has the given description using any <see cref=""System.StringComparison""/>.
        /// Returns null if no enum with given description was found.
        /// </summary>
        /// <param name=""description"">The <see cref=""System.ComponentModel.DescriptionAttribute""/> value</param>
        public static PaymentMethod? GetEnumFromDescriptionFast(string description, StringComparison stringComparison)
        {
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            return description switch
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            {
                _ when string.Equals(""Credit Card"", description, stringComparison) => PaymentMethod.Credit,
                _ when string.Equals(""Debit Card"", description, stringComparison) => PaymentMethod.Debit,
                _ => null
            };
        }

    }
}";
            extensionCode.ShouldBe(expectedGeneratedHelper);
        }

        [Fact]
        public void Enum_With_ThrowForEnumWithoutDescription_With_All_Members_Descripted_Should_Generate_Helper_With_Default()
        {
            var source = @"
using System.ComponentModel;

namespace SourceGeneratorTest.Console
{
    [GenerateHelper(GenerateHelperOption.ThrowForEnumWithoutDescription)]
    public enum PaymentMethod
    {
        [Description(""Credit Card"")]
        Credit,
        [Description(""Debit Card"")]
        Debit
    }
}";

            var (attributeCode, extensionCode) = GetGeneratedOutput(source);
            attributeCode.ShouldNotBeNullOrEmpty();
            extensionCode.ShouldNotBeNull();

            const string expectedGeneratedHelper = @"// <auto-generated />
using System;

namespace SourceGeneratorTest.Console
{
    public static class PaymentMethodHelper
    {
        private static readonly string[] _descriptions = new string[]
        {
            ""Credit Card"",
            ""Debit Card"",
        };

        /// <summary>
        /// Get an array of all the descriptions available. Members without or with empty <see cref=""System.ComponentModel.DescriptionAttribute""/>
        /// will not be included, not even as themselves.
        /// </summary>
        public static string[] GetAvailableDescriptions()
        {
            return _descriptions;
        }

        public static string GetDescriptionFast(this PaymentMethod @enum)
        {
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            return @enum switch
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            {
                PaymentMethod.Credit => ""Credit Card"",
                PaymentMethod.Debit => ""Debit Card""
            };
        }

        /// <summary>
        /// Returns the enum that has the given description. Compares using <see cref=""System.StringComparison.InvariantCultureIgnoreCase""/>.
        /// Throws <see cref=""System.InvalidOperationException""/> if no enum with given description was found.
        /// </summary>
        /// <param name=""description"">The <see cref=""System.ComponentModel.DescriptionAttribute""/> value</param>
        public static PaymentMethod? GetEnumFromDescriptionFast(string description)
        {
            return GetEnumFromDescriptionFast(description, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Returns the enum that has the given description using any <see cref=""System.StringComparison""/>.
        /// Throws <see cref=""System.InvalidOperationException""/> if no enum with given description was found.
        /// </summary>
        /// <param name=""description"">The <see cref=""System.ComponentModel.DescriptionAttribute""/> value</param>
        public static PaymentMethod? GetEnumFromDescriptionFast(string description, StringComparison stringComparison)
        {
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            return description switch
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            {
                _ when string.Equals(""Credit Card"", description, stringComparison) => PaymentMethod.Credit,
                _ when string.Equals(""Debit Card"", description, stringComparison) => PaymentMethod.Debit,
                _ => throw new System.InvalidOperationException($""Enum for description '{description}' was not found."")
            };
        }

    }
}";

            extensionCode.ShouldBe(expectedGeneratedHelper);
        }

        [Fact]
        public void Enum_With_UseItselfWhenNoDescription_With_All_Members_Descripted_Should_Generate_Helper_With_Default()
        {
            var source = @"
using System.ComponentModel;

namespace SourceGeneratorTest.Console
{
    [GenerateHelper(GenerateHelperOption.UseItselfWhenNoDescription)]
    public enum PaymentMethod
    {
        [Description(""Credit Card"")]
        Credit,
        [Description(""Debit Card"")]
        Debit
    }
}";

            var (attributeCode, extensionCode) = GetGeneratedOutput(source);
            attributeCode.ShouldNotBeNullOrEmpty();
            extensionCode.ShouldNotBeNull();

            const string expectedGeneratedHelper = @"// <auto-generated />
using System;

namespace SourceGeneratorTest.Console
{
    public static class PaymentMethodHelper
    {
        private static readonly string[] _descriptions = new string[]
        {
            ""Credit Card"",
            ""Debit Card"",
        };

        /// <summary>
        /// Get an array of all the descriptions available. Members without or with empty <see cref=""System.ComponentModel.DescriptionAttribute""/>
        /// will not be included, not even as themselves.
        /// </summary>
        public static string[] GetAvailableDescriptions()
        {
            return _descriptions;
        }

        public static string GetDescriptionFast(this PaymentMethod @enum)
        {
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            return @enum switch
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            {
                PaymentMethod.Credit => ""Credit Card"",
                PaymentMethod.Debit => ""Debit Card""
            };
        }

        /// <summary>
        /// Returns the enum that has the given description. Compares using <see cref=""System.StringComparison.InvariantCultureIgnoreCase""/>.
        /// </summary>
        /// <param name=""description"">The <see cref=""System.ComponentModel.DescriptionAttribute""/> value</param>
        public static PaymentMethod? GetEnumFromDescriptionFast(string description)
        {
            return GetEnumFromDescriptionFast(description, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Returns the enum that has the given description using any <see cref=""System.StringComparison""/>.
        /// </summary>
        /// <param name=""description"">The <see cref=""System.ComponentModel.DescriptionAttribute""/> value</param>
        public static PaymentMethod? GetEnumFromDescriptionFast(string description, StringComparison stringComparison)
        {
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            return description switch
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            {
                _ when string.Equals(""Credit Card"", description, stringComparison) => PaymentMethod.Credit,
                _ when string.Equals(""Debit Card"", description, stringComparison) => PaymentMethod.Debit,
                _ => null
            };
        }

    }
}";

            extensionCode.ShouldBe(expectedGeneratedHelper);
        }

        [Fact]
        public void Enum_With_UseItselfWhenNoDescription_Should_Generate_Helper_With_Duplicates()
        {
            var source = @"
using System.ComponentModel;

namespace SourceGeneratorTest.Console
{
    [GenerateHelper(GenerateHelperOption.UseItselfWhenNoDescription)]
    public enum PaymentMethod
    {
        [Description(""Cart�o de cr�dito"")]
        Credit,
        [Description(""Pix"")]
            Pix,

        [Description(""Cart�o de cr�dito"")]
        Debit,

        Boleto,
        Dinheiro
    }
}";

            var (attributeCode, extensionCode) = GetGeneratedOutput(source);
            attributeCode.ShouldNotBeNullOrEmpty();
            extensionCode.ShouldNotBeNull();

            const string expectedGeneratedHelper = @"// <auto-generated />
using System;

namespace SourceGeneratorTest.Console
{
    public static class PaymentMethodHelper
    {
        private static readonly string[] _descriptions = new string[]
        {
            ""Cart�o de cr�dito"",
            ""Pix"",
        };

        /// <summary>
        /// Get an array of all the descriptions available. Members without or with empty <see cref=""System.ComponentModel.DescriptionAttribute""/>
        /// will not be included, not even as themselves.
        /// </summary>
        public static string[] GetAvailableDescriptions()
        {
            return _descriptions;
        }

        public static string GetDescriptionFast(this PaymentMethod @enum)
        {
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            return @enum switch
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            {
                PaymentMethod.Credit => ""Cart�o de cr�dito"",
                PaymentMethod.Pix => ""Pix"",
                PaymentMethod.Debit => ""Cart�o de cr�dito"",
                PaymentMethod.Boleto => nameof(PaymentMethod.Boleto),
                PaymentMethod.Dinheiro => nameof(PaymentMethod.Dinheiro)
            };
        }

        /// <summary>
        /// Returns the enum that has the given description. Compares using <see cref=""System.StringComparison.InvariantCultureIgnoreCase""/>.
        /// </summary>
        /// <param name=""description"">The <see cref=""System.ComponentModel.DescriptionAttribute""/> value</param>
        public static PaymentMethod? GetEnumFromDescriptionFast(string description)
        {
            return GetEnumFromDescriptionFast(description, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Returns the enum that has the given description using any <see cref=""System.StringComparison""/>.
        /// </summary>
        /// <param name=""description"">The <see cref=""System.ComponentModel.DescriptionAttribute""/> value</param>
        public static PaymentMethod? GetEnumFromDescriptionFast(string description, StringComparison stringComparison)
        {
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            return description switch
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            {
                _ when string.Equals(""Cart�o de cr�dito"", description, stringComparison) => throw new System.InvalidOperationException($""Multiple members were found with description '{description}'. Could not map description.""),
                _ when string.Equals(""Pix"", description, stringComparison) => PaymentMethod.Pix,
                _ when string.Equals(""Boleto"", description, stringComparison) => PaymentMethod.Boleto,
                _ when string.Equals(""Dinheiro"", description, stringComparison) => PaymentMethod.Dinheiro,
                _ => null
            };
        }

    }
}";

            extensionCode.ShouldBe(expectedGeneratedHelper);
        }

        [Fact]
        public void Enum_With_ThrowForEnumWithoutDescription_Should_Generate_Helper_With_Duplicates()
        {
            var source = @"
using System.ComponentModel;

namespace SourceGeneratorTest.Console
{
    [GenerateHelper(GenerateHelperOption.ThrowForEnumWithoutDescription)]
    public enum PaymentMethod
    {
        [Description(""Cart�o de cr�dito"")]
        Credit,
        [Description(""Pix"")]
            Pix,

        [Description(""Cart�o de cr�dito"")]
        Debit,

        Boleto,
        Dinheiro
    }
}";

            var (attributeCode, extensionCode) = GetGeneratedOutput(source);
            attributeCode.ShouldNotBeNullOrEmpty();
            extensionCode.ShouldNotBeNull();

            const string expectedGeneratedHelper = @"// <auto-generated />
using System;

namespace SourceGeneratorTest.Console
{
    public static class PaymentMethodHelper
    {
        private static readonly string[] _descriptions = new string[]
        {
            ""Cart�o de cr�dito"",
            ""Pix"",
        };

        /// <summary>
        /// Get an array of all the descriptions available. Members without or with empty <see cref=""System.ComponentModel.DescriptionAttribute""/>
        /// will not be included, not even as themselves.
        /// </summary>
        public static string[] GetAvailableDescriptions()
        {
            return _descriptions;
        }

        public static string GetDescriptionFast(this PaymentMethod @enum)
        {
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            return @enum switch
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            {
                PaymentMethod.Credit => ""Cart�o de cr�dito"",
                PaymentMethod.Pix => ""Pix"",
                PaymentMethod.Debit => ""Cart�o de cr�dito"",
                PaymentMethod.Boleto => throw new System.InvalidOperationException(""Description for member Boleto was not found.""),
                PaymentMethod.Dinheiro => throw new System.InvalidOperationException(""Description for member Dinheiro was not found."")
            };
        }

        /// <summary>
        /// Returns the enum that has the given description. Compares using <see cref=""System.StringComparison.InvariantCultureIgnoreCase""/>.
        /// Throws <see cref=""System.InvalidOperationException""/> if no enum with given description was found.
        /// </summary>
        /// <param name=""description"">The <see cref=""System.ComponentModel.DescriptionAttribute""/> value</param>
        public static PaymentMethod? GetEnumFromDescriptionFast(string description)
        {
            return GetEnumFromDescriptionFast(description, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Returns the enum that has the given description using any <see cref=""System.StringComparison""/>.
        /// Throws <see cref=""System.InvalidOperationException""/> if no enum with given description was found.
        /// </summary>
        /// <param name=""description"">The <see cref=""System.ComponentModel.DescriptionAttribute""/> value</param>
        public static PaymentMethod? GetEnumFromDescriptionFast(string description, StringComparison stringComparison)
        {
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            return description switch
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            {
                _ when string.Equals(""Cart�o de cr�dito"", description, stringComparison) => throw new System.InvalidOperationException($""Multiple members were found with description '{description}'. Could not map description.""),
                _ when string.Equals(""Pix"", description, stringComparison) => PaymentMethod.Pix,
                _ => throw new System.InvalidOperationException($""Enum for description '{description}' was not found."")
            };
        }

    }
}";

            extensionCode.ShouldBe(expectedGeneratedHelper);
        }

        [Fact]
        public void Enum_With_IgnoreEnumWithoutDescription_Should_Generate_Helper_With_Duplicates()
        {
            var source = @"
using System.ComponentModel;

namespace SourceGeneratorTest.Console
{
    [GenerateHelper(GenerateHelperOption.IgnoreEnumWithoutDescription)]
    public enum PaymentMethod
    {
        [Description(""Cart�o de cr�dito"")]
        Credit,
        [Description(""Pix"")]
            Pix,

        [Description(""Cart�o de cr�dito"")]
        Debit,

        Boleto,
        Dinheiro
    }
}";

            var (attributeCode, extensionCode) = GetGeneratedOutput(source);
            attributeCode.ShouldNotBeNullOrEmpty();
            extensionCode.ShouldNotBeNull();

            const string expectedGeneratedHelper = @"// <auto-generated />
using System;

namespace SourceGeneratorTest.Console
{
    public static class PaymentMethodHelper
    {
        private static readonly string[] _descriptions = new string[]
        {
            ""Cart�o de cr�dito"",
            ""Pix"",
        };

        /// <summary>
        /// Get an array of all the descriptions available. Members without or with empty <see cref=""System.ComponentModel.DescriptionAttribute""/>
        /// will not be included, not even as themselves.
        /// </summary>
        public static string[] GetAvailableDescriptions()
        {
            return _descriptions;
        }

        public static string GetDescriptionFast(this PaymentMethod @enum)
        {
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            return @enum switch
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            {
                PaymentMethod.Credit => ""Cart�o de cr�dito"",
                PaymentMethod.Pix => ""Pix"",
                PaymentMethod.Debit => ""Cart�o de cr�dito"",
                _ => null
            };
        }

        /// <summary>
        /// Returns the enum that has the given description. Compares using <see cref=""System.StringComparison.InvariantCultureIgnoreCase""/>.
        /// Returns null if no enum with given description was found.
        /// </summary>
        /// <param name=""description"">The <see cref=""System.ComponentModel.DescriptionAttribute""/> value</param>
        public static PaymentMethod? GetEnumFromDescriptionFast(string description)
        {
            return GetEnumFromDescriptionFast(description, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Returns the enum that has the given description using any <see cref=""System.StringComparison""/>.
        /// Returns null if no enum with given description was found.
        /// </summary>
        /// <param name=""description"">The <see cref=""System.ComponentModel.DescriptionAttribute""/> value</param>
        public static PaymentMethod? GetEnumFromDescriptionFast(string description, StringComparison stringComparison)
        {
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            return description switch
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            {
                _ when string.Equals(""Cart�o de cr�dito"", description, stringComparison) => throw new System.InvalidOperationException($""Multiple members were found with description '{description}'. Could not map description.""),
                _ when string.Equals(""Pix"", description, stringComparison) => PaymentMethod.Pix,
                _ => null
            };
        }

    }
}";

            extensionCode.ShouldBe(expectedGeneratedHelper);
        }

        private (string, string) GetGeneratedOutput(string source, bool executable = false)
        {
            var outputCompilation = CreateCompilation(source, executable);
            var trees = outputCompilation.SyntaxTrees.Reverse().Take(2).Reverse().ToList();
            foreach (var tree in trees)
            {
                _output.WriteLine(Path.GetFileName(tree.FilePath) + ":");
                _output.WriteLine(tree.ToString());
            }
            return (trees.First().ToString(), trees[1].ToString());
        }

        private static Compilation CreateCompilation(string source, bool executable)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            var references = new List<MetadataReference>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));

            var compilation = CSharpCompilation.Create("Foo",
                                                       new SyntaxTree[] { syntaxTree },
                                                       references,
                                                       new CSharpCompilationOptions(executable ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary));

            var generator = new EnumHelperGenerator();

            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generateDiagnostics);

            var compileDiagnostics = outputCompilation.GetDiagnostics();
            compileDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error).ShouldBeFalse("Failed: " + compileDiagnostics.FirstOrDefault()?.GetMessage());

            generateDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error).ShouldBeFalse("Failed: " + generateDiagnostics.FirstOrDefault()?.GetMessage());
            return outputCompilation;
        }
    }
}
