namespace UniversalRPG.Web;

/// <summary>
/// Why a run is being held up, when the answer is not a number of frames.
/// </summary>
/// <remarks>
/// <para>
/// The engine's <c>Game_Interpreter</c> has a <c>_waitCount</c> and a
/// <c>_waitMode</c>, and they are two different waits. A 230 sets
/// <c>_waitCount</c>, and <c>updateWaitCount</c> counts it down. A 201 sets
/// <c>_waitMode</c> to <c>"transfer"</c>, and <c>updateWaitMode</c> asks
/// <c>$gamePlayer.isTransferring()</c> every frame until the map has changed.
/// </para>
/// <para>
/// <b>K-125 modelled the count and K-127 the movement, and neither of those
/// is this.</b> A condition wait has no length: it is over when the thing it
/// is waiting for is done, and a caller passing frames cannot end it. A reader
/// that treated a transfer as a wait of some length would either hold the page
/// for ever or cut it short, and neither is what the engine does.
///
/// </para>
/// <para>
/// The engine's own modes are <c>message</c>, <c>transfer</c>, <c>scroll</c>,
/// <c>route</c> and <c>until</c>. **Only <see cref="Transfer"/> is modelled
/// here**, because only a transfer is something this reader can be told about;
/// the others need a scrolling map, a moving character and a plugin callback
/// that this repository does not run.
/// </para>
/// </remarks>
public enum MzWaitMode
{
    /// <summary>Not held up by a condition. A 230's frames are held here.</summary>
    None = 0,

    /// <summary>
    /// Held until the reserved transfer has been carried out — until the player
    /// is no longer on their way somewhere.
    /// </summary>
    /// <remarks>
    /// **This is the one condition a reader can actually be told about**, and
    /// it is the reason this enum exists rather than a flag on the interpreter.
    /// </remarks>
    Transfer = 1,
}
