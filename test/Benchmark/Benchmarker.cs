using BenchmarkDotNet.Attributes;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class Benchmarker
    {
        [Benchmark]
        public string ToString_Native_With_5_Members()
        {
            FewMembersEnum sorteado = FewMembersEnum.Test05;
            return sorteado.ToString();
        }

        [Benchmark]
        public string ToString_Native_With_100_Members()
        {
            ManyMembersEnum sorteado = ManyMembersEnum.Test75;
            return sorteado.ToString();
        }

        [Benchmark]
        public string GetDesriptionFast_Generated_With_5_Members()
        {
            FewMembersEnum sorteado = FewMembersEnum.Test05;
            return sorteado.GetDescriptionFast();
        }

        [Benchmark]
        public string GetDesriptionFast_Generated_With_100_Members()
        {
            ManyMembersEnum sorteado = ManyMembersEnum.Test75;
            return sorteado.GetDescriptionFast();
        }

        [Benchmark]
        public string GetDesriptionFromEnum_Reflection_With_5_Members()
        {
            FewMembersEnum sorteado = FewMembersEnum.Test05;
            return EnumUtils.GetDescriptionFromEnum(sorteado);
        }

        [Benchmark]
        public string GetDesriptionFromEnum_Reflection_With_100_Members()
        {
            ManyMembersEnum sorteado = ManyMembersEnum.Test75;
            return EnumUtils.GetDescriptionFromEnum(sorteado);
        }

        [Benchmark]
        public string GetDesriptionFromEnum_Generator_With_5_Members()
        {
            FewMembersEnum sorteado = FewMembersEnum.Test05;
            return sorteado.GetDescriptionFast();
        }

        [Benchmark]
        public string GetDesriptionFromEnum_Generator_With_100_Members()
        {
            ManyMembersEnum sorteado = ManyMembersEnum.Test75;
            return sorteado.GetDescriptionFast();
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
