using System;
using Godot;

namespace UniversalRPG.App.Ui;

/// <summary>
/// The four sound channels <c>Rm2kAudioState</c> carries, played out.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the layer that was missing, and it is
/// missing on purpose in every other place.</strong> --
/// <strong>No <c>IEngineRuntime</c> is a Godot node</strong>, --
/// <strong>the contract hands a host two paths and a selection and
/// nothing else</strong>, -- <strong>and a host that reached into
/// Godot itself would break every test that runs it
/// headless.</strong>
/// </para>
/// <para>
/// <strong>And this is the same shape as
/// <c>MzAudioOutput</c>:</strong> -- <strong>the host hands over a
/// plain value, this turns it into something Godot can show or
/// hear, and nothing in between knows which host it came
/// from.</strong>
/// </para>
/// <para>
/// <strong>And the three rules, each read out of the
/// engine:</strong>
/// </para>
/// <list type="number">
/// <item>
/// <strong>An empty name is silence and not an empty
/// sound.</strong> -- <strong>And a reader that loaded a file for
/// it would play a sound the game asked to have stopped.</strong>
/// </item>
/// <item>
/// <strong>The volume is a percentage of the game's own, and the
/// pitch is a percentage of its own.</strong> --
/// <strong>And a reader that used them as factors would halve the
/// music.</strong>
/// </item>
/// <item>
/// <strong>A fade is counted in frames, and the game runs at sixty
/// a second.</strong> -- <strong>And <c>FadeFramesLeft</c> is the
/// number the engine decrements once per frame.</strong>
/// </item>
/// </list>
/// </remarks>
public partial class Rm2kAudioOutput : Node
{
    private const int Kanaele = 4;

    private readonly AudioStreamPlayer[] _player = new AudioStreamPlayer[Kanaele];
    private readonly AudioStreamWav[] _wavStream = new AudioStreamWav[Kanaele];
    private readonly AudioStreamMP3[] _mp3Stream = new AudioStreamMP3[Kanaele];
    private readonly AudioStreamOggVorbis[] _oggStream = new AudioStreamOggVorbis[Kanaele];
    private readonly string[] _geladen = new string[Kanaele];
    private readonly string[] _ordner =
    {
        "BGM", "BGS", "ME", "SE",
    };
    private readonly string[] _extensions =
    {
        ".mid", ".mid", ".mid", ".wav",
    };
    private string _wurzel = "";
    private string _schluessel = "";

    /// <summary>
    /// And it builds the four players and puts them in the tree.
    /// </summary>
    public override void _Ready()
    {
        for (var i = 0; i < Kanaele; i++)
        {
            _player[i] = new AudioStreamPlayer();
            _player[i].Name = "rm2k" + _ordner[i].ToLower();
            AddChild(_player[i]);
            _geladen[i] = "";
        }
    }

    /// <summary>
    /// And it reads the channels of one frame.
    /// </summary>
    /// <param name="pAudio">What <c>Rm2kAudioState</c> holds.</param>
    /// <remarks>
    /// <para>
    /// <strong>And a channel whose name has not changed is not
    /// loaded again</strong>, -- <strong>because re-reading per
    /// frame would read the disk sixty times a second.</strong>
    /// </para>
    /// </remarks>
    public void SetzeKanaele(UniversalRPG.Rm2k.Simulation.Rm2kAudioState pAudio)
    {
        if (_wurzel.Length == 0)
        {
            return;
        }

        Trage(pAudio.Bgm, 0);
        Trage(pAudio.BackgroundSound, 1);
        Trage(pAudio.BattleMusic, 2);
        Trage(pAudio.SoundEffect, 3);
    }

    /// <summary>
    /// And where the files are, and the key they are encrypted with.
    /// </summary>
    /// <param name="pWurzel">The game's own directory.</param>
    /// <param name="pSchluessel">
    /// The hexadecimal key, -- <strong>and an empty one means the
    /// game has no encrypted assets</strong>.
    /// </param>
    public void SetzeWurzel(string pWurzel, string pSchluessel)
    {
        _wurzel = pWurzel;
        _schluessel = pSchluessel;
    }

