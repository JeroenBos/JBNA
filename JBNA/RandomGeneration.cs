using JBSnorro;
using JBSnorro.Collections;
using BitArray = JBSnorro.Collections.BitArray;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBNA
{
    internal class RandomGeneration
    {
        public const ulong MaxByteCountForAnything = 10000;
        public static Nature CreateRandomHaploidNature(IReadOnlyList<CistronSpec> specs, Random random, bool add_defaults = true)
        {
            // Add defaults
            var cistronSpecByAlleles = specs.ToDictionary(cistron => cistron.Allele);
            if (add_defaults)
            {
                var defaultCistronSpecs = CistronSpec.Defaults.Where(cistron => !cistronSpecByAlleles.ContainsKey(cistron.Allele));
                specs = specs.Concat(defaultCistronSpecs).ToArray();
            }

            // int minByteCount = specs.Aggregate(0, (s, order) => s + order.Interpreter.MinByteCount);
            // int maxByteCount = specs.Aggregate(0, (s, order) => s + order.Interpreter.MaxByteCount);

            int chromosomeCount = 1 + specs.Count / 20;

            if (chromosomeCount != 1)
                throw new NotImplementedException("Divide cistrons over chromosomes");

            // order in which the cistrons will be placed in the chromosomes
            var order = random.GenerateUniqueRandomNumbers(specs.Count, specs.Count);

            var nature = new Nature(specs, random);
            return nature;
        }
        public static HaploidalGenome CreateRandomHaploid(Nature nature, Random random)
        {
            var genome = new HaploidalGenome(nature, out List<Chromosome> chromosomes);

            var sequencesToInsert = new List<BitArrayReadOnlySegment>();
            foreach (var spec in nature.Objects.Values)
            {
                var initialEncoding = GetInitialEncoding(spec, nature, random);
                sequencesToInsert.Add(initialEncoding);
            }

            ulong nonJunkLength = sequencesToInsert.Aggregate(0UL, (s, array) => s + array.Length);
            ulong totalLength;
            long junkLength;
            if (nature.CistronsByAllele.TryGetValue(Allele.JunkRatio, out CistronSpec? junkJBNARatio))
            {
                var interpreter = (ICistronInterpreter<float>)junkJBNARatio.Interpreter;
                var encodedJunkRatio = interpreter.InitialEncodedValue;
                if (encodedJunkRatio != null)
                {
                    float junkRatio = interpreter.Interpret(encodedJunkRatio);
                    totalLength = Math.Max(nonJunkLength, (ulong)(nonJunkLength / (1 - junkRatio)));
                    junkLength = (long)(totalLength - nonJunkLength);
                }
                else
                {
                    totalLength = nonJunkLength;
                    junkLength = 0;
                }
            }
            else
            {
                totalLength = nonJunkLength;
                junkLength = 0;
            }

            // zip them into chromosomes
            List<ulong> splitIndices = Enumerable.Range(0, sequencesToInsert.Count)
                                               .Select(_ => (ulong)random.NextInt64(junkLength + 1))
                                               .OrderBy(_ => _)
                                               .Select((indexInChromosome, cistronIndex) => indexInChromosome + (cistronIndex == 0 ? 0UL : sequencesToInsert[cistronIndex - 1].Length))
                                               .Scan(0, (junkSkipCount, cumulativeIndexInChromosome) => junkSkipCount + cumulativeIndexInChromosome)
                                               .ToList();


            byte[] chromosome = new byte[totalLength];
            random.NextBytes(chromosome);
            chromosomes.Add(new Chromosome(chromosome, genome.CodonCollection));
#if DEBUG
            BitArray bitArray = new BitArray(chromosome.Length);
#endif
            foreach (var (insertIndex, sequence) in Enumerable.Zip(splitIndices, sequencesToInsert))
            {
#if DEBUG
                for (ulong i = insertIndex; i < insertIndex + sequence.Length; i++)
                    if (bitArray[i])
                        throw new Exception();
                    else
                        bitArray[i] = true;
#endif
                sequence.CopyTo(chromosome, insertIndex);
            }
            return genome;
        }
        private static BitArrayReadOnlySegment GetInitialEncoding(CistronSpec spec, Nature nature, Random random)
        {
            if (!nature.ReverseObjects.TryGetValue(spec, out TCodon startCodon))
                throw new Exception($"No start codon assigned to '{spec.Allele}'");

            BitArrayReadOnlySegment? initialEncoding = spec.Interpreter.InitialEncodedValue;
            if (initialEncoding != null)
            {
                return initialEncoding.Wrap(startCodon, nature.StartCodonBitCount, nature.StopCodon, nature.StopCodonBitCount);
            }
            else
            {
                var cistronMaxLength = (long)Math.Min(spec.Interpreter.MaxBitCount - spec.Interpreter.MinBitCount, MaxByteCountForAnything);
                var cistronLength = spec.Interpreter.MinBitCount + (ulong)random.NextInt64(cistronMaxLength);
                
                var result = BitArray.InitializeRandom(cistronLength + (ulong)nature.StartCodonBitCount + (ulong)nature.StopCodonBitCount, random);
                result.Set(startCodon, nature.StartCodonBitCount, 0);
                result.Set(nature.StopCodon, checked((int)(cistronLength - (ulong)nature.StopCodonBitCount)), 0);
                return result[Range.All];
            }
        }
    }

    public class GenomeInviableException : Exception
    {
        public GenomeInviableException() : base() { }
        public GenomeInviableException(string message) : base(message) { }
        public GenomeInviableException(string message, Exception innerException) : base(message, innerException) { }
    }


}

