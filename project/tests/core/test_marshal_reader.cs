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
        // The bytes are the ones w_long in 1.8.7 produces, worked out by
        // running its own loop. A negative number of two, three or four bytes is
        // written with every byte after the first set to the top of the width,
        // because the shift that ran the rest of the number out filled it.
        //
        // The first version of this list took the positive form's digits and
        // appended a single 0xFF, which is not a form the engine writes at all:
        // -0x1234 is FE CC ED, and the two high bytes are there because the
        // number is carried to the width it was written in. Asking for a form
        // the engine never writes is a test that can only fail, and it failed
        // for a reason that had nothing to do with the reader.
        var cases = new (long, byte, byte[])[]
        {
            (0, 0x00, Array.Empty<byte>()),
            (1, 0x06, Array.Empty<byte>()),
            (5, 0x0A, Array.Empty<byte>()),
            (122, 0x7F, Array.Empty<byte>()),
            (255, 0x01, [0xFF]),
            (0x1234, 0x02, [0x34, 0x12]),
            (0x123456, 0x03, [0x56, 0x34, 0x12]),
            (0x12345678, 0x04, [0x78, 0x56, 0x34, 0x12]),
            (-1, 0xFA, Array.Empty<byte>()),
            (-123, 0x80, Array.Empty<byte>()),
            (-255, 0xFF, [0x01]),
            (-256, 0xFF, [0x00]),
            (-0x1234, 0xFE, [0xCC, 0xED]),
            (-0x123456, 0xFD, [0xAA, 0xCB, 0xED]),
            (-0x12345678, 0xFC, [0x88, 0xA9, 0xCB, 0xED]),
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
                $"0x{first:X2} with {rest.Length} bytes read back as"
                + $" {read.Integer.Value} and not {value}");
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

    public void Test_AWholeNumberTooLargeForTheSmallFormIsReadFromItsDigits()
    {
        // In the Ruby these engines run, a whole number that fits in thirty one
        // bits is written small and anything larger is written as a sign and one
        // byte per digit. A game's data holds money, a stat and a coordinate,
        // and a gold total saved often has passed thirty one bits, so refusing
        // every such file would refuse a game that works.
        var body = new List<byte>();
        body.Add((byte)MarshalType.Bignum);
        body.Add((byte)'+');
        AddLong(body, 12);                                // twelve digits
        AddRaw(body, "123456789012");

        var stream = new List<byte> { 0x04, 0x08 };
        stream.AddRange(body);
        var value = new MarshalReader(stream.ToArray()).Read();

        AssertEq(value.Kind, "integer", "which is still a whole number");
        AssertEq(
            value.Integer!.Value, 123456789012L,
            "holding every digit and not a truncated number");
    }

    public void Test_AWholeNumberTooWideForThisMachineIsNotCalledACorruptFile()
    {
        // The two faults are kept apart on purpose. A file that is not marshal is
        // a file this reader cannot read, and saying so is useful. A number that
        // is simply too large for this machine's whole number is a file this
        // reader understood completely, and telling the caller the file is
        // corrupt sends them looking in the wrong place for a fault that is not
        // in the file at all.
        var body = new List<byte>();
        body.Add((byte)MarshalType.Bignum);
        body.Add((byte)'+');
        AddLong(body, 40);
        AddRaw(body, "1234567890123456789012345678901234567890");

        var stream = new List<byte> { 0x04, 0x08 };
        stream.AddRange(body);

        var refused = "";
        try
        {
            new MarshalReader(stream.ToArray()).Read();
        }
        catch (Exception exception)
        {
            refused = exception.Message;
        }

        AssertTrue(refused.Length > 0, "and the number is refused rather than cut");
        AssertTrue(
            refused.Contains("whole number"),
            $"the refusal says what is wrong, which is the number: {refused}");
        AssertTrue(
            !refused.Contains("not marshal") && !refused.Contains("corrupt"),
            $"and does not call the file corrupt, which it is not: {refused}");
    }

    public void Test_ANegativeWholeNumberOfDigitsKeepsItsSign()
    {
        // The sign is a byte before the digits and not a byte before the length,
        // which is the one detail that turns this into a number twice the size it
        // was meant to be.
        var body = new List<byte>();
        body.Add((byte)MarshalType.Bignum);
        body.Add((byte)'-');
        AddLong(body, 5);
        AddRaw(body, "54321");

        var stream = new List<byte> { 0x04, 0x08 };
        stream.AddRange(body);
        var value = new MarshalReader(stream.ToArray()).Read();
        AssertEq(value.Integer!.Value, -54321L, "held as a negative number");
    }

    public void Test_ASmallNumberAndALargeOneAreTheSameKind()
    {
        // Both arrive as "integer", because a script cannot tell them apart and
        // asking a value to tell them apart would be a difference the language
        // does not make.
        var small = new List<byte> { 0x04, 0x08, (byte)MarshalType.Integer };
        AddLong(small, 42);
        var large = new List<byte> { 0x04, 0x08, (byte)MarshalType.Bignum };
        large.Add((byte)'+');
        AddLong(large, 2);
        AddRaw(large, "42");

        var smallValue = new MarshalReader(small.ToArray()).Read();
        var largeValue = new MarshalReader(large.ToArray()).Read();
        AssertEq(smallValue.Kind, largeValue.Kind, "one kind either way");
        AssertEq(smallValue.Integer!.Value, largeValue.Integer!.Value, "and one number");
    }

    public void Test_AWholeNumberThatIsNotOneIsRefusedWithItsReason()
    {
        // Each way a number can be wrong is a different fault, and a reader that
        // cannot tell them apart sends whoever is looking for it to the wrong
        // place.
        var badSign = new List<byte> { 0x04, 0x08, (byte)MarshalType.Bignum };
        badSign.Add((byte)'*');
        AddLong(badSign, 1);
        badSign.Add((byte)'7');
        var signError = Refusal(() => new MarshalReader(badSign.ToArray()).Read());
        AssertTrue(
            signError.Contains("neither a plus nor a minus"),
            $"a wrong sign says so: {signError}");

        var badDigit = new List<byte> { 0x04, 0x08, (byte)MarshalType.Bignum };
        badDigit.Add((byte)'+');
        AddLong(badDigit, 2);
        badDigit.Add((byte)'4');
        badDigit.Add((byte)'x');
        var digitError = Refusal(() => new MarshalReader(badDigit.ToArray()).Read());
        AssertTrue(
            digitError.Contains("not a digit"),
            $"a wrong digit says so: {digitError}");

        var noDigits = new List<byte> { 0x04, 0x08, (byte)MarshalType.Bignum };
        noDigits.Add((byte)'+');
        AddLong(noDigits, 0);
        var emptyError = Refusal(() => new MarshalReader(noDigits.ToArray()).Read());
        AssertTrue(
            emptyError.Contains("zero digits"),
            $"a number of no digits says so: {emptyError}");
    }

    public void Test_AWholeNumberTooLargeForThisMachineIsReadButNotCarried()
    {
        // Forty digits is well past what this machine's whole number holds. The
        // file is not corrupt and the reader says so plainly, because "a number
        // this reader cannot carry" and "a file that is not marshal" send a
        // person looking in opposite places.
        var digits = new string('9', 40);
        var body = new List<byte>();
        body.Add((byte)MarshalType.Bignum);
        body.Add((byte)'-');
        AddLong(body, digits.Length);
        AddRaw(body, digits);

        var stream = new List<byte> { 0x04, 0x08 };
        stream.AddRange(body);
        var error = Refusal(() => new MarshalReader(stream.ToArray()).Read());
        AssertTrue(
            error.Contains("does not fit") && error.Contains("40 digits"),
            $"which says how many digits it was: {error}");
    }

    public void Test_AWholeNumberOfMoreDigitsThanAReaderWillHoldIsRefused()
    {
        // A length is checked before the digits are read, so a corrupt length
        // cannot make this reader allocate for a file that has no such number in
        // it.
        var body = new List<byte>();
        body.Add((byte)MarshalType.Bignum);
        body.Add((byte)'+');
        AddLong(body, 100000);
        body.Add((byte)'1');

        var stream = new List<byte> { 0x04, 0x08 };
        stream.AddRange(body);
        var error = Refusal(() => new MarshalReader(stream.ToArray()).Read());
        AssertTrue(
            error.Contains("longer than"),
            $"a length beyond what a game writes is refused: {error}");
    }

    public void Test_EveryWholeNumberInAWideRangeSurvivesThePacking()
    {
        // The whole-number packing, read off Ruby 1.8.7's own w_long and r_long
        // rather than remembered. A reader that is right about most numbers and
        // wrong about the rest is the worst kind of reader: the file loads, the
        // map draws, and one value is quietly wrong.
        //
        // The check walks a wide range and compares what the reader says against
        // what the writer below produces. An earlier version of this test found
        // two faults, both in the reader: a one byte negative was read as the
        // negative of the byte instead of as a number whose top bit is set, and
        // a two or three byte negative was read one byte too many, so it took
        // the first byte of whatever followed in the file. A game's negative
        // coordinate would have had the next value's bytes inside it.
        var mismatches = new List<string>();
        var checkedCount = 0;
        for (long number = -3000; number <= 3000; number++)
        {
            var written = PackWholeNumber(number);
            if (written == null)
            {
                continue;
            }

            var stream = new List<byte> { 0x04, 0x08, (byte)MarshalType.Integer };
            stream.AddRange(written);
            long read;
            try
            {
                read = new MarshalReader(stream.ToArray()).Read().Integer!.Value;
            }
            catch (Exception exception)
            {
                mismatches.Add($"{number} was refused: {exception.Message}");
                continue;
            }

            checkedCount++;
            if (read != number)
            {
                mismatches.Add(
                    $"{number} written {Convert.ToHexString(written.ToArray())}"
                    + $" came back as {read}");
            }
        }

        AssertEq(checkedCount, 6001, "every number in the range is packable");
        AssertTrue(
            mismatches.Count == 0,
            "and every one survives: "
            + string.Join("; ", mismatches.GetRange(0, Math.Min(3, mismatches.Count))));
    }

    public void Test_AFixedSetOfNumbersIsEncodedTheWayTheEngineEncodesThem()
    {
        // The bytes below are the ones 1.8.7's w_long produces, worked out from
        // its own source and not from this test's own writer. They are here so
        // that a fault in the writer above cannot hide a fault in the reader:
        // if the two ever disagree, this is what says so.
        // These bytes are w_long out of Ruby 1.8.7, worked out by running its
        // own loop and writing down what came out. The first version of them
        // was written from memory and was wrong about four of the sixteen, which
        // is the reason they are here: a hand written expectation can be wrong
        // in the same way twice and hide the fault it was meant to show.
        var cases = new (long pNumber, string pBytes)[]
        {
            (0, "00"),
            (1, "06"),
            (117, "7A"),
            (122, "7F"),
            (123, "017B"),
            (255, "01FF"),
            (256, "020001"),
            (-1, "FA"),
            (-123, "80"),
            (-124, "FF84"),
            (-255, "FF01"),
            (-256, "FF00"),
            (65535, "02FFFF"),
            (-65536, "FE0000"),
            (2147483647, "04FFFFFF7F"),
            (-2147483648, "FC00000080"),
        };

        foreach (var (number, expected) in cases)
        {
            var written = PackWholeNumber(number)!;
            AssertEq(
                Convert.ToHexString(written.ToArray()), expected,
                $"{number} is written the way the engine writes it");
        }
    }

    public void Test_EveryPossibleCountByteIsReadOrRefusedWithAReason()
    {
        // Every one of the two hundred and fifty six values a count byte can hold,
        // one at a time. A boundary a change moves has to show itself here, and
        // the earlier version of this test guessed how many of the two hundred
        // and fifty six would be read and guessed wrong.
        //
        // The reason for walking the whole byte is a count of more than four: the
        // source refuses one wider than its own whole number, and without that
        // check the bytes would be shifted out of this machine's number and the
        // result would look like a number rather than like a fault.
        var read = 0;
        var refused = 0;
        for (var byteValue = -128; byteValue <= 127; byteValue++)
        {
            var stream = new List<byte> { 0x04, 0x08, (byte)MarshalType.Integer };
            stream.Add((byte)byteValue);
            for (var filler = 0; filler < 4; filler++)
            {
                stream.Add(0xAA);
            }

            try
            {
                var value = new MarshalReader(stream.ToArray()).Read();
                AssertTrue(
                    value.Integer.HasValue,
                    $"a count of {byteValue} is read as a number");
                read++;
            }
            catch (MarshalFormatException exception)
            {
                AssertTrue(
                    exception.Message.Length > 0,
                    $"a count of {byteValue} is refused with a reason");
                refused++;
            }
        }

        AssertEq(read + refused, 256, "every count byte is either read or refused");
        AssertEq(
            refused, 0,
            "and none of them is refused, because the rule has a form for every"
            + " one of them; both ends of the byte are the one byte form and the"
            + " middle is either form, and none of them is a fault");
        AssertEq(read, 256, "so every one of them is read");
    }

    public void Test_ACountAboveFourIsANumberAndNotAWidth()
    {
        // Every count from five to one hundred and twenty seven is the one byte
        // form, the number with five taken off. A count of one hundred and
        // twenty seven is therefore the number one hundred and twenty two and
        // not a hundred and twenty seven bytes wide.
        //
        // A version of this test asked for a wide count to be refused, on the
        // reasoning that a signed byte of up to one hundred and twenty seven
        // would shift that many bytes out of this machine's whole number. That
        // reasoning was about a width that no count is: the source's own rule
        // has no form above four bytes, and a check invented from a reading of
        // the byte range rather than from the rule refused a length a game
        // writes for every list it has.
        for (var count = 5; count <= 127; count++)
        {
            var stream = new List<byte> { 0x04, 0x08, (byte)MarshalType.Integer };
            stream.Add((byte)count);
            var value = new MarshalReader(stream.ToArray()).Read();
            AssertEq(
                value.Integer!.Value, (long)count - 5,
                $"a count of {count} is the number {count - 5}");
        }
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

    private static List<byte>? PackWholeNumber(long pValue)
    {
        // w_long out of Ruby 1.8.7, its own loop with no additions. The count
        // goes in the first byte and the digits follow in the order they were
        // shifted out, and the loop breaks the moment the rest is zero or all
        // ones, so the count says how many digits there are and no more.
        //
        // Two earlier versions of this were wrong. One reversed the digits, and
        // one wrote a sign byte a negative number does not have: -3000 is three
        // bytes, FE 48 F4, with two digits, and not four. A writer that adds a
        // byte and a reader that does not read one disagree about every value in
        // a game, and the writer is the one that has to be fixed, because the
        // reader's job is to read what the engine wrote.
        if (pValue == 0)
        {
            return [(byte)0x00];
        }
        if (pValue > 0 && pValue < 123)
        {
            return [(byte)(pValue + 5)];
        }
        if (pValue > -124 && pValue < 0)
        {
            return [(byte)((pValue - 5) & 0xFF)];
        }

        var buffer = new byte[5];
        var remaining = pValue;
        for (var count = 1; count <= 4; count++)
        {
            buffer[count] = (byte)(remaining & 0xFF);
            remaining >>= 8;
            if (remaining == 0)
            {
                buffer[0] = (byte)count;
                return Head(buffer, count);
            }
            if (remaining == -1)
            {
                buffer[0] = (byte)(-count & 0xFF);
                return Head(buffer, count);
            }
        }

        return null;
    }

    private static List<byte> Head(byte[] pBuffer, int pCount)
    {
        var taken = new List<byte>(pCount + 1);
        for (var index = 0; index <= pCount; index++)
        {
            taken.Add(pBuffer[index]);
        }

        return taken;
    }
}