    /// <summary>
    /// And one fade step, and the engine's own <c>updateFadeOut</c>.
    /// </summary>
    /// <param name="pKanal">Which channel.</param>
    /// <param name="pFrames">How many frames are left.</param>
    /// <param name="pLautstärke">The volume (0-100).</param>
    /// <remarks>
    /// <para>
    /// <strong>And the engine moves the volume down by the number it
    /// would have at zero</strong>, -- <strong>and a reader that set
    /// the volume to zero the moment the fade began would be wrong
    /// for every frame in between.</strong>
    /// </para>
    /// </remarks>
    public void BlendAus(int pKanal, int pFrames, int pLautstärke)
    {
        if (pKanal < 0 || pKanal >= Kanaele)
        {
            return;
        }

        var player = _player[pKanal];
        if (pFrames > 0)
        {
            player.Play();
        }
        else
        {
            player.Stop();
        }

        player.VolumeDb = Mathf.LinearToDb(
            Mathf.Clamp(pLautstärke / 100.0f, 0.0001f, 1.0f));
    }

    /// <summary>And stops everything, which is what a game's end does.</summary>
    public void StoppeAlle()
    {
        for (var i = 0; i < Kanaele; i++)
        {
            _player[i].Stop();
            _player[i].VolumeDb = 0.0f;
        }
    }

    /// <summary>And one channel, and whether its file was there.</summary>
    /// <param name="pKanal">Which one.</param>
    /// <returns>
    /// Whether the name is not empty and the file could be read,
    /// -- <strong>and false is not an error</strong>: it is what a
    /// channel the game never filled in looks like.
    /// </returns>
    public bool Traegt(int pKanal) =>
        pKanal >= 0 && pKanal < Kanaele && _player[pKanal].Playing;

    private void Trage(UniversalRPG.Rm2k.Simulation.Rm2kTrack? pKanal, int pIndex)
    {
        var player = _player[pIndex];
        var name = pKanal?.Name ?? "";

        if (name.Length == 0)
        {
            player.Stop();
            _geladen[pIndex] = "";
            return;
        }

        if (_geladen[pIndex] != name)
        {
            // Try multiple extensions for RM2K/RM2K3
            string? pfad = null;
            var extensionsToTry = new[] { ".mid", ".mp3", ".ogg", ".wav" };
            if (pIndex == 3) // SE channel prefers .wav
            {
                extensionsToTry = new[] { ".wav", ".ogg", ".mp3" };
            }

            foreach (var ext in extensionsToTry)
            {
                var tryPath = System.IO.Path.Combine(_wurzel, _ordner[pIndex], name + ext);
                if (System.IO.File.Exists(tryPath))
                {
                    pfad = tryPath;
                    break;
                }
            }

            if (pfad == null)
            {
                player.Stop();
                _geladen[pIndex] = "";
                return;
            }

            try
            {
                AudioStream stream = null;
                var ext = System.IO.Path.GetExtension(pfad).ToLowerInvariant();
                switch (ext)
                {
                    case ".wav":
                        var wavStream = new AudioStreamWav();
                        wavStream.Data = System.IO.File.ReadAllBytes(pfad);
                        stream = wavStream;
                        break;
                    case ".mp3":
                        stream = AudioStreamMP3.LoadFromFile(pfad);
                        break;
                    case ".ogg":
                        stream = AudioStreamOggVorbis.LoadFromFile(pfad);
                        break;
                    case ".mid":
                        // MIDI not directly supported by Godot, try to load as WAV if converted
                        // For now, skip MIDI files
                        player.Stop();
                        _geladen[pIndex] = "";
                        return;
                    default:
                        player.Stop();
                        _geladen[pIndex] = "";
                        return;
                }

                if (stream != null)
                {
                    _geladen[pIndex] = name;
                    player.Stream = stream;
                }
            }
            catch (Exception)
            {
                player.Stop();
                _geladen[pIndex] = "";
                return;
            }
        }

        // Apply pitch and volume
        var volume = pKanal?.Volume ?? 100;
        var tempo = pKanal?.Tempo ?? 100;

        player.PitchScale = Mathf.Clamp(tempo / 100.0f, 0.01f, 4.0f);
        player.VolumeDb = Mathf.LinearToDb(
            Mathf.Clamp(volume / 100.0f, 0.0001f, 1.0f));

        if (!player.Playing)
        {
            player.Play();
        }
    }
}