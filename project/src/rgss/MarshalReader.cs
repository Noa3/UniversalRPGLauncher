using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalRPG.Rgss;

/// <summary>
/// Reads a Ruby Marshal stream as data.
/// </summary>
/// <remarks>
/// <para>
/// This is the format RPG Maker XP, VX and VX Ace use for their
/// <c>.rxdata</c>, <c>.rvdata</c> and <c>.rvdata2</c> files. Reading it is
/// necessary before anything about those games can be interpreted, and reading
/// it executes nothing: the result is a tree of values, not live objects.
/// </para>
/// <para>
/// A stream is a two byte version followed by one value. Values are a type
/// byte and a payload, and the payload's shape belongs to the type. Symbols and
/// objects are written once and referenced afterwards, so the reader keeps a
/// table and returns a link for a later occurrence rather than copying the
/// value. Resolving links would need the object identities the format does not
/// record, so a link stays a link and the table is exposed.
/// </para>
/// <para>
/// Integers are the part that is easy to get wrong. A marshalled integer is a
/// type byte and then one to five bytes, and the first of those encodes both the
/// sign and the width in one value. Eight values are special, the rest are a
/// sign extended byte with an offset of five. A reader that treats the first
/// byte as a plain length decodes small numbers correctly and everything else
/// as something plausible but wrong.
/// </para>
/// <para>
/// A stream this reader does not understand raises
/// <see cref="MarshalFormatException"/> rather than returning a partial tree.
/// A partial tree of a game's data is worse than an honest failure, because
/// nothing marks it as incomplete.
/// </para>
/// </remarks>
public sealed class MarshalReader
{
    /// <summary>The major version this reader implements.</summary>
    public const byte MajorVersion = 4;

    /// <summary>The minor version this reader implements.</summary>
    public const byte MinorVersion = 8;

    private const int MaxDepth = 64;
    private const int MaxCollectionLength = 1 << 24;

    private readonly byte[] _bytes;
    private int _offset;

    /// <summary>The symbols defined so far, in the order the stream defined them.</summary>
    public List<string> Symbols { get; } = [];

    /// <summary>How many objects the stream has defined so far.</summary>
    public int ObjectCount { get; private set; }

    public MarshalReader(byte[] pBytes)
    {
        _bytes = pBytes ?? throw new ArgumentNullException(nameof(pBytes));
    }

    /// <summary>Where the reader has reached in the stream.</summary>
    public int Position => _offset;

    /// <summary>
    /// Reads the stream's single top level value.
    /// </summary>
    /// <exception cref="MarshalFormatException">
    /// The stream is not a version this reader implements, or a value inside it
    /// is not a type the specification defines.
    /// </exception>
    public MarshalValue Read()
    {
        if (_bytes.Length < 2)
        {
            throw new MarshalFormatException(
                $"A marshal stream of {_bytes.Length} bytes is too short to hold a version.");
        }
        var major = _bytes[0];
        var minor = _bytes[1];
        if (major != MajorVersion)
        {
            throw new MarshalFormatException(
                $"A marshal stream of major version {major} cannot be read by a version "
                + $"{MajorVersion} reader. Different major versions are not compatible.");
        }
        if (minor > MinorVersion)
        {
            // A newer minor version may use types this reader has never heard
            // of, and reading part of it would be a guess.
            throw new MarshalFormatException(
                $"A marshal stream of minor version {minor} is newer than the "
                + $"{MinorVersion} this reader implements.");
        }
        _offset = 2;
        return ReadValue(0);
    }

