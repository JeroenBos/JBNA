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

// thoughts:
//
//abstract class ExplicitlyRangedFunction<T, TResult>
//{
//    public T Min { get; }
//    public T Max { get; }
//    abstract T Scale(T input); // or more like Map
//    public Func<T, TResult> Interpret { get; }
//}
//class DeferredRangedFunction<T, TResult>
//{
//    public Func<T, Func<Func<T, T> /*scale/map function of intput*/, TResult>> Interpret { get; }
//}
//class SelfdescriptivelyRangedFunction<T, TResult>
//{
//    public Func<T, TResult> Interpret { get; }
//}
// they're all ranged. It's just that some ranges are default and not necessarily sensible.
// where can ranges come from?
// - up front by all the way from the Nature.
// - encoded in the cistrons
// - the more generic case, only known just becore the function is to be called.

// in the first case the signature is going to be just T -> U
// in the second case, the signature is also going to be just T -> U
// in the third case, the signature is going to be (T -> T) -> T -> U
//     and when you look at it like that it might as well be that the (T -> T) part is the responsibility of the caller. 
//     That would simplify the story here significantly
//
// on second 



public static class FunctionalCistronExtensions
{
    class MappedInterpreter<T, TIntermediate, TResult> : ICistronInterpreter<Func<T, TResult>>
    {
        private readonly ICistronInterpreter<Func<T, TIntermediate>> baseInterpreter;
        private readonly Func<TIntermediate, TResult> map;

        public MappedInterpreter(ICistronInterpreter<Func<T, TIntermediate>> interpreter, Func<TIntermediate, TResult> map)
        {
            this.baseInterpreter = interpreter ?? throw new ArgumentNullException(nameof(interpreter));
            this.map = map ?? throw new ArgumentNullException(nameof(map));
        }

        public Func<T, TResult> Interpret(BitArrayReadOnlySegment cistron)
        {
            var baseFunction = baseInterpreter.Interpret(cistron);
            return x => map(baseFunction(x));
        }

        ulong ICistronInterpreter.MinBitCount => baseInterpreter.MinBitCount;
        ulong ICistronInterpreter.MaxBitCount => baseInterpreter.MaxBitCount;
    }
    /// <summary>
    /// Composition in the sense of g º f.
    /// </summary>
    class ComposedInterpreter<T, TIntermediate, TResult> : ICistronInterpreter<Func<T, TResult>>
    {
        private readonly ICistronInterpreter<Func<TIntermediate, TResult>> baseInterpreter;
        private readonly Func<T, TIntermediate> innerFunction;

        public ComposedInterpreter(ICistronInterpreter<Func<TIntermediate, TResult>> interpreter, Func<T, TIntermediate> innerFunction)
        {
            this.baseInterpreter = interpreter ?? throw new ArgumentNullException(nameof(interpreter));
            this.innerFunction  = innerFunction ?? throw new ArgumentNullException(nameof(innerFunction));
        }

        public Func<T, TResult> Interpret(BitArrayReadOnlySegment cistron)
        {
            var baseFunction = baseInterpreter.Interpret(cistron);
            return x => baseFunction(innerFunction(x));
        }

        ulong ICistronInterpreter.MinBitCount => baseInterpreter.MinBitCount;
        ulong ICistronInterpreter.MaxBitCount => baseInterpreter.MaxBitCount;
    }
    public static ICistronInterpreter<Func<T, TResult>> Map<T, TIntermediate, TResult>(this ICistronInterpreter<Func<T, TIntermediate>> interpreter, Func<TIntermediate, TResult> map)
    {
        return new MappedInterpreter<T, TIntermediate, TResult>(interpreter, map);
    }
    public static ICistronInterpreter<Func<T, TResult>> Compose<T, TIntermediate, TResult>(this ICistronInterpreter<Func<TIntermediate, TResult>> interpreter, Func<T, TIntermediate> innerFunction)
    {
        return new ComposedInterpreter<T, TIntermediate, TResult>(interpreter, innerFunction);
    }

}