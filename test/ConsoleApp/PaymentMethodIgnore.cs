using EnumUtilitiesGenerator.Attributes;
using System.ComponentModel;

namespace SourceGeneratorTest.Console
{
    [GenerateHelper(GenerateExtensionOption.IgnoreEnumWithoutDescription)]
    public enum PaymentMethodIgnore
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