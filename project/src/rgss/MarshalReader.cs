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
                throw new MarshalFormatException(
                    "A bignum cannot be read by this reader, because a game's data uses"
                    + " fixnums for every value that fits and a bignum here would need"
                    + " arbitrary precision this reader does not carry.");
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
        var first = ReadByte();
        switch (first)
        {
            case 0x00:
                return 0;
            case 0x01:
                return ReadByte();
            case 0xFF:
                return -ReadByte();
            case 0x02:
                return ReadWideLittleEndian(2);
            case 0xFE:
                return -ReadWideLittleEndian(2);
            case 0x03:
                return ReadWideLittleEndian(3);
            case 0xFD:
                return -ReadWideLittleEndian(3);
            case 0x04:
                return ReadWideLittleEndian(4);
            case 0xFC:
                return -ReadWideLittleEndian(4);
            default:
            {
                // A sign extended byte with an offset of five. Values from
                // 0x05 to 0x7F are five through one hundred and twenty six, and
                // 0xFB down to 0x80 are minus four down to minus one hundred
                // and twenty one.
                var signed = (sbyte)first;
                return signed > 0 ? signed - 5 : signed + 5;
            }
        }
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
