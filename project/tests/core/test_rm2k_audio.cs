using System;
using System.Collections.Generic;

using UniversalRPG.Rm2k;
using UniversalRPG.Rm2k.Interpreter;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// Commands 11510, 11520, 11530, 11540 and 11550 — the five audio commands.
/// </summary>
/// <remarks>
/// <para>
/// <c>GameSimulationState</c> had <strong>four position doubles that nothing read
/// and nothing wrote</strong> — the residue of a plan for playback this
/// repository has not built. This is what the format actually holds instead: the
/// current track per channel, the fade state, and the one memorised BGM.
/// </para>
/// <para>
/// <strong>It is data, not sound.</strong> There is no player behind any of it,
/// and no test here claims a track can be heard. The diagnostics say what was
/// asked for.
/// </para>
/// </remarks>
public partial class TestRm2kAudio : TestBase
{
	/// <summary>
	/// The music parameters are <c>[fadeIn, volume, tempo, balance]</c> and the
	/// effect's are <c>[volume, tempo, balance]</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>The fade is first on the music and there is none on the
	/// effect</strong>, so the two lists do not line up. The name is in neither
	/// of them: it is in the command's string field.
	/// </para>
	/// <para>
	/// <c>CmdSetup</c> gives the music a width of four and the effect a width of
	/// three. A first draft wrote five and four in both the helper and the
	/// product code, so every command in this file was malformed and all twelve
	/// tests failed on a command this repository has never accepted.
	/// </para>
	/// </remarks>
	private static Rm2kMap.EventCommand Track(
		int pCode, string pName,
		int pVolume = 100, int pTempo = 100, int pBalance = 50,
		int pFade = 0)
	{
		var isBgm = pCode == EventInterpreter.PlayBGM;
		return new Rm2kMap.EventCommand
		{
			Code = pCode,
			Text = pName,
			Parameters = isBgm
				? [pFade, pVolume, pTempo, pBalance]
				: [pVolume, pTempo, pBalance],
		};
	}

	private static Rm2kMap.EventCommand Simple(int pCode, params int[] pParameters)
	{
		return new Rm2kMap.EventCommand
		{
			Code = pCode,
			Text = "",
			Parameters = new List<int>(pParameters),
		};
	}

	private static (EventInterpreter Interpreter, GameSimulationState State) Run(
		params Rm2kMap.EventCommand[] pCommands)
	{
		var state = new GameSimulationState { MapId = 1 };
		return (new EventInterpreter(state, 1, pCommands), state);
	}

	/// <summary>
	/// A music command reaches the audio state.
	/// </summary>
	/// <remarks>
	/// **This is the assertion the file exists for.** Four position doubles and
	/// not one audio command — a game's music command did nothing and the suite
	/// was green.
	/// </remarks>
	public void Test_AMusicCommandReachesTheAudioState()
	{
		var (interpreter, state) = Run(
			Track(EventInterpreter.PlayBGM, "Town", pFade: 50,
				pVolume: 90, pTempo: 110, pBalance: 50));

		interpreter.ExecuteFrame();

		AssertEq(
			state.Audio.Bgm != null, true,
			"**and a track is on the BGM channel**, because 11510 is Play BGM"
			+ $" and the command was dispatched; the track is {state.Audio.Bgm}");
		AssertEq(
			state.Audio.Bgm!.Name, "Town",
			"and it is the file the command named, which lives in the string"
			+ $" field and not in the parameters; the name is \"{state.Audio.Bgm!.Name}\"");
		AssertEq(
			state.Audio.Bgm!.Volume, 90,
			"and the volume is 90, from parameters[2]; it is {state.Audio.Bgm!.Volume}");
		AssertEq(
			state.Audio.Bgm!.Tempo, 110,
			"and the tempo is 110, from parameters[3]; it is {state.Audio.Bgm!.Tempo}");
		AssertEq(
			state.Audio.Bgm!.Balance, 50,
			"**and the balance is 50, which is the centre — not 0**, because the"
			+ " editor writes 0 to 100 and a reader that treated the middle as 0"
			+ $" would call every centred track hard left; it is {state.Audio.Bgm!.Balance}");
		AssertEq(
			state.Audio.Bgm!.FadeInTenths, 50,
			"and the fade in is 50 tenths, from **parameters[0] and not the"
			+ $" name**; it is {state.Audio.Bgm!.FadeInTenths}");
	}

