using BenchmarkDotNet.Attributes;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class Benchmarker
    {
        [Benchmark]
        public string ToString_Native_With_5_Members()
        {
            return FewMembersEnum.Test05.ToString();
        }

        [Benchmark]
        public string ToString_Native_With_100_Members()
        {
            return ManyMembersEnum.Test75.ToString();
        }

        [Benchmark]
        public string GetDesriptionFast_Generated_With_5_Members()
        {
            return FewMembersEnum.Test05.GetDescriptionFast();
        }

        [Benchmark]
        public string GetDesriptionFast_Generated_With_100_Members()
        {
            return ManyMembersEnum.Test75.GetDescriptionFast();
        }

        [Benchmark]
        public string GetDesriptionFromEnum_Reflection_With_5_Members()
        {
            return EnumUtils.GetDescriptionFromEnum(FewMembersEnum.Test05);
        }

        [Benchmark]
        public string GetDesriptionFromEnum_Reflection_With_100_Members()
        {
            return EnumUtils.GetDescriptionFromEnum(ManyMembersEnum.Test75);
        }

        [Benchmark]
        public string GetDesriptionFromEnum_Generator_With_5_Members()
        {
            return FewMembersEnum.Test05.GetDescriptionFast();
        }

        [Benchmark]
        public string GetDesriptionFromEnum_Generator_With_100_Members()
        {
            return ManyMembersEnum.Test75.GetDescriptionFast();
        }

        [Benchmark]
        public FewMembersEnum GetEnumFromDesription_Reflection_With_5_Members()
        {
            return EnumUtils.GetEnumFromDescription<FewMembersEnum>("Teste05");
        }

        [Benchmark]
        public ManyMembersEnum GetEnumFromDesription_Reflection_With_100_Members()
        {
            return EnumUtils.GetEnumFromDescription<ManyMembersEnum>("Teste75");
        }

        [Benchmark]
        public FewMembersEnum? GetEnumFromDesription_Generated_With_5_Members()
        {
            return FewMembersEnumHelper.GetEnumFromDescriptionFast("Teste05");
        }

        [Benchmark]
        public ManyMembersEnum? GetEnumFromDesription_Generated_With_100_Members()
        {
            return ManyMembersEnumHelper.GetEnumFromDescriptionFast("Teste75");
        }
    }
}
