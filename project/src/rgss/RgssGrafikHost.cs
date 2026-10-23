using System;
using System.Collections.Generic;
using System.IO;

namespace UniversalRPG.Rgss;

/// <summary>
/// Drives the runtime's frame while a game's own loop is running.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And this is the inversion a real RGSS host has.</strong> XP's
/// <c>Scene_Title#main</c> is a <c>loop do ... end</c> whose body calls
/// <c>Graphics.update</c>, <c>Input.update</c> and the scene's own update --
/// **the loop belongs to the game**, and <c>Graphics.update</c> is the frame:
/// it presents one and returns, and the game calls it again.
/// </para>
/// <para>
/// <strong>And that is why this host exists and the module does not answer
/// it.</strong> Measured: the built-in <c>Graphics</c> module counted frames in
/// Ruby, so the game's own <c>main</c> was never run -- the runtime called
/// <c>$scene.update</c> instead, the scene's setup (where XP builds its windows
/// and sprites) never happened, and nothing was drawn while every report said
/// zero problems. <strong>And a frame cannot be counted in Ruby</strong>: a
/// frame is the host's work, and the host is where the surface will be.
/// </para>
/// <para>
/// <strong>And the budget is the only way out, and it is honest.</strong> The
/// game's loop has no exit until the player leaves the scene, so a host that
/// runs a game must be able to say "that is enough frames" -- and it says it by
/// throwing out of <c>update</c>, which unwinds the game's own loop the same way
/// a real quit does. **And a run that stopped at its budget is not a
/// failure**: it is a run that measured what it came for.
/// </para>
/// </remarks>
public sealed class RgssGrafikHost : IRubyHost
{
    /// <summary>The host this one asks when the name is not its own.</summary>
    public IRubyHost? Fallback { get; set; }

    /// <summary>Owned pixel storage; access only from the interpreter's execution owner.</summary>
    public RgssBitmapStore Bitmaps { get; } = new();
    public RgssSpriteStore Sprites { get; } = new();
    internal System.Threading.CancellationToken ExecutionCancellation { get; set; }
    internal RgssRenderedFrame? RenderedFrame { get; private set; }
    internal void ReleaseRenderResources()
    {
        Bitmaps.ReleaseAll();
        Sprites.ReleaseAll();
        RenderedFrame = null;
    }

    /// <summary>The game's own folder, which is where its images are.</summary>
    public string? Wurzel { get; set; }

    /// <summary>
    /// The folder each kind of image lives in, in XP's own layout.
    /// </summary>
    /// <remarks>
    /// <strong>And these are XP's names and not a reading of one game.</strong>
    /// Measured, the game this runtime is tested against ships three folders of
    /// its own -- Battlers, Pictures, Titles -- and takes its windowskin from the
    /// RTP, which its Game.ini names as RTP1=Standard; a cache built from that one
    /// game would find a windowskin in one game in a thousand.
    /// </remarks>
    private static readonly Dictionary<string, string> Ordner =
        new(StringComparer.Ordinal)
        {
            ["windowskins"] = "Windowskins",
            ["pictures"] = "Pictures",
            ["battlebacks"] = "Battlebacks",
            ["animations"] = "Animations",
            ["characters"] = "Characters",
            ["tilesets"] = "Tilesets",
            ["autotiles"] = "Autotiles",
            ["battlers"] = "Battlers",
            ["fogs"] = "Fogs",
            ["panoramas"] = "Panoramas",
            ["titles"] = "Titles",
        };

    /// <summary>The width in pixels, 640 for RGSS 1.0.</summary>
    public int Breite { get; } = 640;

    /// <summary>The height in pixels, 480 for RGSS 1.0.</summary>
    public int Hoehe { get; } = 480;

    /// <summary>The frames this host has presented.</summary>
    public int Frames { get; private set; }

    /// <summary>
    /// How many frames to present before stopping the game's own loop.
    /// </summary>
    /// <remarks>
    /// <strong>And zero means no budget, which is what a real game wants.</strong>
    /// A measurement sets it; a player does not.
    /// </remarks>
    public int Budget { get; set; }

    /// <summary>Whether the run ended because the budget was reached.</summary>
    public bool BudgetErreicht { get; private set; }

    /// <summary>
    /// What the runtime does for one frame.
    /// </summary>
    /// <remarks>
    /// <strong>And this is where the surface will be.</strong> Today it is the
    /// frame count and nothing else, because there is nothing to draw on yet.
    /// </remarks>
    public Action<int>? Rahmen { get; set; }

