
namespace JBNA.Interpreters;

/// <summary>
/// An interpreter that's just a discrimitating field and a wrapper around a main interpreter, 
/// and the field determines which same-allele cistrons are merged together.
/// Basically the field is a sub-allele, cooperating with <see cref="CistronSpec.Merger"/>
/// </summary>
public class MergableCistronInterpreter<T> : ICistronInterpreter<DiscriminatedMergableInterpretation<T>>, ICistronInterpreter<T>
{
    private const int fieldBitCount = 4;
    private readonly ICistronInterpreter<T> _interpreter;
    public ulong MinBitCount => _interpreter.MinBitCount + fieldBitCount;
    public ulong MaxBitCount => _interpreter.MaxBitCount + fieldBitCount;


    public MergableCistronInterpreter(ICistronInterpreter<T> interpreter)
    {
        _interpreter = interpreter ?? throw new ArgumentNullException(nameof(interpreter));
    }


    public DiscriminatedMergableInterpretation<T> Interpret(BitReader cistronReader)
    {
        ushort discriminator = cistronReader.ReadUInt16(fieldBitCount);
        var value = _interpreter.Interpret(cistronReader);
        return new(discriminator, value);
    }

    T ICistronInterpreter<T>.Interpret(BitReader cistronReader) => Interpret(cistronReader).Value;
    object ICistronInterpreter.Interpret(BitReader cistronReader) => Interpret(cistronReader);
}

///// <summary>
///// Genome doesn't know about this type as it's too late (Interpret has already been called). 
///// So then there's not really a point for this type anymore. Then the tuple type must be the carrier of the information.
///// </summary>
//public interface IMergableCistronInterpreter<T> : ICistronInterpreter<(int Discriminator, T Value)>
//{

//}

/// <summary>
/// <see cref="CistronSpec.Merger"/> should check for this type.
/// </summary>
/// <param name="Discriminator">The field that determines which similar allele underlying values should cooperate with each other.</param>
/// <param name="Value">The underlying value.</param>
public readonly record struct DiscriminatedMergableInterpretation<T>(ushort Discriminator, T Value)
{
}