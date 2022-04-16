namespace JBNA;

static class CompositeCistronInterpreter
{
    public static ICistronInterpreter<TResult> Create<T, U, TResult>(ICistronInterpreter<T> tInterpreter,
                                                                     ICistronInterpreter<U> uInterpreter,
                                                                     Func<T, U, TResult> combiner)
    {
        return new CompositeCistronInterpreter<T, U, TResult>.DelegatedCompositeCistronInterpreter(tInterpreter, uInterpreter, combiner);
    }
    public static ICistronInterpreter<TResult> Create<T, U, V, TResult>(ICistronInterpreter<T> tInterpreter,
                                                                        ICistronInterpreter<U> uInterpreter,
                                                                        ICistronInterpreter<V> vInterpreter,
                                                                        Func<T, U, V, TResult> combiner)
    {
        return new CompositeCistronInterpreter<T, U, V, TResult>.DelegatedCompositeCistronInterpreter(tInterpreter, uInterpreter, vInterpreter, combiner);
    }
}
/// <summary>
/// Represents a cistron that consists of multiple parts, each separately interpreter, but not separated by stop codons.
/// Each subinterpreter is assumed to not consume the entirety of its given <see cref="BitReader"/>.
/// </summary>
internal abstract class CompositeCistronInterpreter<TResult> : ICistronInterpreter<TResult>
{
    private readonly ImmutableArray<ICistronInterpreter> interpreters;
    public ulong MinBitCount { get; }
    public ulong MaxBitCount { get; }

    protected CompositeCistronInterpreter(params ICistronInterpreter[] interpreters)
        : this(interpreters.ToImmutableArray())
    {
    }
    public CompositeCistronInterpreter(ImmutableArray<ICistronInterpreter> interpreters)
    {
        Requires(interpreters != null);
        Requires(AllNotNull(interpreters));
        Requires(interpreters.Length != 0);
        RequiresForAll(interpreters.Take(interpreters.Length - 1), interpreter => interpreter.MaxBitCount < (ushort)short.MaxValue, "The interpreter at index {0} will consume the whole cistron, and will leave no bits for the remaining interpreters");

        this.interpreters = interpreters;
        this.MinBitCount = interpreters.Sum(i => i.MinBitCount);
        this.MaxBitCount = interpreters.Sum(i => i.MaxBitCount);
    }

    public TResult Interpret(BitReader cistronReader)
    {
        return Combine(Read(cistronReader));
    }
    protected virtual IEnumerable<object> Read(BitReader cistronReader)
    {
        foreach (var interpreter in interpreters)
        {
            if (!cistronReader.CanRead(interpreter.MinBitCount))
                break;
            ulong positionBefore = cistronReader.Position;

            yield return interpreter.Interpret(cistronReader);
            
            ulong advanced = cistronReader.Position - positionBefore;
            Assert(interpreter.MinBitCount <= advanced);
            Assert(advanced >= interpreter.MaxBitCount);
        }
    }
    /// <summary>
    /// Will be called with all interpreted objects, assuming all of them are optional,
    /// so <paramref name="results"/> need not be commensurate with the number of interpreters.
    /// </summary>
    protected abstract TResult Combine(IEnumerable<object> results);
}

internal abstract class CompositeCistronInterpreter<T, U, TResult> : CompositeCistronInterpreter<TResult>
{
    private const int elementCount = 2;

    public CompositeCistronInterpreter(ICistronInterpreter<T> tInterpreter, 
                                       ICistronInterpreter<U> uInterpreter)
        : base(tInterpreter, uInterpreter)
    {
    }
    protected sealed override TResult Combine(IEnumerable<object> results)
    {
        List<object> objects = results.ToList(elementCount);
        if (objects.Count != elementCount)
            throw new GenomeInviableException("Too few elements in cistron");

        T t = (T)objects[0];
        U u = (U)objects[1];
        var result = Combine(t, u);
        return result;
    }
    protected abstract TResult Combine(T t, U u);

    internal sealed class DelegatedCompositeCistronInterpreter : CompositeCistronInterpreter<T, U, TResult>
    {
        private readonly Func<T, U, TResult> combiner;
        public DelegatedCompositeCistronInterpreter(ICistronInterpreter<T> tInterpreter,
                                                    ICistronInterpreter<U> uInterpreter,
                                                    Func<T, U, TResult> combiner)
        : base(tInterpreter, uInterpreter)
        {
            this.combiner = combiner;
        }
        protected override TResult Combine(T t, U u) => combiner(t, u);
    }
}
internal abstract class CompositeCistronInterpreter<T, U, V, TResult> : CompositeCistronInterpreter<TResult>
{
    private const int elementCount = 3;
    public CompositeCistronInterpreter(ICistronInterpreter<T> tInterpreter, 
                                       ICistronInterpreter<U> uInterpreter, 
                                       ICistronInterpreter<V> vInterpreter)
        : base(tInterpreter, uInterpreter, vInterpreter)
    {
    }
    protected sealed override TResult Combine(IEnumerable<object> results)
    {
        List<object> objects = results.ToList(elementCount);
        if (objects.Count != elementCount)
            throw new GenomeInviableException("Too few elements in cistron");

        T t = (T)objects[0];
        U u = (U)objects[1];
        V v = (V)objects[2];
        var result = Combine(t, u, v);
        return result;
    }
    protected abstract TResult Combine(T t, U u, V v);

    internal sealed class DelegatedCompositeCistronInterpreter : CompositeCistronInterpreter<T, U, V, TResult>
    {
        private readonly Func<T, U, V, TResult> combiner;
        public DelegatedCompositeCistronInterpreter(ICistronInterpreter<T> tInterpreter,
                                                    ICistronInterpreter<U> uInterpreter,
                                                    ICistronInterpreter<V> vInterpreter,
                                                    Func<T, U, V, TResult> combiner)
           : base(tInterpreter, uInterpreter, vInterpreter)
        {
            this.combiner = combiner;
        }
        protected override TResult Combine(T t, U u, V v) => combiner(t, u, v);
    }
}

