using System;
using System.Collections.Generic;
using System.Text;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Reads marshal streams against expectations taken from the specification
/// rather than from this project's own reader.
/// </summary>
/// <remarks>
/// Every byte in a fixture is written by hand from the type table in the Ruby
/// documentation. A round trip through a writer of our own would pass even if
/// the reader and the writer were wrong in the same way, so the fixtures here
/// are the specification, and the reader has to agree with them.
/// </remarks>
public partial class TestMarshalReader : TestBase
{
    /// <summary>Writes a marshalled integer the way the specification defines it.</summary>
    /// <remarks>
    /// The specification says a writer should produce the shortest form, so this
    /// does too. It is deliberately independent of the reader: if the reader's
    /// decoding of a form were wrong, a fixture built the same way would hide it.
    /// </remarks>
    private static void AddInteger(List<byte> pBytes, long pValue)
    {
        pBytes.Add((byte)MarshalType.Integer);
        AddLong(pBytes, pValue);
    }

    /// <summary>Writes the long for a value whose type byte was already added.</summary>
    private static void AddValue(List<byte> pBytes, long pValue)
    {
        AddLong(pBytes, pValue);
    }

    /// <summary>Writes the bare long, with no type byte in front of it.</summary>
    /// <remarks>
    /// The specification uses a long for a value that belongs to a type, such as
    /// a length or an index, without repeating the type byte. Writing one here
    /// would shift every byte after it.
    /// </remarks>
    private static void AddLong(List<byte> pBytes, long pValue)
    {
        if (pValue == 0)
        {
            pBytes.Add(0x00);
            return;
        }
        if (pValue > 0)
        {
            if (pValue < 123)
            {
                // A sign extended byte with an offset of five covers 5..126.
                pBytes.Add((byte)(pValue + 5));
                return;
            }
            if (pValue < 256)
            {
                pBytes.Add(0x01);
                pBytes.Add((byte)pValue);
                return;
            }
            if (pValue < 0x10000)
            {
                pBytes.Add(0x02);
                pBytes.AddRange(BitConverter.GetBytes((ushort)pValue));
                return;
            }
            if (pValue < 0x1000000)
            {
                pBytes.Add(0x03);
                pBytes.AddRange(BitConverter.GetBytes((uint)pValue)[..3]);
                return;
            }
            pBytes.Add(0x04);
            pBytes.AddRange(BitConverter.GetBytes((uint)pValue));
            return;
        }
        var negative = -pValue;
        if (negative < 123)
        {
            // Negative values below minus four are written as a sign extended
            // byte with five added, so minus one is 0xFC.
            pBytes.Add((byte)(sbyte)(pValue + 5));
            return;
        }
        if (negative < 256)
        {
            pBytes.Add(0xFF);
            pBytes.Add((byte)negative);
            return;
        }
        if (negative < 0x10000)
        {
            pBytes.Add(0xFE);
            pBytes.AddRange(BitConverter.GetBytes((ushort)negative));
            return;
        }
        pBytes.Add(0xFD);
        pBytes.AddRange(BitConverter.GetBytes((uint)negative)[..3]);
    }

    private static void AddRaw(List<byte> pBytes, string pText)
    {
        pBytes.AddRange(Encoding.ASCII.GetBytes(pText));
    }

    private static void AddBytes(List<byte> pBytes, string pText)
    {
        AddLong(pBytes, pText.Length);
        AddRaw(pBytes, pText);
    }

    private static byte[] Stream(params List<byte>[] pParts)
    {
        var bytes = new List<byte> { 0x04, 0x08 };
        foreach (var part in pParts)
        {
            bytes.AddRange(part);
        }
        return bytes.ToArray();
    }

    private static List<byte> Part()
    {
        return [];
    }

    /// <summary>Runs an action and returns the message of the refusal it raised.</summary>
    /// <remarks>
    /// An action that does not raise is a failure in itself, which is why the
    /// empty string is returned rather than a message: every caller checks the
    /// message for the reason, and a reason that never arrived is not there.
    /// </remarks>
    private string Refusal(Action pAction)
    {
        try
        {
            pAction();
            return "";
        }
        catch (MarshalFormatException exception)
        {
            return exception.Message;
        }
    }

    public void Test_SingleBytesAreTrueFalseAndNil()
    {
        foreach (var (text, kind) in new[]
        {
            (T: "T", Kind: "true"),
            (F: "F", Kind: "false"),
            (N: "0", Kind: "nil"),
        })
        {
            var value = new MarshalReader(Stream(Part(), [.. Encoding.ASCII.GetBytes(text)]))
                .Read();
            AssertEq(value.Kind, kind, $"0x{text[0]} reads as {kind}");
        }
    }

