using System.Text.RegularExpressions;

namespace JBNA;

/// <summary>
/// A one-dimensional pattern is a sequence that is either repeated or scaled onto the target dimension.
/// </summary>
class Pattern1DInterpreter : CompositeCistronInterpreter<bool, int, IDimensionfulDiscreteFunction, IDimensionfulDiscreteFunction>
{
    // TODO: maybe implement through composition, then we can implement DimensionfulContinuousFunction simultaneously in one class (also through composition of course)

    public static Pattern1DInterpreter Create(Nature nature, bool? repeats = null, int? patternLength = null, ICistronInterpreter<IDimensionfulDiscreteFunction>? patternInterpreter = null)
    {
        Assert(nature != null);
        Assert(patternLength == null || patternLength > 0);

        return Create(nature, 
                      repeats?.CreateConstantInterpreter(), 
                      patternLength?.CreateConstantInterpreter(), 
                      patternInterpreter);
    }
    public static Pattern1DInterpreter Create(Nature nature, ICistronInterpreter<bool>? repeatsInterpreter = null, ICistronInterpreter<int>? patternLengthInterpreter = null, ICistronInterpreter<IDimensionfulDiscreteFunction>? patternInterpreter = null)
    {
        Assert(nature != null);

        // TODO: this could use a static WeaklyCachedDictionary?

        return new Pattern1DInterpreter(
            repeatsInterpreter ?? BooleanInterpreter.Instance,
            patternLengthInterpreter ?? Int32Interpreter.Create(nature.PatternLengthBitCount),
            patternInterpreter ?? nature.FunctionFactory.DiscreteFunctionInterpreter
        );
    }

    protected Pattern1DInterpreter(ICistronInterpreter<bool> repeatsInterpreter, ICistronInterpreter<int> patternLengthInterpreter, ICistronInterpreter<IDimensionfulDiscreteFunction> patternInterpreter)
        : base(repeatsInterpreter, patternLengthInterpreter, patternInterpreter)
    {
    }

    /// <returns>a function taking a value plus the size of its dimension, and returns the value of the pattern at that position. </returns>
    protected override IDimensionfulDiscreteFunction Combine(bool repeats, int patternLength, IDimensionfulDiscreteFunction pattern)
    {
        return IDimensionfulDiscreteFunction.Create(impl);
        float impl(OneDimensionalDiscreteQuantity arg)
        {
            // let's say there's a lattice of length L
            // and a pattern of length P
            // then the incoming arg.Value is L

            // and intermediate quantity's Value is P
            // L and P are in the same units
            var (L_value, L, L_offset) = arg;

            int P = patternLength;
            int P_offset = 0;
            int P_value;

            if (repeats)
            {
                P_value = (L_value - L_offset) % P + P_offset;
            }
            else
            {
                P_value = (int)Math.Round((double)((L_value - L_offset) * P) / L) + P_offset;
            }
            var patternArg = new OneDimensionalDiscreteQuantity(P_value, P, P_offset);
            var result = pattern.Invoke(patternArg);
            return result;
        }
    }
}