using System;
using System.Collections.Generic;
using System.Text;

using UniversalRPG.Tests.Framework;
using UniversalRPG.Wolf;

namespace UniversalRPG.Tests.Core;

/// <summary>
/// WOLF's three sound channels, and what a sound step asks for.
/// </summary>
/// <remarks>
/// <para>
/// The material guide gives the shape: BGM is background music, BGS is
/// background sound — it calls it an ambient sound and names rain, wind and a
/// heartbeat — and SE is a sound effect, which does not loop.
/// </para>
/// <para>
/// <strong>100 is the standard volume and 0 is the standard volume too,</strong>
/// which is the rule a boolean cannot hold. The game settings page says a zero
/// was converted to 100 before version 3.681 and that the setting can keep it,
/// and the case it was added for is a background sound mixed at zero used for
/// interactive music.
/// </para>
/// </remarks>
public partial class TestWolfAudio : TestBase
{
	/// <summary>A step whose name is the given text and whose numbers are these.</summary>
	private static WolfMoveRouteStep Sound(
		int pChannel,
		string pName,
		int pVolume = 100,
		int pFrequency = 100,
		int pTime = 0)
	{
		var bytes = new List<byte>();
		foreach (var ch in pName)
		{
			bytes.Add((byte)ch);
		}
		return new WolfMoveRouteStep
		{
			Type = WolfMoveRouteType.SetSound,
			Arguments = [pChannel, pVolume, pFrequency, pTime],
			ByteArguments = bytes,
		};
	}

	/// <summary>A board with open ground, for the route tests.</summary>
	private static WolfCharacterBoard Board()
	{
		var board = new WolfCharacterBoard(new WolfVariableBands());
		board.LoadMap(1, new WolfPassabilityGrid(20, 20));
		return board;
	}

	// ---- Die Kanaele

	/// <summary>
	/// There are three channels and they are not interchangeable.
	/// </summary>
	/// <remarks>
	/// <strong>BGM, BGS and SE, and the third does not loop.</strong> The guide
	/// calls BGS an ambient sound and names rain, wind and a heartbeat as its
	/// uses. A reader with one list would have a heartbeat replace the town
	/// theme, because the two are the same kind of thing and neither is
	/// interchangeable with the other.
	/// </remarks>
	public void Test_ThereAreThreeChannels()	{
		var audio = new WolfAudioState();
		audio.Play(new WolfSoundRequest
		{
			Channel = WolfSoundChannel.Bgm,
			Name = "BGM/town.ogg",
		});
		audio.Play(new WolfSoundRequest
		{
			Channel = WolfSoundChannel.Bgs,
			Name = "BGS/rain.ogg",
		});
		audio.Play(new WolfSoundRequest
		{
			Channel = WolfSoundChannel.Se,
			Name = "SE/step.ogg",
		});

		AssertEq(
			audio.Current(WolfSoundChannel.Bgm)!.Name, "BGM/town.ogg",
			"**and the music is the town's**");
		AssertEq(
			audio.Current(WolfSoundChannel.Bgs)!.Name, "BGS/rain.ogg",
			"**and the ambient sound is the rain's**, which a reader with one"
			+ " list would have overwritten with the music");
		AssertEq(
			audio.Current(WolfSoundChannel.Se)!.Name, "SE/step.ogg",
			"**and the effect is its own**");
	}

	/// <summary>
	/// A silent channel is nothing and not a track with an empty name.
	/// </summary>
	/// <remarks>
	/// <strong>Null and not an empty track.</strong> A reader that returned a
	/// track with a name of "" would let a caller play something, and the
	/// symptom would be a sound named after nothing.
	/// </remarks>
	public void Test_ASilentChannelIsNothing()	{
		var audio = new WolfAudioState();

		AssertEq(
			audio.Current(WolfSoundChannel.Bgm), null,
			"**and a channel nobody played is null**, and not a track with an"
			+ $" empty name; it is {(audio.Current(WolfSoundChannel.Bgm) == null ? "null" : "a track")}");
		audio.Play(new WolfSoundRequest
		{
			Channel = WolfSoundChannel.Bgm,
			Name = "BGM/town.ogg",
		});
		audio.Stop(WolfSoundChannel.Bgm);
		AssertEq(
			audio.Current(WolfSoundChannel.Bgm), null,
			"**and stopping it makes it null again**, which is what a stop is");
	}

