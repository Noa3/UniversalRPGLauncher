using System;
using System.Collections.Generic;
using System.Linq;
using UniversalRPG.Web;

namespace UniversalRPG.Mz;

/// <summary>
/// The battle turn order (Zugfolge), determined by agility.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the engine's own order is in
/// <c>BattleManager.makeActionOrders</c>.</strong> Measured at
/// <c>rmmz_objects.js</c>:
/// </para>
/// <code>
/// BattleManager.makeActionOrders = function() {
///     const battlers = [];
///     if (!this._surprise) {
///         battlers.push(...$gameParty.battleMembers());
///     }
///     battlers.push(...$gameTroop.members());
///     battlers.forEach(battler => {
///         battler.makeActions();
///     });
///     battlers.sort((a, b) => b.agi - a.agi);
///     this._actionBattlers = battlers;
/// };
/// </code>
/// <para>
/// <strong>And <c>agi</c> is the actor's agility parameter.</strong>
/// For actors it comes from the class parameters at their level,
/// modified by equipment. For enemies it is the enemy's <c>agi</c>
/// parameter from the database.
/// </para>
/// </remarks>
public sealed class MzZugfolge
{
    /// <summary>One entry in the turn order.</summary>
    public sealed class Eintrag
    {
        /// <summary>Zero for actor, one for enemy.</summary>
        public int Art { get; init; } // 0 = actor, 1 = enemy

        /// <summary>The actor or enemy index.</summary>
        public int Index { get; init; }

        /// <summary>The agility value used for sorting.</summary>
        public int Agility { get; init; }

        /// <summary>Whether this battler can act (alive, not stunned, etc.).</summary>
        public bool KannHandeln { get; set; } = true;
    }

    private readonly List<Eintrag> _reihenfolge = new();
    private int _aktuellerIndex = 0;

    /// <summary>Builds the turn order from party and troop.</summary>
    /// <param name="pActors">The actors in the party.</param>
    /// <param name="pPartyMembers">The actor IDs in the party (battle members).</param>
    /// <param name="pGegner">The enemies in the troop.</param>
    /// <param name="pEnemiesData">The Enemies.json data file.</param>
    /// <param name="pClassesData">The Classes.json data file.</param>
    public MzZugfolge(
        MzGameActors pActors,
        HashSet<int> pPartyMembers,
        IReadOnlyList<MzEnemy> pGegner,
        MzDataFile pEnemiesData,
        MzDataFile pClassesData)
    {
        // Actors first (if not surprise)
        foreach (var actorId in pPartyMembers.OrderBy(x => x))
        {
            var actor = pActors.Actor(actorId);
            if (actor != null)
            {
                var agi = GetActorAgility(actor, pClassesData);
                _reihenfolge.Add(new Eintrag
                {
                    Art = 0,
                    Index = actorId,
                    Agility = agi,
                });
            }
        }

        // Then enemies
        for (int i = 0; i < pGegner.Count; i++)
        {
            var gegner = pGegner[i];
            if (gegner.Sichtbar)
            {
                var agi = GetEnemyAgility(gegner, pEnemiesData);
                _reihenfolge.Add(new Eintrag
                {
                    Art = 1,
                    Index = i,
                    Agility = agi,
                });
            }
        }

        // Sort by agility descending (higher agi acts first)
        _reihenfolge.Sort((a, b) => b.Agility.CompareTo(a.Agility));
    }

    /// <summary>Gets the next battler who can act.</summary>
    /// <returns>The next entry, or null if round is over.</returns>
    public Eintrag? Naechster()
    {
        while (_aktuellerIndex < _reihenfolge.Count)
        {
            var eintrag = _reihenfolge[_aktuellerIndex];
            _aktuellerIndex++;
            if (eintrag.KannHandeln)
            {
                return eintrag;
            }
        }
        return null;
    }

    /// <summary>Resets for a new round.</summary>
    public void NeueRunde()
    {
        _aktuellerIndex = 0;
        foreach (var e in _reihenfolge)
        {
            e.KannHandeln = true;
        }
    }

    /// <summary>Marks a battler as unable to act this round.</summary>
    public void SetzeUnfaehig(int pArt, int pIndex)
    {
        var e = _reihenfolge.FirstOrDefault(x => x.Art == pArt && x.Index == pIndex);
        if (e != null)
        {
            e.KannHandeln = false;
        }
    }

    /// <summary>Gets the full order for debugging.</summary>
    public IReadOnlyList<Eintrag> Reihenfolge => _reihenfolge;

    private static int GetActorAgility(MzActor pActor, MzDataFile pClassesData)
    {
        // Base agility from class at this level
        // param 6 is agi (0=mhp, 1=mmp, 2=atk, 3=def, 4=mat, 5=mdf, 6=agi, 7=luk)
        var baseAgi = GetClassParam(pActor.ClassId, pActor.Level, 6, pClassesData);

        // Equipment bonuses would go here
        // For now, return base agility
        return baseAgi;
    }

    private static int GetEnemyAgility(MzEnemy pEnemy, MzDataFile pEnemiesData)
    {
        // Enemy agility from database - entries list skips index 0 (null)
        var entries = pEnemiesData.Entries;
        if (pEnemy.Klasse >= 1 && pEnemy.Klasse < entries.Count)
        {
            var enemyData = entries[pEnemy.Klasse];
            var agi = enemyData.Member("agi");
            if (agi != null && agi.Kind == MzKind.Number)
            {
                return agi.IntOr(10);
            }
        }
        return 10; // default
    }

    private static int GetClassParam(int pClassId, int pLevel, int pParamIndex, MzDataFile pClassesData)
    {
        // Classes.json entries list skips index 0 (null)
        var entries = pClassesData.Entries;
        if (pClassId >= 1 && pClassId < entries.Count)
        {
            var classData = entries[pClassId];
            // params is an array of 8 arrays (one per level, 1-based)
            // Each sub-array has 8 values: [mhp, mmp, atk, def, mat, mdf, agi, luk]
            var paramsObj = classData.Member("params");
            if (paramsObj != null && paramsObj.Kind == MzKind.Array && paramsObj.Items.Count > pLevel)
            {
                var levelParams = paramsObj.Items[pLevel];
                if (levelParams != null && levelParams.Kind == MzKind.Array && levelParams.Items.Count > pParamIndex)
                {
                    var param = levelParams.Items[pParamIndex];
                    if (param != null && param.Kind == MzKind.Number)
                    {
                        return param.IntOr(10);
                    }
                }
            }
        }

        return 10;
    }
}