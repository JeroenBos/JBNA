using JBSnorro.Collections;

namespace JBNA;

public delegate TResult RangelessDelegate<T, TResult>(T input, T min, T max);
public interface IRangelessFunctionSpec<T, TResult> : ICistronInterpreter<Func<T, T, T, TResult>>
{
    new RangelessDelegate<T, TResult> Interpret(BitArrayReadOnlySegment cistron);
}


public static class CistronExtensions
{
    /// <summary>
    /// Creates a rangeless function from a function (with domain D and range R) and a function mapping from a value and range onto D.
    /// <summary>
    /// <typeparam="TResult"> Represents the range R. </typeparam>
    /// <typeparam="TDomain"> Represents the domain D. </typeparam>
    public static IRangelessFunctionSpec<T, TResult> ToRangelessFunctionSpec<T, TDomain, TResult>(
        this ICistronInterpreter<Func<TDomain, TResult>> rangedFunctionSpec,
        RangelessDelegate<T, TDomain> mapToRange)
    {
        return new RangelessFunctionSpec<T, TDomain, TResult>(rangedFunctionSpec, mapToRange);
    }

    public static ICistronInterpreter<Func<T, TResult>> ToRangedFunctionSpec<T, TDomain, TResult>(
        this IRangelessFunctionSpec<TDomain, TResult> rangelessFunctionSpec,
        TDomain min,
        TDomain max,
        Func<T, TDomain, TDomain, TDomain> map)
    {
        return new RangedFunctionSpec<T, TDomain, TResult>(rangelessFunctionSpec, min, max, map);
    }
    public static ICistronInterpreter<Func<T, TResult>> ToRangedFunctionSpec<T, TDomain, TResult>(
        this IRangelessFunctionSpec<TDomain, TResult> rangelessFunctionSpec,
        TDomain min,
        TDomain max,
        Func<T, TDomain> map)
    {
        return rangelessFunctionSpec.ToRangedFunctionSpec<T, TDomain, TResult>(min, max, (input, min, max) => map(input));
    }
}


class RangedFunctionSpec<T, TIntermediate, TResult> : ICistronInterpreter<Func<T, TResult>>
{
    public ulong MinBitCount => rangelessFunctionSpec.MinBitCount;
    public ulong MaxBitCount => rangelessFunctionSpec.MaxBitCount;
    private readonly IRangelessFunctionSpec<TIntermediate, TResult> rangelessFunctionSpec;
    private readonly TIntermediate min;
    private readonly TIntermediate max;
    private readonly Func<T, TIntermediate, TIntermediate, TIntermediate> map;
    public RangedFunctionSpec(IRangelessFunctionSpec<TIntermediate, TResult> rangelessFunctionSpec,
                              TIntermediate min,
                              TIntermediate max,
                              Func<T, TIntermediate /*min*/, TIntermediate /*max*/, TIntermediate> map)
    {
        this.rangelessFunctionSpec = rangelessFunctionSpec;
        this.min = min;
        this.max = max;
        this.map = map;
    }
    public Func<T, TResult> Interpret(BitArrayReadOnlySegment cistron)
    {
        return f;
        TResult f(T input)
        {
            RangelessDelegate<TIntermediate, TResult> g = this.rangelessFunctionSpec.Interpret(cistron);
            var mappedInput = this.map(input, this.min, this.max);
            var result = g(mappedInput, this.min, this.max);
            return result;
        }
    }

}


class RangelessFunctionSpec<T, TIntermediate, TResult> : IRangelessFunctionSpec<T, TResult>
{
    public ulong MinBitCount => rangedFunctionSpec.MinBitCount;
    public ulong MaxBitCount => rangedFunctionSpec.MaxBitCount;
    private readonly ICistronInterpreter<Func<TIntermediate, TResult>> rangedFunctionSpec;
    private readonly RangelessDelegate<T, TIntermediate> mapToRange;
    public RangelessFunctionSpec(ICistronInterpreter<Func<TIntermediate, TResult>> rangedFunctionSpec,
                                 RangelessDelegate<T, TIntermediate> mapToRange)
    {
        this.rangedFunctionSpec = rangedFunctionSpec;
        this.mapToRange = mapToRange;
    }
    public RangelessDelegate<T, TResult> Interpret(BitArrayReadOnlySegment cistron)
    {
        // we assume the function kind is already known. That is, this is a particular kind of function,
        // as defined by the rangedFunctionSpec.
        // one thing that functions may differ on is in whether they have a prespecified range


        return f;
        TResult f(T input, T min, T max)
        {
            var inRange = this.mapToRange(input, min, max);
            Func<TIntermediate, TResult> g = this.rangedFunctionSpec.Interpret(cistron);
            var result = g(inRange);
            return result;
        }
    }

    Func<T, T, T, TResult> ICistronInterpreter<Func<T, T, T, TResult>>.Interpret(BitArrayReadOnlySegment cistron)
    {
        return new Func<T, T, T, TResult>(Interpret(cistron));
    }
}
