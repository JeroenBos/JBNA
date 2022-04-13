using JBSnorro;
using System.Collections.Immutable;
using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using static JBSnorro.Diagnostics.Contract;

namespace JBNA;

public class ReadOnlyStartCodonCollection<T> where T : notnull
{
    public int StartCodonBitCount { get; }
    public int StopCodonBitCount { get; }
    public TCodon StopCodon { get; }
    /// <summary>
    /// Map from start codon to cistron type.
    /// </summary>
    public IReadOnlyDictionary<TCodon, T> Objects { get; }
    public IReadOnlyDictionary<T, TCodon> ReverseObjects { get; }
    /// <summary>
    /// This dictionary is a O(1) way of finding the index of a spec in this.Specs
    /// </summary>
    public Dictionary<T, int> SpecIndices { get; }

    public Dictionary<Allele, T> CistronsByAllele { get; }
    public IReadOnlyList<TCodon> StartCodons { get; }


    private static Dictionary<T, int> SpecToIndices(IEnumerable<T> specs)
    {
        return Enumerable.Zip(specs, Enumerable.Range(0, int.MaxValue)).ToDictionary(t => t.First, t => t.Second);
    }
    private static IReadOnlyList<TCodon> CreateRandomKeys(int count, Random random)
    {
        return random.GenerateUniqueRandomNumbers(drawCount: count, max: 254);
    }
    public ReadOnlyStartCodonCollection(IReadOnlyCollection<T> objects, Random random) : this(objects, CreateRandomKeys(objects.Count, random))
    {
    }
    private ReadOnlyStartCodonCollection(IReadOnlyCollection<T> objects, IReadOnlyList<TCodon> keys)
    {
        Assert(objects.Count < 253);
        StartCodonBitCount = 16;
        Assert(!keys.Contains((TCodon)255));

        var dict = new Dictionary<TCodon, T>(objects.Count);
        int i = 0;
        foreach (var obj in objects)
        {
            TCodon key;
            checked
            {
                key = (TCodon)(keys[i] + 1); // + 1 to skip StopCodon
            }
            dict.Add(key, obj);
            i++;
        }
        this.Objects = dict;
        this.StopCodon = 0;
        Assert(!dict.ContainsKey(this.StopCodon));
        this.ReverseObjects = this.Objects.ToDictionary(keySelector: _ => _.Value, elementSelector: _ => _.Key);
        this.SpecIndices = SpecToIndices(objects);

        CistronsByAllele = this.Objects.Values.ToDictionary(keySelector: c => ((CistronSpec)(object)c).Allele, elementSelector: c => c);
        StartCodons = this.Objects.Keys.ToArray();
    }


    /// <summary>
    /// Splits the data into cistrons, by finding all ranges that start with any of the codon starts. The returned ranges exclude the start and stop codons.
    /// </summary>
    public IEnumerable<KeyValuePair<TCodon, Range>> FindAllCodons(BitArray data)
    {
        Contract.Assert<NotImplementedException>(data.Length >= int.MaxValue);
        ulong index = 0;
        while (true)
        {
            var (startCodonStartIndex, codonIndex) = data.IndexOfAny(this.StartCodons, StartCodonBitCount, startIndex: index);
            if (startCodonStartIndex == -1)
                break;

            TCodon codon = this.StartCodons[codonIndex];
            ulong cistronStartIndex = (ulong)(startCodonStartIndex + this.StartCodonBitCount);
            var stopCodonIndex = data.IndexOf(this.StopCodon, StartCodonBitCount, cistronStartIndex);

            if (stopCodonIndex == -1)
            {
                yield return KeyValuePair.Create(codon, new Range((int)cistronStartIndex, Index.End));
                break;
            }
            else
            {
                yield return KeyValuePair.Create(codon, new Range((int)cistronStartIndex, (int)stopCodonIndex));
                index = (ulong)stopCodonIndex + (ulong)this.StopCodonBitCount;
            }
        }
    }
    /// <summary>
    /// Splits the data into cistrons, by finding all ranges that start with any of the codon starts. 
    /// The returned ranges exclude the start and stop codons.
    /// Returns only the cistrons of the specified type.
    /// </summary>
    public IEnumerable<KeyValuePair<TCodon, Range>> FindAllCodons(BitArray data, TCodon startCodon)
    {
        return this.FindAllCodons(data).Where(p => p.Key == startCodon);
    }
    /// <summary>
    /// Splits the data into cistrons, by finding all ranges that start with any of the codon starts. The returned ranges exclude the start and stop codons.
    /// </summary>
    public IEnumerable<KeyValuePair<T, Range>> FindAllCistrons(BitArray data)
    {
        return this.FindAllCodons(data).Select(p => KeyValuePair.Create(Objects[p.Key], p.Value));
    }
    /// <summary>
    /// Splits the data into cistrons, by finding all ranges that start with any of the codon starts. 
    /// The returned ranges exclude the start and stop codons.
    /// Returns only the cistrons of the specified type.
    /// </summary>
    public IEnumerable<KeyValuePair<T, Range>> FindAllCistrons(BitArray data, TCodon startCodon)
    {
        return this.FindAllCodons(data, startCodon).Select(p => KeyValuePair.Create(Objects[p.Key], p.Value));
    }
}

public class Nature : ReadOnlyStartCodonCollection<CistronSpec>
{
    public ulong MaxCistronLength => ushort.MaxValue; // TODO: implement. is not used everywhere yet
    public UlongValue SubCistronStopCodon => new(0b1000_0001, 8);

    // public ICistronInterpreter Pattern1DInterpreter = new CistronInterpreter();
    public int PatternLengthBitCount => 8;
    public int FunctionTypeBitCount => 8;
    public int FunctionRangeBitCount => 8;
    public int FunctionTypeCount => FunctionSpecFactory.FunctionTypeCount;

    public FunctionSpecFactory FunctionFactory { get; }
    
    public Nature(IReadOnlyCollection<CistronSpec> objects, Random random)
        : base(objects, random)
    {
        this.FunctionFactory = new FunctionSpecFactory(this);
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
