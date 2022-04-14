
using JBSnorro;
using JBSnorro.Collections;
using JBSnorro.Diagnostics;
using JBSnorro.Extensions;
using System;
using System.Diagnostics;
using static JBSnorro.Diagnostics.Contract;

namespace JBNA;

public sealed class Chromosome : IHomologousSet<Chromosome>
{
    internal ulong Length => this.data.Length;
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
            return this.nature
                       .FindAllCistrons(this.data)
                       .Select(p => p.Key.Allele);
        }
    }
    int IHomologousSet<Chromosome>.Count => 1;

    public IEnumerable<(CistronSpec, Func<object>)> FindCistrons()
    {
        this.frozen = true;
        foreach (var (cistronSpec, range) in nature.FindAllCistrons(this.data))
        {
            bool hasImplicitStopCodon = Index.End.Equals(range.End);
            if (hasImplicitStopCodon && !cistronSpec.Interpreter.ImplicitStopCodonAllowed)
            {
                throw new GenomeInviableException("Implicit stop codon not allowed");
            }
            if (cistronSpec.Merger?.CouldIgnoreInvalidCistrons != true)
            {
                CheckLength(range, this.data.Length, cistronSpec.Interpreter, this.nature);
            }


            yield return (cistronSpec, () =>
            {
                CheckLength(range, this.data.Length, cistronSpec.Interpreter, this.nature);
                return cistronSpec.Interpreter.Interpret(this.data.SelectSegment(range));
            });

            static void CheckLength(Range cistronRange, ulong rangeContainerLength, ICistronInterpreter interpreter, Nature nature)
            {
                ulong cistronLength = (ulong)cistronRange.GetOffsetAndLength(checked((int)rangeContainerLength)).Length;
                if (cistronLength < interpreter.MinBitCount)
                    throw new GenomeInviableException("Cistron too short");
                if (cistronLength > interpreter.MaxBitCount)
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
            var splitIndices = random.ManySorted(crossoverCount, 0, checked((int)Math.Min(mate.Length, this.Length)));
            int startSide = random.Next(2); // random start side
            bool endsOnThisSide = (startSide == 0) == ((crossoverCount % 2) == 0);
            var newChromosomeData = new BitArray(length: endsOnThisSide ? this.Length : mate.Length);
            int side = startSide;
            foreach (var range in splitIndices.Append(checked((int)newChromosomeData.Length)).Windowed2(0))
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

        ulong[] mutationBitIndices = randomlySelectBitIndices(mutationRate, mutationRateStdDev, this.nature.MinimumNumberOfMutationsPerOffspring, random);
        if (mutationBitIndices.Length != 0)
        {

        }
        this.Mutate(mutationBitIndices);

        this.ResizeMutate(interpret, random);
    }
    private void Mutate(ulong[] mutationBitIndices)
    {
        Assert(!this.frozen, "Can't mutate frozen chromosome");
        Assert(mutationBitIndices.All(b => (ulong)b < this.data.Length));

        foreach (var i in mutationBitIndices)
            Mutate(i);

        void Mutate(ulong mutationBitIndex)
        {
            this.data[mutationBitIndex] = !this.data[mutationBitIndex];
        }
    }

    private ulong[] randomlySelectBitIndices(float rate, float rateStdDev, int minimum, Random random)
    {
        Requires(minimum >= 0);

        float mutationCountMu = rate * this.Length;
        float mutationCountSi = rateStdDev * this.Length;
        int mutationCount = minimum + Math.Max(0, (int)random.Normal(mutationCountMu, mutationCountSi));

        ulong[] mutationIndices = random.ManySorted(mutationCount, 0, this.Length);
        return mutationIndices;
    }
    private void ResizeMutate(Func<Allele, object?> interpret, Random random)
    {
        Assert(!this.frozen, "Can't resize frozen chromosome");

        float insertionRate = GetBitInsertionRate(interpret);
        ulong[] insertionBitIndices = randomlySelectBitIndices(insertionRate, insertionRate, nature.MinimumNumberOfBitInsertionsPerOffspring, random);
        var insertionBits = insertionBitIndices.Select(_ => random.Next(2) == 0).ToArray();
        this.InsertBits(insertionBitIndices, insertionBits);


        float removalRate = GetBitInsertionRate(interpret);
        ulong[] removalBitIndices = randomlySelectBitIndices(removalRate, removalRate, nature.MinimumNumberOfBitRemovalsPerOffspring, random);
        this.RemoveBits(insertionBitIndices.Unique().ToArray());
    }
    private void RemoveBits(ulong[] bitIndices)
    {
        Requires(bitIndices != null);
        Requires(bitIndices.AreUnique());

        this.data.RemoveAt(bitIndices);
    }
    private void InsertBits(ulong[] bitIndices, bool[] bits)
    {
        Assert(bitIndices.Length == bits.Length);
        
        Array.Sort(bitIndices);

        this.data.InsertRange(bitIndices, bits);
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

    [DebuggerHidden]
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