using JBSnorro;
using JBNA;
using JBNA.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static JBSnorro.Diagnostics.Contract;
using JBSnorro.Collections;

// new FloodFillTests().TestFloodfillF();
new IntegrationTests().Test_Function_Converges();
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
            var random = new Random(1);
            var specs = new[] { new CistronSpec(Allele.CustomRangeStart, interpreter: NumberSpec.CreateUniformFloatFactory(0, 10)) };
            var nature = RandomGeneration.CreateRandomHaploidNature(specs, random, add_defaults: false);

            float scoreFunction(object?[] cistrons)
            {
                Assert(cistrons.Length == 1, "Defaults infiltrated");
                Assert(cistrons[0] is float);

                return 10f - (float)cistrons[0]!;
            }

            var evolution = new Evolution<Chromosome>(nature, RandomGeneration.CreateRandomHaploid, scoreFunction, populationSize, random: random);
            var finalScore = evolution.Evolve(maxTime);

            Assert(finalScore[0] > 9.5);
        }

        [Fact]
        public void Test_Number_Converges_With_Defaults()
        {
            int populationSize = 100;
            int maxTime = 10;
            var random = new Random(1);
            var specs = new[] { new CistronSpec(Allele.CustomRangeStart, interpreter: NumberSpec.CreateUniformFloatFactory(0, 10)) };
            var nature = RandomGeneration.CreateRandomHaploidNature(specs, random, add_defaults: true);
            var genome = RandomGeneration.CreateRandomHaploid(nature, random);

            float scoreFunction(object?[] cistrons)
            {
                Assert(cistrons.Length >= 1, "Defaults forgotten");
                Assert(cistrons[0] is float);

                return 10f - (float)cistrons[0]!;
            }

            var evolution = new Evolution<Chromosome>(nature, RandomGeneration.CreateRandomHaploid, scoreFunction, populationSize, random: random);
            var finalScore = evolution.Evolve(maxTime);

            Assert(finalScore[0] > 9.5);
        }

        [Fact]
        public void Test_Function_Converges()
        {
            int populationSize = 10;
            int maxTime = 20;
            var random = new Random(1);
            var specs = new[] { new CistronSpec(Allele.CustomRangeStart, interpreter: FunctionSpecFactory.FourierFunctionInterpreter) };
            var nature = RandomGeneration.CreateRandomHaploidNature(specs, random, add_defaults: false);
            var genome = RandomGeneration.CreateRandomHaploid(nature, random);

            var samplingPoints = Enumerable.Range(0, 10).Select(i => (float)(i / (2 * Math.PI))).ToList();
            Func<float, float> realFunction = f => (float)Math.Sin(f);
            float scoreFunction(object?[] interpretedCistrons)
            {
                Assert(interpretedCistrons.Length == 1);
                Assert(interpretedCistrons[0] is DimensionfulContinuousFunction);
                var f = (DimensionfulContinuousFunction)interpretedCistrons[0]!;

                float difference = 0;
                foreach (var point in samplingPoints)
                {
                    var prediction = f(new OneDimensionalContinuousQuantity(point, Length: 1));
                    var real = realFunction(point);
                    var diff = Math.Abs(prediction - real);
                    difference += diff;
                }
                return 100 - difference; // because higher is better
            }

            var evolution = new Evolution<Chromosome>(nature, RandomGeneration.CreateRandomHaploid, scoreFunction, populationSize, random: random);
            var finalScore = evolution.Evolve(maxTime);

            Assert(finalScore[0] > 9.5);
        }
        [Fact]
        public void Test_Bits_Can_Be_Inserted_at_the_end_of_a_chromosome()
        {
            var random = new Random();
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
    }
}