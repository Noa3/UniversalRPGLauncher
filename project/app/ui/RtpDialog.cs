using System;
using Godot;
using UniversalRPG.App.Library;
using UniversalRPG.Rtp;

namespace UniversalRPG.App.Ui;

/// <summary>
/// The one question this launcher asks before it fetches something.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And it asks only when a runtime is missing.</strong> A
/// game whose own <c>Game.ini</c> carries <c>RTP=</c> with an empty
/// value, or none at all, is never interrupted -- <strong>and
/// <see cref="RtpPruefer"/> is what decides that, not this
/// class.</strong>
/// </para>
/// <para>
/// <strong>And there are three answers and all three are
/// offered.</strong> "Download and unpack" is the one the user asked
/// for, "Start anyway" is there because a game may be RTP
/// independent, and "Cancel" is there because a question with no way
/// out is not a question.
/// </para>
/// <para>
/// <strong>And the size comes from the measured table and not from
/// the file on disk</strong>, -- <strong>because the file may not
/// exist yet and that is the whole point of the
/// question.</strong>
/// </para>
/// </remarks>
public static class RtpDialog
{
	/// <summary>
	/// And asks, and waits for the answer.
	/// </summary>
	/// <param name="pParent">Where the dialog goes.</param>
	/// <param name="pSpiel">The game that needs it.</param>
	/// <param name="pBedarf">What the check found.</param>
	/// <returns>What the user chose.</returns>
	/// <remarks>
	/// <para>
	/// <strong>And this returns a bool and not a result object</strong>,
	/// -- <strong>because the caller has exactly one decision to make
	/// and it is whether to start.</strong>
	/// </para>
	/// </remarks>
	public enum Antwort
	{
		/// <summary>Und der Benutzer will die Laufzeit.</summary>
		Laden,

		/// <summary>Und der Benutzer startet ohne.</summary>
		Trotzdem,

		/// <summary>Und der Benutzer bricht ab.</summary>
		Abbrechen,
	}

	public static async System.Threading.Tasks.Task<Antwort> Fragen(
		Node pParent, GameLibrary.GameEntry pSpiel, RtpPruefer.Ergebnis pBedarf)
	{
		var gewaehlt = Antwort.Abbrechen;
		var dialog = new ConfirmationDialog();
		dialog.Title = Tr("RTP_DIALOG_TITLE");
		dialog.DialogText = Text(pSpiel, pBedarf);
		dialog.OkButtonText = Tr("RTP_DIALOG_DOWNLOAD");
		dialog.CancelButtonText = Tr("RTP_DIALOG_CANCEL");

		// **Und "trotzdem starten"  ist  ein  eigener  Knopf,  und
		// nicht  der  Abbruch.** --
		// **Und ein Spiel kann RTP-unabhaengig  sein,  selbst  wenn
		// seine  `Game.ini`  ein  RTP  nennt.**
		var ohne = new Button();
		ohne.Text = Tr("RTP_DIALOG_START_WITHOUT");
		ohne.Pressed += () =>
		{
			dialog.Hide();
			gewaehlt = Antwort.Trotzdem;
		};
		dialog.AddChild(ohne);

		dialog.Confirmed += () => gewaehlt = Antwort.Laden;
		dialog.Canceled += () => gewaehlt = Antwort.Abbrechen;

		pParent.AddChild(dialog);
		dialog.PopupCentered(new Vector2I(560, 260));

		// **Und  gewartet  wird  wirklich,  bis  der  Benutzer
		// entschieden  hat.** --
		// **Und ein Dialog,  der  sofort  zurueckgibt,  fragt  gar
		// nicht.**
		// **Und `ProcessFrame`  ist  ein  Ereignis  und  keine
		// Methode**,  --
		// **und die  Schleife  wartet  wirklich,  bis  der
		// Benutzer  entschieden  hat.** --
		// **Und  ein  Dialog,  der  sofort  zurueckgibt,  fragt
		// gar nicht.**
		while (dialog.Visible)
		{
			await pParent.ToSignal(pParent.GetTree(), "process_frame");
		}

		pParent.RemoveChild(dialog);
		dialog.QueueFree();
		return gewaehlt;
	}

	private static string Text(
		GameLibrary.GameEntry pSpiel, RtpPruefer.Ergebnis pBedarf)
	{
		var groesse = RtpArchivFakten.Alle().TryGetValue(
			pBedarf.EngineId, out var fakt)
			? $"{fakt.Bytes / 1024.0 / 1024.0:0.0} MB"
			: "";

		return Tr("RTP_DIALOG_BODY")
			.Replace("{game}", pSpiel.Title)
			.Replace("{engine}", pSpiel.Detection.GetEngineName())
			.Replace("{rtp}", pBedarf.Nennung)
			.Replace("{size}", groesse);
	}

	private static string Tr(string pKey) =>
		TranslationServer.Translate(pKey);
}