    /// <summary>The names this host answers, and its fallback's.</summary>
    public IReadOnlyList<string> KnownMethods
    {
        get
        {
            // **And the helper the cache calls is one of the names.** A host that
            // answers a name and does not declare it is a host whose own answers
            // read as gaps -- measured, and that is what the message did.
            var namen = new List<string> { "update", "wait", "BildGroesse", "rgss_present_frame", "rgss_bitmap", "rgss_sprite" };
            if (Fallback != null)
            {
                namen.AddRange(Fallback.KnownMethods);
            }

            return namen;
        }
    }

    /// <inheritdoc/>
    public RubyValue? CallMethod(
        RubyValue pReceiver, string pMethod, IReadOnlyList<RubyValue> pArguments)
    {
        // **And the cache asks for a size by a bare name**, which is how a
        // module reaches the host for something only the host can do -- reading
        // a file. **And the name is the host's own**, so it is answered here and
        // nowhere else.
        if (pMethod == "BildGroesse")
        {
            var art = pArguments.Count > 0 && pArguments[0].Kind == RubyValueKind.Symbol
                ? pArguments[0].Name ?? string.Empty
                : string.Empty;
            var name = pArguments.Count > 1 && pArguments[1].Kind == RubyValueKind.String
                ? System.Text.Encoding.UTF8.GetString(pArguments[1].Bytes)
                : string.Empty;
            return BildGroesse(art, name);
        }

        if (pMethod == "rgss_bitmap") return CallBitmap(pArguments);
        if (pMethod == "rgss_sprite") return Sprites.Call(pArguments, Bitmaps);

        if (pMethod == "rgss_present_frame")
        {
            if (pArguments.Count != 0 && pArguments.Count != 2)
            {
                throw new RubyRuntimeException("ArgumentError", "rgss_present_frame accepts zero arguments or width and height.");
            }
            var width = pArguments.Count == 2 && pArguments[0].Kind != RubyValueKind.Nil
                ? BitmapCoordinate(pArguments[0]) : Breite;
            var height = pArguments.Count == 2 && pArguments[1].Kind != RubyValueKind.Nil
                ? BitmapCoordinate(pArguments[1]) : Hoehe;
            PresentFrame(width, height);
            return RubyValue.Nil;
        }

        if (pReceiver.Kind != RubyValueKind.Symbol || pReceiver.Name != "Graphics")
        {
            return Fallback?.CallMethod(pReceiver, pMethod, pArguments);
        }

        switch (pMethod)
        {
            case "update":
                PresentFrame();
                return RubyValue.Nil;

            case "wait":
                // **And `wait` is a number of frames, and each one is a frame.**
                var anzahl = pArguments.Count > 0
                    && pArguments[0].Kind == RubyValueKind.Integer
                        ? (int)pArguments[0].Integer
                        : 0;
                for (var i = 0; i < anzahl; i++)
                {
                    PresentFrame();
                }

                return RubyValue.Nil;

            default:
                return Fallback?.CallMethod(pReceiver, pMethod, pArguments);
        }
    }

