namespace JBNA;

internal class RandomGeneration
{
    public const ulong MaxByteCountForAnything = 10000;
    public static Nature CreateRandomHaploidNature(IReadOnlyList<CistronSpec.Builder> specs, Random random, bool add_defaults = true)
    {
        // Add defaults
        var cistronSpecByAlleles = specs.ToDictionary(cistron => cistron.Allele);
        if (add_defaults)
        {
            var defaultCistronSpecs = CistronSpec.Defaults.Where(cistron => !cistronSpecByAlleles.ContainsKey(cistron.Allele))
                                                          .Select(spec => (CistronSpec.Builder)spec);
            specs = specs.Concat(defaultCistronSpecs).ToArray();
        }

        // int minByteCount = specs.Aggregate(0, (s, order) => s + order.Interpreter.MinByteCount);
        // int maxByteCount = specs.Aggregate(0, (s, order) => s + order.Interpreter.MaxByteCount);

        int chromosomeCount = 1 + specs.Count / 20;

        if (chromosomeCount != 1)
            throw new NotImplementedException("Divide cistrons over chromosomes");

        // order in which the cistrons will be placed in the chromosomes
        var order = random.ManyUnique(specs.Count, specs.Count);

        var nature = new Nature(specs, random);
        return nature;
    }
    public static HaploidalGenome CreateRandomHaploid(Nature nature)
    {
        var genome = new HaploidalGenome(nature, out List<Chromosome> chromosomes);

        var sequencesToInsert = nature.Codons.Specs.Values
                                                     .Select(spec => GetInitialEncoding(spec, nature, nature.Random))
                                                     .ToList();

        ulong nonJunkLength = sequencesToInsert.Aggregate(0UL, (s, array) => s + array.Length);
        long junkLength = getJunkLength(nature, nonJunkLength);
        ulong totalLength = (ulong)junkLength + nonJunkLength;


        // zip them into chromosomes
        var insertionIndices = getInsertionIndices(sequencesToInsert, junkLength, nature.Random);


        var chromosomeData = BitArray.InitializeRandom(totalLength, nature.Random); // the junk
        foreach (var (insertionIndex, sequence) in Enumerable.Zip(insertionIndices, sequencesToInsert))
        {
            sequence.CopyTo(chromosomeData, insertionIndex);
        }
        chromosomes.Add(new Chromosome(chromosomeData, genome.Nature));
        return genome;


        static long getJunkLength(Nature nature, ulong nonJunkLength)
        {
            if (nature.Codons.CistronsByAllele.TryGetValue(Allele.JunkRatio, out CistronSpec? junkJBNARatio))
            {
                var interpreter = (ICistronInterpreter<float>)junkJBNARatio.Interpreter;
                var encodedJunkRatio = interpreter.InitialEncodedValue;
                if (encodedJunkRatio != null)
                {
                    float junkRatio = interpreter.Interpret(encodedJunkRatio.ToBitReader());
                    var totalLength = Math.Max(nonJunkLength, (ulong)(nonJunkLength / (1 - junkRatio)));
                    var junkLength = (long)(totalLength - nonJunkLength);
                    return junkLength;
                }
            }
            return 0;
        }

        static IEnumerable<ulong> getInsertionIndices(List<BitArrayReadOnlySegment> sequencesToInsert, long junkLength, Random random)
        {
            return Enumerable.Range(0, sequencesToInsert.Count)
                             .Select(_ => (ulong)random.NextInt64(junkLength + 1))
                             .OrderBy(_ => _)
                             .Select((junkSkipCount, cistronIndex) => junkSkipCount + (cistronIndex == 0 ? 0UL : sequencesToInsert[cistronIndex - 1].Length))
                             .Scan(0UL, (junkSkipCount, indexInChromosome) => indexInChromosome + junkSkipCount);
        }
    }
    private static BitArrayReadOnlySegment GetInitialEncoding(CistronSpec spec, Nature nature, Random random)
    {
        if (!nature.Codons.SpecsReversed.TryGetValue(spec, out TCodon startCodon))
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
            result.Set(nature.StopCodon, nature.StopCodonBitCount, result.Length - (ulong)nature.StopCodonBitCount);
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