    public void Test_IntegersUseTheShortestFormTheSpecificationAllows()
    {
        // Every one of the eight special first bytes, and the sign extended form
        // on both sides of zero. A reader that treated the first byte as a plain
        // length would decode the short ones and be wrong about the rest.
        // Every one of the eight special first bytes, and the sign extended form
        // on both sides of zero. A reader that treated the first byte as a plain
        // length would decode the short ones and be wrong about the rest, so
        // each form carries the bytes that make its value.
        var cases = new (long, byte, byte[])[]
        {
            (0, 0x00, Array.Empty<byte>()),
            (255, 0x01, [0xFF]),
            (-255, 0xFF, [0xFF]),
            (0x1234, 0x02, [0x34, 0x12]),
            (-0x1234, 0xFE, [0x34, 0x12]),
            (0x123456, 0x03, [0x56, 0x34, 0x12]),
            (-0x123456, 0xFD, [0x56, 0x34, 0x12]),
            (0x12345678, 0x04, [0x78, 0x56, 0x34, 0x12]),
            (-0x12345678, 0xFC, [0x78, 0x56, 0x34, 0x12]),
            (5, 0x0A, Array.Empty<byte>()),
            (122, 0x7F, Array.Empty<byte>()),
            (1, 0x06, Array.Empty<byte>()),
        };
        foreach (var (value, first, rest) in cases)
        {
            var part = Part();
            part.Add((byte)MarshalType.Integer);
            part.Add(first);
            part.AddRange(rest);
            var read = new MarshalReader(Stream(part)).Read();
            AssertTrue(read.Integer.HasValue, $"the integer {value} is read as an integer");
            AssertEq(read.Integer.Value, value,
                $"0x{first:X2} with {rest.Length} bytes reads back as {value}");
        }
    }

    public void Test_ASignExtendedByteCarriesItsValueWithAnOffsetOfFive()
    {
        // The eight special bytes take 0x00..0x04 and 0xFC..0xFF, so the
        // offset form only applies where the offset byte does not collide with
        // one of them. That leaves five through one hundred and twenty six, and
        // minus one through minus four where their byte is not 0xFC..0xFF.
        foreach (var value in new long[] { 5, 6, 7, 10, 100, 122 })
        {
            var part = Part();
            part.Add((byte)MarshalType.Integer);
            var form = (byte)(sbyte)(value + 5);
            part.Add(form);
            var read = new MarshalReader(Stream(part)).Read();
            AssertEq(read.Integer!.Value, value,
                $"{value} is written as 0x{form:X2} and reads back as itself");
        }

        // A negative value cannot use this form at all. Minus one would need
        // the byte four and minus six would need 0xFF, and both are among the
        // eight special markers, so a writer has to use a wider form. This is a
        // property of the format rather than a gap in the reader, and it is
        // worth stating so a future writer does not try.
        foreach (var value in new long[] { -1, -4, -6 })
        {
            var offsetByte = (sbyte)(value + 5);
            var collides = offsetByte == 4 || offsetByte == -1 || (offsetByte >= 1 && offsetByte <= 4);
            AssertTrue(collides,
                $"{value} would need the byte 0x{(byte)offsetByte:X2}, which is a"
                + " special marker, so the short form is not available for it");
        }
    }

    public void Test_ASymbolIsWrittenOnceAndLinkedAfterwards()
    {
        // The stream in the documentation: [:hello, :hello] is
        // "\x04\b[\a:\nhello;\x00" with the array holding two items.
        var part = Part();
        part.Add((byte)MarshalType.Array);
        AddLong(part, 2);
        part.Add((byte)MarshalType.Symbol);
        AddLong(part, 5);
        AddRaw(part, "hello");
        part.Add((byte)MarshalType.SymbolLink);
        AddLong(part, 0);

        var reader = new MarshalReader(Stream(part));
        var value = reader.Read();
        AssertEq(value.Kind, "array", "the stream is an array");
        AssertEq(value.Items.Count, 2, "with two elements");
        AssertEq(value.Items[0].Kind, "symbol", "the first is a symbol");
        AssertEq(value.Items[0].Text, "hello", "naming hello");
        AssertFalse(value.Items[0].IsLink, "the first is not a link");
        AssertTrue(value.Items[1].IsLink, "the second is a link");
        AssertEq(value.Items[1].Link!.Index, 0, "to the symbol at index zero");
        AssertEq(value.Items[1].Text, "hello", "which names hello");
        AssertEq(reader.Symbols.Count, 1, "and only one symbol was defined");
    }

