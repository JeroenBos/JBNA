namespace JBNA;
using IDiscreteFunction = IIntegratedDimensionfulDiscreteFunction;
using IContinuousFunction = IIntegratedDimensionfulContinuousFunction;

/// <summary>
/// This represents the collection of all function and function-derived interpreters. Handles instantiating and collecting them.
/// </summary>
public class FunctionSpecFactory
{
    private readonly ImmutableArray<ICistronInterpreter<IDiscreteFunction>> discreteFunctionTypes;
    public int DiscreteFunctionTypeCount => discreteFunctionTypes.Length;
    private readonly ImmutableArray<ICistronInterpreter<IContinuousFunction>> continuousFunctionTypes;
    public int ContinuousFunctionTypeCount => continuousFunctionTypes.Length;

    public Nature Nature { get; }
    // public ICistronInterpreter<DimensionfulDiscreteFunction> DiscretePatternInterpreter { get; } as opposed to the function
    /// <summary>
    /// Converts the cistron into a pattern function.
    /// Gets a function that gets the value of the pattern given the position and length of the (only) dimension.
    /// The cistron is expected to start with a caller-responsible number of bits describing the 'type of pattern'.
    /// Also configuration, e.g. whether the pattern repeats or is stretched to a particular size, could be at the start of the cistron.
    /// </summary>
    public ICistronInterpreter<IDiscreteFunction> FourierFunctionInterpreter { get; }
    /// <summary>
    /// An interpreter that assumes self-encoded which function type is to be interpreted.
    /// </summary>
    public ICistronInterpreter<IDiscreteFunction> DiscreteFunctionInterpreter { get; }
    /// <summary>
    /// An interpreter that assumes self-encoded which function type is to be interpreted.
    /// </summary>
    public ICistronInterpreter<IContinuousFunction> ContinuousFunctionInterpreter { get; }



    // distribution members

    private readonly WeaklyCachedFactoryWithNullKey<ICistronInterpreter<IDiscreteFunction>, ICistronInterpreter<IIntegratedDimensionfulDiscreteFunction>> discreteDistributionInterpreters;
    public ICistronInterpreter<IIntegratedDimensionfulDiscreteFunction> GetDiscreteDistributionInterpreter(ICistronInterpreter<IDiscreteFunction>? functionInterpreter = null)
    {
        return discreteDistributionInterpreters[functionInterpreter];
    }



    // pattern members

    /// <summary>
    /// Gets a discrete pattern interpreter. A pattern is a sequence that is either repeated or scaled onto the discrete target dimension.
    /// </summary>
    public ICistronInterpreter<IDimensionfulDiscreteFunction> DiscretePatternInterpreter { get; }
    public ICistronInterpreter<IDimensionfulDiscreteFunction> GetDiscretePatternInterpreter(bool? repeats = null, int? patternLength = null)
    {
        return Pattern1DInterpreter.Create(this.Nature, repeats, patternLength);
    }
    //public ICistronInterpreter<DimensionfulContinuousFunction> ContinuousPatternInterpreter { get; }


    internal FunctionSpecFactory(Nature nature, Action<FunctionSpecFactory>? setFunctionFactory)
    {
        this.Nature = nature ?? throw new ArgumentNullException(nameof(nature));
        setFunctionFactory?.Invoke(this);

        // all functions types:
        this.FourierFunctionInterpreter = FourierFunctionCistronInterpreter.Singleton;

        // aggregate all function types:
        static IEnumerable<ICistronInterpreter<IDiscreteFunction>> all(FunctionSpecFactory @this)
        {
            yield return @this.FourierFunctionInterpreter;
        }
        this.discreteFunctionTypes = all(this).ToImmutableArray();
        this.DiscreteFunctionInterpreter = ChoiceCistronInterpreter.Create(discreteFunctionTypes, reader => reader.ReadInt32(this.Nature.FunctionTypeBitCount) % this.discreteFunctionTypes.Length);
        this.continuousFunctionTypes = discreteFunctionTypes.OfType<ICistronInterpreter<IContinuousFunction>>().ToImmutableArray();
        this.ContinuousFunctionInterpreter = ChoiceCistronInterpreter.Create(continuousFunctionTypes, reader => reader.ReadInt32(this.Nature.FunctionTypeBitCount) % this.continuousFunctionTypes.Length);

        // all function-derived
        this.DiscretePatternInterpreter = Pattern1DInterpreter.Create(this.Nature, null as bool?);
        this.discreteDistributionInterpreters = WeaklyCachedFactory.CreateWithNullableKey<ICistronInterpreter<IDiscreteFunction>, ICistronInterpreter<IIntegratedDimensionfulDiscreteFunction>>(functionInterpreter => new DistributionInterpreter(functionInterpreter ?? this.DiscreteFunctionInterpreter));
        this.InterpretImplicitlyRanged1DFunctionInterpreter = createInterpretImplicitlyRanged1DFunctionInterpreter();
    }
    /// <summary>
    /// An interpreter that reads its own range.
    /// </summary>
    public ICistronInterpreter<Func<int, float>> InterpretImplicitlyRanged1DFunctionInterpreter { get; }
    private ICistronInterpreter<Func<int, float>> createInterpretImplicitlyRanged1DFunctionInterpreter()
    {
        return CompositeCistronInterpreter.Create(Int32Interpreter.Create(Nature.FunctionRangeBitCount), Int32Interpreter.Create(Nature.FunctionRangeBitCount), Nature.FunctionFactory.DiscreteFunctionInterpreter, Combiner);
        
        Func<int, float> Combiner(int extremum1, int extremum2, IDiscreteFunction f)
        {
            const int minimumRangeDimensionality = 4; // to require less bits to represent the range, because the dimensionality will probably never be under this

            var min = Math.Min(extremum1, extremum2) * minimumRangeDimensionality;
            var max = Math.Max(extremum1, extremum2) * minimumRangeDimensionality;

            return x => f.Invoke(new OneDimensionalDiscreteQuantity(x, max - min, min));
        }
    }
}
