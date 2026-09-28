namespace UniversalRPG.Rm2k.Simulation;

/// <summary>
/// One playing sound, from liblcf's <c>rpg::Music</c> and <c>rpg::Sound</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>This is data, not a playing sound.</strong> The file, the four
/// numbers and the fade are what a command asked for; nothing here has a
/// player, a mixer or a device behind it, and a reader that reported a track as
/// "playing" would be claiming a sound nobody can hear.
/// </para>
/// <para>
/// The four numbers are the format's, and all four are bounded by the editor's
/// own ranges rather than by a scale invented here: volume 0 to 100, tempo 50
/// to 200, balance 0 to 100 with 50 in the middle, and the fade in tenths of a
/// second.
/// </para>
/// </remarks>
public sealed class Rm2kTrack
{
	/// <summary>The file name, from the command's string field.</summary>
	public string Name { get; init; } = "";

	/// <summary>Volume in percent, from <c>parameters[2]</c>.</summary>
	public int Volume { get; init; }

	/// <summary>Tempo in percent, from <c>parameters[3]</c>.</summary>
	public int Tempo { get; init; }

	/// <summary>
	/// Balance in percent, from <c>parameters[4]</c>, where 50 is centre and
	/// the two ends are 0 and 100.
	/// </summary>
	/// <remarks>
	/// **Not -100 to 100.** The editor writes 0 to 100 and the engine stores it
	/// as written, so a reader that treated the middle as 0 would have called
	/// every centred track hard left.
	/// </remarks>
	public int Balance { get; init; }

	/// <summary>
	/// The fade in on this track, in tenths of a second, from
	/// <c>parameters[1]</c>.
	/// </summary>
	public int FadeInTenths { get; init; }

	/// <summary>
	/// What this reader can honestly say about the track.
	/// </summary>
	public override string ToString()
	{
		return $"{Name} vol {Volume} tempo {Tempo} balance {Balance}"
			+ (FadeInTenths > 0 ? $" fadein {FadeInTenths}" : string.Empty);
	}
}

/// <summary>
/// The audio the commands ask for, and what has been asked for since.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Four position doubles sat in the simulation state, and nothing read
/// or wrote them.</strong> They were the residue of a plan for playback this
/// repository has not built, and a double that no command moves is a claim
/// about time that nothing keeps. They are replaced by what the format
/// actually holds: the current track per channel, the fade state, and the one
/// memorised BGM that <c>11530</c> and <c>11540</c> are about.
/// </para>
/// <para>
/// <strong>The four channels are not interchangeable.</strong> BGM loops and
/// fades; SE plays once over it; ME is the battle music and follows the same
/// rules as BGM; BGS is a looping ambient sound. A reader that kept one list for
/// all four would have let a battle's music overwrite the town theme.
/// </para>
/// </remarks>
public sealed class Rm2kAudioState
{
	public const int MinVolume = 0;
	public const int MaxVolume = 100;
	public const int MinTempo = 50;
	public const int MaxTempo = 200;
	public const int MinBalance = 0;

	/// <summary>
	/// The balance a centred track carries, and not 0.
	/// </summary>
	public const int CentreBalance = 50;
	public const int MaxBalance = 100;

	/// <summary>The longest fade the editor writes, in tenths of a second.</summary>
	public const int MaxFadeTenths = 600;

	/// <summary>
	/// The longest a track file name may be, from
	/// <c>PresentationState.MaxPictureNameCharacters</c> and the same
	/// reasoning: this reader does not touch a filesystem, so the bound is
	/// about what a diagnostic can carry, not about what a path allows.
	/// </summary>
	public const int MaxNameCharacters = 256;

	/// <summary>The background music, the channel <c>11510</c> writes.</summary>
	public Rm2kTrack? Bgm { get; private set; }

	/// <summary>The sound effect, the channel <c>11550</c> writes.</summary>
	/// <remarks>
	/// **The last one played, not a list.** <c>SePlay</c> starts a new sound
	/// over whatever was playing, and the format keeps no history here. A
	/// reader that kept a list would be reporting a queue the game never asked
	/// for.
	/// </remarks>
	public Rm2kTrack? SoundEffect { get; private set; }

	/// <summary>The battle music, which follows the same rules as the BGM.</summary>
	public Rm2kTrack? BattleMusic { get; private set; }

	/// <summary>The background sound, a loop that runs under the music.</summary>
	public Rm2kTrack? BackgroundSound { get; private set; }

	/// <summary>The fade out in progress on the BGM, in tenths, or 0.</summary>
	public int BgmFadeOutTenths { get; private set; }

	/// <summary>The fade out in progress on the battle music, in tenths, or 0.</summary>
	public int BattleMusicFadeOutTenths { get; private set; }

	/// <summary>
	/// The track <c>11530</c> memorised, or null.
	/// </summary>
	/// <remarks>
	/// <strong>One slot, and it is the BGM.</strong> The reference's
	/// <c>MemorizeBGM</c> and <c>PlayMemorizedBGM</c> take no parameters and
	/// touch only that one, and there is no second memorised track anywhere in
	/// the format. A reader that offered four slots would be offering a
	/// feature the file does not have.
	/// </remarks>
	public Rm2kTrack? MemorizedBgm { get; private set; }

