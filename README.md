# EnumUtilitiesGenerator

A source generator for C# that uses Roslyn to create a small helper class for your Enums, with helpfull mapping between an enum member and its description attribute. By using it you avoid using reflection or boilerplate code to map enums and descriptions.

# Installation

Install the generator via nuget:

`Install-Package EnumUtilitiesGenerator -Version 0.1.2`

# How to use it

Add the attribute GenerateHelper to the enums you want to map members and descriptions, like so:

```csharp
[GenerateHelper(GenerateHelperOption.UseItselfWhenNoDescription)]
public enum PaymentMethod
{
    [Description("Credit card")]
    Credit,
    [Description("Debit card")]
    Debit,
    Cash
}
```

That is enough to generate a helper class with 2 methods with compile-time mapping, for each enum found in the consuming project.