    private MarshalValue ReadValue(int pDepth)
    {
        if (pDepth > MaxDepth)
        {
            throw new MarshalFormatException(
                $"A marshal stream nests deeper than the {MaxDepth} level limit.");
        }
        var type = ReadByte();
        switch (type)
        {
            case MarshalType.Nil:
                return new MarshalValue { Kind = "nil" };
            case MarshalType.True:
                return new MarshalValue { Kind = "true" };
            case MarshalType.False:
                return new MarshalValue { Kind = "false" };
            case MarshalType.Integer:
                return new MarshalValue { Kind = "integer", Integer = ReadLong() };
            case MarshalType.Float:
                return ReadFloat();
            case MarshalType.Symbol:
                return ReadSymbol();
            case MarshalType.SymbolLink:
                return ReadSymbolLink();
            case MarshalType.String:
                return ReadString();
            case MarshalType.ObjectLink:
                return ReadObjectLink();
            case MarshalType.Array:
                return ReadArray(pDepth);
            case MarshalType.Hash:
            case MarshalType.HashWithDefault:
                return ReadHash(pDepth, type == MarshalType.HashWithDefault);
            case MarshalType.Object:
            case MarshalType.UserMarshal:
            case MarshalType.UserDefined:
            case MarshalType.Class:
            case MarshalType.Module:
            case MarshalType.UserClass:
            case MarshalType.Struct:
            case MarshalType.Regexp:
                return ReadTyped(pDepth, type);
            case MarshalType.InstanceVariables:
                return ReadWithInstanceVariables(pDepth);
            case MarshalType.Extended:
                return ReadExtended(pDepth);
            case MarshalType.Bignum:
                return ReadBignum();
            default:
                throw new MarshalFormatException(
                    $"0x{type:X2} is not a type the marshal specification defines, at byte {_offset - 1}.");
        }
    }

    private byte ReadByte()
    {
        if (_offset >= _bytes.Length)
        {
            throw new MarshalFormatException(
                $"A marshal stream ends at byte {_offset} while a value was still expected.");
        }
        return _bytes[_offset++];
    }

    /// <summary>
    /// Reads a marshalled integer.
    /// </summary>
    /// <remarks>
    /// The first byte after the type byte carries the sign and the width at
    /// once. Eight values are special: zero, a positive byte, a negative byte,
    /// and three more pairs that add one byte of width each. Everything else is
    /// a sign extended byte with an offset of five, which is how a value from
    /// minus four to one hundred and twenty fits in one byte.
    /// </remarks>
    private long ReadLong()
    {
        // This is r_long out of Ruby 1.8.7, in the order that source reads:
        //
        //   the count is a signed byte
        //   zero                        is the number zero
        //   five to 127                 is the number minus five, in one byte
        //   minus 129 to minus 5        is the number plus five, in one byte
        //   one to four                 is that many bytes as they are
        //   minus one to minus four     is that many bytes with the sign
        //                                carried to the width of the count
        //
        // The first version of this was a table of the eight byte values it had
        // seen, and the table disagreed with the source in both directions: it
        // read a one byte negative as the negative of the byte rather than as a
        // number whose top bit is set, and it read two and three byte negatives
        // one byte too many, taking the first byte of whatever followed in the
        // file. A game holding a negative coordinate would then have had the
        // next value's bytes inside its own.
        var count = (sbyte)ReadByte();

        if (count == 0)
        {
            return 0;
        }
        if (count > 0)
        {
            if (count < 5)
            {
                return ReadWideLittleEndian(count);
            }

            // Five to one hundred and twenty seven is the one byte form: the
            // number with five taken off. Every one of those counts is a number
            // and not a width, so there is nothing here to refuse, and a check
            // that refused one of them would refuse a length a game writes for
            // every list it has.
            return count - 5;
        }
        if (count < -4)
        {
            return count + 5;
        }

        return ReadSignExtended(-count);
    }

    /// <summary>
    /// A number of the given width whose top bit is set, so it runs to the edge
    /// of the width and has to be carried as a signed value.
    /// </summary>
    /// <remarks>
    /// The short forms of a negative number are written as a negative count and
    /// then as many bytes as the count says, with every byte after the first set
    /// to its top value, so the whole thing is the number sign extended to the
    /// width. Negating the unsigned value instead, which looks the same for a
    /// one byte number and is not the same for any other, reads one byte too many
    /// and takes the first byte of whatever follows in the file. A negative
    /// number in a game's data would then come back as a number with bits from
    /// the next value in it, which no later layer can tell from a real one.
    /// </remarks>
    private long ReadSignExtended(int pByteCount)
    {
        // The source starts at all ones and then, for each byte of the count,
        // clears that byte's place and writes the byte read. So the value that
        // comes out is the one whose bits above the count's width stay set, and
        // the top of it is the sign. Testing only the top bit of what was read,
        // which is what this did at first, is the same rule read the wrong way
        // round: it says -256 is zero because the byte that was read is zero,
        // while what makes it negative is the width it was written in.
        long value = -1;
        for (var index = 0; index < pByteCount; index++)
        {
            value &= ~((long)0xFF << (index * 8));
            value |= (long)ReadByte() << (index * 8);
        }

        return value;
    }

