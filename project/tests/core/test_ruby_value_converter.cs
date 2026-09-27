using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Turns what a Marshal file said into what the language would call it.
/// </summary>
/// <remarks>
/// The values here are built by hand rather than decoded from a stream, so a
/// failure says which conversion is wrong instead of which byte. The one thing
/// checked end to end is that a real stream and these values agree, because two
/// layers that read the same file differently are worse than one layer.
/// </remarks>
public partial class TestRubyValueConverter : TestBase
{
    private static MarshalValue Of(string pKind) => new() { Kind = pKind };

    private static MarshalValue Whole(string pKind, long pValue) =>
        new() { Kind = pKind, Integer = pValue };

    private static MarshalValue Fraction(string pKind, double pValue) =>
        new() { Kind = pKind, Real = pValue };

    private static MarshalValue Bytes(string pKind, params byte[] pBytes) =>
        new() { Kind = pKind, Bytes = pBytes };

    private static List<byte> Body(Action<List<byte>> pFill)
    {
        var body = new List<byte>();
        pFill(body);
        return body;
    }

    private static string Refusal(Action pAction)
    {
        try
        {
            pAction();
            return "";
        }
        catch (RubyValueConversionException exception)
        {
            return exception.Message;
        }
    }

    public void Test_TheOnesWithoutContentBecomeThemselves()
    {
        var converter = new RubyValueConverter();
        AssertTrue(converter.Convert(Of("nil")).IsNil, "nil converts to nil");
        AssertEq(
            converter.Convert(Of("true")).Boolean, true,
            "true converts to the true value");
        AssertEq(
            converter.Convert(Of("false")).Boolean, false,
            "and false to the false value");
    }

    public void Test_ANumberConvertsToTheKindTheLanguageCallsIt()
    {
        var converter = new RubyValueConverter();
        AssertEq(
            converter.Convert(Whole("int", 42)).Kind, RubyValueKind.Integer,
            "a whole number is an integer");
        AssertEq(
            converter.Convert(Whole("int", 42)).Integer, 42L,
            "holding forty two");
        AssertEq(
            converter.Convert(Fraction("float", 2.5)).Kind, RubyValueKind.Float,
            "and a fraction is a float");
    }

    public void Test_ABooleanIsNotAWholeNumber()
    {
        // Ruby lets a whole number stand in for either, but the language's own
        // name for them is a name, and a reader that folded them into the numbers
        // would lose the difference a game can observe.
        var converter = new RubyValueConverter();
        var value = converter.Convert(Of("true"));
        AssertEq(value.Kind, RubyValueKind.Boolean, "true is a boolean");
        AssertTrue(
            value.Kind != RubyValueKind.Integer,
            "and not a whole number that happens to be one");
    }

    public void Test_AStringKeepsItsBytes()
    {
        var converter = new RubyValueConverter();
        var value = converter.Convert(Bytes("str", 0x83, 0x65, 0x83, 0x58));
        AssertEq(value.Kind, RubyValueKind.String, "bytes are a string");
        AssertEq(value.Bytes.Length, 4, "with all four of them");
    }

    public void Test_ASymbolBecomesAName()
    {
        var converter = new RubyValueConverter();
        AssertEq(
            converter.Convert(new MarshalValue { Kind = "sym", Text = "window" }).Name,
            "window",
            "a name carries over as a name");
        AssertEq(
            converter.Convert(Bytes("sym", (byte)'w', (byte)'i', (byte)'n')).Name,
            "win",
            "and one that only has bytes still becomes a name");
    }

    public void Test_APatternCarriesItsTextAndFlags()
    {
        var converter = new RubyValueConverter();
        var value = converter.Convert(new MarshalValue
        {
            Kind = "regexp",
            Text = "a.*z",
            Integer = 3,
        });
        AssertEq(value.Kind, RubyValueKind.Regexp, "a pattern is a pattern");
        AssertEq(value.Source, "a.*z", "with its text");
        AssertEq(value.Options, 3, "and its flags");
    }

