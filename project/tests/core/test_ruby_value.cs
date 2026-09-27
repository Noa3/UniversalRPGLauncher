using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Rgss;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// The values a Ruby script could have, held as data.
/// </summary>
/// <remarks>
/// Two things are being checked here. The first is that a value says what it is
/// and what is in it. The second, and the more useful one, is that the reader
/// this already had and this one agree: a file decoded by one and read by the
/// other must not quietly change shape, or the two layers will disagree about a
/// game's data without either of them being wrong.
/// </remarks>
public partial class TestRubyValue : TestBase
{
    public void Test_TheAbsenceOfAValueIsItselfAValue()
    {
        AssertEq(RubyValue.Nil.Kind, RubyValueKind.Nil, "nil is a kind of its own");
        AssertTrue(RubyValue.Nil.IsNil, "and it says so");
        AssertTrue(RubyValue.Nil.Equals(RubyValue.Nil), "two nils are the same");
    }

    public void Test_AWholeNumberAndAFractionAreDifferentKinds()
    {
        var whole = RubyValue.OfInteger(42);
        AssertEq(whole.Kind, RubyValueKind.Integer, "a whole number is an integer");
        AssertEq(whole.Integer, 42L, "holding forty two");

        var fraction = RubyValue.OfReal(1.5);
        AssertEq(fraction.Kind, RubyValueKind.Float, "a number with a fraction is a float");
        AssertEq(fraction.Real, 1.5, "holding one and a half");
    }

    public void Test_OneAndTheSameNumberIsOneValueAndADifferentNumberIsNot()
    {
        // Identity of a value, which is a smaller question than the one a script
        // asks when it compares two of them.
        AssertTrue(
            RubyValue.OfInteger(7).Equals(RubyValue.OfInteger(7)),
            "two integers with the same number are the same integer");
        AssertTrue(
            !RubyValue.OfInteger(7).Equals(RubyValue.OfInteger(8)),
            "and two with different numbers are not");
    }

    public void Test_AStringIsItsBytesAndNotADecodedText()
    {
        // A game's strings are in whatever encoding its author used, and this
        // reader does not decide which. A value that decoded on the way in would
        // have made a choice the file never stated.
        var value = RubyValue.OfBytes([0x83, 0x65, 0x83, 0x58]);
        AssertEq(value.Kind, RubyValueKind.String, "bytes are a string");
        AssertEq(value.Bytes.Length, 4, "with all four of them");
        AssertEq(value.Bytes[0], (byte)0x83, "and the first is unchanged");
        AssertTrue(
            !typeof(RubyValue).GetProperties()
                .Select(pProperty => pProperty.Name)
                .Contains("Text"),
            "and there is no text at all, because nothing decoded it");
    }

    public void Test_TwoStringsWithTheSameBytesAreTheSameString()
    {
        AssertTrue(
            RubyValue.OfBytes([1, 2, 3]).Equals(RubyValue.OfBytes([1, 2, 3])),
            "the same bytes are the same string");
        AssertTrue(
            !RubyValue.OfBytes([1, 2, 3]).Equals(RubyValue.OfBytes([1, 2, 4])),
            "and different bytes are not");
    }

    public void Test_ASymbolIsANameAndTwoNamesAreNotTheSameName()
    {
        var value = RubyValue.OfSymbol("window");
        AssertEq(value.Kind, RubyValueKind.Symbol, "a name is a symbol");
        AssertEq(value.Name, "window", "which is window");
        AssertTrue(
            RubyValue.OfSymbol("a").Equals(RubyValue.OfSymbol("a")),
            "the same name twice is the same symbol");
        AssertTrue(
            !RubyValue.OfSymbol("a").Equals(RubyValue.OfSymbol("b")),
            "and two names are two symbols");
    }

    public void Test_APatternIsKeptAsTextAndFlagsAndIsNeverRun()
    {
        var value = RubyValue.OfRegexp("a.*z", 3);
        AssertEq(value.Kind, RubyValueKind.Regexp, "a pattern is a regexp");
        AssertEq(value.Source, "a.*z", "with its text as written");
        AssertEq(value.Options, 3, "and its flags");
        AssertTrue(
            !value.Equals(RubyValue.OfRegexp("a.*z", 5)),
            "and different flags make it a different pattern");
    }

    public void Test_AGamesOwnObjectKeepsItsClassAsTextAndNotAsAType()
    {
        // The class is the name from the file. Nothing here has looked up
        // whether such a class exists, and nothing here could load it if it did.
        var value = RubyValue.OfEmptyObject("Sprite");
        AssertEq(value.Kind, RubyValueKind.Object, "a game value is an object");
        AssertEq(value.ClassName, "Sprite", "naming the class as written");
        AssertEq(value.Members.Count, 0, "with nothing in it yet");
    }