	/// <summary>Whether the BGM has been asked to stop, by a fade or a script.</summary>
	public bool IsBgmFadingOut => BgmFadeOutTenths > 0;

	/// <summary>
	/// Starts a track on a channel, from <c>Game_System::BgmPlay</c> and
	/// <c>SePlay</c>.
	/// </summary>
	/// <remarks>
	/// <strong>A fade in progress is cancelled</strong>, because the reference's
	/// <c>BgmPlay</c> replaces the channel and a reader that let an old fade
	/// run would fade out a track that never started. A cancel that nobody
	/// announced is a state change nobody can see, so it is returned.
	/// </remarks>
	/// <returns>True if a pending fade was cancelled.</returns>
	public bool Play(
		Rm2kChannel pChannel, string pName, int pVolume, int pTempo,
		int pBalance, int pFadeInTenths)
	{
		if (pName == null || pName.Length == 0 || pName.Length > MaxNameCharacters)
		{
			return false;
		}
		if (pVolume < MinVolume || pVolume > MaxVolume
			|| pTempo < MinTempo || pTempo > MaxTempo
			|| pBalance < MinBalance || pBalance > MaxBalance
			|| pFadeInTenths < 0 || pFadeInTenths > MaxFadeTenths)
		{
			return false;
		}
		var track = new Rm2kTrack
		{
			Name = pName,
			Volume = pVolume,
			Tempo = pTempo,
			Balance = pBalance,
			FadeInTenths = pFadeInTenths,
		};
		var cancelledFade = false;
		switch (pChannel)
		{
			case Rm2kChannel.BackgroundMusic:
				cancelledFade = BgmFadeOutTenths > 0;
				BgmFadeOutTenths = 0;
				Bgm = track;
				break;
			case Rm2kChannel.SoundEffect:
				SoundEffect = track;
				break;
			case Rm2kChannel.BattleMusic:
				cancelledFade = BattleMusicFadeOutTenths > 0;
				BattleMusicFadeOutTenths = 0;
				BattleMusic = track;
				break;
			case Rm2kChannel.BackgroundSound:
				BackgroundSound = track;
				break;
			default:
				return false;
		}
		return cancelledFade;
	}

	/// <summary>
	/// Fades a channel out, from <c>BgmFade</c>, in tenths of a second.
	/// </summary>
	/// <remarks>
	/// <strong>Fading a channel that is silent is a no-op, not a track.</strong>
	/// The reference passes the number straight to the audio system, which has
	/// nothing to fade; a reader that invented an empty track to fade would
	/// claim a sound that was never asked for.
	/// </remarks>
	public bool FadeOut(Rm2kChannel pChannel, int pTenths)
	{
		if (pTenths < 0 || pTenths > MaxFadeTenths)
		{
			return false;
		}
		switch (pChannel)
		{
			case Rm2kChannel.BackgroundMusic:
				if (Bgm == null)
				{
					return false;
				}
				BgmFadeOutTenths = pTenths;
				return true;
			case Rm2kChannel.BattleMusic:
				if (BattleMusic == null)
				{
					return false;
				}
				BattleMusicFadeOutTenths = pTenths;
				return true;
			default:
				// The sound effect and the background sound have no fade in
				// this format, and a reader that offered one would be offering
				// something RPG_RT does not have.
				return false;
		}
	}

	/// <summary>
	/// Stores the current BGM for <c>11540</c>, from
	/// <c>Game_System::MemorizeBGM</c>.
	/// </summary>
	/// <remarks>
	/// **Memorizing nothing stores nothing, and says so.</strong> The reference
	/// copies the pointer, so memorizing an empty channel leaves it empty; a
	/// reader that stored a placeholder would hand a game a track it never
	/// played.
	/// </remarks>
	public bool MemorizeBgm()
	{
		if (Bgm == null)
		{
			return false;
		}
		MemorizedBgm = Bgm;
		return true;
	}

	/// <summary>
	/// Plays the memorised BGM again, from <c>PlayMemorizedBGM</c>.
	/// </summary>
	public bool PlayMemorizedBgm()
	{
		if (MemorizedBgm == null)
		{
			return false;
		}
		Bgm = MemorizedBgm;
		return true;
	}

	/// <summary>
	/// Returns every channel to silence, for a new game.
	/// </summary>
	/// <remarks>
	/// **The memorised track goes with the rest.** It belongs to this game's
	/// music, and a new game that inherited it could replay a track from a
	/// game the player has not seen yet.
	/// </remarks>
	public void Reset()
	{
		Bgm = null;
		SoundEffect = null;
		BattleMusic = null;
		BackgroundSound = null;
		BgmFadeOutTenths = 0;
		BattleMusicFadeOutTenths = 0;
		MemorizedBgm = null;
	}
}

/// <summary>
/// The four audio channels, from <c>Game_System</c>'s four play methods.
/// </summary>
public enum Rm2kChannel
{
	BackgroundMusic = 0,
	SoundEffect = 1,
	BattleMusic = 2,
	BackgroundSound = 3,
}
