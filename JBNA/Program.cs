global using TCodon = System.Byte;
global using HaploidalGenome = JBNA.Genome<JBNA.Chromosome>;
global using DiploidalGenome = JBNA.Genome<JBNA.DiploidChromosome>;

using JBSnorro;
using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;

namespace JBNA;

public class Genome<TPloidality> where TPloidality : IHomologousSet<TPloidality>
{
    internal const float DefaultMutationRate = 0.01f;
    internal const float DefaultMutationRateStdDev = DefaultMutationRate / 4;
    internal static readonly ImmutableArray<float> DefaultCrossoverRates = new float[] { 0.3f, 0.5f, 0.2f }.Scan((a, b) => a + b, 0f).ToImmutableArray();

    public Genome(IReadOnlyList<TPloidality> chromosomes, IEnumerable<CistronSpec_LNCE> specs, Random random)
    {
        this.Chromosomes = chromosomes;
        this.CodonCollection = new ReadOnlyStartCodonCollection<CistronSpec_LNCE>(specs.ToList(), random);
    }
    /// <summary>
    /// Creates the CodonCollection before the chromosomes need to be created; useful for random chromosome generation.
    /// </summary>
    internal Genome(ReadOnlyStartCodonCollection<CistronSpec_LNCE> nature, out List<TPloidality> chromosomes)
    {
        this.CodonCollection = nature;
        this.Chromosomes = chromosomes = new List<TPloidality>();
    }
    private Genome(ReadOnlyStartCodonCollection<CistronSpec_LNCE> nature, List<TPloidality> chromosomes)
    {
        this.CodonCollection = nature;
        this.Chromosomes = chromosomes;
    }
    private Dictionary<CistronSpec_LNCE, int> SpecIndices => CodonCollection.SpecIndices;
    public IReadOnlyCollection<CistronSpec_LNCE> Specs => SpecIndices.Keys;
    public IReadOnlyList<TPloidality> Chromosomes { get; }
    internal ReadOnlyStartCodonCollection<CistronSpec_LNCE> CodonCollection { get; }
    private object?[]? interpretations;
    private IReadOnlyList<TCodon> StartCodons => CodonCollection.StartCodons;  // for serialiation
    /// <summary>
    /// Gets the interpreters of the cistrons in the order of the specs.
    /// </summary>
    private Func<object>?[] GetInterpreters()
    {
        var interpreters = new Func<object>?[this.Specs.Count];
        foreach (var (spec, interpreter) in Chromosomes.SelectMany(c => c.FindCistrons()))
        {
            int specIndex = this.SpecIndices[spec]; // must be present, otherwise throw
            var alreadyPresent = interpreters[specIndex];
            if (alreadyPresent != null)
            {
                if (spec.Merger == null)
                    throw new GenomeInviableException($"Multiple of the same unmergable cistron type detected ({spec})");
                interpreters[specIndex] = spec.Merger.Merge(alreadyPresent, interpreter);
            }
            else
            {
                interpreters[specIndex] = interpreter;
            }
        }
        foreach (var (spec, interpreter) in Enumerable.Zip(this.Specs, interpreters))
        {
            if (spec.Required && interpreter == null)
                throw new GenomeInviableException($"Mandatory cistron not present");
        }
        return interpreters;
    }
    /// <summary>
    /// Gets the values of the cistrons in the order of the specs.
    /// </summary>
    public object?[] Interpret()
    {
        if (this.interpretations == null)
        {
            var interpreters = GetInterpreters();
            var interpretations = new object?[this.Specs.Count];
            for (int i = 0; i < this.Specs.Count; i++)
            {
                interpretations[i] = interpreters[i]?.Invoke();
            }
            this.interpretations = interpretations;
        }
        return this.interpretations!;
    }
    public Genome<TPloidality> Reproduce(Random random)
    {
        var chromosomes = this.Chromosomes.Select(c => c.Reproduce(this.Interpret, random)).ToList();
        return new Genome<TPloidality>(this.CodonCollection, chromosomes);
    }
    public Genome<TPloidality> Reproduce(Genome<TPloidality> mate, Random random)
    {
        //Assert(ReferenceEquals(this.SpecIndices, mate.SpecIndices));
        Assert(ReferenceEquals(this.StartCodons, mate.StartCodons));
        Assert(ReferenceEquals(this.CodonCollection, mate.CodonCollection));

        var chromosomes = Enumerable.Zip(this.Chromosomes, mate.Chromosomes).Select(c => c.First.Reproduce(c.Second, this.Interpret, random)).ToList();
        return new Genome<TPloidality>(this.CodonCollection, chromosomes);
    }
    internal object? Interpret(Allele allele)
    {
        var interpretations = this.Interpret();
        var result = SpecIndices.Keys.Select((value, i) => new { value, i })
                                     .Where(p => p.value.Allele == allele)
                                     .Select(p => this.interpretations[p.i])
                                     .ToList();
        return result;
    }
    internal object? Interpret(IReadOnlyList<Allele> alleles)
    {
        var result = new Dictionary<Allele, object?>(alleles.Count);
        foreach (var allele in alleles)
        {
            result[allele] = Interpret(allele);
        }
        return result;
    }
}

