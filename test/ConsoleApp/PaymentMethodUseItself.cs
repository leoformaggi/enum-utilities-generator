using System.ComponentModel;

namespace SourceGeneratorTest.Console
{
    [GenerateHelper(GenerateHelperOption.UseItselfWhenNoDescription)]
    public enum PaymentMethodUseItself
    {
        [Description("Credit card")]
        Credit,

        [Description("Debit card")]
        Debit,

        Cash
    }
}