	// ---- Die Null-Lautstaerke

	/// <summary>
	/// A volume of zero is standard under the old rule and silent under the
	/// new one.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Two settings and two answers, and the reader asks.</strong> The
	/// material guide says a zero is played at the standard volume, and the
	/// game settings page says that before 3.681 a zero was silently converted
	/// to 100 and that the setting now keeps it — the case being a background
	/// sound mixed at zero for interactive music.
	/// </para>
	/// <para>
	/// A reader that picked one of the two would be wrong for every game made
	/// with the other, and a background sound that was meant to be inaudible
	 /// would play at full volume.
	/// </para>
	/// </remarks>
	public void Test_AZeroVolumeDependsOnTheSetting()	{
		var entry = new WolfSoundEntry { Volume = 0 };

		AssertEq(
			entry.EffectiveVolume(false), WolfSoundEntry.StandardVolume,
			"**and under the old rule a zero plays at the standard volume**,"
			+ $" which is 100; it is {entry.EffectiveVolume(false)}");
		AssertEq(
			entry.EffectiveVolume(true), 0,
			"**and under the new one it stays at zero**, which is the case the"
			+ $" setting was added for; it is {entry.EffectiveVolume(true)}");

		// **And the state says which one it is using.**
		var audio = new WolfAudioState();
		AssertEq(
			audio.KeepZeroVolume, false,
			"**and the state uses the old rule by default**, because that is"
			+ $" what a game without the setting was built against;"
			+ $" it is {audio.KeepZeroVolume}");
	}

	/// <summary>
	/// A hundred is the standard, and anything else is itself.
	/// </summary>
	/// <remarks>
	/// <strong>Not clamped.</strong> The guide says 1 to 100 is quieter, above
	/// 100 is louder, and warns that an unexpected loudness can come out of it
	/// — so a reader that clamped to 100 would refuse exactly the case the help
	/// says a game can do.
	/// </remarks>
	public void Test_TheStandardIsAHundredAndTheRestIsKept()	{
		AssertEq(
			WolfSoundEntry.StandardVolume, 100,
			"**and 100 is the standard volume**, per the guide's 100が標準;"
			+ $" it is {WolfSoundEntry.StandardVolume}");
		AssertEq(
			new WolfSoundEntry { Volume = 150 }.EffectiveVolume(true), 150,
			"**and 150 stays 150**, because the guide says a value above 100 is"
			+ $" louder and warns about it rather than forbidding it;"
			+ $" it is {new WolfSoundEntry { Volume = 150 }.EffectiveVolume(true)}");
		AssertEq(
			new WolfSoundEntry { Volume = 30 }.EffectiveVolume(true), 30,
			"**and 30 stays 30**;"
			+ $" it is {new WolfSoundEntry { Volume = 30 }.EffectiveVolume(true)}");
	}

	// ---- Die Verzoegerung