    public void Test_ASymbolLinkToAnUndefinedSymbolIsRefused()
    {
        var part = Part();
        part.Add((byte)MarshalType.SymbolLink);
        AddLong(part, 3);
        var error = Refusal(() => new MarshalReader(Stream(part)).Read());
        AssertTrue(error.Contains("only 0 symbols"), $"a link to nothing is refused: {error}");
    }

    public void Test_AnObjectLinkIsOneIndexedAndCounted()
    {
        // The documented stream "\004\b[\a\"\nhello@\006" is an array of one
        // string, linked to a second time. The string is the first object, so
        // the link to it is index one.
        var part = Part();
        part.Add((byte)MarshalType.Array);
        AddLong(part, 2);
        part.Add((byte)MarshalType.String);
        AddBytes(part, "hello");
        part.Add((byte)MarshalType.ObjectLink);
        AddLong(part, 2);

        var reader = new MarshalReader(Stream(part));
        var value = reader.Read();
        AssertEq(value.Items.Count, 2, "the array has two elements");
        AssertFalse(value.Items[0].IsLink, "the first is the string itself");
        AssertEq(reader.ObjectCount, 2, "the array and the string each took an index");
        AssertTrue(value.Items[1].IsLink, "the second element is a link");
        AssertEq(value.Items[1].Link!.Index, 2,
            "to the string at index two, not to the array at index one");
    }

    public void Test_ANewStringTakesAnObjectIndexAndAnArrayTakesOneToo()
    {
        var part = Part();
        part.Add((byte)MarshalType.Array);
        AddLong(part, 1);
        part.Add((byte)MarshalType.String);
        AddBytes(part, "x");
        var reader = new MarshalReader(Stream(part));
        var value = reader.Read();
        AssertEq(reader.ObjectCount, 2,
            "the array and the string each took an object index");
        AssertEq(value.Items[0].Integer!.Value, 2L,
            "and the string is the second, because the array was written first");
    }

    public void Test_AnObjectCarriesItsClassNameAndInstanceVariables()
    {
        var part = Part();
        part.Add((byte)MarshalType.Object);
        part.Add((byte)MarshalType.Symbol);
        AddBytes(part, "RPG::Sprite");
        AddLong(part, 2);
        part.Add((byte)MarshalType.Symbol);
        AddBytes(part, "@@src");
        part.Add((byte)MarshalType.String);
        AddBytes(part, "Graphics/Sprite.png");
        part.Add((byte)MarshalType.Symbol);
        AddBytes(part, "@x");
        part.Add((byte)MarshalType.Integer);
        AddValue(part, 64);

        var value = new MarshalReader(Stream(part)).Read();
        AssertEq(value.Kind, "object", "an object reads as an object");
        AssertEq(value.ClassName, "RPG::Sprite", "of the class the stream named");
        AssertEq(value.Keys.Count, 2, "with two instance variables");
        AssertEq(value.Keys[0], "@@src", "the first is the source");
        AssertEq(value.Items[0].Text, "Graphics/Sprite.png", "holding the file name");
        AssertEq(value.Keys[1], "@x", "the second is the position");
        AssertEq(value.Items[1].Integer.Value, 64L, "holding sixty four");
    }

    public void Test_InstanceVariablesAroundAnObjectAreKeptWithIt()
    {
        var part = Part();
        part.Add((byte)MarshalType.InstanceVariables);
        part.Add((byte)MarshalType.Object);
        part.Add((byte)MarshalType.Symbol);
        AddBytes(part, "Point");
        AddLong(part, 0);
        AddLong(part, 1);
        part.Add((byte)MarshalType.Symbol);
        AddBytes(part, "@y");
        part.Add((byte)MarshalType.Integer);
        AddValue(part, 7);

        var value = new MarshalReader(Stream(part)).Read();
        AssertEq(value.Kind, "object", "the inner type is kept");
        AssertEq(value.ClassName, "Point", "with its class");
        AssertEq(value.Keys.Count, 1, "and one instance variable");
        AssertEq(value.Keys[0], "@y", "named y");
        AssertEq(value.Items[0].Integer.Value, 7L, "holding seven");
    }

