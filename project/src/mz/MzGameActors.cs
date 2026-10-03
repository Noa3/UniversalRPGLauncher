using System;
using System.Collections.Generic;
using System.Linq;

namespace UniversalRPG.Mz;

/// <summary>
/// The actor and party objects the game's scripts ask about, as the
/// engine declares them.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is a split, and the split is the
/// finding.</strong> The game's 84 calls on a party member split like
/// this:
///
/// <code>
/// .changeEquipById()   37   Engine
/// .equips()            14   Engine
/// .hasSkill()           1   Engine
/// .skillMasteryLevel() 13   Plugin
/// .pha()                12   Plugin
/// ._isZAKO()             7   Plugin
/// .addState()            5   Plugin
/// </code>
///
/// <strong>And so the engine carries 52 of 84 calls and a plugin
/// carries 32</strong>, -- <strong>and this reader answers the 52 and
/// names the 32.</strong>
/// </para>
/// <para>
/// <strong>And <c>changeEquipById</c> is the one that needs the data
/// files</strong>, -- <strong>because the engine writes:</strong>
/// </para>
/// <code>
/// changeEquipById(etypeId, itemId) {
///     const slotId = etypeId - 1;
///     if (this.equipSlots()[slotId] === 1) {
///         this.changeEquip(slotId, $dataWeapons[itemId]);
///     } else {
///         this.changeEquip(slotId, $dataArmors[itemId]);
///     }
/// }
/// </code>
/// <para>
/// <strong>And <c>etypeId</c> is one-based and becomes
/// <c>slotId</c> by subtracting one</strong>, -- <strong>and a reader
/// that used it directly would put a sword in the helmet
/// slot.</strong> -- <strong>And whether it is a weapon or an armour
/// is decided by <c>equipSlots()[slotId] === 1</c></strong>, --
/// <strong>and a reader that always looked in Weapons would put every
/// item in the wrong place.</strong>
/// </para>
/// </remarks>
public sealed class MzGameActors
{
    private readonly Dictionary<int, MzActor> _actoren = new();
    private readonly Dictionary<int, MzAusrüstung> _waffen = new();
    private readonly Dictionary<int, MzAusrüstung> _rüstungen = new();

    /// <summary>And the equip slot kinds, as the data file writes them.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And <c>1</c> is a weapon and <c>2</c> is an armour
    /// and <c>3</c> is an accessory</strong>, -- <strong>and the
    /// engine tests <c>=== 1</c> and sends everything else to
    /// <c>$dataArmors</c></strong>, -- <strong>which is why a reader
    /// must not treat 3 as a third file.</strong>
    /// </para>
    /// </remarks>
    public Dictionary<int, int> AusruestungsSlots { get; } = new()
    {
        [1] = 1, [2] = 1, [3] = 1, [4] = 2, [5] = 2,
    };

    /// <summary>And puts a weapon into the data files.</summary>
    /// <param name="pId">Its number.</param>
    /// <param name="pAusrüstung">The item.</param>
    public void SetzeWaffe(int pId, MzAusrüstung pAusrüstung) =>
        _waffen[pId] = pAusrüstung;

    /// <summary>And puts an armour into the data files.</summary>
    /// <param name="pId">Its number.</param>
    /// <param name="pAusrüstung">The item.</param>
    public void SetzeRuestung(int pId, MzAusrüstung pAusrüstung) =>
        _rüstungen[pId] = pAusrüstung;

    /// <summary>And puts an actor into the world.</summary>
    /// <param name="pId">Its number.</param>
    /// <param name="pActor">The actor.</param>
    public void Setze(int pId, MzActor pActor) => _actoren[pId] = pActor;

    /// <summary>
    /// And <c>actor(actorId)</c>, and it gives nothing when there is
    /// none.
    /// </summary>
    /// <param name="pId">The number the script wrote.</param>
    /// <returns>The actor, or null.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the engine writes <c>return $dataActors[actorId]
    /// ? new Game_Actor(actorId) : null</c></strong>, -- <strong>and
    /// the game writes <c>$gameActors.actor(2).changeEquipById(3,
    /// 152)</c> 37 times without a question mark</strong>, --
    /// <strong>and that works because actor 2 exists in this
    /// game.</strong>
    /// </para>
    /// </remarks>
    public MzActor? Actor(int pId) =>
        _actoren.TryGetValue(pId, out var a) ? a : null;

    /// <summary>And how many actors are in the world.</summary>
    public int Anzahl => _actoren.Count;