    public void Test_ListAndMappingBecomeDifferentShapes()
    {
        // Ruby tells the two apart by asking the value, not by what is inside, so
        // an empty list and an empty whole value are not the same thing.
        var converter = new RubyValueConverter();
        var list = converter.Convert(new MarshalValue
        {
            Kind = "array",
            Items = [Whole("int", 1), Whole("int", 2)],
        });
        AssertTrue(list.IsList, "a list is a list");
        AssertEq(list.Items.Count, 2, "with both of its elements");

        var mapping = converter.Convert(new MarshalValue
        {
            Kind = "hash",
            Items = [new MarshalValue { Kind = "sym", Text = "a" }, Whole("int", 1)],
        });
        AssertTrue(!mapping.IsList, "a mapping is not a list");
        AssertEq(mapping.Members.Count, 1, "and holds its one entry");
    }

    public void Test_AGamesValueKeepsItsClassAsWrittenAndItsMembers()
    {
        var converter = new RubyValueConverter();
        var value = converter.Convert(new MarshalValue
        {
            Kind = "Object",
            ClassName = "RPG::Actor",
            Keys = ["@name", "@hp"],
            Items = [Bytes("str", 65), Whole("int", 100)],
        });
        AssertEq(value.Kind, RubyValueKind.Object, "a game's value is an object");
        AssertEq(value.ClassName, "RPG::Actor", "naming the class as written");
        AssertEq(value.Members.Count, 2, "with both members");
        AssertEq(
            value.Members[RubyValue.OfSymbol("@name")].Bytes[0], (byte)65,
            "and the first member's bytes");
    }

    public void Test_AStructBecomesAnObjectWithItsMemberNames()
    {
        var converter = new RubyValueConverter();
        var value = converter.Convert(new MarshalValue
        {
            Kind = "Struct",
            ClassName = "Point",
            Keys = ["x", "y"],
            Items = [Whole("int", 3), Whole("int", 4)],
        });
        AssertEq(value.ClassName, "Point", "the struct's class carries over");
        AssertEq(value.Members.Count, 2, "with both of its fields");
        AssertEq(
            value.Members[RubyValue.OfSymbol("x")].Integer, 3L,
            "and the first field is three");
    }

    public void Test_ALinkBecomesTheSameValueItNames()
    {
        // A link is the file saying two names stand for one thing. Following it
        // is what keeps a game's data from quietly growing a second copy of
        // something that was one.
        var converter = new RubyValueConverter();
        var first = converter.Convert(new MarshalValue
        {
            Kind = "string",
            Bytes = [65, 66],
            Integer = 1,
        });
        var second = converter.Convert(new MarshalValue
        {
            Kind = "string",
            Link = new MarshalLink { Kind = "link", Index = 1 },
        });
        AssertTrue(
            ReferenceEquals(first, second),
            "a link resolves to the very same value, not an equal one");
    }

    /// <summary>
    /// Writes a Marshal long, which is not eight bytes.
    /// </summary>
    /// <remarks>
    /// A Marshal stream packs a whole number into the fewest bytes that hold it
    /// and marks which form it used. Writing eight bytes for a small number
    /// produces a different stream from the one a game writes, and a reader
    /// that accepted it would be accepting something the format does not have.
    /// </remarks>
    private static void AddLong(List<byte> pBytes, long pValue)
    {
        if (pValue > 0 && pValue < 123)
        {
            pBytes.Add((byte)(pValue + 5));
            return;
        }
        if (pValue > 0 && pValue < 256)
        {
            pBytes.Add(0x01);
            pBytes.Add((byte)pValue);
            return;
        }
        if (pValue == 0)
        {
            pBytes.Add(0x00);
            return;
        }
        throw new InvalidOperationException($"the test only writes small positives, not {pValue}");
    }

    private static void AddRaw(List<byte> pBytes, string pText)
    {
        pBytes.AddRange(Encoding.ASCII.GetBytes(pText));
    }

