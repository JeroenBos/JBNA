namespace JBNA;

public class Genome<TPloidality> where TPloidality : IHomologousSet<TPloidality>
{
    private Dictionary<CistronSpec, int> SpecIndices => Nature.Codons.SpecIndices;
    public IReadOnlyCollection<CistronSpec> Specs => SpecIndices.Keys;
    public IReadOnlyList<TPloidality> Chromosomes { get; }
    internal Nature Nature { get; }
    private object?[]? interpretations;
    private IReadOnlyList<TCodon> StartCodons => Nature.Codons.StartCodons;  // for serialiation


    public Genome(IReadOnlyList<TPloidality> chromosomes, IReadOnlyCollection<CistronSpec.Builder> specs, Random random)
    {
        this.Chromosomes = chromosomes;
        this.Nature = new Nature(specs, random);
    }
    /// <summary>
    /// Creates the CodonCollection before the chromosomes need to be created; useful for random chromosome generation.
    /// </summary>
    internal Genome(Nature nature, out List<TPloidality> chromosomes)
    {
        this.Nature = nature;
        this.Chromosomes = chromosomes = new List<TPloidality>();
    }
    private Genome(Nature nature, List<TPloidality> chromosomes)
    {
        this.Nature = nature;
        this.Chromosomes = chromosomes;
    }

    /// <summary>
    /// Gets the interpreters of the cistrons in the order of the specs.
    /// </summary>
    private Func<object>?[] GetInterpreters()
    {
        var interpreters = new Func<object>?[this.Specs.Count];
        foreach (var (spec, interpreter) in Chromosomes.SelectMany([DebuggerHidden] (c) => c.FindCistrons()))
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
            {
                // this is generally not caught, as it is supposed to be true _by construction_
                throw new GenomeInviableException($"Mandatory cistron not present");
            }
        }
        return interpreters;
    }
    /// <summary>
    /// Gets the values of the cistrons in the order of the specs.
    /// </summary>
    //[DebuggerHidden]
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
        return new Genome<TPloidality>(this.Nature, chromosomes);
    }
    public Genome<TPloidality> Reproduce(Genome<TPloidality> mate, Random random)
    {
        //Assert(ReferenceEquals(this.SpecIndices, mate.SpecIndices));
        Assert(ReferenceEquals(this.StartCodons, mate.StartCodons));
        Assert(ReferenceEquals(this.Nature, mate.Nature));

        var chromosomes = Enumerable.Zip(this.Chromosomes, mate.Chromosomes).Select(c => c.First.Reproduce(c.Second, this.Interpret, random)).ToList();
        return new Genome<TPloidality>(this.Nature, chromosomes);
    }
    internal object? Interpret(Allele allele)
    {
        this.Interpret(); // make sure cache is populated
        var result = SpecIndices.Keys.Select((value, i) => new { value, i })
                                     .Where(p => p.value.Allele == allele)
                                     .Select(p => this.interpretations![p.i])
                                     .Where(p => p != null)
                                     .ToList();
        if (result.Count > 1)
            throw new NotImplementedException("Hmmm, should the cistrons be indexed by startcodons, or alleles? ");
        // depending on the usage, I'd say. Can an Allele have multiple startCodons? In real life yes, here I'm inclined to say no.
        // if the answer _is_ no, then the length of the result must be 0 or 1 (either codon present or not) because merging has already happened
        if (result.Count == 0)
            return null;
        return result[0];
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
