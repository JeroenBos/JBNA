
using JBSnorro;
using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using System;

namespace JBNA;

public sealed class Chromosome : IHomologousSet<Chromosome>
{
    internal int Length => this.data.Length;
    internal readonly Nature nature;
    private readonly BitArray data;
    private bool frozen = false;
    public Chromosome(BitArray data, Nature codonCollection)
    {
        this.data = data;
        this.nature = codonCollection;
    }
    internal Chromosome(TCodon[] data, Nature codonCollection)
    {
        this.data = BitArray.FromRef(data);
        this.nature = codonCollection;
    }


    public IEnumerable<Allele> Alleles
    {
        get
        {
            this.data.Find(
            return this.nature.FindAllCodons(this.data).Select(_ => _.Key.Allele);
        }
    }
    int IHomologousSet<Chromosome>.Count => 1;

    public IEnumerable<(CistronSpec, Func<object>)> FindCistrons()
    {
        this.frozen = true;
        foreach (var (cistronSpec, range) in nature.FindAllCodons(this.data))
        {
            if (Index.End.Equals(range.End))
            {
                if (!cistronSpec.Interpreter.ImplicitStopCodonAllowed)
                    throw new GenomeInviableException("Implicit stop codon not allowed");
                if (range.GetOffsetAndLength(this.data.Length).Length < cistronSpec.Interpreter.MinByteCount)
                    throw new GenomeInviableException("Implicit stop codon sequence too short");
            }
            if (cistronSpec.Merger?.CouldIgnoreInvalidCistrons != true)
            {
                CheckLength(this.data, range, cistronSpec.Interpreter);
            }


            yield return (cistronSpec, () =>
            {
                CheckLength(this.data, range, cistronSpec.Interpreter);
                return cistronSpec.Interpreter.Interpret(this.data.SelectSegment(range));
            }
            );

            static void CheckLength(BitArray data, Range range, ICistronInterpreter interpreter)
            {
                int cistronLength = range.GetOffsetAndLength(data.Length).Length;
                if (cistronLength < interpreter.MinByteCount)
                    throw new GenomeInviableException("Cistron too short");
                if (cistronLength > interpreter.MaxByteCount)
                    throw new GenomeInviableException("Cistron too long");
            }
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
        // crossover (is between the pair of the diploid)
        var newChromosome = Crossover();
        newChromosome.Mutate(interpret, random);
        return newChromosome;


        Chromosome Crossover()
        {
            int crossoverCount = GetCrossoverCount(interpret, random); // number in [0, ~3]
            var splitIndices = random.ManySorted(crossoverCount, 0, Math.Min(mate.Length, this.Length));
            int startSide = random.Next(2); // random start side
            bool endsOnThisSide = (startSide == 0) == ((crossoverCount % 2) == 0);
            var newChromosomeData = new BitArray(length: endsOnThisSide ? this.Length : mate.Length);
            int side = startSide;
            foreach (var range in splitIndices.Append(newChromosomeData.Length).Windowed2(0))
            {
                var source = side == 0 ? this : mate;
                source.CopyTo(newChromosomeData, range.First, range.Second - range.First, range.First);
                side = 1 - side; // alternate 
            }
            return new Chromosome(newChromosomeData, this.nature);
        }
    }

    private static int GetCrossoverCount(Func<Allele, object?> interpret, Random random)
    {
        var r = random.Next();
        object? value = interpret(Allele.CrossoverRate);
        IEnumerable<float> cumulativeCrossoverCountProbability;
        if (value == null)
            cumulativeCrossoverCountProbability = CistronSpec.DefaultCrossoverRates;
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
    internal void CopyTo(BitArray array, int sourceStartBitIndex, int length, int destStartBitIndex)
    {
        Contract.Assert(sourceStartBitIndex >= 0);
        Contract.Assert(length >= 0);
        Contract.Assert(destStartBitIndex >= 0);
        CopyTo(array, (ulong)sourceStartBitIndex, (ulong)length, (ulong)destStartBitIndex);
    }
    internal void CopyTo(BitArray array, ulong sourceStartBitIndex, ulong length, ulong destStartBitIndex)
    {
        this.data.CopyTo(array, sourceStartBitIndex, length, destStartBitIndex);
    }
    internal void Mutate(Func<Allele, object?> interpret, Random random)
    {
        Assert(!this.frozen, "Can't mutate frozen chromosome");

        float mutationRate = GetMutationRate(interpret);
        float mutationRateStdDev = GetMutationRateStdDev(interpret);

        int[] mutationBitIndices = randomlySelectBitIndices(mutationRate, mutationRateStdDev, random);
        this.Mutate(mutationBitIndices);

        this.ResizeMutate(interpret, random);
    }
    private void Mutate(int[] mutationBitIndices)
    {
        Assert(!this.frozen, "Can't mutate frozen chromosome");
        Assert(mutationBitIndices.All(b => b < this.data.Length * 8));

        foreach (var i in mutationBitIndices)
            Mutate(i);

        void Mutate(int mutationBitIndex)
        {
            this.data[mutationBitIndex] = !this.data[mutationBitIndex];
        }
    }

    private int[] randomlySelectBitIndices(float rate, float rateStdDev, Random random)
    {
        float mutationCountMu = rate * this.Length;
        float mutationCountSi = rateStdDev * this.Length;
        int mutationCount = (int)random.Normal(mutationCountMu, mutationCountSi);

        int[] mutationIndices = random.ManySorted(mutationCount, 0, 8 * this.Length);
        return mutationIndices;
    }
    private void ResizeMutate(Func<Allele, object?> interpret, Random random)
    {
        Assert(!this.frozen, "Can't resize frozen chromosome");

        float insertionRate = GetBitInsertionRate(interpret);
        // for simplicity let's not have a separate std dev here (yet)
        int[] insertionBitIndices = randomlySelectBitIndices(insertionRate, insertionRate, random);
        List<bool> insertionBits = insertionBitIndices.Select(_ => random.Next(2) == 0).ToList();

        this.InsertBits(insertionBitIndices, insertionBits);


        float removalRate = GetBitInsertionRate(interpret);
        // for simplicity let's not have a separate std dev here (yet)
        int[] removalBitIndices = randomlySelectBitIndices(removalRate, removalRate, random);

        this.RemoveBits(insertionBitIndices);
    }
    private void RemoveBits(int[] bitIndices)
    {
        if (bitIndices.Length != 0)
            throw new NotImplementedException();
    }
    private void InsertBits(int[] bitIndices, IList<bool> bits)
    {
        Assert(bitIndices.Length == bits.Count);

        // we're in the middle of converting everything to use bits rather than bytes. This is next
        throw new NotImplementedException();
    }
    private static float GetMutationRate(Func<Allele, object?> interpret)
    {
        object? value = interpret(Allele.DefaultMutationRate);
        if (value == null)
            return CistronSpec.DefaultMutationRate;
        return (float)value;
    }
    private static float GetMutationRateStdDev(Func<Allele, object?> interpret)
    {
        object? value = interpret(Allele.DefaultMutationRateStdDev);
        if (value == null)
            return CistronSpec.DefaultMutationRateStdDev;
        return (float)value;
    }
    private static float GetBitInsertionRate(Func<Allele, object?> interpret)
    {
        object? value = interpret(Allele.BitInsertionRate);
        if (value == null)
            return CistronSpec.DefaultBitInsertionRate;
        return (float)value;
    }
    private static float GetBitRemovalRate(Func<Allele, object?> interpret)
    {
        object? value = interpret(Allele.BitRemovalRate);
        if (value == null)
            return CistronSpec.DefaultBitRemovalRate;
        return (float)value;
    }
    private Chromosome Clone()
    {
        return new Chromosome(this.data.Clone(), this.nature);
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
    private Nature codonCollection => A.nature;

    int IHomologousSet<DiploidChromosome>.Count => 2;
    public DiploidChromosome(Chromosome a, Chromosome b)
    {
        Assert(ReferenceEquals(a.nature, b.nature));
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


    public IEnumerable<(CistronSpec, Func<object>)> FindCistrons()
    {
        throw new NotImplementedException();
    }

    public DiploidChromosome Reproduce(Func<Allele, object?> interpret, Random random)
    {
        Console.WriteLine("Warning: reproducing diploidal with itself");
        return Reproduce(this, interpret, random);
    }

}