    public void Test_AStreamIsReadWithTheNumberingItsLinksUse()
    {
        // The numbering is the reader's own, and the specification is explicit
        // that a stream holds one copy of each object, the first object having
        // the number one. The documented stream for an array holding the same
        // string twice is "\004\b[\a\"\nhello@\006", and in it the string
        // is object two: the array took one because a container is numbered
        // before its contents, which is what lets a container hold a reference
        // to itself.
        var body = new List<byte>();
        body.Add((byte)MarshalType.Array);
        AddLong(body, 2);                                // two elements
        body.Add((byte)MarshalType.String);
        AddLong(body, 5);
        AddRaw(body, "hello");
        body.Add((byte)MarshalType.ObjectLink);
        AddLong(body, 2);                                // which is the string

        var stream = new List<byte> { 0x04, 0x08 };
        stream.AddRange(body);

        var root = new MarshalReader(stream.ToArray()).Read();
        AssertEq(root.Kind, "array", "the stream is an array");
        AssertEq(root.Items.Count, 2, "with two elements");
        AssertEq(root.Items[0].Text, "hello", "the first naming hello");
        AssertTrue(root.Items[1].IsLink, "the second is a link to it");
        AssertEq(root.Items[1].Link!.Index, 2, "naming the string, which is object two");

        // The link and the value it names become the very same value, which only
        // holds when the converter files values under the reader's own numbers.
        var converter = new RubyValueConverter();
        var first = converter.Convert(root.Items[0]);
        var second = converter.Convert(root.Items[1]);
        AssertTrue(
            ReferenceEquals(first, second),
            "the link is the very same value, not a second copy of it");
        AssertEq(second.Bytes.Length, 5, "and it still holds what it held");
    }

    public void Test_AContainerCanHoldAReferenceToItself()
    {
        // A container is numbered before its contents are read, which is the only
        // reason this can work. A reader that numbered afterwards could never
        // resolve a container pointing at itself, and a game's data does exactly
        // this whenever a structure names itself.
        var body = new List<byte>();
        body.Add((byte)MarshalType.Array);
        AddLong(body, 1);                                // one element
        body.Add((byte)MarshalType.ObjectLink);
        AddLong(body, 1);                                // which is this array

        var stream = new List<byte> { 0x04, 0x08 };
        stream.AddRange(body);
        var root = new MarshalReader(stream.ToArray()).Read();

        AssertEq(root.Kind, "array", "the stream is an array");
        AssertEq(root.Integer!.Value, 1L, "numbered one, before its contents");
        AssertEq(root.Items[0].Integer!.Value, 1L, "and its element names it");
    }

    public void Test_TheFirstObjectIsNumberOneAndTheFirstSymbolNumberZero()
    {
        // Both halves of the reader's numbering, read from real streams rather
        // than from memory. A converter that counted both from zero would put
        // every object link one off, and one that counted both from one would
        // put every symbol link one off the other way.
        var withArray = new List<byte> { 0x04, 0x08 };
        withArray.Add((byte)MarshalType.Array);
        AddLong(withArray, 1);
        withArray.Add((byte)MarshalType.String);
        AddLong(withArray, 5);
        AddRaw(withArray, "hello");
        var array = new MarshalReader(withArray.ToArray()).Read();
        AssertEq(array.Integer!.Value, 1L, "the array is object one");
        AssertEq(array.Items[0].Integer!.Value, 2L, "and the string it holds is object two");

        var withSymbol = new List<byte> { 0x04, 0x08 };
        withSymbol.Add((byte)MarshalType.Symbol);
        AddLong(withSymbol, 5);
        AddRaw(withSymbol, "hello");
        var symbol = new MarshalReader(withSymbol.ToArray()).Read();
        AssertEq(symbol.Integer!.Value, 0L, "the first symbol is at zero");
    }

    public void Test_ASymbolLinkFollowsTheReadersOwnNumbering()
    {
        // Symbols are numbered from zero and objects from one, by the same
        // reader, and a link carries whichever the writer used. The two kinds of
        // link therefore do not share a space, and a converter that counted both
        // from one would put every symbol link one off.
        var body = new List<byte>();
        body.Add((byte)MarshalType.Symbol);
        AddLong(body, 5);
        AddRaw(body, "hello");
        body.Add((byte)MarshalType.SymbolLink);
        AddLong(body, 0);

        var stream = new List<byte> { 0x04, 0x08 };
        stream.AddRange(body);
        var decoded = new MarshalReader(stream.ToArray()).Read();
        AssertEq(decoded.Kind, "symbol", "the root is the symbol");
        AssertEq(decoded.Integer!.Value, 0L, "and the first symbol is at zero");

        var converter = new RubyValueConverter();
        AssertEq(
            converter.Convert(decoded).Name, "hello",
            "which still names hello once converted");

        // The link to it, read on its own, is refused because the symbol it names
        // was never converted into this converter. Following a link needs the
        // value it names to have been met, and a converter has no way to invent it.
        var alone = new RubyValueConverter();
        var error = Refusal(() => alone.Convert(new MarshalValue
        {
            Kind = "symbol",
            Link = new MarshalLink { Kind = "link", Index = 0 },
        }));
        AssertTrue(
            error.Contains("cannot be followed"),
            $"a link to a value that was never met is refused: {error}");
    }

