namespace JBNA;

public static class ChoiceCistronInterpreter
{
    public static ICistronInterpreter<T> Create<T>(ImmutableArray<ICistronInterpreter<T>> options, Func<BitReader, int> choose)
    {
        return new ChoiceCistronInterpreter<T>(options, choose);
    }
    public static ICistronInterpreter<T> Create<T>(IEnumerable<ICistronInterpreter<T>> options, Func<BitReader, int> choose)
    {
        return new ChoiceCistronInterpreter<T>(options.ToImmutableArray(), choose);
    }
}
sealed class ChoiceCistronInterpreter<T> : ICistronInterpreter<T>
{

    private readonly ImmutableArray<ICistronInterpreter<T>> options;
    private readonly Func<BitReader, int> choose;
    public ulong MinBitCount { get; }
    public ulong MaxBitCount { get; }

    internal ChoiceCistronInterpreter(ImmutableArray<ICistronInterpreter<T>> options, Func<BitReader, int> choose)
    {
        Requires(options != null);
        Requires(choose != null);
        Requires(options.Length > 0);
        Requires(AllNotNull(options));

        this.options = options;
        this.choose = choose;
        (this.MinBitCount, this.MaxBitCount) = ComputeMinMaxBitCount(options);

        static (ulong, ulong) ComputeMinMaxBitCount(IEnumerable<ICistronInterpreter> subInterpreters)
        {
            var min = subInterpreters.Min(spec => spec.MinBitCount);
            var max = subInterpreters.Max(spec => spec.MaxBitCount);
            return (min, max);
        }
    }


    public T Interpret(BitReader cistronReader)
    {
        var choiceIndex = this.choose(cistronReader);
        Assert(0 <= choiceIndex && choiceIndex < this.options.Length);

        var choice = options[choiceIndex];
        return choice.Interpret(cistronReader);
    }
}