	/// <summary>
	/// Sixty frames are one second, and an effect's time is a delay.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>The material guide gives the conversion directly</strong> — delay
	/// a sound effect by one second and it says sixty frames.
	/// </para>
	/// <para>
	/// <strong>A delay and not a fade.</strong> The guide says the sound
	/// command's fade time becomes "delay the playback" for an effect, and a
	/// reader that treated it as a fade would have the effect start quietly and
	/// grow, which is not what the field means.
	/// </para>
	/// </remarks>
	public void Test_AnEffectDelaysAndMusicFades()	{
		AssertEq(
			WolfSoundRequest.OneSecondInFrames, 60,
			"**and sixty frames are one second**, the guide's own conversion;"
			+ $" it is {WolfSoundRequest.OneSecondInFrames}");

		var board = Board();
		board.Add(new WolfCharacter { Id = 1, X = 1, Y = 1 });
		board.StartRoute(1, new WolfMoveRoute
		{
			Steps =
			[
				Sound(WolfSoundChannel.Se, "SE/door.ogg", 100, 100, 60),
				Sound(WolfSoundChannel.Bgm, "BGM/town.ogg", 100, 100, 60),
			],
			Mode = WolfMoveRouteMode.Custom,
		});

		board.Tick();
		board.Tick();

		AssertEq(
			board.Audio.Current(WolfSoundChannel.Se)!.DelayFrames, 60,
			"**and the effect's sixty frames are a delay**, which is what a one"
			+ " second wait means;"
			+ $" it is {board.Audio.Current(WolfSoundChannel.Se)!.DelayFrames}");
		AssertEq(
			board.Audio.Current(WolfSoundChannel.Se)!.FadeFrames, 0,
			"**and it is not a fade**, because the guide says the fade time"
			+ " becomes a delay for an effect and the other way round would have"
			+ $" it start quietly; it is {board.Audio.Current(WolfSoundChannel.Se)!.FadeFrames}");
		AssertEq(
			board.Audio.Current(WolfSoundChannel.Bgm)!.FadeFrames, 60,
			"**and the music's sixty frames are a fade**, which is the guide's"
			+ $" description of the field; it is {board.Audio.Current(WolfSoundChannel.Bgm)!.FadeFrames}");
		AssertEq(
			board.Audio.Current(WolfSoundChannel.Bgm)!.DelayFrames, 0,
			"**and the music does not delay**, because the field is one of the"
			+ $" two and not both; it is {board.Audio.Current(WolfSoundChannel.Bgm)!.DelayFrames}");
	}

	// ---- Der Tonschritt

	/// <summary>
	/// The name is in the single byte arguments and the numbers in the four
	/// byte ones.
	/// </summary>
	/// <remarks>
	/// <strong>A file name is text, and text is in the byte list.</strong> A
	/// route step's shape is a type, a count of four byte values, those, a
	/// count of single byte values, and those — and a reader that looked for the
	/// name in the first list would find three integers and wonder why no track
	/// played.
	/// </remarks>
	public void Test_TheNameComesFromTheByteArguments()	{
		var step = Sound(WolfSoundChannel.Se, "SE/door.ogg", 80, 90, 30);

		AssertEq(
			WolfCharacterBoard.NameOf(step), "SE/door.ogg",
			"**and the name is the whole text**;"
			+ $" it is \"{WolfCharacterBoard.NameOf(step)}\"");
		AssertEq(
			WolfCharacterBoard.NameOf(new WolfMoveRouteStep
			{
				Type = WolfMoveRouteType.SetSound,
			}),
			"",
			"**and a step with no bytes has no name**, and not a name of one"
			+ $" character; it is \"{WolfCharacterBoard.NameOf(new WolfMoveRouteStep { Type = WolfMoveRouteType.SetSound })}\"");
	}

	/// <summary>
	/// A sound step runs and does not stop the route.
	/// </summary>
	/// <remarks>
	/// <strong>Instant, and the route carries on.</strong> A player that opened a
	/// door and whose route then ended would have the door's script stop at the
	/// door, and a reader that gave the step a frame budget would make every
	/// footstep wait for frames nobody spends.
	/// </remarks>
	public void Test_ASoundStepDoesNotStopTheRoute()	{
		var board = Board();
		board.Add(new WolfCharacter { Id = 1, X = 1, Y = 1 });
		board.StartRoute(1, new WolfMoveRoute
		{
			Steps =
			[
				Sound(WolfSoundChannel.Se, "SE/door.ogg"),
				Sound(WolfSoundChannel.Bgm, "BGM/town.ogg"),
			],
			Mode = WolfMoveRouteMode.Custom,
		});

		board.Tick();

		AssertEq(
			board.Audio.Current(WolfSoundChannel.Bgm)!.Name, "BGM/town.ogg",
			"**and the second sound ran in the same frame as the first**, because"
			+ " a sound takes no time and the route carries on; it is"
			+ $" \"{board.Audio.Current(WolfSoundChannel.Bgm)!.Name}\"");
	}