    public void Test_ASymbolLinkIsFollowedWhenTheValueItNamesWasMetFirst()
    {
        var body = new List<byte>();
        body.Add((byte)MarshalType.Array);
        AddLong(body, 2);                                // two elements
        body.Add((byte)MarshalType.Symbol);
        AddLong(body, 5);
        AddRaw(body, "hello");
        body.Add((byte)MarshalType.SymbolLink);
        AddLong(body, 0);

        var stream = new List<byte> { 0x04, 0x08 };
        stream.AddRange(body);
        var root = new MarshalReader(stream.ToArray()).Read();

        var converter = new RubyValueConverter();
        var first = converter.Convert(root.Items[0]);
        var second = converter.Convert(root.Items[1]);
        AssertEq(first.Name, "hello", "the first names hello");
        AssertEq(second.Name, "hello", "and the link names the same one");
        AssertTrue(
            ReferenceEquals(first, second),
            "because a link is a second name for the same value");
    }

    public void Test_ASelfLinkIsRefusedRatherThanGuessed()
    {
        // A value that points at itself has no value to return yet. Returning
        // something else would give it a second identity inside its own
        // contents, so this is a refusal with the number in it.
        var converter = new RubyValueConverter();
        var error = Refusal(() => converter.Convert(new MarshalValue
        {
            Kind = "Array",
            Items =
            [
                new MarshalValue
                {
                    Kind = "Array",
                    Link = new MarshalLink { Kind = "link", Index = 1 },
                },
            ],
        }));
        AssertTrue(
            error.Contains("1"),
            $"a self link is refused and names the entry: {error}");
    }
    public void Test_AMappingKeyedByANumberKeepsThatNumberAsItsKey()
    {
        // A game's hash is keyed by whatever it was keyed by, and a number is an
        // ordinary key. Assuming every key is a name would lose the entry.
        var converter = new RubyValueConverter();
        var value = converter.Convert(new MarshalValue
        {
            Kind = "hash",
            Items = [Whole("int", 7), Bytes("str", 88)],
        });
        AssertEq(value.Members.Count, 1, "the entry survives");
        AssertEq(
            value.Members[RubyValue.OfInteger(7)].Bytes[0], (byte)88,
            "keyed by the number seven");
    }

    public void Test_AGamesClassSurvivesTheConversionEvenWhenItCarriesNothing()
    {
        // A value with a class and no members is ordinary: a freshly made one, a
        // game object that has not been given anything yet. Dropping the class
        // because there is nothing else would lose the only thing that says what
        // the value is.
        var converter = new RubyValueConverter();
        var value = converter.Convert(new MarshalValue
        {
            Kind = "Object",
            ClassName = "RPG::Sprite",
            Keys = [],
            Items = [],
        });
        AssertEq(value.ClassName, "RPG::Sprite", "the class survives with nothing else");
        AssertEq(value.Members.Count, 0, "and there is still nothing inside");
        AssertTrue(
            !converter.RefusedKinds.Contains("Object"),
            "a value with no members is not a refusal");
    }

    public void Test_EveryClassNameCarriesOverAndNotJustTheFirst()
    {
        // A game has many kinds of value in one file. Carrying the first one's
        // class over and dropping the rest would leave most of a game's data
        // saying nothing about what it is.
        var converter = new RubyValueConverter();
        var names = new[] { "RPG::Actor", "RPG::Item", "Sprite", "Window_Base", "Game_Player" };
        var seen = new List<string>();
        foreach (var name in names)
        {
            seen.Add(converter.Convert(new MarshalValue
            {
                Kind = "Object",
                ClassName = name,
                Keys = [],
                Items = [],
            }).ClassName ?? "");
        }
        for (var index = 0; index < names.Length; index++)
        {
            AssertEq(seen[index], names[index], $"the class {names[index]} carried over");
        }
    }

