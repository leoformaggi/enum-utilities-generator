using EnumUtilitiesGenerator.Attributes;
using System.ComponentModel;

namespace SourceGeneratorTest.Console
{
    [GenerateHelper(GenerateExtensionOption.ThrowForEnumWithoutDescription)]
    public enum PaymentMethodThrow
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