    /// <summary>Reads a little endian unsigned value of the given width.</summary>
    private long ReadWideLittleEndian(int pByteCount)
    {
        long value = 0;
        for (var index = 0; index < pByteCount; index++)
        {
            value |= (long)ReadByte() << (index * 8);
        }
        return value;
    }

    /// <summary>
    /// A whole number too large for the small form, written as decimal digits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In the Ruby the engines of this repository's line run, a whole number
    /// that fits in thirty one bits is written as a small number and anything
    /// larger is written as a signed run of decimal digits, a leading sign and
    /// then one byte per digit. That is a different thing from the large-number
    /// form in Ruby 3, where the letters are the other way round and the digits
    /// are binary shorts, so the two cannot be read with one reader and a file
    /// from a modern Ruby is not a file from an engine.
    /// </para>
    /// <para>
    /// A game's data holds money, a stat, a coordinate and an identifier, and any
    /// of those can pass thirty one bits: a gold total that has been saved often
    /// does. Refusing every such file would refuse a game that works, so the
    /// digits are read and the number is carried when it fits this machine's
    /// whole number, and refused with its digits in the reason when it does not.
    /// The reason matters, because "a number too large" and "a corrupt file" are
    /// different faults and a reader that cannot tell them apart sends whoever is
    /// looking for the fault looking in the wrong place.
    /// </para>
    /// </remarks>
    private MarshalValue ReadBignum()
    {
        var sign = ReadByte();
        if (sign != (byte)'+' && sign != (byte)'-')
        {
            throw new MarshalFormatException(
                $"A whole number's sign at byte {_offset - 1} is"
                + $" 0x{sign:X2}, which is neither a plus nor a minus.");
        }

        var length = ReadLength("a whole number");
        if (length <= 0)
        {
            throw new MarshalFormatException(
                "A whole number of zero digits is not a number.");
        }
        if (length > 1024)
        {
            throw new MarshalFormatException(
                $"A whole number of {length} digits at byte {_offset} is longer than"
                + " this reader will hold, which a game's data never is.");
        }

        var digits = new StringBuilder(length);
        for (var digit = 0; digit < length; digit++)
        {
            var value = ReadByte();
            if (value < (byte)'0' || value > (byte)'9')
            {
                throw new MarshalFormatException(
                    $"A whole number's digit at byte {_offset - 1} is 0x{value:X2},"
                    + " which is not a digit.");
            }

            digits.Append((char)value);
        }

        var text = digits.ToString();
        if (!long.TryParse(
            text, System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out var number))
        {
            throw new MarshalFormatException(
                $"A whole number of {length} digits does not fit this machine's"
                + " whole number, so it is read but not carried.");
        }

        return new MarshalValue
        {
            Kind = "integer",
            Integer = sign == (byte)'-' ? -number : number,
        };
    }

    private MarshalValue ReadFloat()
    {
        var text = ReadAsciiUntilTerminator();
        switch (text)
        {
            case "inf":
                return new MarshalValue { Kind = "float", Real = double.PositiveInfinity };
            case "-inf":
                return new MarshalValue { Kind = "float", Real = double.NegativeInfinity };
            case "nan":
                return new MarshalValue { Kind = "float", Real = double.NaN };
            default:
                if (!double.TryParse(
                    text, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var value))
                {
                    throw new MarshalFormatException(
                        $"A marshal stream has the float \"{text}\", which is not a number.");
                }
                return new MarshalValue { Kind = "float", Real = value };
        }
    }

    private string ReadAsciiUntilTerminator()
    {
        var start = _offset;
        while (_offset < _bytes.Length && _bytes[_offset] != 0x00)
        {
            _offset++;
        }
        if (_offset >= _bytes.Length)
        {
            throw new MarshalFormatException(
                $"A marshal stream's float at byte {start} is not terminated.");
        }
        var text = Encoding.ASCII.GetString(_bytes, start, _offset - start);
        _offset++;
        return text;
    }

    private MarshalValue ReadSymbol()
    {
        var name = ReadBytes();
        var text = Encoding.UTF8.GetString(name);
        // A symbol's index is the order it was defined in, not the order it was
        // written in, so the table grows as symbols are read.
        var index = Symbols.Count;
        Symbols.Add(text);
        return new MarshalValue { Kind = "symbol", Text = text, Integer = index };
    }

