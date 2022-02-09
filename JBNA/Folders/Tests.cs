using JBNA;
using JBNA.Folders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

Assert(DiploidalGenome.DefaultCrossoverRates[0] == 0.3f);
new Tests().Test();

namespace JBNA.Folders
{
    internal class Tests
    {
        public void Test()
        {
            int populationSize = 100;
            int maxTime = 10;
            var random = new Random(1);
            var specs = new[] { NumberSpec.CreateUniformFloatFactory(required: true, 0, 10) };
            var nature = RandomGeneration.CreateRandomHaploidNature(specs, random);
            var genome = RandomGeneration.CreateRandomHaploid(nature, random);

            float scoreFunction(object?[] cistrons)
            {
                Assert(cistrons.Length == 1);
                Assert(cistrons[0] is float);

                return 10f - (float)cistrons[0]!;
            }

            var evolution = new Evolution<Chromosome>(nature, RandomGeneration.CreateRandomHaploid, scoreFunction, populationSize, random: random);
            var finalScore = evolution.Evolve(maxTime);

            Assert(finalScore[0] > 9.5);
        }
    }
}