public class ReadOnlyStartCodonCollection<T> where T : notnull
{
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
        CodonBitLength = 8;
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
        this.StopCodonByteLength = 1;
        this.StopCodon = 0;
        Assert(!dict.ContainsKey(this.StopCodon));
        this.ReverseObjects = this.Objects.ToDictionary(keySelector: _ => _.Value, elementSelector: _ => _.Key);
        this.SpecIndices = SpecToIndices(objects);

        CistronsByAllele = this.Objects.Values.ToDictionary(keySelector: c => ((CistronSpec_LNCE)(object)c).Allele, elementSelector: c => c);
        StartCodons = this.Objects.Keys.ToArray();
    }
    public int CodonBitLength { get; }
    public int CodonByteLength => (CodonBitLength + 7) / 8;
    public TCodon StopCodon { get; }
    public int StopCodonByteLength { get; }
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

    public bool TryFindNext(byte[] data, int startIndex, out T? value)
    {
        return TryFindNext(data, startIndex, out value, out var _);
    }
    public bool TryFindNext(byte[] data, int startIndex, out T? value, out int index)
    {
        Assert(CodonByteLength == 1);
        Assert(CodonBitLength == 8);
        for (int i = startIndex; i < data.Length; i++)
        {
            if (this.Objects.TryGetValue(data[i], out var codon))
            {
                index = i;
                value = codon;
                return true;
            }
        }
        index = -1;
        value  = default;
        return false;
    }
    public Index FindStopCodon(byte[] data, int startIndex)
    {
        Assert(StopCodonByteLength == 1); // implementation depends on it
        for (int i = startIndex; i < data.Length; i++)
        {
            if (data[i] == StopCodon)
                return i;
        }
        return Index.End;
    }
    public IEnumerable<KeyValuePair<T, Range>> TryFind(byte[] data, TCodon codon)
    {
        foreach (var pair in this.Split(data))
        {
            var range = pair.Value;
            if (data[range.Start] == codon)
                yield return pair;
        }
    }

    public IEnumerable<KeyValuePair<T, Range>> Split(byte[] data)
    {
        int index = 0;
        while (TryFindNext(data, index, out var value, out var codonIndex))
        {
            int cistronStartIndex = codonIndex + this.CodonByteLength;
            var stopCodonIndex = FindStopCodon(data, codonIndex);

            if (Index.End.Equals(stopCodonIndex))
            {
                yield return KeyValuePair.Create(value!, new Range(cistronStartIndex, Index.End));
                break;
            }
            else
            {
                yield return KeyValuePair.Create(value!, new Range(cistronStartIndex, stopCodonIndex));
                index = stopCodonIndex.Value + StopCodonByteLength;
            }
        }
    }
}
public interface IHomologousSet<T> where T : IHomologousSet<T>
{
    int Count { get; }
    IEnumerable<(CistronSpec_LNCE, Func<object>)> FindCistrons();
    T Reproduce(Func<Allele, object?> interpret, Random random);
    T Reproduce(T mate, Func<Allele, object?> interpret, Random random);


}
public sealed class Chromosome : IHomologousSet<Chromosome>
{
    internal int Length => this.data.Length;
    internal readonly ReadOnlyStartCodonCollection<CistronSpec_LNCE> codonCollection;
    private readonly byte[] data;
    private bool frozen = false;
    public Chromosome(byte[] data, ReadOnlyStartCodonCollection<CistronSpec_LNCE> codonCollection)
    {
        this.data = data;
        this.codonCollection = codonCollection;
    }

    int IHomologousSet<Chromosome>.Count => 1;

    public IEnumerable<(CistronSpec_LNCE, Func<object>)> FindCistrons()
    {
        this.frozen = true;
        foreach (var (cistronSpec, range) in codonCollection.Split(this.data))
        {
            if (Index.End.Equals(range.End))
            {
                if (!cistronSpec.Spec.ImplicitStopCodonAllowed)
                    throw new GenomeInviableException("Implicit stop codon not allowed");
                if (range.GetOffsetAndLength(this.data.Length).Length < cistronSpec.Spec.MinByteCount)
                    throw new GenomeInviableException("Implicit stop codon sequence too short");
            }
            yield return (cistronSpec, () => cistronSpec.Spec.Interpreter.Create(this.data.AsSpan(range)));
        }
    }
    public Chromosome Reproduce(Func<Allele, object?> interpret, Random random)
    {
        var newChromosome = this.Clone();
        newChromosome.Mutate(interpret, random);
        return newChromosome;
    }