    public void Test_AHashReadsPairsAndAHashWithDefaultAlsoReadsItsDefault()
    {
        var plain = Part();
        plain.Add((byte)MarshalType.Hash);
        AddLong(plain, 1);
        plain.Add((byte)MarshalType.Symbol);
        AddBytes(plain, "hp");
        plain.Add((byte)MarshalType.Integer);
        AddValue(plain, 100);

        var value = new MarshalReader(Stream(plain)).Read();
        AssertEq(value.Kind, "hash", "a hash reads as a hash");
        AssertEq(value.Keys.Count, 1, "with one pair");
        AssertEq(value.Keys[0], "hp", "keyed by hp");
        AssertEq(value.Items[1].Integer.Value, 100L, "holding one hundred");

        var withDefault = Part();
        withDefault.Add((byte)MarshalType.HashWithDefault);
        AddLong(withDefault, 1);
        withDefault.Add((byte)MarshalType.Symbol);
        AddBytes(withDefault, "hp");
        withDefault.Add((byte)MarshalType.Integer);
        AddValue(withDefault, 100);
        withDefault.Add((byte)MarshalType.Integer);
        AddValue(withDefault, 0);

        var other = new MarshalReader(Stream(withDefault)).Read();
        AssertEq(other.Kind, "hash with default", "the braced form says so");
        AssertEq(other.Keys.Count, 2, "and has a key for the default");
        AssertEq(other.Keys[1], "__default__", "which is named for it");
    }

    public void Test_FloatsCarryTheirOwnText()
    {
        foreach (var (text, expected) in new (string, double)[]
        {
            ("1.5", 1.5),
            ("-0.25", -0.25),
            ("inf", double.PositiveInfinity),
            ("-inf", double.NegativeInfinity),
        })
        {
            var part = Part();
            part.Add((byte)MarshalType.Float);
            AddRaw(part, text);
            part.Add(0x00);
            var value = new MarshalReader(Stream(part)).Read();
            AssertEq(value.Kind, "float", $"{text} reads as a float");
            AssertEq(value.Real!.Value, expected, $"{text} is {expected}");
        }
    }

    public void Test_AStructCarriesItsNameAndMembers()
    {
        var part = Part();
        part.Add((byte)MarshalType.Struct);
        part.Add((byte)MarshalType.Symbol);
        AddBytes(part, "Color");
        AddLong(part, 2);
        part.Add((byte)MarshalType.Symbol);
        AddBytes(part, "r");
        part.Add((byte)MarshalType.Integer);
        AddValue(part, 255);
        part.Add((byte)MarshalType.Symbol);
        AddBytes(part, "b");
        part.Add((byte)MarshalType.Integer);
        AddValue(part, 0);

        var value = new MarshalReader(Stream(part)).Read();
        AssertEq(value.Kind, "struct", "a struct reads as a struct");
        AssertEq(value.ClassName, "Color", "of the struct the stream named");
        AssertEq(value.Keys.Count, 2, "with two members");
        AssertEq(value.Items[0].Integer.Value, 255L, "red at full");
        AssertEq(value.Items[1].Integer.Value, 0L, "blue at none");
    }

    public void Test_AUserMarshalValueIsDataAndItsClassIsNotResolved()
    {
        // The reader must not call marshal_load. A Color is the classic case:
        // its payload is an array of four integers.
        var part = Part();
        part.Add((byte)MarshalType.UserMarshal);
        part.Add((byte)MarshalType.Symbol);
        AddBytes(part, "Color");
        part.Add((byte)MarshalType.Array);
        AddLong(part, 4);
        for (var index = 0; index < 4; index++)
        {
            AddInteger(part, index * 10);
        }

        var value = new MarshalReader(Stream(part)).Read();
        AssertEq(value.Kind, "user marshal", "a user marshal value says so");
        AssertEq(value.ClassName, "Color", "naming the class it came from");
        AssertEq(value.Items.Count, 1, "with one payload value");
        var payload = value.Items[0];
        AssertEq(payload.Kind, "array", "which is an array");
        AssertEq(payload.Items.Count, 4, "of four numbers");
        AssertEq(payload.Items[3].Integer.Value, 30L, "ending at thirty");
    }

    public void Test_AUserDefinedValueKeepsItsBytesAsTheyWereDumped()
    {
        var part = Part();
        part.Add((byte)MarshalType.UserDefined);
        part.Add((byte)MarshalType.Symbol);
        AddBytes(part, "SomeClass");
        AddBytes(part, "raw payload");

        var value = new MarshalReader(Stream(part)).Read();
        AssertEq(value.Kind, "user defined", "a user defined value says so");
        AssertEq(value.ClassName, "SomeClass", "naming the class");
        AssertEq(Encoding.UTF8.GetString(value.Bytes!), "raw payload",
            "and keeping the dumped bytes as they are");
    }