    /// <summary>And the reader names what it answers.</summary>
    /// <returns>The method names.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And <c>Game_Actor</c> has 130 methods in the game's
    /// own engine file</strong>, -- <strong>and this reader answers
    /// three of them</strong>, -- <strong>plus <c>actor</c> itself,
    /// which is how a caller gets here.</strong>
    /// </para>
    /// </remarks>
    public IReadOnlyCollection<string> BekannteMethoden() =>
        new[] { "actor", "changeEquipById", "equips", "hasSkill" };

    /// <summary>
    /// And the ones this reader does not answer, and why.
    /// </summary>
    /// <returns>The names.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this is the point of the method.</strong> The game
    /// calls these 32 times and this repository does not answer them,
    /// -- <strong>and a list of what a reader does not answer is
    /// worth more than a percentage that hides
    /// it.</strong>
    /// </para>
    /// </remarks>
    public IReadOnlyCollection<string> NichtBeantworteteMethoden() =>
        new[]
        {
            "skillMasteryLevel", "setSkillMasteryLevel", "skillMasteryUses",
            "pha", "pdr", "mdf", "addState", "removeState", "setHp",
            "gainHp", "isStateAffected", "_isZAKO", "_deadCount",
            "_damageableCount", "_characterName", "_shouldShowQuestGuide",
        };
}

/// <summary>One actor, as far as this reader answers.</summary>
/// <remarks>
/// <para>
/// <strong>And <c>equips()</c> is not the array the engine
/// stores</strong>, -- <strong>the engine writes:</strong>
/// </para>
/// <code>
/// equips() { return this._equips.map(item => item.object()); }
/// </code>
/// <para>
/// <strong>And it maps to <c>item.object()</c></strong>, -- <strong>and
/// <c>null.object()</c> would throw</strong>, -- <strong>so
/// <c>Game_Item.object()</c> must answer <c>null</c> for an empty
/// slot</strong>, -- <strong>and the game reads
/// <c>$gameActors.actor(2).equips()[1]</c> 14 times and writes
/// <c>if (!$gameActors.actor(2).equips()[1])</c> 5 times</strong>,
/// -- <strong>so an empty slot must come back as nothing and not as
/// a placeholder.</strong>
/// </para>
/// </remarks>
public sealed class MzActor
{
    private readonly List<MzAusrüstung?> _ausruestung = new();
    private readonly HashSet<int> _skills = new();

    /// <summary>And its own number.</summary>
    public int Id { get; set; }

    /// <summary>And the name a script may read.</summary>
    public string Name { get; set; } = "";

    /// <summary>And <c>equips()</c>, and it maps to nothing.</summary>
    /// <returns>
    /// The item per slot, -- <strong>and an empty slot is
    /// null</strong>, -- <strong>because the engine maps
    /// <c>item.object()</c> and that is null for an empty
    /// slot.</strong>
    /// </returns>
    public IReadOnlyList<MzAusrüstung?> Equips() => _ausruestung;

    /// <summary>And gives a slot its item.</summary>
    /// <param name="pSlot">Which slot, counting from zero.</param>
    /// <param name="pAusrüstung">The item, or null to empty it.</param>
    public void SetzeSlot(int pSlot, MzAusrüstung? pAusrüstung)
    {
        while (_ausruestung.Count <= pSlot)
        {
            _ausruestung.Add(null);
        }

        _ausruestung[pSlot] = pAusrüstung;
    }

    /// <summary>And <c>hasSkill(skillId)</c>.</summary>
    /// <param name="pSkillId">The skill's number.</param>
    /// <returns>Whether it knows it.</returns>
    public bool HatSkill(int pSkillId) => _skills.Contains(pSkillId);

    /// <summary>And gives it a skill.</summary>
    /// <param name="pSkillId">The skill's number.</param>
    public void LerneSkill(int pSkillId) => _skills.Add(pSkillId);

    /// <summary>And how many skills it knows.</summary>
    /// <returns>The count, -- <strong>and for a test.</strong></returns>
    public int SkillAnzahl => _skills.Count;
}

/// <summary>One weapon or armour.</summary>
public sealed class MzAusrüstung
{
    /// <summary>And its number in the data file.</summary>
    public int Id { get; init; }

    /// <summary>And the name a script may read.</summary>
    public string Name { get; init; } = "";

    /// <summary>And <c>iconIndex</c>, -- <strong>and the game reads it
    /// three times from <c>$gameParty.lastItem()</c>.</strong></summary>
    public int IconIndex { get; init; }
}
