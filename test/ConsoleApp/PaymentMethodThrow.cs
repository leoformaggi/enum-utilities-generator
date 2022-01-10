using EnumUtilitiesGenerator.Attributes;
using System.ComponentModel;

namespace SourceGeneratorTest.Console
{
    [GenerateHelper(GenerateHelperOption.ThrowForEnumWithoutDescription)]
    public enum PaymentMethodThrow
    {
        [Description("Credit card")]
        Credit,

        [Description("Debit card")]
        Debit,

        Cash
    }
}