using EnumUtilitiesGenerator.Attributes;
using System.ComponentModel;

namespace SourceGeneratorTest.Console
{
    [GenerateHelper(GenerateExtensionOption.UseItselfWhenNoDescription)]
    public enum PaymentMethodUseItself
    {
        [Description("Cartão de crédito")]
        Credit,
        [Description("Pix")]
        Pix,

        [Description]
        Debit,

        Boleto,
        Dinheiro
    }
}