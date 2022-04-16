using JBNA.Tests;
using Xunit;

// new FloodFillTests().TestFloodfillF();
new IntegrationTests().CanApproximateSineWave();
Console.WriteLine("Done");

namespace JBNA.Tests
{
    public class IntegrationTests
    {
        [Fact]
        public void Test_Number_Converges()
        {
            int populationSize = 100;
            int maxTime = 11;
            var specs = new CistronSpec.Builder[] { new CistronSpec(Allele.CustomRangeStart, interpreter: NumberSpec.CreateUniformFloatFactory(0, 10)) };
            var nature = RandomGeneration.CreateRandomHaploidNature(specs, new Random(1), add_defaults: false);

            float scoreFunction(object?[] cistrons)
            {
                Assert(cistrons.Length == 1, "Defaults infiltrated");
                Assert(cistrons[0] is float);

                return 10f - (float)cistrons[0]!;
            }

            var evolution = new Evolution<Chromosome>(nature, RandomGeneration.CreateRandomHaploid, scoreFunction, populationSize);
            var scores = evolution.Evolve(maxTime);

            Assert(scores[^1] > 9.5);
        }

        [Fact]
        public void Test_Number_Converges_With_Defaults()
        {
            int populationSize = 100;
            int maxTime = 10;
            var specs = new CistronSpec.Builder[] { new CistronSpec(Allele.CustomRangeStart, interpreter: NumberSpec.CreateUniformFloatFactory(0, 10)) };
            var nature = RandomGeneration.CreateRandomHaploidNature(specs, new Random(1), add_defaults: true);

            float scoreFunction(object?[] cistrons)
            {
                Assert(cistrons.Length >= 1, "Defaults forgotten");
                Assert(cistrons[0] is float);

                return 10f - (float)cistrons[0]!;
            }

            var evolution = new Evolution<Chromosome>(nature, RandomGeneration.CreateRandomHaploid, scoreFunction, populationSize);
            var scores = evolution.Evolve(maxTime);

            Assert(scores[^1] > 9.5);
        }

        [Fact]
        public void Test_Function_Converges()
        {
            int populationSize = 10;
            int maxTime = 20;
            var specs = new CistronSpec.Builder[] { new CistronSpec.Builder(Allele.CustomRangeStart, GetInterpreter: (Nature nature) => nature.FunctionFactory.FourierFunctionInterpreter) };
            var nature = RandomGeneration.CreateRandomHaploidNature(specs, new Random(1), add_defaults: false);

            var samplingPoints = Enumerable.Range(0, 10).Select(i => (float)(i / (2 * Math.PI))).ToList();
            Func<float, float> realFunction = f => (float)Math.Sin(f);
            float scoreFunction(object?[] interpretedCistrons)
            {
                Assert(interpretedCistrons.Length == 1);
                Assert(interpretedCistrons[0] is IDimensionfulContinuousFunction);
                var f = (IDimensionfulContinuousFunction)interpretedCistrons[0]!;

                float difference = 0;
                foreach (var point in samplingPoints)
                {
                    var prediction = f.Invoke(new OneDimensionalContinuousQuantity(point, Length: 1));
                    var real = realFunction(point);
                    var diff = Math.Abs(prediction - real);
                    difference += diff;
                }
                return 100 - difference; // because higher is better
            }

            var evolution = new Evolution<Chromosome>(nature, RandomGeneration.CreateRandomHaploid, scoreFunction, populationSize);
            var scores = evolution.Evolve(maxTime);

            Assert(scores[^1] > 9.5);
        }
        [Fact]
        public void Test_Bits_Can_Be_Inserted_at_the_end_of_a_chromosome()
        {
            var random = new Random(1);
            var nature = new Nature(Array.Empty<CistronSpec>(), random)
            {
                MinimumNumberOfMutationsPerOffspring = 0,
                MinimumNumberOfBitInsertionsPerOffspring = 0,
                MinimumNumberOfBitRemovalsPerOffspring = 0,
            };

            for (int i = 0; i < 100; i++)
            {
                var chromosome = new Chromosome(new BitArray(new bool[] { false, false }), nature);
                chromosome.Mutate(mutationRates, random);
                var output = new BitArray(length: 64);
                chromosome.CopyTo(output, 0, chromosome.Length, 0);
                if (output[0..3].Equals(new BitArray(new bool[] { false, false, true })))
                {
                    return;
                }
            }

            static object? mutationRates(Allele allele)
            {
                const float length = 2;
                return allele switch
                {
                    Allele.BitInsertionRate => 1f / length,
                    Allele.BitInsertionRateStdDev => 0f,
                    Allele.DefaultBitMutationRate => 0f,
                    Allele.DefaultBitMutationRateStdDev => 0f,
                    _ => null
                };
            }


            Assert(false, "No attempt succeeded");
        }

        [Fact]
        public void CanApproximateSineWave()
        {
            Func<double, double> realFunction = Math.Sin;
            const int Length = 100;
            const int maxTime = 10000;
            const int populationSize = 100;


            var spec = new CistronSpec.Builder(Allele.CustomRangeStart, _ => HistogramFunction.ScalingFunctionInterpreter);
            var nature = RandomGeneration.CreateRandomHaploidNature(spec.ToSingletonList(), new Random(1), add_defaults: false);
            float scoreFunction(IDimensionfulDiscreteFunction f)
            {
                var samplingPoints = Enumerable.Range(0, Length);
                float difference = 0;
                foreach (var point in samplingPoints)
                {
                    var prediction = f.Invoke(new OneDimensionalDiscreteQuantity(point, Length));
                    var real = (float)realFunction(point);
                    var diff = Math.Abs(prediction - real);
                    difference += diff;
                }
                var score = -difference; // because higher is better
                return score;
            }
            var evolution = new Evolution<Chromosome>(nature, RandomGeneration.CreateRandomHaploid, CastScoreFunction<IDimensionfulDiscreteFunction>(scoreFunction), populationSize);

            var scores = evolution.Evolve(maxTime);
            var finalScore = scores[^1];
            Assert(finalScore < 0, "Something is wrong");
            Assert(finalScore > -10, "It did not perform well");
        }

        static Func<object?[], float> CastScoreFunction<T>(Func<T, float> singleElementScoreFunction)
        {
            return wrappedScoreFunction;
            float wrappedScoreFunction(object?[] interpretedCistrons)
            {

                Assert(interpretedCistrons.Length == 1);
                Assert(interpretedCistrons[0] is T);
                var t = (T)interpretedCistrons[0]!;

                return singleElementScoreFunction(t);
            }
        }
    }
}