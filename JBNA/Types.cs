global using TCodon = System.UInt64;
global using HaploidalGenome = JBNA.Genome<JBNA.Chromosome>;
global using DiploidalGenome = JBNA.Genome<JBNA.DiploidChromosome>;
using System.Collections.ObjectModel;
using JBSnorro.Collections;

namespace JBNA;

public interface IHomologousSet<T> where T : IHomologousSet<T>
{
    int Count { get; }
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
    object Interpret(BitArrayReadOnlySegment cistron) => ((ICistronInterpreter<object>)this).Interpret(cistron); // only works for reference types
    ulong MinBitCount { get; }
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
     new T Interpret(BitArrayReadOnlySegment cistron);
}