    private MarshalValue ReadSymbolLink()
    {
        var index = (int)ReadLong();
        if (index < 0 || index >= Symbols.Count)
        {
            throw new MarshalFormatException(
                $"A marshal stream links to symbol {index}, but only {Symbols.Count} "
                + "symbols have been defined.");
        }
        return new MarshalValue
        {
            Kind = "symbol link",
            Text = Symbols[index],
            Integer = index,
            Link = new MarshalLink { Kind = "symbol", Index = index },
        };
    }

    private MarshalValue ReadObjectLink()
    {
        var index = (int)ReadLong();
        if (index < 1 || index > ObjectCount)
        {
            throw new MarshalFormatException(
                $"A marshal stream links to object {index}, but only {ObjectCount} "
                + "objects have been defined. Object links are one indexed.");
        }
        return new MarshalValue
        {
            Kind = "object link",
            Integer = index,
            Link = new MarshalLink { Kind = "object", Index = index },
        };
    }

    private MarshalValue ReadString()
    {
        var raw = ReadBytes();
        return new MarshalValue
        {
            Kind = "string",
            Bytes = raw,
            Text = Encoding.UTF8.GetString(raw),
            Integer = ++ObjectCount,
        };
    }

    private byte[] ReadBytes()
    {
        var length = ReadLong();
        if (length < 0 || length > MaxCollectionLength)
        {
            throw new MarshalFormatException(
                $"A marshal stream declares {length} bytes, outside 0 to {MaxCollectionLength}.");
        }
        if (_offset + length > _bytes.Length)
        {
            throw new MarshalFormatException(
                $"A marshal stream declares {length} bytes at byte {_offset}, which runs"
                + " past the end of the stream.");
        }
        // The bound above keeps this inside the range of an int.
        var count = (int)length;
        var raw = new byte[count];
        Array.Copy(_bytes, _offset, raw, 0, count);
        _offset += count;
        return raw;
    }

    private MarshalValue ReadArray(int pDepth)
    {
        var objectIndex = ++ObjectCount;
        var count = ReadLength("array");
        var items = new List<MarshalValue>(Math.Min(count, 1024));
        for (var element = 0; element < count; element++)
        {
            items.Add(ReadValue(pDepth + 1));
        }
        return new MarshalValue
        {
            Kind = "array",
            Items = items,
            Integer = objectIndex,
        };
    }

    private MarshalValue ReadHash(int pDepth, bool pHasDefault)
    {
        var objectIndex = ++ObjectCount;
        var count = ReadLength("hash");
        var keys = new List<string>(Math.Min(count, 1024));
        var items = new List<MarshalValue>(Math.Min(count * 2, 1024));
        for (var index = 0; index < count; index++)
        {
            var key = ReadValue(pDepth + 1);
            var value = ReadValue(pDepth + 1);
            keys.Add(DescribeKey(key));
            items.Add(key);
            items.Add(value);
        }
        if (pHasDefault)
        {
            var defaultValue = ReadValue(pDepth + 1);
            keys.Add("__default__");
            items.Add(defaultValue);
        }
        return new MarshalValue
        {
            Kind = pHasDefault ? "hash with default" : "hash",
            Keys = keys,
            Items = items,
            Integer = objectIndex,
        };
    }

