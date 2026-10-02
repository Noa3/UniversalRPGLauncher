using System;
using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// Where the game is showing, and what it can go back to.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this exists because <c>push</c> and <c>goto</c> are not
/// the same operation</strong> -- <strong>and an event list writes
/// both</strong>.
/// </para>
/// <para>
/// Measured at <c>rpg_managers.js</c>:
/// </para>
/// <code>
/// static goto(sceneClass) {
///     if (sceneClass) { this._nextScene = new sceneClass(); }
///     if (this._scene) { this._scene.stop(); }
/// };
/// static push(sceneClass) {
///     this._stack.push(this._scene.constructor);
///     this.goto(sceneClass);
/// };
/// static pop() {
///     if (this._stack.length &gt; 0) {
///         this.goto(this._stack.pop());
///     } else {
///         this.exit();
///     }
/// };
/// </code>
/// <para>
/// <strong>And the difference is one line</strong>:
/// <strong><c>push</c> remembers the scene it came from and
/// <c>goto</c> does not.</strong> <strong>A reader that treated both as
/// "a scene changed" would let <c>352 Save</c> be walked back out of,
/// and it cannot be</strong> -- <strong>and it would let <c>354 Return
/// to Title</c> be walked back out of either, and that one cannot
/// either.</strong>
/// </para>
/// <para>
/// <strong>And what is recorded is the name and not the scene.</strong>
/// <strong><c>this._nextScene = new sceneClass()</c> constructs a
/// <c>Scene_Save</c> and runs its <c>create</c></strong> -- <strong>and
/// this repository builds no scene</strong>, <strong>so it records which
/// scene the engine would build and lets the caller decide what that
/// means.</strong>
/// </para>
/// </remarks>
public sealed class MzSceneStack
{
    /// <summary>The scene the game shows, and where it would go.</summary>
    public string Current { get; private set; } = "";

    /// <summary>The scene the engine will build next, or empty.</summary>
    public string Next { get; private set; } = "";

    /// <summary>The scenes a <c>push</c> remembered, innermost first.</summary>
    public IReadOnlyList<string> Stack => _stack;

    private readonly List<string> _stack = new();

    /// <summary>Whether the engine would pop, rather than exit.</summary>
    public bool CanPop => _stack.Count > 0;

    /// <summary>
    /// <c>SceneManager.goto</c>, which forgets where it came from.
    /// </summary>
    public void GeheZu(string pSzene)
    {
        Next = pSzene;
        if (pSzene.Length > 0)
        {
            Current = pSzene;
        }
    }

    /// <summary>
    /// <c>SceneManager.push</c>, which remembers where it came from.
    /// </summary>
    public void Schiebe(string pSzene)
    {
        if (Current.Length > 0)
        {
            _stack.Add(Current);
        }

        GeheZu(pSzene);
    }

    /// <summary>
    /// <c>SceneManager.pop</c>, or the engine's <c>exit</c> when the stack
    /// is empty.
    /// </summary>
    /// <returns>
    /// The scene it went back to, and empty when the engine would have
    /// <c>goto(null)</c> and left the game.
    /// </returns>
    public string Poppe()
    {
        if (_stack.Count == 0)
        {
            GeheZu("");
            return "";
        }

        var zurueck = _stack[_stack.Count - 1];
        _stack.RemoveAt(_stack.Count - 1);
        GeheZu(zurueck);
        return zurueck;
    }

    /// <summary>One line, for an action and for a log.</summary>
    public override string ToString() =>
        Current.Length > 0
            ? Current + (CanPop ? " over " + _stack[_stack.Count - 1]
                : " with nothing to go back to")
            : "no scene";
}
