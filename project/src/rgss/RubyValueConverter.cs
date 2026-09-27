using System;
using System.Collections.Generic;

namespace UniversalRPG.Rgss;

/// <summary>
/// Turns a decoded Marshal value into a value of the language.
/// </summary>
/// <remarks>
/// <para>
/// The two forms are kept apart on purpose. A <see cref="MarshalValue"/>
/// describes what a file said, in that file's own terms: it keeps a string's
/// bytes and a value's class name, and it has no opinion about whether a name is
/// a symbol or a class. A <see cref="RubyValue"/> describes what the language
/// would call that, in the language's terms.
/// </para>
/// <para>
/// Converting between them is a decision, and every decision here is reversible
/// or reported. A Marshal form with no language equivalent is refused with its
/// kind rather than approximated, because a value that is nearly right is a
/// value a game cannot be trusted with.
/// </para>
/// </remarks>
public sealed class RubyValueConverter
{
    private readonly Dictionary<int, RubyValue> _byLink = [];

    /// <summary>
    /// The numbers that have been taken but whose values are not finished.
    /// </summary>
    /// <remarks>
    /// A number is reserved before the value it belongs to is converted, so a
    /// value that points at itself finds a number that exists rather than one
    /// that has not been handed out. Following such a link is a refusal rather
    /// than a guess, because there is no value to return yet.
    /// </remarks>
    private readonly HashSet<int> _reserved = [];

    /// <summary>
    /// The number a value was defined under, or null when it carries none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reader numbers the values a link can point at and keeps that number
    /// beside the value it read, so this converter has no numbering of its own to
    /// get right. Counting again here would be a second opinion about a number
    /// that was already decided, and the two would disagree the moment one of
    /// them started from zero or counted something the other did not.
    /// </para>
    /// <para>
    /// The reader numbers symbols from zero and objects from one. That
    /// difference is the reader's, not this one's, and it is the reason the
    /// number is read rather than computed here.
    /// </para>
    /// </remarks>
    private static int? NumberOf(MarshalValue pValue)
    {
        return pValue.Integer.HasValue ? (int)pValue.Integer.Value : null;
    }

    /// <summary>
    /// The count of values that have no language equivalent and were refused.
    /// </summary>
    public int RefusedCount { get; private set; }

    /// <summary>
    /// The kinds that were refused, in the order they were met.
    /// </summary>
    public IReadOnlyList<string> RefusedKinds => _refused;

    private readonly List<string> _refused = [];

    /// <summary>
    /// Converts a decoded value, following its links.
    /// </summary>
    /// <param name="pValue">The value as the Marshal reader described it.</param>
    /// <returns>The value as the language would name it.</returns>
    /// <exception cref="RubyValueConversionException">
    /// The value has no language equivalent, or its payload is not the shape its
    /// kind promises.
    /// </exception>
    public RubyValue Convert(MarshalValue pValue)
    {
        if (pValue == null)
        {
            throw new ArgumentNullException(nameof(pValue));
        }

        if (pValue.IsLink)
        {
            // A link names a value that appeared earlier in the stream. The
            // earlier one is kept, so the link becomes the very same value and
            // two names in a game's data stay the same thing, which is what the
            // file said when it wrote a link rather than a copy.
            if (_byLink.TryGetValue(pValue.Link!.Index, out var earlier))
            {
                return earlier;
            }
            if (_reserved.Contains(pValue.Link!.Index))
            {
                // The entry exists but is still being read, which is what a value
                // that points at itself looks like. Nothing can be returned yet,
                // and returning something else would give the value a second
                // identity inside its own contents.
                throw new RubyValueConversionException(
                    $"Entry {pValue.Link!.Index} is still being read, so a link to it"
                    + " cannot be followed from inside the value that is being read.");
            }
            throw new RubyValueConversionException(
                $"A link to entry {pValue.Link!.Index} cannot be followed, "
                + "because that entry was not read before it was referred to.");
        }

        // The reader's number is reserved before the contents are converted, so
        // a value that points at itself finds a number that already exists.
        var index = NumberOf(pValue);
        if (index.HasValue)
        {
            _reserved.Add(index.Value);
        }
        var converted = ConvertShape(pValue);
        if (index.HasValue)
        {
            _reserved.Remove(index.Value);
            // The value is filed only once it is complete, because a link is a
            // second name for the very same value and must not find a half
            // filled one.
            _byLink[index.Value] = converted;
        }

        return converted;
    }

