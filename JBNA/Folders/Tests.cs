using JBNA;
using JBNA.Folders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// new FloodFillTests().TestFloodfillF();
new Tests().Test_Function_Converges();
Console.WriteLine("Done");

namespace JBNA.Folders
{
    internal class Tests
    {
        public void Test_Number_Converges()
        {
            int populationSize = 100;
            int maxTime = 11;
            var random = new Random(1);
            var specs = new CistronSpec[] { new CistronSpec { Interpreter = NumberSpec.CreateUniformFloatFactory(0, 10) } };
            var nature = RandomGeneration.CreateRandomHaploidNature(specs, random, add_defaults: false);
            var genome = RandomGeneration.CreateRandomHaploid(nature, random);

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

        public void Test_Number_Converges_With_Defaults()
        {
            int populationSize = 100;
            int maxTime = 1001;
            var random = new Random(1);
            var specs = new CistronSpec[] { new CistronSpec { Interpreter = NumberSpec.CreateUniformFloatFactory(0, 10) } };
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

        public void Test_Function_Converges()
        {
            int populationSize = 10;
            int maxTime = 20;
            var random = new Random(1);
            var specs = new CistronSpec[] { new CistronSpec() { Interpreter = FunctionSpecFactory.CreateFourierFunction() } };
            var nature = RandomGeneration.CreateRandomHaploidNature(specs, random, add_defaults: false);
            var genome = RandomGeneration.CreateRandomHaploid(nature, random);

            var samplingPoints = Enumerable.Range(0, 10).Select(i => (float)(i / (2 * Math.PI))).ToList();
            Func<float, float> realFunction = f => (float)Math.Sin(f);
            float scoreFunction(object?[] cistrons)
            {
                Assert(cistrons.Length == 1);
                Assert(cistrons[0] is Func<float, float>);
                var f = (Func<float, float>)cistrons[0]!;

                float difference = 0;
                foreach (var point in samplingPoints)
                {
                    var prediction = f(point);
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
    }
}
