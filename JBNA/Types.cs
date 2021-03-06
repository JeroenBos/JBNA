global using TCodon = System.UInt64;
global using HaploidalGenome = JBNA.Genome<JBNA.Chromosome>;
global using DiploidalGenome = JBNA.Genome<JBNA.DiploidChromosome>;

namespace JBNA;

public interface IHomologousSet<T> where T : IHomologousSet<T>
{
    int Count { get; }
    ulong Length { get; }
    IEnumerable<(CistronSpec, Func<object>)> FindCistrons();
    T Reproduce(Func<Allele, object?> interpret, Random random);
    T Reproduce(T mate, Func<Allele, object?> interpret, Random random);
}

public enum Allele
{
    None = 0,
    JunkRatio,
    DefaultBitMutationRate,
    DefaultBitMutationRateStdDev,
    CrossoverRate,
    BitInsertionRate,
    BitInsertionRateStdDev,
    BitRemovalRate,
    BitRemovalRateStdDev,

    CustomRangeStart = 256
}
public interface ICistronInterpreter
{
    /// <summary>
    /// The purpose of this function is to transform the data in a <typeparamref name="T"/> 
    /// where similar input corresponding to similar output, is a approximately continuous fashion.
    /// </summary>
    /// <returns> A series of bit representing a cistron. Specifically it does not include the start and stop codons. </returns>
    // I'm starting to think this shouldn't exist, and that all callers of this function should just require a ICistronInterpreter<object> instead.... although.... ValueTypes??
    [DebuggerHidden]
    object Interpret(BitReader cistronReader)
    {
#if DEBUG
        if (this.GetType().GetInterfaces().Where(t => t.IsGenericTypeDefinition && t.GetGenericTypeDefinition() == typeof(ICistronInterpreter<>)).Count() != 1)
            throw new Exception($"The type '{this.GetType().FullName}' should explicitly implement ICistronInterpreter.Interpret to resolve ambigite as this which ICistrioninterpreter<>.Interpret should be called");
#endif

        return ((ICistronInterpreter<object>)this).Interpret(cistronReader); // only works for reference types
    }
    /// <summary>
    /// This is the absolute minimum number of bits required for any interpretation to succeed.
    /// It's unclear to be how to express the minimum number of bits required for all interpretations to succeed (or to at least not fail on insufficient bits).
    /// </summary>
    ulong MinBitCount { get; }
    /// <summary>
    /// This is the maximum number of bits this interpreter could ever read. If more are given, they'll be ignored.
    /// </summary>
    ulong MaxBitCount { get; }
    BitArrayReadOnlySegment? InitialEncodedValue => default;
    bool ImplicitStopCodonAllowed => MaxBitCount > int.MaxValue / 2;
    // byte[] ReverseEngineer(TCodon startCodon, T? value, TCodon stopCodon);

}
/// <summary>
/// Decides which is recessive, dominant, or merges.
/// </summary>
public interface IMultiCistronMerger
{
    bool CouldIgnoreInvalidCistrons => true;
    Func<object> Merge(Func<object> previous, Func<object> current);
}
public interface ICistronInterpreter<out T> : ICistronInterpreter
{
    /// <inheritdoc/>
    new T Interpret(BitReader cistronReader);
}