    public void Test_TwoObjectsWithTheSameMembersAreStillTwoObjects()
    {
        // An object in a game is defined by its identity. Two that happen to hold
        // the same fields are two objects, and treating them as one would be a
        // real behaviour a game could observe.
        var members = new Dictionary<RubyValue, RubyValue>
        {
            [RubyValue.OfSymbol("x")] = RubyValue.OfInteger(1),
        };
        var first = RubyValue.OfObject("Sprite", members);
        var second = RubyValue.OfObject("Sprite", members);
        AssertTrue(!first.Equals(second), "two objects with the same members differ");
        AssertTrue(first.Equals(first), "while an object is the same as itself");
    }

    public void Test_AnObjectKeepsWhatWasPutInIt()
    {
        var value = RubyValue.OfObject("Sprite", new Dictionary<RubyValue, RubyValue>
        {
            [RubyValue.OfSymbol("x")] = RubyValue.OfInteger(10),
            [RubyValue.OfSymbol("y")] = RubyValue.OfInteger(20),
        });
        AssertEq(value.Members.Count, 2, "holding both members");
        AssertEq(
            value.Members[RubyValue.OfSymbol("x")].Integer, 10L,
            "and the first is ten");
    }

    public void Test_ValuesOfDifferentKindsAreNeverTheSame()
    {
        AssertTrue(
            !RubyValue.OfInteger(1).Equals(RubyValue.OfReal(1.0)),
            "the number one as a whole and as a fraction are two values");
        AssertTrue(
            !RubyValue.OfSymbol("1").Equals(RubyValue.OfInteger(1)),
            "a name that looks like a number is a name");
        AssertTrue(
            !RubyValue.Nil.Equals(RubyValue.OfInteger(0)),
            "and nothing is not the number zero");
    }

    public void Test_TheSameValueAlwaysHashesTheSameWay()
    {
        // Without this two equal values could land in different buckets, and a
        // dictionary keyed by them would start losing entries.
        var first = RubyValue.OfBytes([7, 8, 9]);
        var second = RubyValue.OfBytes([7, 8, 9]);
        AssertEq(first.GetHashCode(), second.GetHashCode(), "equal strings hash the same");

        var bySymbol = RubyValue.OfSymbol("name");
        AssertEq(
            bySymbol.GetHashCode(), RubyValue.OfSymbol("name").GetHashCode(),
            "equal symbols hash the same");

        var byInt = RubyValue.OfInteger(123456789);
        AssertEq(
            byInt.GetHashCode(), RubyValue.OfInteger(123456789).GetHashCode(),
            "equal numbers hash the same");
    }

    public void Test_DifferentValuesDoNotCollideIntoOneBucket()
    {
        // A hash that puts many different values on one number is still correct,
        // so this cannot prove much on its own. It does catch a reader that
        // hashed every value the same, which would make a lookup a linear scan.
        var kinds = new[]
        {
            RubyValue.Nil,
            RubyValue.OfInteger(1),
            RubyValue.OfReal(1.0),
            RubyValue.OfBytes([1]),
            RubyValue.OfSymbol("1"),
            RubyValue.OfRegexp("1", 0),
            RubyValue.OfEmptyObject("1"),
        };
        var seen = new Dictionary<int, string>();
        foreach (var value in kinds)
        {
            var hash = value.GetHashCode();
            if (seen.TryGetValue(hash, out var other))
            {
                AssertTrue(false, $"{value} collides with {other} on hash {hash}");
                return;
            }
            seen[hash] = value.ToString();
        }
        AssertTrue(true, "each kind hashes apart from the others");
    }

    public void Test_AMemberKeyedByAValueCanBeFoundWithAnEqualOne()
    {
        // This is the point of hashing a value at all, so it is checked rather
        // than assumed: a dictionary keyed by names has to work with a key built
        // somewhere else.
        var members = new Dictionary<RubyValue, RubyValue>
        {
            [RubyValue.OfSymbol("x")] = RubyValue.OfInteger(1),
        };
        AssertTrue(
            members.TryGetValue(RubyValue.OfSymbol("x"), out var found),
            "a key built apart still finds its entry");
        AssertEq(found!.Integer, 1L, "and finds the value that was put there");
    }

    public void Test_TheStringFormSaysWhatItIsWithoutRevealingTheBytes()
    {
        // Printing a game's bytes as if they were text would put mojibake into a
        // log, so the form reports the size instead.
        var value = RubyValue.OfBytes([0x83, 0x65]);
        AssertEq(value.ToString(), "\"2 bytes\"", "a string says how many bytes it has");
        AssertEq(RubyValue.OfInteger(5).ToString(), "5", "a number says what it is");
        AssertEq(RubyValue.Nil.ToString(), "nil", "and nil says nil");
    }
}