    /// <summary>
    /// Converts a value's shape, without its class and without filing it.
    /// </summary>
    /// <param name="pValue">The value as the reader described it.</param>
    private RubyValue ConvertShape(MarshalValue pValue)
    {
        switch (pValue.Kind)
        {
            case "nil":
            case "Nil":
                return RubyValue.Nil;
            case "true":
            case "True":
                return RubyValue.OfBoolean(true);
            case "false":
            case "False":
                return RubyValue.OfBoolean(false);
            case "int":
            case "Int":
            case "integer":
            case "Integer":
            case "Fixnum":
            {
                if (!pValue.Integer.HasValue)
                {
                    return Refuse(pValue, "it says a whole number and carries none");
                }
                return RubyValue.OfInteger(pValue.Integer.Value);
            }
            case "float":
            case "Float":
            {
                if (!pValue.Real.HasValue)
                {
                    return Refuse(pValue, "it says a fraction and carries none");
                }
                return RubyValue.OfReal(pValue.Real.Value);
            }
            case "str":
            case "string":
            case "String":
            {
                if (pValue.Bytes == null)
                {
                    return Refuse(pValue, "it says a string and carries no bytes");
                }
                return RubyValue.OfBytes(pValue.Bytes);
            }
            case "sym":
            case "symbol":
            case "Symbol":
            {
                // A symbol is a name. The reader keeps a symbol's bytes, and this
                // is the one place that turns them into a name, because a name is
                // what a symbol is and the file's own encoding is not this
                // reader's decision to make twice.
                return RubyValue.OfSymbol(DecodeName(pValue));
            }
            case "regexp":
            case "Regexp":
            {
                if (pValue.Text == null)
                {
                    return Refuse(pValue, "it says a pattern and carries no text");
                }
                return RubyValue.OfRegexp(pValue.Text, (int)pValue.Integer.GetValueOrDefault());
            }
            case "array":
            case "Array":
            {
                var elements = new List<RubyValue>(pValue.Items.Count);
                foreach (var item in pValue.Items)
                {
                    elements.Add(Convert(item));
                }
                return RubyValue.OfArray(elements);
            }
            case "hash":
            case "Hash":
            {
                // A mapping's keys can be any value, so they are converted and
                // used as keys rather than assumed to be names. A game's hash
                // keyed by a number is ordinary Ruby.
                var members = new Dictionary<RubyValue, RubyValue>();
                for (var index = 0; index < pValue.Items.Count; index += 2)
                {
                    if (index + 1 >= pValue.Items.Count)
                    {
                        return Refuse(pValue, "a mapping needs a key and a value for each entry");
                    }
                    var key = Convert(pValue.Items[index]);
                    var item = Convert(pValue.Items[index + 1]);
                    if (!members.TryAdd(key, item))
                    {
                        return Refuse(
                            pValue,
                            $"it holds the key {key} twice, which one mapping cannot do");
                    }
                }
                return RubyValue.OfObject(null, members);
            }
            case "object":
            case "Object":
            case "struct":
            case "Struct":
            case "userdef":
            case "UserDefined":
            {
                var members = new Dictionary<RubyValue, RubyValue>();
                for (var index = 0; index < pValue.Items.Count; index++)
                {
                    if (index >= pValue.Keys.Count)
                    {
                        return Refuse(pValue, "it has a member without a name");
                    }
                    var name = RubyValue.OfSymbol(pValue.Keys[index]);
                    if (!members.TryAdd(name, Convert(pValue.Items[index])))
                    {
                        return Refuse(pValue, $"it holds the member {pValue.Keys[index]} twice");
                    }
                }
                return RubyValue.OfObject(pValue.ClassName, members);
            }
        }

        // Anything left is something the language has no name for, and the point
        // of stopping is that a value one can only approximate is worse than none.
        RefusedCount++;
        _refused.Add(pValue.Kind);
        throw new RubyValueConversionException(
            $"A value of kind '{pValue.Kind}' has no equivalent in the language's own kinds.");
    }

    private RubyValue Refuse(MarshalValue pValue, string pWhy)
    {
        RefusedCount++;
        _refused.Add(pValue.Kind);
        throw new RubyValueConversionException($"A value of kind '{pValue.Kind}' is refused: {pWhy}.");
    }

    private static string DecodeName(MarshalValue pValue)
    {
        if (pValue.Text != null)
        {
            return pValue.Text;
        }
        if (pValue.Bytes != null)
        {
            // A name is text, and the only encoding a name can be read as
            // without guessing is the one the file already said. Bytes that are
            // not valid that way are a refusal, not a replacement character.
            return System.Text.Encoding.UTF8.GetString(pValue.Bytes);
        }
        throw new RubyValueConversionException(
            "A symbol carries neither a name nor the bytes to make one from.");
    }
}

/// <summary>The exception a value with no language equivalent raises.</summary>
public sealed class RubyValueConversionException : Exception
{
    public RubyValueConversionException(string pMessage)
        : base(pMessage)
    {
    }
}
