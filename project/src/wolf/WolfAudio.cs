using System;
using System.Collections.Generic;

namespace UniversalRPG.Wolf;

/// <summary>
/// WOLF's three sound channels, and what a sound command asks for.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Data and not a player.</strong> The file, the numbers and the delay
/// are what a command asked for. Nothing here has an audio device behind it,
/// and a reader that reported a track as playing would be claiming a sound
/// nobody can hear. This is the same shape
/// <c>Rm2kAudioState</c> has, and for the same reason.
/// </para>
/// <para>
/// <strong>Three channels and not one.</strong> BGM is background music, BGS is
/// background sound — the material guide calls it an ambient sound, and names
/// rain, wind and a heartbeat as its uses — and SE is a sound effect, which
/// does not loop. A reader with one list would have a heartbeat replace the town
/// theme.
/// </para>
/// </remarks>
public static class WolfSoundChannel
{
	/// <summary>BGM — background music, which loops and fades.</summary>
	public const int Bgm = 0;

	/// <summary>BGS — background sound, a looping ambient sound.</summary>
	public const int Bgs = 1;

	/// <summary>SE — a sound effect, which plays once and does not loop.</summary>
	public const int Se = 2;

	/// <summary>The highest channel number.</summary>
	public const int MaxChannel = 2;
}

/// <summary>
/// What the sound database holds for one entry.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Volume 100 is standard and 0 is standard too.</strong> The material
/// guide says it twice for both BGM and SE: 100 is the normal volume, 1 to 100 is
/// quieter, above 100 is louder, and <em>an entry of 0 is also played at the
/// normal volume</em>. The game settings page adds that from version 3.681 the
/// two are separate — 0 can stay at 0 — and that the old behaviour converted 0 to
/// 100. <strong>Which of the two a game uses is a setting, and this reader does
/// not have one</strong>, so a zero is reported as zero and named as the
/// ambiguous value it is.
/// </para>
/// <para>
/// <strong>Frequency 100 is standard and 0 is standard too</strong>, in the same
/// way and for the same reason.
/// </para>
/// </remarks>
public sealed class WolfSoundEntry
{
	/// <summary>The volume at which the editor plays a track unaltered.</summary>
	public const int StandardVolume = 100;

	/// <summary>The frequency at which the editor plays a track unaltered.</summary>
	public const int StandardFrequency = 100;

	/// <summary>The file name, from the system database.</summary>
	public string Name { get; init; } = "";

	/// <summary>
	/// The volume in percent, where 100 is standard and 0 is ambiguous.
	/// </summary>
	/// <remarks>
	/// <strong>Zero is kept and not turned into 100.</strong> The material guide
	/// says a zero is played at the standard volume, and the game settings page
	/// says that before version 3.681 it was silently converted to 100 and that
	/// the setting now keeps it at zero. A reader that converted would make a
	/// background sound mixed at zero play at full volume — which is the case the
	/// setting was added for.
	/// </remarks>
	public int Volume { get; init; }

	/// <summary>
	/// The frequency in percent, where 100 is standard and 0 is ambiguous.
	/// </summary>
	/// <remarks>
	/// <strong>Below 100 is lower and slower, above is higher and faster</strong>,
	/// per the material guide. For a MIDI file only the tempo changes; for a
	/// sample both do. That difference is a property of the file, and this
	/// reader does not open files.
	/// </remarks>
	public int Frequency { get; init; }

	/// <summary>
	/// Where a looping channel starts, in milliseconds, or a MIDI key.
	/// </summary>
	/// <remarks>
	/// <strong>Two meanings and one field, and the file decides which.</strong> The
	/// material guide says it plainly: for a registered MIDI file this is the key,
	/// and for a sample it is the loop position in milliseconds. A reader that
	/// assumed one of them would seek a sample to a key number and start it in
	/// the wrong place.
	/// </remarks>
	public int LoopPoint { get; init; }

	/// <summary>
	/// Whether the editor frees the file after playing it once.
	/// </summary>
	/// <remarks>
	/// <strong>This is about the editor, and not about the game.</strong> The
	/// material guide says the field is <c>メモリから解放する</c> and describes it
	/// as how WOLF RPG Editor itself loads the sound, choosing 0 for a sound that
	/// plays repeatedly and 1 for one that plays once. <strong>A game does not
	/// carry the editor's memory strategy</strong>, so this is recorded and not
	/// acted on.
	/// </remarks>
	public int ReleasePolicy { get; init; }