    public void Test_TrueAndFalseStayTwoValues()
    {
        // A game asks these two apart constantly. A reader that made them one
        // value would answer yes to no.
        var converter = new RubyValueConverter();
        var yes = converter.Convert(Of("true"));
        var no = converter.Convert(Of("false"));
        AssertTrue(!yes.Equals(no), "true and false are two values");
        AssertTrue(yes.Boolean, "the first is true");
        AssertTrue(!no.Boolean, "and the second is false");
    }

    public void Test_TrueAndFalseAreNotTheSameAsTheNumbersThatStandInForThem()
    {
        // Ruby lets a whole number stand in for either, and the language keeps
        // them apart anyway. Folding them together would lose the difference
    // between a name and a number, which is a difference a game can observe.
        var converter = new RubyValueConverter();
        AssertTrue(
            !converter.Convert(Of("true")).Equals(converter.Convert(Whole("int", 1))),
            "true is not the number one");
        AssertTrue(
            !converter.Convert(Of("false")).Equals(RubyValue.Nil),
            "and false is not nothing");
    }

    public void Test_EveryKindTheReaderEmitsSurvivesTheConversion()
    {
        // The bug this closes is worth the test on its own: the converter was
        // written against kind names spelled out from memory, and two of them
        // were not the names the reader emits. A whole number arrives as
        // "integer" and a string as "string", so every number and every string
        // in a real game's data was refused. Reading each kind from a real
        // stream is the only way that cannot happen again, because the stream
        // decides the name and this test asks for no name at all.
        var cases = new (string pWhat, List<byte> pBody, RubyValueKind pKind)[]
        {
            ("nil", Body(b => b.Add((byte)MarshalType.Nil)), RubyValueKind.Nil),
            ("true", Body(b => b.Add((byte)MarshalType.True)), RubyValueKind.Boolean),
            ("false", Body(b => b.Add((byte)MarshalType.False)), RubyValueKind.Boolean),
            ("a whole number", Body(b =>
            {
                b.Add((byte)MarshalType.Integer);
                AddLong(b, 42);
            }), RubyValueKind.Integer),
            ("a fraction", Body(b =>
            {
                b.Add((byte)MarshalType.Float);
                AddRaw(b, "2.5");                             // the digits
                b.Add(0x00);                                  // and a zero terminates it
            }), RubyValueKind.Float),
            ("a string", Body(b =>
            {
                b.Add((byte)MarshalType.String);
                AddLong(b, 5);
                AddRaw(b, "hello");
            }), RubyValueKind.String),
            ("a symbol", Body(b =>
            {
                b.Add((byte)MarshalType.Symbol);
                AddLong(b, 5);
                AddRaw(b, "hello");
            }), RubyValueKind.Symbol),
            ("a list", Body(b =>
            {
                b.Add((byte)MarshalType.Array);
                AddLong(b, 0);
            }), RubyValueKind.Object),
            ("a mapping", Body(b =>
            {
                b.Add((byte)MarshalType.Hash);
                AddLong(b, 0);
            }), RubyValueKind.Object),
        };

        foreach (var (what, body, expected) in cases)
        {
            var stream = new List<byte> { 0x04, 0x08 };
            stream.AddRange(body);
            MarshalValue decoded;
            try
            {
                decoded = new MarshalReader(stream.ToArray()).Read();
            }
            catch (Exception exception)
            {
                AssertTrue(false, $"{what} could not be read: {exception.Message}");
                return;
            }
            try
            {
                var value = new RubyValueConverter().Convert(decoded);
                AssertEq(
                    value.Kind, expected,
                    $"{what} arrives as '{decoded.Kind}' and converts to the right kind");
            }
            catch (Exception exception)
            {
                AssertTrue(
                    false, $"{what} arrives as '{decoded.Kind}' and was refused: {exception.Message}");
                return;
            }
        }
    }

    public void Test_AWholeNumberFromARealStreamKeepsItsValue()
    {
        // The same stream as above, checked for the number itself, because a
        // reader that refused the kind would also refuse the value and a test
        // that only checked the refusal would not say which happened.
        var body = new List<byte>();
        body.Add((byte)MarshalType.Integer);
        AddLong(body, 42);
        var stream = new List<byte> { 0x04, 0x08 };
        stream.AddRange(body);
        var decoded = new MarshalReader(stream.ToArray()).Read();
        AssertEq(decoded.Kind, "integer", "the reader calls it an integer");
        var value = new RubyValueConverter().Convert(decoded);
        AssertEq(value.Kind, RubyValueKind.Integer, "and it converts to one");
        AssertEq(value.Integer, 42L, "holding forty two");
    }

