using System.ComponentModel;

namespace SourceGeneratorTest.Console
{
    [GenerateHelper(GenerateHelperOption.IgnoreEnumWithoutDescription)]
    public enum PaymentMethodIgnore
    {
        [Description("Credit card")]
        Credit,

        [Description("Debit card")]
        Debit,

        Cash
    }
}