	/// <summary>
	/// A disabled channel records nothing.
	/// </summary>
	/// <remarks>
	/// <strong>The config window has separate switches</strong> — one for BGM and
	/// BGS, one for SE — and a game that turned the effects off should not have
	/// them accumulate in a state a save file would then carry.
	/// </remarks>
	public void Test_ADisabledChannelRecordsNothing()	{
		var board = Board();
		board.Add(new WolfCharacter { Id = 1, X = 1, Y = 1 });
		board.Audio.EffectEnabled = false;
		board.StartRoute(1, new WolfMoveRoute
		{
			Steps = [Sound(WolfSoundChannel.Se, "SE/step.ogg")],
			Mode = WolfMoveRouteMode.Custom,
		});

		board.Tick();

		AssertEq(
			board.Audio.Current(WolfSoundChannel.Se), null,
			"**and the effect channel stays silent**, because the config window"
			+ " has its own switch and a reader that ignored it would keep a list"
			+ $" a save would carry; it is {(board.Audio.Current(WolfSoundChannel.Se) == null ? "null" : "a track")}");
	}

	/// <summary>
	/// A sound with no file name is not played.
	/// </summary>
	/// <remarks>
	/// <strong>An empty name is not a sound.</strong> A reader that stored it
	/// would leave the channel playing something with no file, and the next
	/// command to ask what is playing would answer with a name of nothing.
	/// </remarks>
	public void Test_ASoundWithNoNameIsNotPlayed()	{
		var audio = new WolfAudioState();
		var played = audio.Play(new WolfSoundRequest
		{
			Channel = WolfSoundChannel.Bgm,
			Name = "",
		});

		AssertEq(
			played, false,
			"**and the request is refused**, because a channel with an empty name"
			+ $" is a channel playing nothing; it is {played}");
		AssertEq(
			audio.Current(WolfSoundChannel.Bgm), null,
			"**and the channel is still silent**");
	}

	/// <summary>
	/// A channel this reader does not have is the music one.
	/// </summary>
	/// <remarks>
	/// <strong>The silent answer and not the loud wrong one.</strong> A step from
	/// a newer editor could name a channel that does not exist here, and a
	/// reader that guessed the first channel would play something rather than
	/// nothing.
	/// </remarks>
	public void Test_AnUnknownChannelIsTheMusicOne()	{
		var board = Board();
		board.Add(new WolfCharacter { Id = 1, X = 1, Y = 1 });
		board.StartRoute(1, new WolfMoveRoute
		{
			Steps = [Sound(9, "SE/new.ogg")],
			Mode = WolfMoveRouteMode.Custom,
		});

		board.Tick();

		AssertEq(
			board.Audio.Current(WolfSoundChannel.Bgm)!.Name, "SE/new.ogg",
			"**and a channel number of 9 lands on the music channel**, because a"
			+ " reader that refused it would lose the sound and one that guessed"
			+ " the effects channel would put a music track where a click"
			+ $" belongs; it is \"{board.Audio.Current(WolfSoundChannel.Bgm)!.Name}\"");
	}

	/// <summary>
	/// A new game silences every channel.
	/// </summary>
	/// <remarks>
	/// <strong>The state is cleared with the rest of the board.</strong> A game
	/// that started with the last game's music would have the new game's title
	/// screen playing the old one's theme.
	/// </remarks>
	public void Test_ANewGameSilencesEveryChannel()	{
		var board = Board();
		board.Add(new WolfCharacter { Id = 1, X = 1, Y = 1 });
		board.StartRoute(1, new WolfMoveRoute
		{
			Steps = [Sound(WolfSoundChannel.Bgm, "BGM/old.ogg")],
			Mode = WolfMoveRouteMode.Custom,
		});
		board.Tick();

		board.Clear();

		AssertEq(
			board.Audio.Current(WolfSoundChannel.Bgm), null,
			"**and the music is gone**, because a new game that kept the last"
			+ " one's theme would open its title screen with it;"
			+ $" it is {(board.Audio.Current(WolfSoundChannel.Bgm) == null ? "null" : "a track")}");
	}
}
