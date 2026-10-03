using System;
using System.IO;
using UniversalRPG.Rtp;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// That the dialog's words exist in every language, and that they
/// carry the placeholders the body needs.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is a translation test and not a dialog
/// test.</strong> A dialog cannot be shown in a headless run, -- and a
/// message that only exists in German is a message five of the
/// interface's seven languages never see.
/// </para>
/// <para>
/// <strong>And the placeholders are measured</strong>:
/// <c>{game}</c>, <c>{engine}</c>, <c>{rtp}</c> and <c>{size}</c> are
/// what <c>RtpDialog.Text</c> replaces, -- <strong>and a translation
/// that drops one leaves a brace in the user's face.</strong>
/// </para>
/// </remarks>
public partial class TestRtpDialogTexte : TestBase
{
    private static readonly string[] Sprachen =
    {
        "en", "de", "es", "fr", "ja", "ko", "zh_CN",
    };

    private static readonly string[] Schluessel =
    {
        "RTP_DIALOG_TITLE", "RTP_DIALOG_BODY", "RTP_DIALOG_DOWNLOAD",
        "RTP_DIALOG_START_WITHOUT", "RTP_DIALOG_CANCEL",
    };

    private static string Po(string pSprache) => File.ReadAllText(
        "E:/URPG/project/locale/" + pSprache + ".po");

    /// <summary>
    /// And every language carries every key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is measured as a missing key and not as a
    /// failed lookup</strong>, -- <strong>because a lookup that falls
    /// back to English silently and a missing entry loudly are two
    /// different failures.</strong>
    /// </para>
    /// </remarks>
    public void Test_JedeSpracheHatJedenSchluessel()
    {
        var fehlend = 0;
        foreach (var sprache in Sprachen)
        {
            var t = Po(sprache);
            foreach (var k in Schluessel)
            {
                var wert = WertZu(t, k);
                if (wert.Length == 0)
                {
                    fehlend++;
                    Console.WriteLine($"{sprache}: {k} fehlt oder leer");
                }
            }
        }

        AssertEq(0, fehlend,
            "**and every one of the seven languages carries all five"
                + " keys with a text** -- and the interface already"
                + " speaks seven languages, and a dialog that exists"
                + " only in one of them is a dialog six users never"
                + " see");
    }

    /// <summary>
    /// And the text of one key, or empty when there is none.
    /// </summary>
    /// <param name="pPo">The whole catalog.</param>
    /// <param name="pKey">The key.</param>
    /// <returns>The text, and never null.</returns>
    /// <remarks>
    /// <para>
    /// <strong>And this reads the entry that follows the key</strong>,
    /// -- <strong>and not the first <c>msgstr ""</c> in the
    /// file</strong>, -- <strong>and my first version did the latter
    /// and found the header every time</strong>, --
    /// <strong>and reported five missing keys in a language that has
    /// all five.</strong>
    /// </para>
    /// </remarks>
    private static string WertZu(string pPo, string pKey)
    {
        var i = pPo.IndexOf("msgid \"" + pKey + "\"", StringComparison.Ordinal);
        if (i < 0)
        {
            return "";
        }

        var anfang = pPo.IndexOf("msgstr \"", i, StringComparison.Ordinal);
        if (anfang < 0)
        {
            return "";
        }

        anfang += 8;
        var ende = pPo.IndexOf('"', anfang);
        return ende < 0 ? "" : pPo.Substring(anfang, ende - anfang);
    }
    /// <summary>
    /// And no language drops a placeholder.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the four placeholders are what
    /// <c>RtpDialog.Text</c> replaces</strong>, -- <strong>and a
    /// translation that renames one leaves a brace in the
    /// user's face.</strong>
    /// </para>
    /// </remarks>
    public void Test_DiePlatzhalterBleiben()
    {
        foreach (var sprache in Sprachen)
        {
            var t = Po(sprache);
            var i = t.IndexOf("msgid \"RTP_DIALOG_BODY\"");
            if (i < 0)
            {
                continue;
            }

            var anfang = t.IndexOf("msgstr \"", i) + 8;
            var zeile = t.Substring(anfang, t.IndexOf('"', anfang) - anfang);
            foreach (var platzhalter in new[] { "{game}", "{engine}", "{rtp}", "{size}" })
            {
                AssertTrue(zeile.Contains(platzhalter),
                    $"**and `{sprache}` keeps `{platzhalter}` in the body**"
                        + " -- and the dialog replaces exactly these four,"
                        + " and a translation that drops one leaves a"
                        + " brace in the user's face");
            }
        }
    }
}