    /// <summary>
    /// This is implemented mostly as convenience to be called for duploidal cells.
    /// From that perspective: crosses over between the pair in this cell, and returns one set of the resulting pair.
    /// </summary>
    internal Chromosome Reproduce(Chromosome mate, Func<Allele, object?> interpret, Random random)
    {
        Assert(this.Length == mate.Length);
        int length = this.Length;

        // crossover (is between the pair of the diploid)
        int crossoverCount = GetCrossoverCount(interpret, random); // number in [0, ~3]
        var splitIndices = random.ManySorted(crossoverCount, 0, length);

        byte[] newChromosome = new byte[length];
        int side = random.Next(2); // random start side
        foreach (var range in splitIndices.Append(length).Windowed2(0))
        {
            var source = side == 0 ? this : mate;
            source.CopyTo(newChromosome, range.First, range.Second - range.First);
            side = 1 - side; // alternate 
        }
        return new Chromosome(newChromosome, this.codonCollection);


        throw new InvalidOperationException("Reproducing haploidal chromosomes diploidally, which doesn't make sense");
        // alternative implementation:
        // return Reproduce(interpret, random);
    }

    private static int GetCrossoverCount(Func<Allele, object?> interpret, Random random)
    {
        var r = random.Next();
        object? value = interpret(Allele.CrossoverRate);
        IEnumerable<float> cumulativeCrossoverCountProbability;
        if (value == null)
            cumulativeCrossoverCountProbability = Genome<DiploidChromosome>.DefaultCrossoverRates;
        else
            cumulativeCrossoverCountProbability = ((System.Collections.IEnumerable)value).Cast<float>();

        int i = 0;
        foreach (float cumulativeP in cumulativeCrossoverCountProbability)
        {
            if (cumulativeP < r)
                break;
            i++;
        }
        return i;
    }
    internal void CopyTo(byte[] array, int startIndex, int length)
    {
        System.Array.Copy(this.data, startIndex, array, startIndex, length);
    }
    internal void Mutate(Func<Allele, object?> interpret, Random random)
    {
        Assert(!this.frozen, "Can't mutate frozen chromosome");

        float mutationRate = GetMutationRate(interpret);
        float mutationRateStdDev = GetMutationRateStdDev(interpret);

        float mutationCountMu = mutationRate * this.Length;
        float mutationCountSi = mutationRateStdDev * this.Length;
        int mutationCount = (int)random.Normal(mutationCountMu, mutationCountSi);

        int[] mutationIndices = random.ManySorted(mutationCount, 0, 8 * this.Length);
        this.Mutate(mutationIndices);
    }

    private void Mutate(int[] mutationBitIndices)
    {
        Assert(!this.frozen, "Can't mutate frozen chromosome");
        Assert(mutationBitIndices.All(b => b < this.data.Length * 8));

        foreach (var i in mutationBitIndices)
            Mutate(i);

        void Mutate(int mutationBitIndex)
        {
            int byteIndex = mutationBitIndex / 8;
            int bitIndex = mutationBitIndex % 8;
            byte mask = (byte)(1 << bitIndex);
            this.data[byteIndex] ^= mask; // flip the bit


            // byte current = this.data[byteIndex];
            // int result;
            // if (current.HasBit(bitIndex))
            //     result = current & ~mask;
            // else
            //     result = current | mask;
            // 
            // Assert(0 <= result && result < 256);
            // this.data[byteIndex] = (byte)result;
        }
    }

    private static float GetMutationRate(Func<Allele, object?> interpret)
    {
        object? value = interpret(Allele.DefaultMutationRate);
        if (value == null)
            return Genome<DiploidChromosome>.DefaultMutationRate;
        return (float)value;
    }
    private static float GetMutationRateStdDev(Func<Allele, object?> interpret)
    {
        object? value = interpret(Allele.DefaultMutationRate);
        if (value == null)
            return Genome<DiploidChromosome>.DefaultMutationRate;
        return (float)value;
    }
    private Chromosome Clone()
    {
        return new Chromosome((byte[])this.data.Clone(), this.codonCollection);
    }

    Chromosome IHomologousSet<Chromosome>.Reproduce(Chromosome mate, Func<Allele, object?> interpret, Random random)
    {
        // Console.WriteLine("Warning: calling reproduce on potentially haploidal chromosome");
        // direct calls of this.Reproduce(...) don't need the warning as it's internal
        // Edit: actually because P is restricted to IHomologousSet<Chromosome>, we always go via here...
        return this.Reproduce(mate, interpret, random);
    }
}
public sealed class DiploidChromosome : IHomologousSet<DiploidChromosome>
{
    public Chromosome A { get; }
    public Chromosome B { get; }
    private ReadOnlyStartCodonCollection<CistronSpec_LNCE> codonCollection => A.codonCollection;

