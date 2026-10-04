using System;

namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// And what a skill costs, and whether a hero may pay it.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the reference is <c>Algo::CalcSkillCost</c>:</strong>
///
/// <code>
/// int CalcSkillCost(const lcf::rpg::Skill&amp; skill, int max_sp, bool half_sp_cost) {
///     const auto div = half_sp_cost ? 2 : 1;
///     return (Player::IsRPG2k3() &amp;&amp; skill.sp_type == SpType_percent)
///         ? max_sp * skill.sp_percent / 100 / div
///         : (skill.sp_cost + static_cast&lt;int&gt;(half_sp_cost)) / div;
/// }
/// </code>
///
/// <strong>And <c>sp_type</c> is field <c>0x09</c> and
/// <c>sp_percent</c> is <c>0x0A</c> and <c>sp_cost</c> is
/// <c>0x0B</c></strong>.
/// </para>
/// <para>
/// <strong>And a percentage is computed from the maximum and not from
/// what is left</strong>, -- <strong>because that is what the reference
/// passes</strong>, -- <strong>and a cost that shrinks as a hero
/// spends would be a rule this repository invented</strong>.
/// </para>
/// <para>
/// <strong>And the reference refuses when the cost is greater than the
/// points</strong>, -- <strong>not greater or equal</strong>, -- <strong>and
/// that off by one is the difference between a hero with exactly
/// enough and one without</strong>.
/// </para>
/// </remarks>
public static class Rm2kFertigkeitKosten
{
    /// <summary>And liblcf's fixed point cost.</summary>
    public const int SpTypFest = 0;

    /// <summary>And liblcf's percentage cost.</summary>
    public const int SpTypProzent = 1;

    /// <summary>
    /// And what the skill costs a hero with that maximum.
    /// </summary>
    /// <param name="pSkill">The skill entry from the bank.</param>
    /// <param name="pMaxSp">The hero's maximum skill points.</param>
    /// <returns>The cost in points, and zero when the bank has
    /// none of the three fields.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And a missing field is not a zero cost that is
    /// charged</strong>, -- <strong>it is an absent cost</strong>, --
    /// <strong>and this returns zero rather than reading a field that
    /// is not there</strong>.
    /// </para>
    /// </remarks>
    public static int Kosten(
        Godot.Collections.Dictionary pSkill, int pMaxSp)
    {
        if (pSkill == null
            || !pSkill.TryGetValue("sp_type", out var rohTyp))
        {
            return 0;
        }

        if (rohTyp.AsInt32() == SpTypProzent)
        {
            var prozent = pSkill.TryGetValue("sp_percent",
                out var rohProzent) ? rohProzent.AsInt32() : 0;
            return pMaxSp * prozent / 100;
        }

        return pSkill.TryGetValue("sp_cost", out var rohKosten)
            ? rohKosten.AsInt32() : 0;
    }

    /// <summary>
    /// And whether the hero has the points to pay it.
    /// </summary>
    /// <param name="pSkill">The skill entry from the bank.</param>
    /// <param name="pAktuelleSp">The hero's current skill points.</param>
    /// <param name="pMaxSp">The hero's maximum skill points.</param>
    /// <returns>Whether the skill may be used.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And the comparison is the reference's</strong>, -- <strong>
    /// <c>CalculateSkillCost(skill_id) &gt; GetSp()</c> refuses</strong>,
    /// -- <strong>so a hero with exactly the cost left may still
    /// cast</strong>.
    /// </para>
    /// </remarks>
    public static bool Bezahlbar(
        Godot.Collections.Dictionary pSkill,
        int pAktuelleSp,
        int pMaxSp)
    {
        return Kosten(pSkill, pMaxSp) <= pAktuelleSp;
    }
}