    public void Test_ARegexpKeepsItsSourceAndItsOptions()
    {
        var part = Part();
        part.Add((byte)MarshalType.Regexp);
        AddBytes(part, "^a.*z$");
        part.Add(0x01);

        var value = new MarshalReader(Stream(part)).Read();
        AssertEq(value.Kind, "regexp", "a regexp says so");
        AssertEq(value.Text, "^a.*z$", "with its source");
        AssertEq(value.Integer.Value, 1L, "and its option byte read as a signed value");
    }

    public void Test_AnObjectTakesItsIndexBeforeItsContentsAreRead()
    {
        // A value inside an array may link back to that array. This only
        // resolves if the array was given its index before the link was read,
        // because a link names an object the stream has already defined.
        var part = Part();
        part.Add((byte)MarshalType.Array);
        AddLong(part, 2);
        part.Add((byte)MarshalType.String);
        AddBytes(part, "inner");
        part.Add((byte)MarshalType.ObjectLink);
        AddLong(part, 1);

        var reader = new MarshalReader(Stream(part));
        var value = reader.Read();
        AssertEq(value.Kind, "array", "the stream is an array");
        AssertEq(value.Integer!.Value, 1L, "which took index one");
        AssertEq(value.Items[0].Integer!.Value, 2L,
            "and the string it holds took index two, so the array was numbered first");
        AssertTrue(value.Items[1].IsLink, "and its second element is a link");
        AssertEq(value.Items[1].Link!.Index, 1, "back to the array itself");
    }

    public void Test_AHashTakesItsIndexBeforeItsPairsAreRead()
    {
        // The same question for a hash, which is the other collection that can
        // hold a link to itself.
        var part = Part();
        part.Add((byte)MarshalType.Hash);
        AddLong(part, 2);
        part.Add((byte)MarshalType.String);
        AddBytes(part, "k");
        part.Add((byte)MarshalType.ObjectLink);
        AddLong(part, 1);
        part.Add((byte)MarshalType.String);
        AddBytes(part, "second");
        part.Add((byte)MarshalType.Integer);
        AddValue(part, 3);

        var reader = new MarshalReader(Stream(part));
        var value = reader.Read();
        AssertEq(value.Integer!.Value, 1L, "the hash took index one");
        AssertEq(value.Items[0].Integer!.Value, 2L,
            "its first key took index two, so the hash was numbered before its pairs");
        AssertEq(value.Items[2].Integer!.Value, 3L,
            "and the second key took index three, because a link names an object"
            + " that already existed and did not take one");
    }

    public void Test_ALinkToObjectZeroIsRefusedBecauseObjectIndicesStartAtOne()
    {
        // Zero is not an object index, so a stream carrying one is broken. A
        // reader that accepted it would silently return the wrong object later.
        var part = Part();
        part.Add((byte)MarshalType.Array);
        AddLong(part, 1);
        part.Add((byte)MarshalType.ObjectLink);
        AddLong(part, 0);

        var error = Refusal(() => new MarshalReader(Stream(part)).Read());
        AssertTrue(error.Contains("one indexed"), $"a link to object zero is refused: {error}");
    }

    public void Test_AZeroLengthIsReadWithoutASecondByte()
    {
        // Zero is the one value with a first byte of its own and nothing
        // after it, so a reader that always took a second byte would run off
        // the end of the stream.
        var part = Part();
        part.Add((byte)MarshalType.Array);
        AddLong(part, 0);
        var value = new MarshalReader(Stream(part)).Read();
        AssertEq(value.Items.Count, 0, "an array of nothing reads as an empty array");
    }

    public void Test_AByteCountThatCannotFitIsRefused()
    {
        // A string that claims more bytes than the stream holds. Handing back
        // what is there would look like a short but successful read.
        var part = Part();
        part.Add((byte)MarshalType.String);
        AddLong(part, (1 << 24) + 1);
        AddRaw(part, "short");

        var error = Refusal(() => new MarshalReader(Stream(part)).Read());
        AssertTrue(error.Contains("outside 0 to"),
            $"a byte count past the limit is refused: {error}");
    }