    int IHomologousSet<DiploidChromosome>.Count => 2;
    public DiploidChromosome(Chromosome a, Chromosome b)
    {
        Assert(ReferenceEquals(a.codonCollection, b.codonCollection));
        this.A = a;
        this.B = b;
    }

    public DiploidChromosome Reproduce(DiploidChromosome mate, Func<Allele, object?> interpret, Random random)
    {
        Assert(ReferenceEquals(mate.codonCollection, this.codonCollection));

        // crossover (is within a diploid pair, not between pairs)
        var newA = this.Crossover(interpret, random);
        newA.Mutate(interpret, random);

        var newB = mate.Crossover(interpret, random);
        newB.Mutate(interpret, random);

        return new DiploidChromosome(newA, newB);
    }
    /// <summary>
    /// Crosses over between the pair in this cell, and returns one set of the resulting pair.
    /// </summary>
    private Chromosome Crossover(Func<Allele, object?> interpret, Random random)
    {
        Assert(this.A.Length == this.B.Length);
        return this.A.Reproduce(this.B, interpret, random);
    }


    public IEnumerable<(CistronSpec_LNCE, Func<object>)> FindCistrons()
    {
        throw new NotImplementedException();
    }

    public DiploidChromosome Reproduce(Func<Allele, object?> interpret, Random random)
    {
        Console.WriteLine("Warning: reproducing diploidal with itself");
        return Reproduce(this, interpret, random);
    }

}
public enum Allele
{
    Custom = 0,
    JunkRatio = 1,
    DefaultMutationRate = 2,
    CrossoverRate = 3,

}
public class CistronSpec_LNCE // LNCE stands for Later not Cistron Epxression
{
    static CistronSpec_LNCE()
    {
        var defaults = new List<CistronSpec_LNCE>();
        defaults.Add(new CistronSpec_LNCE()
        {
            Meta = true,
            Spec = NumberSpec.CreateUniformFloatFactory(0, 4),
        });

        Defaults = defaults;
    }
    public static IReadOnlyCollection<CistronSpec_LNCE> Defaults { get; }


    public Allele Allele { get; init; } = Allele.Custom;
    public bool Meta { get; init; } = false;
    public bool Required { get; init; } = true;
    public ICistronSpec Spec { get; init; } = default!;

    public IMultiCistronMerger? Merger { get; }
}
public interface ICistronSpec
{
    ICistronInterpreter Interpreter => this.DeferToUpcast<ICistronInterpreter>();
    int MinBitCount => Interpreter.MinBitCount;
    int MaxBitCount => Interpreter.MaxBitCount;
    int MinByteCount => Interpreter.MinByteCount;
    int MaxByteCount => Interpreter.MaxByteCount;
    bool ImplicitStopCodonAllowed => Interpreter.MaxBitCount > int.MaxValue / 2;
}
public interface IMultiCistronMerger  // decides which is recessive, dominant, or merges
{
    Func<object> Merge(Func<object> previous, Func<object> current);
}
public interface ICistronSpec<out T> : ICistronSpec
{
    new ICistronInterpreter<T> Interpreter { get; }
}
public interface ICistronInterpreter
{
    public int MinBitCount { get; }
    public int MaxBitCount { get; }
    public int MinByteCount => (MinBitCount + 7) / 8;
    public int MaxByteCount { get { checked { return (MaxBitCount + 7) / 8; } } }
    object Create(ReadOnlySpan<byte> cistron);
    ReadOnlyCollection<byte>? InitialEncodedValue => this.DeferToUpcast<ReadOnlyCollection<byte>?>();
    // byte[] ReverseEngineer(TCodon startCodon, object? value, TCodon stopCodon) => ((ICistronInterpreter<object>)this).ReverseEngineer(startCodon, value, stopCodon);
}
public interface ICistronInterpreter<out T> : ICistronInterpreter
{
    /// <summary>
    /// The purpose of this function is to transform the data in a <typeparamref name="T"/> 
    /// where similar input corresponding to similar output, is a approximately continuous fashion.
    /// </summary>
    /// <returns> An <see cref="ICistron"/> or an <see cref="ICistron"/>-like.</returns>
    new T Create(ReadOnlySpan<byte> cistron);
    new ReadOnlyCollection<byte>? InitialEncodedValue => default;
    // byte[] ReverseEngineer(TCodon startCodon, T? value, TCodon stopCodon);
}

public interface ICistron
{

}
