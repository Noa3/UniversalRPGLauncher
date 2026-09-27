using System;
using System.Collections.Generic;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Builds the LDB system section that gives a database a starting party, which
/// the pinned easyrpg-testgame database does not have.
/// </summary>
/// <remarks>
/// <para>
/// The pinned LDB has eight actors with real character sprites, but its system
/// section carries no <c>party_size</c> and no <c>party</c>, so the verified
/// <c>Game_Player::ResetGraphic</c> path yields no hero and the hero sprite is
/// never built. Every check on the hero's drawn position therefore has nothing
/// to look at.
/// </para>
/// <para>
/// Inside a struct a field is <c>id + length + payload</c> with both numbers in
/// BER, but the two payloads here use different encodings. <c>party_size</c> is
/// a signed BER integer, because the library reads it through its signed BER
/// decoder. <c>party</c> is a packed list of two byte little endian values,
/// because the library reads it as <c>(short)(lo | hi &lt;&lt; 8)</c>. Writing
/// either one in the other's encoding parses and then reports trailing bytes or
/// an empty party, which is the exact failure this builder exists to avoid.
/// </para>
/// </remarks>
public static class Rm2kPartyFixtureBuilder
{
    /// <summary>
    /// LDB section id for the system struct, verified from the parser's own
    /// section table: 0x16 is "system".
    /// </summary>
    public const int SystemSectionId = 0x16;

    /// <summary>
    /// liblcf <c>rpg::System</c> chunk ids: <c>party_size</c> 0x15 paired with
    /// the <c>party</c> array 0x16.
    /// </summary>
    public const int SystemPartySize = 0x15;

    public const int SystemParty = 0x16;

    /// <summary>
    /// BER, which is what the reader decodes for every field id and length:
    /// seven bits per byte, most significant group first, the high bit set on
    /// all but the last byte.
    /// </summary>
    public static List<byte> Ber(int pValue)
    {
        if (pValue < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pValue), "BER values are unsigned in this format.");
        }
        var groups = new List<int>();
        var remaining = pValue;
        do
        {
            groups.Add(remaining & 0x7F);
            remaining >>= 7;
        }
        while (remaining > 0);
        groups.Reverse();
        var bytes = new List<byte>(groups.Count);
        for (var index = 0; index < groups.Count; index++)
        {
            bytes.Add((byte)(groups[index] | (index < groups.Count - 1 ? 0x80 : 0x00)));
        }
        return bytes;
    }

    /// <summary>
    /// A two byte little endian value, the payload form of an array field. The
    /// library reads the party list as
    /// <c>(short)(lo | hi &lt;&lt; 8)</c>, so this is not BER.
    /// </summary>
    private static List<byte> Int16(int pValue)
    {
        return [(byte)(pValue & 0xFF), (byte)((pValue >> 8) & 0xFF)];
    }

    /// <summary>
    /// One struct field: a BER id, a BER length, and the payload.
    /// </summary>
    private static void AddField(List<byte> pBody, int pId, List<byte> pPayload)
    {
        pBody.AddRange(Ber(pId));
        pBody.AddRange(Ber(pPayload.Count));
        pBody.AddRange(pPayload);
    }

    /// <summary>
    /// The system section payload carrying a starting party: the party size as
    /// a two byte integer, the party as packed two byte values, and the struct
    /// terminator that closes the section.
    /// </summary>
    public static List<byte> SystemSectionBody(IReadOnlyList<int> pParty)
    {
        var partyBytes = new List<byte>();
        foreach (var actorId in pParty)
        {
            partyBytes.AddRange(Int16(actorId));
        }

        var body = new List<byte>();
        // The size field is a signed BER integer, read by the library's own
        // integer decoder, while the party list it counts is packed two byte
        // values. Mixing the two encodings parses and then reports trailing
        // bytes, or an empty party, which is why they are separate helpers.
        AddField(body, SystemPartySize, Ber(pParty.Count));
        AddField(body, SystemParty, partyBytes);
        body.Add(0x00);   // the struct terminator the reader requires
        return body;
    }
}