    private MarshalValue ReadTyped(int pDepth, byte pType)
    {
        // A regexp is the one type in this group that carries no class name.
        if (pType == MarshalType.Regexp)
        {
            var regexpSource = ReadBytes();
            var regexpOptions = (sbyte)ReadByte();
            return new MarshalValue
            {
                Kind = "regexp",
                Bytes = regexpSource,
                Text = Encoding.UTF8.GetString(regexpSource),
                Integer = regexpOptions,
            };
        }

        var className = ReadClassName(pType);
        switch (pType)
        {
            case MarshalType.Object:
            {
                var count = ReadLength("object");
                var keys = new List<string>(Math.Min(count, 1024));
                var items = new List<MarshalValue>(Math.Min(count * 2, 1024));
                for (var index = 0; index < count; index++)
                {
                    keys.Add(ReadInstanceVariableName());
                    items.Add(ReadValue(pDepth + 1));
                }
                return new MarshalValue
                {
                    Kind = "object",
                    ClassName = className,
                    Keys = keys,
                    Items = items,
                    Integer = ++ObjectCount,
                };
            }

            case MarshalType.Struct:
            {
                var count = ReadLength("struct");
                var keys = new List<string>(Math.Min(count, 1024));
                var items = new List<MarshalValue>(Math.Min(count * 2, 1024));
                for (var index = 0; index < count; index++)
                {
                    keys.Add(ReadInstanceVariableName());
                    items.Add(ReadValue(pDepth + 1));
                }
                return new MarshalValue
                {
                    Kind = "struct",
                    ClassName = className,
                    Keys = keys,
                    Items = items,
                    Integer = ++ObjectCount,
                };
            }


            case MarshalType.UserDefined:
            {
                var payload = ReadBytes();
                return new MarshalValue
                {
                    Kind = "user defined",
                    ClassName = className,
                    Bytes = payload,
                    Integer = ++ObjectCount,
                };
            }

            case MarshalType.UserClass:
            {
                var wrapped = ReadValue(pDepth + 1);
                var one = new List<MarshalValue> { wrapped };
                return new MarshalValue
                {
                    Kind = "user class",
                    ClassName = className,
                    Items = one,
                    Integer = ++ObjectCount,
                };
            }

            case MarshalType.UserMarshal:
            {
                var data = ReadValue(pDepth + 1);
                var one = new List<MarshalValue> { data };
                return new MarshalValue
                {
                    Kind = "user marshal",
                    ClassName = className,
                    Items = one,
                    Integer = ++ObjectCount,
                };
            }

            case MarshalType.Class:
            case MarshalType.Module:
            default:
                return new MarshalValue
                {
                    Kind = pType == MarshalType.Class ? "class" : "module",
                    ClassName = className,
                    Integer = ++ObjectCount,
                };
        }
    }

    private string ReadClassName(byte pType)
    {
        var value = ReadValue(1);
        if (value.Text == null)
        {
            throw new MarshalFormatException(
                $"A marshal stream's type 0x{(int)pType:X2} is followed by something "
                + "that is not a class name.");
        }
        return value.Text;
    }

    private string ReadInstanceVariableName()
    {
        var value = ReadValue(1);
        var name = value.Text;
        if (name == null)
        {
            throw new MarshalFormatException(
                "A marshal stream's instance variable name is not a symbol.");
        }
        return name;
    }

    private MarshalValue ReadWithInstanceVariables(int pDepth)
    {
        var inner = ReadValue(pDepth + 1);
        var count = ReadLength("instance variables");
        var keys = new List<string>(inner.Keys) { };
        keys.AddRange(new string[count]);
        var items = new List<MarshalValue>(inner.Items);
        for (var index = 0; index < count; index++)
        {
            keys[inner.Keys.Count + index] = ReadInstanceVariableName();
            items.Add(ReadValue(pDepth + 1));
        }
        return new MarshalValue
        {
            Kind = inner.Kind,
            ClassName = inner.ClassName,
            Integer = inner.Integer,
            Bytes = inner.Bytes,
            Text = inner.Text,
            Real = inner.Real,
            Link = inner.Link,
            Keys = keys,
            Items = items,
        };
    }

    private MarshalValue ReadExtended(int pDepth)
    {
        var inner = ReadValue(pDepth + 1);
        // The module an object is extended by follows the object. It is read so
        // the cursor ends up in the right place, and the wrapper keeps the
        // inner value's own fields rather than flattening the two together.
        _ = ReadClassName(MarshalType.Extended);
        return new MarshalValue
        {
            Kind = inner.Kind,
            ClassName = inner.ClassName,
            Integer = inner.Integer,
            Bytes = inner.Bytes,
            Text = inner.Text,
            Real = inner.Real,
            Link = inner.Link,
            Keys = inner.Keys,
            Items = inner.Items,
        };
    }

    private int ReadLength(string pWhat)
    {
        var length = ReadLong();
        if (length < 0 || length > MaxCollectionLength)
        {
            throw new MarshalFormatException(
                $"A marshal stream's {pWhat} declares {length} elements, outside 0 to "
                + $"{MaxCollectionLength}.");
        }
        return (int)length;
    }

    private static string DescribeKey(MarshalValue pKey)
    {
        return pKey.Text ?? pKey.ToString()!;
    }
}