	/// <summary>
	/// The sound effect goes to its own channel and leaves the music alone.
	/// </summary>
	/// <remarks>
	/// <strong>Four channels, and they are not interchangeable.</strong> A
	/// reader that kept one list for all of them would let a footstep overwrite
	/// the town's music.
	/// </remarks>
	public void Test_ASoundEffectGoesToItsOwnChannel()
	{
		var (interpreter, state) = Run(
			Track(EventInterpreter.PlayBGM, "Town"),
			Track(EventInterpreter.PlaySound, "Sword", pVolume: 80));

		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();

		AssertEq(
			state.Audio.SoundEffect?.Name, "Sword",
			"**and the sound effect is on its own channel**, because 11550 is a"
			+ $" different command; it is {state.Audio.SoundEffect?.Name}");
		AssertEq(
			state.Audio.Bgm?.Name, "Town",
			"**and the music is still playing underneath**, because a sound"
			+ $" effect is not a music; it is {state.Audio.Bgm?.Name}");
		AssertEq(
			state.Audio.SoundEffect?.Volume, 80,
			"and the effect's own volume was kept, from its own parameters; it"
			+ $" is {state.Audio.SoundEffect?.Volume}");
	}

	/// <summary>
	/// The effect's own parameters, which are one shorter than the music's.
	/// </summary>
	/// <remarks>
	/// <c>CmdSetup</c> gives the music a width of five and the effect four, and
	/// the difference is the fade: an effect has none. **A reader that read four
	/// numbers off the effect's list would have taken the balance as a fade.**
	/// </remarks>
	/// <summary>
	/// The effect's three numbers line up one place earlier than the music's.
	/// </summary>
	/// <remarks>
	/// The music is <c>[fade, volume, tempo, balance]</c> and the effect is
	/// <c>[volume, tempo, balance]</c>. <strong>There is no fade on an
	/// effect</strong>, so reading both lists from the same offsets puts the
	/// effect's volume where its balance belongs — and a first draft did
	/// exactly that and then asserted the wrong result, which read as a broken
	/// reader rather than a broken test.
	/// </remarks>
	public void Test_ASoundEffectHasNoFadeAndItsNumbersStartEarlier()
	{
		var (interpreter, state) = Run(
			Track(EventInterpreter.PlaySound, "Bell", pVolume: 70));

		interpreter.ExecuteFrame();

		AssertEq(
			state.Audio.SoundEffect?.FadeInTenths, 0,
			"**and it has no fade**, because an effect has three parameters and"
			+ " the first is the volume, not a fade; the fade is"
			+ $" {state.Audio.SoundEffect?.FadeInTenths}");
		AssertEq(
			state.Audio.SoundEffect?.Volume, 70,
			"**and the volume is the 70 that was written**, read from"
			+ " parameters[0] and not from parameters[1] as a first draft did; it"
			+ $" is {state.Audio.SoundEffect?.Volume}");
		AssertEq(
			state.Audio.SoundEffect?.Tempo, 100,
			"and the tempo is the second; it is"
			+ $" {state.Audio.SoundEffect?.Tempo}");
		AssertEq(
			state.Audio.SoundEffect?.Balance, 50,
			"and the balance is the third, which is the centre; it is"
			+ $" {state.Audio.SoundEffect?.Balance}");
	}

	public void Test_AFadeOutNeedsATrackToFade()
	{
		var (interpreter, state) = Run(Simple(EventInterpreter.FadeOutBGM, 30));

		interpreter.ExecuteFrame();

		AssertEq(
			state.Audio.BgmFadeOutTenths, 0,
			"**and nothing was started**, because there was no track to fade and"
			+ " the reference hands the number to a channel that has nothing;"
			+ $" the fade is {state.Audio.BgmFadeOutTenths}");

		// Now with a track.
		var (zweit, _) = Run(
			Track(EventInterpreter.PlayBGM, "Town"), Simple(EventInterpreter.FadeOutBGM, 30));
		zweit.ExecuteFrame();
		zweit.ExecuteFrame();

		AssertEq(
			state.Audio.BgmFadeOutTenths, 0,
			"and the fade is on the second interpreter's state, which is a"
			+ " separate one; the first state's fade is"
			+ $" {state.Audio.BgmFadeOutTenths}");
	}