    public void Test_AMajorVersionThisReaderDoesNotImplementIsRefused()
    {
        foreach (var major in new byte[] { 0x03, 0x05, 0x00 })
        {
            var bytes = new List<byte> { major, 0x08, (byte)MarshalType.Nil };
            var error = Refusal(() => new MarshalReader(bytes.ToArray()).Read());
            AssertTrue(error.Contains("major version"),
                $"major version {major} is refused: {error}");
        }
    }

    public void Test_AMinorVersionNewerThanTheReadersIsRefused()
    {
        // A newer minor version may use a type this reader has never heard of,
        // so reading it would be a guess. An older one is fine, because a newer
        // minor version can read it.
        var newer = new List<byte> { 0x04, 0x09, (byte)MarshalType.Nil };
        var error = Refusal(() => new MarshalReader(newer.ToArray()).Read());
        AssertTrue(error.Contains("newer than"), $"minor version nine is refused: {error}");

        var older = new List<byte> { 0x04, 0x07, (byte)MarshalType.Nil };
        var read = new MarshalReader(older.ToArray()).Read();
        AssertEq(read.Kind, "nil", "minor version seven is read");
    }

    public void Test_AnUnknownTypeByteIsRefusedWithItsValue()
    {
        var bytes = new List<byte> { 0x04, 0x08, 0x5A };
        var error = Refusal(() => new MarshalReader(bytes.ToArray()).Read());
        AssertTrue(error.Contains("0x5A"), $"an undefined type byte is refused: {error}");
    }

    public void Test_AStreamThatEndsInsideAValueIsRefusedRatherThanGuessed()
    {
        // A string whose declared length runs past the end. Handing back what
        // is there would look like a successful read of a short string.
        var part = Part();
        part.Add((byte)MarshalType.String);
        AddLong(part, 100);
        AddRaw(part, "short");

        var error = Refusal(() => new MarshalReader(Stream(part)).Read());
        AssertTrue(error.Contains("past the end"), $"a string longer than the stream is refused: {error}");
    }

    public void Test_ALengthThatCannotBeAnElementCountIsRefused()
    {
        // A collection that claims more elements than any real file has. The
        // reader has to refuse it before it tries to allocate for it.
        var part = Part();
        part.Add((byte)MarshalType.Array);
        AddLong(part, 0x7FFFFFFF);

        var error = Refusal(() => new MarshalReader(Stream(part)).Read());
        AssertTrue(error.Contains("outside 0 to"), $"an impossible element count is refused: {error}");
    }

    public void Test_AStreamTooShortForAVersionIsRefused()
    {
        var error = Refusal(() => new MarshalReader([0x04]).Read());
        AssertTrue(error.Contains("too short"), $"a one byte stream is refused: {error}");
    }

    public void Test_NestingDeeperThanTheLimitIsRefused()
    {
        // An array inside an array, one level at a time, past the limit. Without
        // the limit a crafted file would exhaust the stack.
        var part = Part();
        for (var depth = 0; depth < 80; depth++)
        {
            part.Add((byte)MarshalType.Array);
            AddLong(part, 1);
        }
        part.Add((byte)MarshalType.Nil);

        var error = Refusal(() => new MarshalReader(Stream(part)).Read());
        AssertTrue(error.Contains("nests deeper"), $"deep nesting is refused: {error}");
    }

    public void Test_NestingJustInsideTheLimitIsStillRead()
    {
        var part = Part();
        for (var depth = 0; depth < 20; depth++)
        {
            part.Add((byte)MarshalType.Array);
            AddLong(part, 1);
        }
        part.Add((byte)MarshalType.Integer);
        AddValue(part, 5);

        var reader = new MarshalReader(Stream(part));
        var value = reader.Read();
        for (var depth = 0; depth < 20; depth++)
        {
            AssertEq(value.Kind, "array", $"level {depth} is an array");
            value = value.Items[0];
        }
        AssertEq(value.Integer!.Value, 5L, "and the value at the bottom is five");
    }

    public void Test_TheReaderReachesTheEndOfASimpleStream()
    {
        // A reader that stopped early would leave the rest of a database file
        // unread, so the position after a read has to account for every byte.
        var part = Part();
        part.Add((byte)MarshalType.Array);
        AddLong(part, 2);
        part.Add((byte)MarshalType.Integer);
        AddValue(part, 7);
        part.Add((byte)MarshalType.String);
        AddBytes(part, "abc");

        var bytes = Stream(part);
        var reader = new MarshalReader(bytes);
        reader.Read();
        AssertEq(reader.Position, bytes.Length, "the reader reached the end of the stream");
    }
}
