namespace JBNA;

/// <param name="SpecIndices">
/// This dictionary is a O(1) way of finding the index of a spec in this.Specs
/// </param>

class OptimizedCodonCollection
{
    public CistronSpec this[TCodon index]
    {
        get
        {
            return this.Specs[index];
        }
    }

    public OptimizedCodonCollection(IEnumerable<CistronSpec> objects, IEnumerable<TCodon> keys)
    {
        this.Specs = keys.Zip(objects).ToReadOnlyDictionary(keySelector: _ => _.First, valueSelector: _ => _.Second);
        this.SpecsReversed = this.Specs.ToDictionary(keySelector: _ => _.Value, elementSelector: _ => _.Key);
        this.SpecIndices = SpecToIndices(Specs.Values);

        CistronsByAllele = this.Specs.Values.ToDictionary(keySelector: c => ((CistronSpec)(object)c).Allele, elementSelector: c => c);
        StartCodons = this.Specs.Keys.ToArray();
    }
    public IReadOnlyDictionary<TCodon, CistronSpec> Specs { get; }
    public IReadOnlyDictionary<CistronSpec, TCodon> SpecsReversed { get; }
    public Dictionary<CistronSpec, int> SpecIndices { get; }
    public Dictionary<Allele, CistronSpec> CistronsByAllele { get; }
    public IReadOnlyList<TCodon> StartCodons { get; }

    private static Dictionary<CistronSpec, int> SpecToIndices(IEnumerable<CistronSpec> specs)
    {
        return specs.Select((item, index) => (item, index))
                    .ToDictionary(t => t.item, t => t.index);
    }
}

public class Nature
{
    public int StartCodonBitCount { get; } = 10;
    public int StopCodonBitCount { get; } = 10;
    public TCodon StopCodon { get; } = 1;



    public ulong MaxCistronLength { get; init; } = ushort.MaxValue;
    public UlongValue SubCistronStopCodon { get; init; } = new(0b00_1000_0001, 10);
    public int PatternLengthBitCount { get; init; } = 8;
    public int FunctionTypeBitCount { get; init; } = 8;
    public int FunctionRangeBitCount { get; init; } = 8;
    public int FunctionTypeCount => this.FunctionFactory.DiscreteFunctionTypeCount;
    public int MinimumNumberOfMutationsPerOffspring { get; init; } = 1;
    public int MinimumNumberOfBitInsertionsPerOffspring { get; init; } = 1;
    public int MinimumNumberOfBitRemovalsPerOffspring { get; init; } = 1;

    /// <summary>
    /// Maps from start codon to alleles and to cistron type, and vice versa.
    /// </summary>
    internal OptimizedCodonCollection Codons { get; }
    public FunctionSpecFactory FunctionFactory { get; private set; /* to enable readonly everything, but with circular loops*/ }
    public Random Random { get; }
    public Nature(IReadOnlyCollection<CistronSpec> objects, Random random)
        : this(objects.Select(obj => (CistronSpec.Builder)obj).ToList(), random)
    {

    }
    public Nature(IReadOnlyCollection<CistronSpec.Builder> objects, Random random)
        : this(objects, random.ManyUnique(drawCount: objects.Count, min: 2, max: 254), random) // TODO think about numbers. // +2 to skip 0 and the StopCodon (=1)
    {
    }

    private Nature(IReadOnlyCollection<CistronSpec.Builder> objects, IReadOnlyList<TCodon> keys, Random random)
    {
        this.Random = random ?? throw new ArgumentNullException(nameof(random));
        this.FunctionFactory = new FunctionSpecFactory(this, value => this.FunctionFactory = value);

        this.Codons = new OptimizedCodonCollection(objects.Select(builder => builder.Build(this)), keys);
        Assert(!this.Codons.Specs.ContainsKey(this.StopCodon));
    }




    /// <summary>
    /// Splits the data into cistrons, by finding all ranges that start with any of the codon starts. The returned ranges exclude the start and stop codons.
    /// </summary>
    public IEnumerable<(TCodon StartCodon, Range Range)> FindAllCodons(BitArray data)
    {
        Assert<NotImplementedException>(data.Length <= int.MaxValue);
        ulong startIndex = 0;
        while (true)
        {
            var (startCodonStartIndex, codonIndex) = data.IndexOfAny(this.Codons.StartCodons, StartCodonBitCount, startIndex: startIndex);
            if (startCodonStartIndex == -1)
                break;

            TCodon codon = this.Codons.StartCodons[codonIndex];
            ulong cistronStartIndex = (ulong)(startCodonStartIndex + this.StartCodonBitCount);
            Assert(StopCodon != 0);
            var stopCodonIndex = data.IndexOfLastConsecutive(this.StopCodon, StartCodonBitCount, cistronStartIndex); // LastConsecutive in case the stop codon is just zeroes, but I'm going to disallow that I think

            if (stopCodonIndex == -1)
            {
                yield return (codon, new Range((int)cistronStartIndex, Index.End));
                break;
            }
            else
            {
                Assert(stopCodonIndex > startCodonStartIndex);
                yield return (codon, new Range((int)cistronStartIndex, (int)stopCodonIndex));
                startIndex = (ulong)stopCodonIndex + (ulong)this.StopCodonBitCount + 1;
            }
        }
    }
    /// <summary>
    /// Splits the data into cistrons, by finding all ranges that start with any of the codon starts. 
    /// The returned ranges exclude the start and stop codons.
    /// Returns only the cistrons of the specified type.
    /// </summary>
    public IEnumerable<(TCodon StartCodon, Range Range)> FindAllCodons(BitArray data, TCodon startCodon)
    {
        return this.FindAllCodons(data).Where(p => p.StartCodon == startCodon);
    }
    /// <summary>
    /// Splits the data into cistrons, by finding all ranges that start with any of the codon starts. The returned ranges exclude the start and stop codons.
    /// </summary>
    [DebuggerHidden]
    public IEnumerable<(CistronSpec Spec, Range Range)> FindAllCistrons(BitArray data)
    {
        return this.FindAllCodons(data).Select([DebuggerHidden] (p) => (this.Codons[p.StartCodon], p.Range));
    }
    /// <summary>
    /// Splits the data into cistrons, by finding all ranges that start with any of the codon starts. 
    /// The returned ranges exclude the start and stop codons.
    /// Returns only the cistrons of the specified type.
    /// </summary>
    public IEnumerable<(CistronSpec Spec, Range Range)> FindAllCistrons(BitArray data, TCodon startCodon)
    {
        return this.FindAllCodons(data, startCodon).Select(p => (this.Codons[p.StartCodon], p.Range));
    }
}

public struct UlongValue
{
    public ulong Value { get; }
    public int Length { get; }

    public UlongValue(ulong value, int length)
    {
        if (length < 0 || length > 64) throw new ArgumentOutOfRangeException(nameof(length));
        if ((value >> length) != 0) throw new ArgumentException(nameof(length));

        this.Value = value;
        this.Length = length;
    }
}
