namespace UniversalRPG.Rm2k.Rendering;

/// <summary>
/// Resolves the hero's charset, verified against EasyRPG Player
/// <c>src/game_player.cpp</c> (<c>Game_Player::ResetGraphic</c>).
/// </summary>
/// <remarks>
/// <para>
/// The hero has no event page, so the Player builds the player character from
/// the first party member:
/// <c>auto* actor = Main_Data::game_party-&gt;GetActor(0)</c>, followed by
/// <c>SetSpriteGraphic(ToString(actor-&gt;GetSpriteName()), actor-&gt;GetSpriteIndex())</c>
/// and <c>SetTransparency(actor-&gt;GetSpriteTransparency())</c>. A null actor
/// means an empty sprite, which is a party-less game.
/// </para>
/// <para>
/// The sprite getters are verified in <c>src/game_actor.h</c>:
/// <c>GetSpriteName()</c> returns the runtime override
/// <c>data.sprite_name</c> when it is not empty and otherwise falls back to
/// <c>dbActor-&gt;character_name</c>; <c>GetSpriteIndex()</c> uses
/// <c>data.sprite_id</c> in the same case. <c>SetSprite</c> is what clears the
/// override: it sets <c>sprite_name = ""</c> when the requested graphic equals
/// the database values, so a fresh game always draws the LDB graphic. That is
/// why the database is the single source here and no override is invented.
/// </para>
/// </remarks>
public static class Rm2kHeroSprite
{
    /// <summary>lcf <c>Data::system.party</c> holds the starting actors.</summary>
    public const int NoPartyMember = 0;

    /// <summary>
    /// The hero sprite request for a fresh game, or null when the party has no
    /// leading actor, which the Player renders without a graphic.
    /// </summary>
    /// <param name="pActorCharacterName">LDB actor <c>character_name</c>.</param>
    /// <param name="pActorCharacterIndex">LDB actor <c>character_index</c>.</param>
    public static Rm2kHeroGraphic? FromActor(string pActorCharacterName, int pActorCharacterIndex)
    {
        if (string.IsNullOrEmpty(pActorCharacterName))
        {
            // Verified: SetSprite("", 0) is what a party-less game produces, and
            // an empty sprite name has nothing to load from CharSet.
            return null;
        }
        return new Rm2kHeroGraphic(pActorCharacterName, pActorCharacterIndex);
    }
}

/// <summary>
/// A verified hero charset request: the file name without extension and the
/// cell index, which are exactly what the Player hands to
/// <c>Sprite_Character</c>.
/// </summary>
public readonly record struct Rm2kHeroGraphic(string SpriteName, int CharacterIndex)
{
    /// <summary>
    /// The Player requests charset material from the <c>CharSet</c> directory
    /// (<c>src/cache.cpp</c>), like the chipset from <c>ChipSet</c>.
    /// </summary>
    public string FileName => SpriteName + ".png";
}