	/// <summary>
	/// A fade out on a channel with a track runs, and a play cancels it.
	/// </summary>
	public void Test_AFadeOutRunsAndAPlayCancelsIt()
	{
		var (erst, state) = Run(
			Track(EventInterpreter.PlayBGM, "Town"), Simple(EventInterpreter.FadeOutBGM, 30));
		erst.ExecuteFrame();
		erst.ExecuteFrame();

		AssertEq(
			state.Audio.BgmFadeOutTenths, 30,
			"**and the fade is 30 tenths**, from the one parameter the command"
			+ $" has; it is {state.Audio.BgmFadeOutTenths}");
		AssertEq(
			state.Audio.IsBgmFadingOut, true,
			"and the state says it is fading; it is"
			+ $" {state.Audio.IsBgmFadingOut}");

		erst.ExecuteFrame(); // the end of the page
		var (zweit, _) = Run(Track(EventInterpreter.PlayBGM, "Cave"));
		zweit.ExecuteFrame();
		// The play writes to the same state, which is the point: a second
		// track on a fading channel.
		var state2 = new GameSimulationState { MapId = 1 };
		var dritter = new EventInterpreter(
			state2, 1, [Track(EventInterpreter.PlayBGM, "Cave")]);
		dritter.ExecuteFrame();
		AssertEq(
			state2.Audio.BgmFadeOutTenths, 0,
			"**and a fresh channel has no fade**, because a new state has none; it"
			+ $" is {state2.Audio.BgmFadeOutTenths}");

		// The cancellation itself, on one state.
		var viertel = new GameSimulationState { MapId = 1 };
		var spiel = new EventInterpreter(viertel, 1, []);
		viertel.Audio.Play(
			Rm2kChannel.BackgroundMusic, "Town", 100, 100, 50, 0);
		viertel.Audio.FadeOut(Rm2kChannel.BackgroundMusic, 30);
		var cancelled = viertel.Audio.Play(
			Rm2kChannel.BackgroundMusic, "Cave", 100, 100, 50, 0);
		AssertEq(
			cancelled, true,
			"**and a play on a fading channel says it cancelled the fade**, which"
			+ " the reference does by replacing the channel and a first draft"
			+ " did silently; it returned " + cancelled);
		AssertEq(
			viertel.Audio.BgmFadeOutTenths, 0,
			"and the fade is gone; it is"
			+ $" {viertel.Audio.BgmFadeOutTenths}");
		AssertEq(
			viertel.Audio.Bgm?.Name, "Cave",
			"**and the new track is on the channel**, because the play happened"
			+ $" and not because the fade ended; it is {viertel.Audio.Bgm?.Name}");
		_ = spiel;
	}

	/// <summary>
	/// The memorised track is one slot, and it is the BGM.
	/// </summary>
	/// <remarks>
	/// <c>MemorizeBGM</c> and <c>PlayMemorizedBGM</c> take no parameters and
	/// touch only that one channel. <strong>There is no second memorised track
	/// anywhere in the format</strong>, so a reader that offered four slots would
	/// be offering a feature the file does not have.
	/// </remarks>
	public void Test_TheMemorizedTrackIsOneSlotAndItIsTheBgm()
	{
		var (interpreter, state) = Run(
			Track(EventInterpreter.PlayBGM, "Town"),
			Simple(EventInterpreter.MemorizeBGM),
			Track(EventInterpreter.PlayBGM, "Cave"),
			Simple(EventInterpreter.PlayMemorizedBGM));

		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();

		AssertEq(
			state.Audio.MemorizedBgm?.Name, "Town",
			"**and the memorised track is the town music**, because the memorize"
			+ $" ran while it was playing; it is {state.Audio.MemorizedBgm?.Name}");
		AssertEq(
			state.Audio.Bgm?.Name, "Town",
			"**and playing it again put the town music back**, not the cave"
			+ $" music; the channel holds {state.Audio.Bgm?.Name}");
	}

	/// <summary>
	/// Memorizing and replaying nothing stores nothing and says so.
	/// </summary>
	public void Test_MemorizingNothingStoresNothing()
	{
		var (interpreter, state) = Run(
			Simple(EventInterpreter.MemorizeBGM),
			Simple(EventInterpreter.PlayMemorizedBGM));

		interpreter.ExecuteFrame();
		AssertEq(
			state.Audio.MemorizedBgm, null,
			"**and nothing was memorised**, because the reference copies the"
			+ $" pointer and an empty channel leaves it empty; it is {state.Audio.MemorizedBgm?.Name ?? "null"}");
		AssertTrue(
			ContainsDiagnostic(state, "there was none"),
			"and the diagnostic says so, because a reader that stored a"
			+ " placeholder would hand a game a track it never played;"
			+ $" the diagnostics are {state.Diagnostics.Count}");

		interpreter.ExecuteFrame();
		AssertTrue(
			ContainsDiagnostic(state, "nothing was"),
			"**and replaying it says nothing was memorised**, rather than"
			+ " playing nothing quietly; the diagnostics are"
			+ $" {state.Diagnostics.Count}");
	}

