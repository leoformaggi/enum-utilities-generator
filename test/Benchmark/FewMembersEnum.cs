using System.ComponentModel;

namespace Benchmark
{
    [GenerateHelper(GenerateHelperOption.UseItselfWhenNoDescription)]
    public enum FewMembersEnum
    {
        [Description("Test00")] Test00,
        [Description("Test01")] Test01,
        [Description("Test02")] Test02,
        [Description("Test03")] Test03,
        [Description("Test04")] Test04,
        [Description("Test05")] Test05
    }
}
