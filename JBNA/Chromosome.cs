
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
            }
            );

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

        float mutationRate = GetDefaultBitMutationRate(interpret);
        float mutationRateStdDev = GetDefaultBitMutationRateStdDev(interpret);

        ulong[] mutationBitIndices = randomlySelectBitIndices(mutationRate, mutationRateStdDev, this.nature.MinimumNumberOfMutationsPerOffspring, endIsPossibleIndex: false, random);
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

    private ulong[] randomlySelectBitIndices(float rate, float rateStdDev, int minimum, bool endIsPossibleIndex, Random random)
    {
        Requires(minimum >= 0);

        float mutationCountMu = rate * this.Length;
        float mutationCountSi = rateStdDev * this.Length;
        int mutationCount = minimum + Math.Max(0, (int)random.Normal(mutationCountMu, mutationCountSi));

        ulong[] mutationIndices = random.ManySorted(mutationCount, 0, this.Length + (endIsPossibleIndex ? 1UL : 0));
        return mutationIndices;
    }
    private void ResizeMutate(Func<Allele, object?> interpret, Random random)
    {
        Assert(!this.frozen, "Can't resize frozen chromosome");

        float insertionRate = GetBitInsertionRate(interpret);
        float insertionRateStdDev = GetBitInsertionRateStdDev(interpret);
        ulong[] insertionBitIndices = randomlySelectBitIndices(insertionRate, insertionRateStdDev, nature.MinimumNumberOfBitInsertionsPerOffspring, endIsPossibleIndex: true, random);
        var insertionBits = insertionBitIndices.Select(_ => random.Next(2) == 0).ToArray();

        this.InsertBits(insertionBitIndices, insertionBits);

        float removalRate = GetBitRemovalRate(interpret);
        float removalRateStdDev = GetBitRemovalRateStdDev(interpret);
        ulong[] removalBitIndices = randomlySelectBitIndices(removalRate, removalRate, nature.MinimumNumberOfBitRemovalsPerOffspring, endIsPossibleIndex: false, random);
        if (insertionBitIndices.SequenceEqual(new[] { 2UL }) && insertionBits.SequenceEqual(new[] { true }))
        {
        }
        this.RemoveBits(removalBitIndices.Unique().ToArray());
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
    private static float GetDefaultBitMutationRate(Func<Allele, object?> interpret)
    {
        return GetFloatAllele(interpret, Allele.DefaultBitMutationRate, CistronSpec.DefaultMutationRate);
    }
    private static float GetDefaultBitMutationRateStdDev(Func<Allele, object?> interpret)
    {
        return GetFloatAllele(interpret, Allele.DefaultBitMutationRateStdDev, CistronSpec.DefaultMutationRateStdDev);
    }
    private static float GetBitInsertionRate(Func<Allele, object?> interpret)
    {
        return GetFloatAllele(interpret, Allele.BitInsertionRate, CistronSpec.DefaultBitInsertionRate);
    }
    private static float GetBitInsertionRateStdDev(Func<Allele, object?> interpret)
    {
        return GetFloatAllele(interpret, Allele.BitInsertionRateStdDev, CistronSpec.DefaultBitInsertionRateStdDev);
    }
    private static float GetBitRemovalRate(Func<Allele, object?> interpret)
    {
        return GetFloatAllele(interpret, Allele.BitRemovalRate, CistronSpec.DefaultBitRemovalRate);
    }
    private static float GetBitRemovalRateStdDev(Func<Allele, object?> interpret)
    {
        return GetFloatAllele(interpret, Allele.BitRemovalRateStdDev, CistronSpec.DefaultBitRemovalRateStdDev);
    }
    private static float GetFloatAllele(Func<Allele, object?> interpret, Allele allele, float @default)
    {
        object? value = interpret(allele);
        if (value == null)
            return @default;
        return (float)value;
    }
    private Chromosome Clone()
    {
        return new Chromosome(this.data.Clone(), this.nature);
    }

    [DebuggerHidden]
    Chromosome IHomologousSet<Chromosome>.Reproduce(Chromosome mate, Func<Allele, object?> interpret, Random random) => this.Reproduce(mate, interpret, random);
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

    DiploidChromosome IHomologousSet<DiploidChromosome>.Reproduce(DiploidChromosome mate, Func<Allele, object?> interpret, Random random)
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

    IEnumerable<(CistronSpec, Func<object>)> IHomologousSet<DiploidChromosome>.FindCistrons()
    {
        throw new NotImplementedException();
    }
    DiploidChromosome IHomologousSet<DiploidChromosome>.Reproduce(Func<Allele, object?> interpret, Random random)
    {
        Console.WriteLine("Warning: reproducing diploidal with itself");
        return ((IHomologousSet<DiploidChromosome>)this).Reproduce(this, interpret, random);
    }
}