    public void Test_AGamesValueFromARealStreamKeepsItsClass()
    {
        // The class is the name from the file and nothing more. A reader that
        // dropped it would leave most of a game's data saying nothing about what
        // it is, and every value would look like every other one.
        var body = new List<byte>();
        body.Add((byte)MarshalType.Object);
        body.Add((byte)':');
        AddLong(body, 6);
        AddRaw(body, "Object");
        AddLong(body, 0);                                // no instance variables
        var stream = new List<byte> { 0x04, 0x08 };
        stream.AddRange(body);
        var decoded = new MarshalReader(stream.ToArray()).Read();
        var value = new RubyValueConverter().Convert(decoded);
        AssertEq(value.Kind, RubyValueKind.Object, "a game's value is an object");
        AssertEq(value.ClassName, "Object", "and it says what class it is");
    }

    public void Test_AValueWithNoLanguageEquivalentIsRefusedWithItsKind()
    {
        // The point of stopping is that a value one can only approximate is worse
        // than none, so the kind is named rather than guessed at.
        var converter = new RubyValueConverter();
        var error = Refusal(() => converter.Convert(Of("bignum")));
        AssertTrue(
            error.Contains("bignum"),
            $"the refusal names the kind it refused: {error}");
    }

    public void Test_APayloadThatContradictsItsKindIsRefused()
    {
        // A kind that says a number and carries none is a file this reader cannot
        // read honestly, and saying so beats producing a zero.
        var converter = new RubyValueConverter();
        AssertTrue(
            Refusal(() => converter.Convert(Of("int"))).Contains("int"),
            "a whole number with no number in it is refused");
        AssertTrue(
            Refusal(() => converter.Convert(Of("str"))).Contains("str"),
            "a string with no bytes is refused");
        AssertTrue(
            Refusal(() => converter.Convert(Of("regexp"))).Contains("regexp"),
            "a pattern with no text is refused");
    }

    public void Test_AMappingWithAMissingHalfIsRefused()
    {
        var converter = new RubyValueConverter();
        var error = Refusal(() => converter.Convert(new MarshalValue
        {
            Kind = "hash",
            Items = [new MarshalValue { Kind = "sym", Text = "lonely" }],
        }));
        AssertTrue(
            error.Contains("key and a value"),
            $"an entry without its half is refused: {error}");
    }

    public void Test_AMappingHoldingTheSameKeyTwiceIsRefused()
    {
        // One mapping cannot hold a key twice, and picking one of the two would
        // be choosing for the game.
        var converter = new RubyValueConverter();
        var error = Refusal(() => converter.Convert(new MarshalValue
        {
            Kind = "hash",
            Items =
            [
                new MarshalValue { Kind = "sym", Text = "a" }, Whole("int", 1),
                new MarshalValue { Kind = "sym", Text = "a" }, Whole("int", 2),
            ],
        }));
        AssertTrue(error.Contains("twice"), $"a repeated key is refused: {error}");
    }

    public void Test_AConverterCountsAndRemembersWhatItRefused()
    {
        // A refusal that goes unrecorded is a refusal nobody acts on, so the
        // count and the kinds are kept.
        var converter = new RubyValueConverter();
        Refusal(() => converter.Convert(Of("bignum")));
        Refusal(() => converter.Convert(Of("time")));
        AssertEq(converter.RefusedCount, 2, "two values were refused");
        AssertEq(converter.RefusedKinds.Count, 2, "and both kinds were kept");
        AssertEq(converter.RefusedKinds[0], "bignum", "the first was a bignum");
    }

    public void Test_ALinkToSomethingNotReadYetIsRefused()
    {
        // The reader puts an index in the stream and keeps the values in the
        // order it met them. A link forward is something this reader cannot
        // follow, and pretending otherwise would invent a value.
        var converter = new RubyValueConverter();
        var error = Refusal(() => converter.Convert(new MarshalValue
        {
            Kind = "str",
            Link = new MarshalLink { Kind = "link", Index = 7 },
        }));
        AssertTrue(error.Contains("7"), $"the refusal names the entry: {error}");
        AssertTrue(
            error.Contains("cannot be followed"),
            $"and says the entry was never read: {error}");
    }
}