	/// <summary>
	/// The volume this entry plays at, given the game's own setting.
	/// </summary>
	/// <param name="pKeepZero">
	/// Whether a volume of zero stays at zero, which is the setting from version
	/// 3.681 onwards.
	/// </param>
	/// <returns>
	/// The volume to play at, with zero resolved when the setting says so.
	/// </returns>
	/// <remarks>
	/// <strong>Two settings and two answers, and the reader asks which.</strong>
	/// The old behaviour converts zero to 100 and the new one keeps it; a
	/// reader that picked one would be wrong for every game made with the other.
	/// </remarks>
	public int EffectiveVolume(bool pKeepZero)
	{
		return Volume == 0 && !pKeepZero ? StandardVolume : Volume;
	}

	/// <summary>
	/// The frequency this entry plays at, given the game's own setting.
	/// </summary>
	/// <param name="pKeepZero">Whether a frequency of zero stays at zero.</param>
	/// <returns>The frequency to play at.</returns>
	public int EffectiveFrequency(bool pKeepZero)
	{
		return Frequency == 0 && !pKeepZero ? StandardFrequency : Frequency;
	}
}

/// <summary>
/// What one sound command asked for.
/// </summary>
/// <remarks>
/// <para>
/// <strong>SE takes a delay and BGM takes a fade, and they are not the same
/// field.</strong> The material guide says the sound command's fade time
/// becomes <em>delay the playback</em> for a sound effect, and gives the unit:
/// sixty frames is one second. A reader that treated a SE delay as a fade would
/// have the effect start quietly and grow, which is not what the field means.
/// </para>
/// <para>
/// <strong>The fade is in frames and grows the volume; there is no fade out on
/// this command.</strong> The guide describes the fade as the BGM volume rising
/// gradually while it starts, and gives a separate command for fading out.
/// </para>
/// </remarks>
public sealed class WolfSoundRequest
{
	/// <summary>How many frames make a second, from the material guide.</summary>
	public const int FramesPerSecond = 60;

	/// <summary>One second of delay, in frames.</summary>
	public const int OneSecondInFrames = FramesPerSecond;

	/// <summary>Which of the three channels this asks for.</summary>
	public int Channel { get; init; }

	/// <summary>The file name, or the database entry's name.</summary>
	public string Name { get; init; } = "";

	/// <summary>The volume in percent, where 100 is standard.</summary>
	public int Volume { get; init; } = WolfSoundEntry.StandardVolume;

	/// <summary>The frequency in percent, where 100 is standard.</summary>
	public int Frequency { get; init; } = WolfSoundEntry.StandardFrequency;

	/// <summary>
	/// The delay in frames, for a sound effect.
	/// </summary>
	/// <remarks>
	/// <strong>Frames and not milliseconds, and sixty make a second.</strong>
	/// The material guide gives the conversion directly, and a reader that read
	/// the number as milliseconds would delay a one second effect by 60
	/// milliseconds — a sixteenth of what the game asked for.
	/// </remarks>
	public int DelayFrames { get; init; }

	/// <summary>
	/// The fade in frames, for a music or background channel.
	/// </summary>
	/// <remarks>
	/// <strong>The same sixty frames a second, and it only grows.</strong> The
	/// guide says the fade makes the volume rise gradually while the track
	/// starts, and that fading out is a separate command.
	/// </remarks>
	public int FadeFrames { get; init; }

	/// <summary>
	/// Where a looping channel starts, in milliseconds or as a MIDI key.
	/// </summary>
	public int LoopPoint { get; init; }

	/// <summary>What this reader can honestly say about the request.</summary>
	public override string ToString()
	{
		var kanal = Channel == WolfSoundChannel.Se
			? "SE"
			: Channel == WolfSoundChannel.Bgs ? "BGS" : "BGM";
		var zeit = Channel == WolfSoundChannel.Se
			? $" delay {DelayFrames}"
			: FadeFrames > 0 ? $" fade {FadeFrames}" : string.Empty;
		return $"{kanal} {Name} vol {Volume} freq {Frequency}{zeit}";
	}
}

