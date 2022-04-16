namespace JBNA;

/// <summary>
/// Splits a Cistron up into multiple cistrons, assuming they're separated by Nature.StopCodon.
/// These subcistrons don't have start codons (keys) to signify them; they are ordered. 
/// The last substopcodon is always implicit.
/// </summary>
public class SubCistronInterpreter : ICistronInterpreter<ImmutableArray<BitArrayReadOnlySegment>>
{
    [DebuggerHidden]
    public static ICistronInterpreter<(T, U)> Create<T, U>(Nature nature, ICistronInterpreter<T> tInterpreter, ICistronInterpreter<U> uInterpreter)
    {
        return Create(nature, tInterpreter, uInterpreter, (t, u) => (t, u));
    }
    [DebuggerHidden]
    public static ICistronInterpreter<TCombined> Create<T, U, TCombined>(Nature nature, ICistronInterpreter<T> tInterpreter, ICistronInterpreter<U> uInterpreter, Func<T, U, TCombined> combiner)
    {
        return new SubCistronInterpreter<T, U, TCombined>(nature, tInterpreter, uInterpreter, combiner);
    }




    public ImmutableArray<ICistronInterpreter> SubInterpreters { get; }
    /// <summary>
    /// The number of cistrons that are mandatory. The remaining interpreters can be foregone.
    /// </summary>
    public int MinCistronCount { get; }
    public ulong MinBitCount { get; }
    public ulong MaxBitCount { get; }

    protected readonly Nature nature;

    public SubCistronInterpreter(Nature nature, params ICistronInterpreter[] subInterpreters)
        : this(nature, subInterpreters.Length, subInterpreters)
    {
    }
    public SubCistronInterpreter(Nature nature, int minCistronCount, params ICistronInterpreter[] subInterpreters)
        : this(nature, minCistronCount, subInterpreters.ToImmutableArray())
    {
    }
    public SubCistronInterpreter(Nature nature, int minCistronCount, ImmutableArray<ICistronInterpreter> subInterpreters)
    {
        Requires(nature != null);
        Requires(subInterpreters != null);
        Requires(0 <= minCistronCount && minCistronCount <= subInterpreters.Length);
        Requires(ForAll(subInterpreters, spec => spec.MinBitCount >= 0 && spec.MinBitCount <= spec.MaxBitCount));

        this.nature = nature;
        this.SubInterpreters = subInterpreters;
        this.MinCistronCount = minCistronCount;
        this.MinBitCount = subInterpreters.Sum(spec => spec.MinBitCount) + (ulong)((minCistronCount - 1) * nature.SubCistronStopCodon.Length);
        this.MaxBitCount = subInterpreters.Sum(spec => spec.MaxBitCount) + (ulong)((subInterpreters.Length - 1) * nature.SubCistronStopCodon.Length);
    }

    protected List<BitArrayReadOnlySegment> Split(BitReader cistronReader)
    {
        var result = impl(cistronReader, this.nature).ToList();
        if (result.Count < this.MinCistronCount)
            throw new GenomeInviableException($"Not enough subcistrons in cistron ({result.Count} < {this.MinCistronCount})");
        return result;


        static IEnumerable<BitArrayReadOnlySegment> impl(BitReader cistronReader, Nature nature)
        {
            BitArrayReadOnlySegment cistron = cistronReader.RemainingSegment;

            int count = 0;
            ulong startBitIndex = 0;
            while (true)
            {

                long stopIndex = cistron.IndexOf(nature.SubCistronStopCodon.Value, nature.SubCistronStopCodon.Length, startBitIndex);
                if (stopIndex == -1)
                {
                    yield return cistron[new Range((int)startBitIndex, Index.End)];
                    yield break;
                }

                long nestStartIndex = stopIndex + nature.SubCistronStopCodon.Length;
                Assert<NotImplementedException>(nestStartIndex <= int.MaxValue);
                var range = new Range((int)startBitIndex, (int)stopIndex);
                yield return cistron[range];

                startBitIndex = (ulong)nestStartIndex;
                count++;
            }
        }
    }
    ImmutableArray<BitArrayReadOnlySegment> ICistronInterpreter<ImmutableArray<BitArrayReadOnlySegment>>.Interpret(BitReader cistronReader)
    {
        return Split(cistronReader).ToImmutableArray();
    }


}

/// <inheritdoc />
internal class SubCistronInterpreter<T, U, TCombined> : SubCistronInterpreter, ICistronInterpreter<TCombined>
{
    private readonly Func<T, U, TCombined> combiner;

    public SubCistronInterpreter(Nature nature, ICistronInterpreter<T> tInterpreter, ICistronInterpreter<U> uInterpreter, Func<T, U, TCombined> combiner)
        : base(nature, tInterpreter, uInterpreter)
    {
        this.combiner = combiner;
    }

    public TCombined Interpret(BitReader cistronReader)
    {
        var subcistrons = base.Split(cistronReader);
        var t = ((ICistronInterpreter<T>)this.SubInterpreters[0]).Interpret(subcistrons[0].ToBitReader());
        var u = ((ICistronInterpreter<U>)this.SubInterpreters[1]).Interpret(subcistrons[1].ToBitReader());
        var result = combiner(t, u);
        return result;
    }
}
/// <inheritdoc />
internal class SubCistronInterpreter<T, U, V, TCombined> : SubCistronInterpreter, ICistronInterpreter<TCombined>
{
    private readonly Func<T, U, V, TCombined> combiner;

    public SubCistronInterpreter(Nature nature, ICistronInterpreter<T> tInterpreter, ICistronInterpreter<U> uInterpreter, ICistronInterpreter<V> vInterpreter, Func<T, U, V, TCombined> combiner)
        : base(nature, tInterpreter, uInterpreter, vInterpreter)
    {
        this.combiner = combiner;
    }

    public TCombined Interpret(BitReader cistronReader)
    {
        var subcistrons = base.Split(cistronReader);
        var t = ((ICistronInterpreter<T>)this.SubInterpreters[0]).Interpret(subcistrons[0].ToBitReader());
        var u = ((ICistronInterpreter<U>)this.SubInterpreters[1]).Interpret(subcistrons[1].ToBitReader());
        var v = ((ICistronInterpreter<V>)this.SubInterpreters[2]).Interpret(subcistrons[2].ToBitReader());
        var result = combiner(t, u, v);
        return result;
    }
}