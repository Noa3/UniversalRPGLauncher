using System;

namespace UniversalRPG.Rm2k.Rendering;

/// <summary>
/// Raised when character animation data is outside the range the RPG Maker
/// 2000 format defines, for example a move speed with no animation table
/// entry. The runtime reports it instead of indexing out of bounds or silently
/// substituting a value, because a wrong animation speed changes how a game
/// looks without changing anything the player did.
/// </summary>
public sealed class Rm2kAnimationDataException : Exception
{
    public Rm2kAnimationDataException(string pMessage)
        : base(pMessage)
    {
    }
}