/// <summary>
/// The sounds the commands ask for, and what has been asked for since.
/// </summary>
/// <remarks>
/// <para>
/// <strong>WOLF had no audio state at all.</strong> The route runner refused the
/// sound step, which was the honest answer while there was nowhere to put one,
/// and the game settings page has a switch for BGM and BGS playback and another
/// for SE — so a game can turn a channel off entirely, and this is where that
/// lives.
/// </para>
/// <para>
/// <strong>Three channels and one current sound each.</strong> BGM and BGS loop
/// and can be faded; SE plays once and is not kept, because the next effect
/// replaces it and a reader that kept a list would grow without bound in a game
/// that plays a footstep every four frames.
/// </para>
/// </remarks>
public sealed class WolfAudioState
{
	private readonly WolfSoundRequest?[] _channels =
		new WolfSoundRequest?[WolfSoundChannel.MaxChannel + 1];

	/// <summary>Whether BGM and BGS play at all, from the config window.</summary>
	public bool MusicEnabled { get; set; } = true;

	/// <summary>Whether SE plays at all, from the config window.</summary>
	public bool EffectEnabled { get; set; } = true;

	/// <summary>
	/// Whether a volume of zero stays at zero rather than becoming 100.
	/// </summary>
	/// <remarks>
	/// <strong>Off by default, and the choice is stated.</strong> The game
	/// settings page says that before version 3.681 a zero was converted to 100
	/// and that the setting can keep it at zero. **A reader with no game settings
	/// in front of it uses the older behaviour**, because that is what a game
	/// without the setting was built against — and a game with the setting says
	/// so.
	/// </remarks>
	public bool KeepZeroVolume { get; set; }

	/// <summary>
	/// What a channel is currently playing, or null when it is silent.
	/// </summary>
	/// <remarks>
	/// <strong>Null and not a silent track.</strong> A reader that returned an
	/// empty track for a silent channel would have a file name of "" that a
	/// caller could play, and the symptom would be a sound named after nothing.
	/// </remarks>
	public WolfSoundRequest? Current(int pChannel)
	{
		return pChannel >= 0 && pChannel <= WolfSoundChannel.MaxChannel
			? _channels[pChannel]
			: null;
	}

	/// <summary>
	/// Records a sound command, and says whether it would be heard.
	/// </summary>
	/// <param name="pRequest">What the command asked for.</param>
	/// <returns>
	/// True when the channel is enabled and the request is a real one.
	/// </returns>
	/// <remarks>
	/// <para>
	/// <strong>An empty name is not a sound</strong>, and a reader that stored it
	/// would leave the channel "playing" something with no file — and the next
	/// command to ask what is playing would answer with a name of nothing.
	/// </para>
	/// <para>
	/// <strong>A disabled channel records nothing.</strong> The config window has
	/// separate switches for the music and the effects, and a game that turned
	/// the effects off should not have them accumulate in a list a save file
	/// would then carry.
	/// </para>
	/// </remarks>
	public bool Play(WolfSoundRequest pRequest)
	{
		if (pRequest.Channel < 0 || pRequest.Channel > WolfSoundChannel.MaxChannel)
		{
			return false;
		}
		if (!IsEnabled(pRequest.Channel))
		{
			return false;
		}
		if (string.IsNullOrEmpty(pRequest.Name))
		{
			return false;
		}
		_channels[pRequest.Channel] = pRequest;
		return true;
	}

	/// <summary>Stops one channel.</summary>
	public void Stop(int pChannel)
	{
		if (pChannel >= 0 && pChannel <= WolfSoundChannel.MaxChannel)
		{
			_channels[pChannel] = null;
		}
	}

	/// <summary>Whether a channel is switched on at all.</summary>
	public bool IsEnabled(int pChannel)
	{
		return pChannel == WolfSoundChannel.Se ? EffectEnabled : MusicEnabled;
	}

	/// <summary>Silences every channel, for a new game.</summary>
	public void Clear()
	{
		for (var channel = 0; channel <= WolfSoundChannel.MaxChannel; channel++)
		{
			_channels[channel] = null;
		}
	}
}
