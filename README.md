# EnumUtilitiesGenerator

A source generator for C# that uses Roslyn to create a small helper class for your Enums, with helpfull mapping between an enum member and its description attribute. By using it you avoid using reflection or boilerplate code to map enums and descriptions.

# Installation

Install the generator via nuget:

`Install-Package EnumUtilitiesGenerator -Version 0.1.6`

# Benchmark

To prove the performance benefit, some benchmarks were made and compared. Since enums with few and lots of members behave differently, those scenarios were covered.
Scenarios: 
- A simple enumMember.ToString()
- Obtaining the DescriptionAttribute value
- Obtaining the enum from a given DescriptionAttribute value

## Results:
```
|                                            Method |           Mean |         Error |        StdDev |         Median |  Gen 0 |  Gen 1 | Allocated |
|-------------------------------------------------- |---------------:|--------------:|--------------:|---------------:|-------:|-------:|----------:|
|                    ToString_Native_With_5_Members |      77.695 ns |     7.4842 ns |    22.0672 ns |      85.540 ns | 0.0057 |      - |      24 B |
|        GetDesriptionFast_Generated_With_5_Members |       2.099 ns |     0.1701 ns |     0.4824 ns |       2.297 ns |      - |      - |         - |
|                  ToString_Native_With_100_Members |      28.546 ns |     0.5210 ns |     0.8847 ns |      28.761 ns | 0.0057 | 0.0003 |      24 B |
|      GetDesriptionFast_Generated_With_100_Members |       2.434 ns |     0.0725 ns |     0.0566 ns |       2.433 ns |      - |      - |         - |
|   GetDesriptionFromEnum_Reflection_With_5_Members |   1,327.510 ns |    10.4239 ns |     9.2405 ns |   1,329.483 ns | 0.0610 |      - |     256 B |
|    GetDesriptionFromEnum_Generator_With_5_Members |       2.034 ns |     0.1203 ns |     0.2260 ns |       2.064 ns |      - |      - |         - |
| GetDesriptionFromEnum_Reflection_With_100_Members |   1,381.913 ns |    27.5457 ns |    72.0823 ns |   1,392.869 ns | 0.0610 |      - |     256 B |
|  GetDesriptionFromEnum_Generator_With_100_Members |       2.617 ns |     0.0500 ns |     0.0443 ns |       2.616 ns |      - |      - |         - |
|   GetEnumFromDesription_Reflection_With_5_Members |   8,545.634 ns |   140.6773 ns |   162.0042 ns |   8,483.183 ns | 0.3510 |      - |   1,496 B |
|    GetEnumFromDesription_Generated_With_5_Members |     320.504 ns |     4.4171 ns |     3.9157 ns |     321.430 ns |      - |      - |         - |
| GetEnumFromDesription_Reflection_With_100_Members | 105,295.058 ns | 2,094.0809 ns | 5,176.0491 ns | 106,267.151 ns | 4.3945 |      - |  18,498 B |
|  GetEnumFromDesription_Generated_With_100_Members |   4,749.037 ns |    66.6945 ns |    62.3861 ns |   4,729.669 ns |      - |      - |         - |
```

It's very clear how the generated code is faster and also does not pressure the garbage collector.

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

That will generate a helper class with 2 methods with compile-time mapping, for each enum found in the consuming project with the GenerateHelper attribute, and an extra method to return all available descriptions.

The generated code:

```csharp
public static class PaymentMethodHelper
{
    private static readonly string[] _descriptions = new string[]
    {
        "Credit card",
        "Debit card",
    };

    public static string[] GetAvailableDescriptions()
    {
        return _descriptions;
    }

    public static string GetDescriptionFast(this PaymentMethod @enum)
    {
        return @enum switch
        {
            PaymentMethod.Credit => "Credit card",
            PaymentMethod.Debit => "Debit card",
            PaymentMethod.Cash => nameof(PaymentMethod.Cash)
        };
    }

    public static PaymentMethod? GetEnumFromDescriptionFast(string description)
    {
        return GetEnumFromDescriptionFast(description, StringComparison.InvariantCultureIgnoreCase);
    }

    public static PaymentMethod? GetEnumFromDescriptionFast(string description, StringComparison stringComparison)
    {
        return description switch
        {
            _ when string.Equals("Credit card", description, stringComparison) => PaymentMethod.Credit,
            _ when string.Equals("Debit card", description, stringComparison) => PaymentMethod.Debit,
            _ when string.Equals("Cash", description, stringComparison) => PaymentMethod.Cash,
            _ => null
        };
    }
}
```

# Behaviour

Each member will be mapped to and from its Description value. Members without the attribute or with an empty attribute will map according to the option chosen:

- IgnoreEnumWithoutDescription: Returns null
- ThrowForEnumWithoutDescription: throws an InvalidOperationException.
- UseItselfWhenNoDescription: will map using the member name. Equivalent to nameof(EnumType.MemberX) or EnumType.MemberX.ToString().

