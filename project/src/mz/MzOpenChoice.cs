using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// A choice the player has been asked, and the texts they were asked
/// with.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is a value, and not game state.</strong> The engine's
/// own <c>command102</c> holds its options in a local
/// <c>const choices = []</c> and hands them to <c>$gameMessage</c>;
/// <strong>a reader that wrote them into a dictionary of game state made
/// a choice outlive the branch that asked it</strong>,
/// <strong>and a second choice overwrote the first one's texts while the
/// first was still on screen.</strong>
/// </para>
/// <para>
/// <strong>And it is not called <c>MzChoice</c>.</strong> That name is
/// taken in this namespace by a group of readers and a nested type for
/// <c>126 Change Items</c>, <strong>and one name for two things in one
/// namespace is how a reader ends up reading an item list where a
/// player's answers are.</strong>
/// </para>
/// <para>
/// <strong>And the last option may cancel.</strong> The help for
/// <c>102</c> lists a <em>Cancel</em> field, and the option texts come
/// off the following <c>402</c>s — <strong>so this reader takes the flag
/// and the texts and says which is which.</strong>
/// </para>
/// </remarks>
public sealed class MzOpenChoice
{
    /// <summary>
    /// What the player is asked, in the game's own order.
    /// </summary>
    /// <param name="pOptions">The option texts.</param>
    /// <param name="pCancel">Whether the last option cancels the choice.
    /// </param>
    public MzOpenChoice(IReadOnlyList<string> pOptions, bool pCancel)
    {
        Options = pOptions;
        CanCancel = pCancel;
    }

    /// <summary>The option texts, in the order the game wrote them.</summary>
    public IReadOnlyList<string> Options { get; }

    /// <summary>Whether the last option cancels the choice.</summary>
    public bool CanCancel { get; }

    /// <summary>How many options there are.</summary>
    public int Count => Options.Count;

    /// <summary>
    /// Whether an answer is one of this choice's options.
    /// </summary>
    /// <param name="pBranch">Which branch, counting from one.</param>
    public bool Takes(int pBranch) => pBranch >= 1 && pBranch <= Options.Count;
}