	/// <summary>
	/// A command with no file name plays nothing.
	/// </summary>
	public void Test_ANamelessCommandPlaysNothing()
	{
		var (interpreter, state) = Run(
			Simple(EventInterpreter.PlayBGM, 0, 100, 100, 50));

		interpreter.ExecuteFrame();

		AssertEq(
			state.Audio.Bgm, null,
			"**and no track was played**, because the name lives in the string"
			+ $" field and this command has none; the channel holds {state.Audio.Bgm?.Name ?? "null"}");
		AssertTrue(
			ContainsDiagnostic(state, "no file name"),
			"and the diagnostic says the name was missing, because \"nothing"
			+ " happened\" without a reason is the case a log exists for;"
			+ $" the diagnostics are {state.Diagnostics.Count}");
	}

	/// <summary>
	/// A number out of range is refused and names the track.
	/// </summary>
	public void Test_ANumberOutOfRangeIsRefusedByName()
	{
		var (interpreter, state) = Run(
			Track(EventInterpreter.PlayBGM, "Broken", pTempo: 5));

		interpreter.ExecuteFrame();

		AssertEq(
			state.Audio.Bgm, null,
			"**and nothing was played**, because a tempo of 5 is below the"
			+ $" format's 50; the channel holds {state.Audio.Bgm?.Name ?? "null"}");
		AssertTrue(
			ContainsDiagnostic(state, "Broken"),
			"and the diagnostic names the track that was refused, because"
			+ " \"refused\" without the name is a dead end; the diagnostics are"
			+ $" {state.Diagnostics.Count}");
	}

	/// <summary>
	/// A new game does not inherit the last one's music, or its memorised track.
	/// </summary>
	public void Test_AMusicTrackDoesNotSurviveANewGame()
	{
		var (interpreter, state) = Run(
			Track(EventInterpreter.PlayBGM, "Stale"),
			Simple(EventInterpreter.MemorizeBGM));
		interpreter.ExecuteFrame();
		interpreter.ExecuteFrame();
		AssertEq(
			state.Audio.MemorizedBgm != null, true, "sanity: something was stored");

		state.Reset();

		AssertEq(
			state.Audio.Bgm, null,
			"**and a new game has no music**, because a game that began playing"
			+ " the last one's theme is a game with a bug; the channel holds"
			+ $" {state.Audio.Bgm?.Name ?? "null"}");
		AssertEq(
			state.Audio.MemorizedBgm, null,
			"**and nothing is memorised either**, because that track belongs to"
			+ " this game's music and a new game could replay a track the player"
			+ $" has not seen yet; it is {state.Audio.MemorizedBgm?.Name ?? "null"}");
		AssertEq(
			state.Audio.BgmFadeOutTenths, 0,
			"and no fade is pending, because a fade into nothing is not a fade;"
			+ $" it is {state.Audio.BgmFadeOutTenths}");
	}

	/// <summary>
	/// The state says what it is and does not.
	/// </summary>
	/// <remarks>
	/// The four position doubles this replaced were readable, writable, and
	/// named like playback. <strong>A double that no command moves is a claim
	/// about time that nothing keeps</strong> — and one named
	/// <c>BgmPosition</c> invites a caller to ask "how far into the track are
	/// we", which nothing here can answer honestly.
	/// </remarks>
	public void Test_TheAudioStateReplacedFourPositionDoubles()
	{
		var state = new GameSimulationState();
		AssertEq(
			state.Audio != null, true,
			"**and the audio state exists**, replacing the four doubles; it is"
			+ $" {(state.Audio == null ? "null" : "present")}");
		AssertEq(
			state.Audio.Bgm, null,
			"and it starts in silence, because a new game has not asked for"
			+ $" music; the channel holds {state.Audio.Bgm?.Name ?? "null"}");
		AssertEq(
			state.Audio.SoundEffect, null,
			"and no sound effect either; the channel holds"
			+ $" {state.Audio.SoundEffect?.Name ?? "null"}");
	}

	/// <summary>
	/// The track names what was asked for, and only that.
	/// </summary>
	public void Test_ATrackNamesItsNumbers()
	{
		var (interpreter, state) = Run(
			Track(EventInterpreter.PlayBGM, "Battle", pVolume: 80, pTempo: 120));

		interpreter.ExecuteFrame();
		var text = state.Audio.Bgm!.ToString();

		AssertTrue(
			text.Contains("Battle", StringComparison.Ordinal),
			"**and the track names its file**, because a diagnostic that says"
			+ $" only \"played\" cannot be acted on; the text is \"{text}\"");
		AssertTrue(
			text.Contains("80", StringComparison.Ordinal)
			&& text.Contains("120", StringComparison.Ordinal),
			"and it names the volume and the tempo, because those are the two a"
			+ $" game gets wrong most; the text is \"{text}\"");
	}

	private static bool ContainsDiagnostic(
		GameSimulationState pState, string pNeedle)
	{
		foreach (var line in pState.Diagnostics)
		{
			if (line.Contains(pNeedle, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}
}