    private RubyValue CallBitmap(IReadOnlyList<RubyValue> arguments)
    {
        if (arguments.Count == 0 || arguments[0].Kind != RubyValueKind.Symbol)
            throw new RubyRuntimeException("ArgumentError", "rgss_bitmap requires an operation symbol.");
        var operation = arguments[0].Name;
        var count = operation switch
        {
            "create" or "cache_path" => 3,
            "clear" or "dispose" or "copy" or "load" => 2,
            "get_pixel" => 4,
            "set_pixel" => 8,
            "fill_rect" => 10,
            _ => throw new RubyRuntimeException("ArgumentError", "Unknown bitmap operation: " + operation),
        };
        if (arguments.Count != count)
            throw new RubyRuntimeException("ArgumentError", "Wrong argument count for bitmap " + operation);
        if (operation == "cache_path")
        {
            if (arguments[1].Kind != RubyValueKind.Symbol || arguments[2].Kind != RubyValueKind.String)
                throw new RubyRuntimeException("TypeError", "Image cache requires a category Symbol and filename String.");
            var name = System.Text.Encoding.UTF8.GetString(arguments[2].Bytes);
            if (Wurzel == null || !Ordner.TryGetValue(arguments[1].Name ?? "", out var folder))
            {
                RecordMissingImage((arguments[1].Name ?? "") + "/" + name);
                return RubyValue.Nil;
            }
            var filename = "Graphics/" + folder + "/" + name;
            try
            {
                RgssBitmapFileLoader.Resolve(Wurzel, filename, ExecutionCancellation);
                return RubyValue.OfBytes(System.Text.Encoding.UTF8.GetBytes(filename));
            }
            catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                RecordMissingImage(filename + " (" + error.Message + ")");
                return RubyValue.Nil;
            }
        }
        if (operation == "load")
        {
            if (arguments[1].Kind != RubyValueKind.String)
                throw new RubyRuntimeException("TypeError", "Bitmap filename must be a String.");
            var filename = System.Text.Encoding.UTF8.GetString(arguments[1].Bytes);
            try
            {
                var loaded = RgssBitmapFileLoader.Load(Bitmaps, Wurzel, filename, ExecutionCancellation);
                return RubyValue.OfArray(new List<RubyValue>
                {
                    RubyValue.OfInteger(loaded.Id), RubyValue.OfInteger(loaded.Width), RubyValue.OfInteger(loaded.Height),
                });
            }
            catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                RecordMissingImage(filename + " (" + error.Message + ")");
                throw new RubyRuntimeException(error is FileNotFoundException ? "Errno::ENOENT" : "RGSSError",
                    "Image '" + filename + "': " + error.Message);
            }
        }
        if (operation == "create")
            return RubyValue.OfInteger(Bitmaps.Create(BitmapInteger(arguments[1]), BitmapInteger(arguments[2])));
        if (arguments[1].Kind != RubyValueKind.Integer)
            throw new RubyRuntimeException("RGSSError", "The bitmap has no pixel surface.");
        var id = arguments[1].Integer;
        switch (operation)
        {
            case "copy": return RubyValue.OfInteger(Bitmaps.Copy(id));
            case "clear": Bitmaps.Clear(id); break;
            case "dispose": Bitmaps.DisposeBitmap(id); break;
            case "get_pixel": return Bitmaps.GetPixel(id, BitmapCoordinate(arguments[2]), BitmapCoordinate(arguments[3]));
            case "fill_rect":
                Bitmaps.FillRect(id, BitmapCoordinate(arguments[2]), BitmapCoordinate(arguments[3]),
                    BitmapCoordinate(arguments[4]), BitmapCoordinate(arguments[5]),
                    BitmapChannel(arguments[6]), BitmapChannel(arguments[7]),
                    BitmapChannel(arguments[8]), BitmapChannel(arguments[9]));
                break;
            case "set_pixel":
                Bitmaps.SetPixel(id, BitmapCoordinate(arguments[2]), BitmapCoordinate(arguments[3]),
                    BitmapChannel(arguments[4]), BitmapChannel(arguments[5]),
                    BitmapChannel(arguments[6]), BitmapChannel(arguments[7]));
                break;
        }
        return RubyValue.Nil;
    }

    private static int BitmapInteger(RubyValue value)
    {
        if (value.Kind != RubyValueKind.Integer || value.Integer < int.MinValue || value.Integer > int.MaxValue)
            throw new RubyRuntimeException("ArgumentError", "Bitmap dimensions must be bounded integers.");
        return (int)value.Integer;
    }

    private static int BitmapCoordinate(RubyValue value)
    {
        var number = value.Kind switch
        {
            RubyValueKind.Integer => (double)value.Integer,
            RubyValueKind.Float => Math.Truncate(value.Real),
            _ => throw new RubyRuntimeException("TypeError", "Bitmap coordinates must be numeric."),
        };
        if (!double.IsFinite(number) || number < int.MinValue || number > int.MaxValue)
            throw new RubyRuntimeException("ArgumentError", "Bitmap coordinates must fit a finite 32-bit integer.");
        return (int)number;
    }

    private static byte BitmapChannel(RubyValue value)
    {
        var number = value.Kind switch
        {
            RubyValueKind.Integer => (double)value.Integer,
            RubyValueKind.Float => value.Real,
            _ => throw new RubyRuntimeException("TypeError", "Bitmap color channels must be numeric."),
        };
        if (!double.IsFinite(number))
            throw new RubyRuntimeException("ArgumentError", "Bitmap color channels must be finite.");
        return (byte)Math.Clamp(number, 0, 255);
    }

    private void PresentFrame(int width = 640, int height = 480)
    {
        RenderedFrame = Sprites.Compose(Bitmaps, width, height, ExecutionCancellation);
        Frames = checked(Frames + 1);
        Rahmen?.Invoke(Frames);
        if (Budget > 0 && Frames >= Budget)
        {
            BudgetErreicht = true;
            throw new RgssFrameBudgetErreicht(Frames);
        }
    }

    /// <inheritdoc/>
    public RubyValue? CallMethodWithBlock(
        RubyValue pReceiver,
        string pMethod,
        IReadOnlyList<RubyValue> pArguments,
        Func<IReadOnlyList<RubyValue>, RubyValue> pYield) =>
        Fallback?.CallMethodWithBlock(pReceiver, pMethod, pArguments, pYield);

    /// <inheritdoc/>
    public RubyValue? LookupConstant(string pName) => Fallback?.LookupConstant(pName);
    /// <summary>
    /// The size of an image, or nil and a diagnostic when there is none.
    /// </summary>
    /// <remarks>
    /// <strong>And the size is read out of the file and not assumed.</strong> A
    /// bitmap whose size was a placeholder would be one a game lays out against
    /// numbers nobody measured, and a window that centres text in it would centre
    /// it in the wrong place. The PNG header carries the size in its first bytes,
    /// so the file is read and the real numbers come back.
    /// </remarks>
    public RubyValue? BildGroesse(string pArt, string pName)
    {
        if (Wurzel == null || !Ordner.TryGetValue(pArt, out var ordner))
        {
            RecordMissingImage(pArt + "/" + pName);
            return null;
        }

        var pfad = Path.Combine(Wurzel, "Graphics", ordner, pName + ".png");
        if (!File.Exists(pfad))
        {
            // **And a name that is not there is said by name.** A game that draws
            // with a nil bitmap should know that it is drawing nothing -- and the
            // RTP is a real answer to where it is, which is why the name of the
            // file is reported and not a bare nil.
            RecordMissingImage("Graphics/" + ordner + "/" + pName + ".png");
            return null;
        }

        var kopf = new byte[33];
        try
        {
            using var strom = File.OpenRead(pfad);
            if (strom.ReadAtLeast(kopf, kopf.Length, throwOnEndOfStream: false) < kopf.Length)
            {
                RecordMissingImage("Graphics/" + ordner + "/" + pName + ".png (truncated)");
                return null;
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            RecordMissingImage("Graphics/" + ordner + "/" + pName + ".png (unreadable: " + error.GetType().Name + ")");
            return null;
        }

        // Metadata only: require the PNG signature and a complete first IHDR chunk.
        // Full image decoding and CRC validation remain the image loader's responsibility.
        ReadOnlySpan<byte> prefix = [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82];
        if (!kopf.AsSpan(0, prefix.Length).SequenceEqual(prefix))
        {
            RecordMissingImage("Graphics/" + ordner + "/" + pName + ".png (invalid PNG header)");
            return null;
        }
        var width = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(kopf.AsSpan(16, 4));
        var height = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(kopf.AsSpan(20, 4));
        if (width <= 0 || height <= 0 || width > RgssBitmapStore.MaximumDimension || height > RgssBitmapStore.MaximumDimension)
        {
            RecordMissingImage("Graphics/" + ordner + "/" + pName + ".png (unsupported dimensions)");
            return null;
        }

        return RubyValue.OfArray(
            new List<RubyValue>
            {
                RubyValue.OfInteger(width),
                RubyValue.OfInteger(height),
            });
    }

    /// <summary>The images a game asked for and this runtime could not find.</summary>
    public IReadOnlyList<string> FehlendeBilder
    {
        get { lock (_missingImagesGate) return Array.AsReadOnly(_fehlende.ToArray()); }
    }

    private readonly object _missingImagesGate = new();
    private readonly List<string> _fehlende = new();

    private void RecordMissingImage(string image)
    {
        lock (_missingImagesGate)
        {
            if (_fehlende.Contains(image)) return;
            if (_fehlende.Count == 64) _fehlende.RemoveAt(0);
            _fehlende.Add(image);
        }
    }
}

/// <summary>
/// The end of a measured run: the host presented the frames it was asked for.
/// </summary>
/// <remarks>
/// <strong>And this is not an error in the game.</strong> A game's loop runs
/// until the player leaves the scene, so a host that measures has to end it --
/// and this exception is how the run says "enough frames" instead of waiting for
/// a scene change that a measurement will never produce.
/// </remarks>
public sealed class RgssFrameBudgetErreicht : Exception
{
    public RgssFrameBudgetErreicht(int pFrames)
        : base("the frame budget was reached after " + pFrames + " frames")
    {
        Frames = pFrames;
    }

    /// <summary>The frames that were presented.</summary>
    public int Frames { get; }

}
