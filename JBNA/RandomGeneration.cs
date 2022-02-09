using System;
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
        public const int MaxByteCountForAnything = 10000;
        public static ReadOnlyStartCodonCollection<CistronSpec_LNCE> CreateRandomHaploidNature(IReadOnlyList<CistronSpec_LNCE> specs, Random random, bool add_defaults = true)
        {
            // Add defaults
            var cistronSpecByAlleles = specs.ToDictionary(cistron => cistron.Allele);
            if (add_defaults)
            {
                var defaultCistronSpecs = CistronSpec_LNCE.Defaults.Where(cistron => !cistronSpecByAlleles.ContainsKey(cistron.Allele));
                specs = specs.Concat(defaultCistronSpecs).ToArray();
            }

            int minByteCount = specs.Aggregate(0, (s, order) => s + order.Spec.MinByteCount);
            int maxByteCount = specs.Aggregate(0, (s, order) => s + order.Spec.MaxByteCount);

            int chromosomeCount = 1 + specs.Count / 20;

            if (chromosomeCount != 1)
                throw new NotImplementedException("Divide cistrons over chromosomes");

            // order in which the cistrons will be placed in the chromosomes
            var order = random.GenerateUniqueRandomNumbers(specs.Count, specs.Count);

            var nature = new ReadOnlyStartCodonCollection<CistronSpec_LNCE>(specs, random);
            return nature;
        }
        public static HaploidalGenome CreateRandomHaploid(ReadOnlyStartCodonCollection<CistronSpec_LNCE> nature, Random random)
        {
            var genome = new HaploidalGenome(nature, out List<Chromosome> chromosomes);

            var sequencesToInsert = new List<byte[]>();
            TCodon stopCodon = genome.CodonCollection.StopCodon;
            foreach (var spec in nature.Objects.Values)
            {
                if (!genome.CodonCollection.ReverseObjects.TryGetValue(spec, out TCodon startCodon))
                    throw new Exception($"No start codon assigned to '{spec.Allele}'");

                ReadOnlyCollection<byte>? initialEncoding = spec.Spec.Interpreter.InitialEncodedValue;
                byte[] encoding;
                if (initialEncoding != null)
                {
                    encoding = new byte[1 + initialEncoding.Count + 1];
                    initialEncoding.CopyTo(encoding, 1);
                }
                else
                {
                    int lengthRange = Math.Min(spec.Spec.MaxByteCount - spec.Spec.MinByteCount, MaxByteCountForAnything);
                    int cistronLength = spec.Spec.MinByteCount + random.Next(lengthRange);
                    encoding = new byte[1 + cistronLength + 1];
                    for (int i = 1; i < encoding.Length - 1; i++)
                    {
                        encoding[i] = (byte)random.Next(256);
                    }
                }
                encoding[0] = startCodon;
                encoding[encoding.Length - 1] = stopCodon;
                sequencesToInsert.Add(encoding);
            }

            int nonJunkLength = sequencesToInsert.Aggregate(0, (s, array) => s + array.Length);
            int totalLength;
            int junkLength;
            if (nature.CistronsByAllele.TryGetValue(Allele.JunkRatio, out CistronSpec_LNCE? junkJBNARatio))
            {
                Assert(junkJBNARatio is ICistronSpec<float>);
                var interpreter = ((ICistronSpec<float>)junkJBNARatio).Interpreter;
                ReadOnlyCollection<byte>? encodedJunkRatio = interpreter.InitialEncodedValue;
                if (encodedJunkRatio != null)
                {
                    float junkRatio = interpreter.Create(encodedJunkRatio.ToArray().AsSpan());
                    totalLength = Math.Max(nonJunkLength, (int)(nonJunkLength / (1 - junkRatio)));
                    junkLength = totalLength - nonJunkLength;
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
            List<int> splitIndices = Enumerable.Range(0, sequencesToInsert.Count)
                                   .Select(_ => random.Next(junkLength + 1))
                                   .OrderBy(_ => _)
                                   .Select((indexInChromosome, cistronIndex) => indexInChromosome + (cistronIndex == 0 ? 0 : sequencesToInsert[cistronIndex - 1].Length))
                                   .ToList();


            byte[] chromosome = new byte[totalLength];
            random.NextBytes(chromosome);
            chromosomes.Add(new Chromosome(chromosome, genome.CodonCollection));

            foreach (var (insertIndex, sequence) in splitIndices.Zip(sequencesToInsert))
            {
                sequence.CopyTo(chromosome, insertIndex);
            }
            return genome;
        }
    }

    public class GenomeInviableException : Exception
    {
        public GenomeInviableException() : base() { }
        public GenomeInviableException(string message) : base(message) { }
        public GenomeInviableException(string message, Exception innerException) : base(message, innerException) { }
    }


}

