using Godot;
using UniversalRPG.Rm2k.Rendering;

namespace UniversalRPG.App.Ui;

/// <summary>
/// The picture an MZ runtime has already painted.
/// </summary>
/// <remarks>
/// <para>
/// <strong>And the runtime paints it.</strong> --
/// <c>PaintedMap = pixel</c> after the tiles are drawn and the figures
/// are put over them, -- <strong>and a test already proves it is not
/// one colour</strong>, -- <strong>so this is not a renderer.</strong>
/// -- <strong>It is a window.</strong>
/// </para>
/// <para>
/// <strong>And the reason it is a separate file is the same one as
/// for <c>Rm2kMapPreview</c>:</strong> -- <strong>no
/// <c>IEngineRuntime</c> is a Godot node</strong>, --
/// <strong>the host hands over a plain buffer and this turns it into
/// something to look at.</strong>
/// </para>
/// <para>
/// <strong>And the map is 14 tiles wide and a tile is 48 pixels.</strong>
/// -- <strong>That is what the project's own data says and what the
/// existing test asserts</strong>, -- <strong>and the signature
/// guards against re-uploading six hundred pixels every
/// frame.</strong>
/// </para>
/// </remarks>
public partial class MzMapPreview : Control
{
    private Rm2kPixelBuffer? _painted;
    private ImageTexture? _textur;
    private string _signatur = "";
    private string _grund = "";

    /// <summary>
    /// Hands it the map, and it uploads once per change.
    /// </summary>
    /// <param name="pPixels">What the runtime painted.</param>
    public void SetzeKarte(Rm2kPixelBuffer? pPixels)
    {
        if (ReferenceEquals(_painted, pPixels))
        {
            return;
        }

        _painted = pPixels;
        _textur = null;
        _signatur = "";
        QueueRedraw();
    }

    /// <summary>
    /// Why there is no picture, and empty when there is one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the runtime has its own reason for every
    /// refusal</strong>, -- <strong>and a preview that said nothing
    /// would leave the reader guessing between three
    /// causes.</strong>
    /// </para>
    /// </remarks>
    public string Grund { get; set; } = "";

    /// <summary>
    /// And the buffer behind the texture, and it is the runtime's.
    /// </summary>
    /// <returns>The buffer, or null.</returns>
    public Rm2kPixelBuffer? Karte() => _painted;

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color("111119"), true);
        var textur = HoleTextur();
        if (textur == null)
        {
            var grund = _grund.Length > 0 ? _grund : "No MZ map painted";
            DrawString(ThemeDB.FallbackFont, new Vector2(12, 24), grund,
                HorizontalAlignment.Left, -1, 14, new Color("aaa7b5"));
            return;
        }

        // **Und das Seitenverhaeltnis  bleibt  erhalten**, --
        // **und 14 Kacheln  mal  48 Pixel  sind  672  breit.**
        var scale = Mathf.Min(
            Size.X / textur.GetWidth(), Size.Y / textur.GetHeight());
        if (scale <= 0.0f)
        {
            return;
        }

        var gezeichnet = new Vector2(
            textur.GetWidth() * scale, textur.GetHeight() * scale);
        var ursprung = new Vector2(
            (Size.X - gezeichnet.X) / 2.0f, (Size.Y - gezeichnet.Y) / 2.0f);
        DrawTextureRect(textur, new Rect2(ursprung, gezeichnet),
            false, Colors.White, false);

        if (_grund.Length > 0)
        {
            DrawString(ThemeDB.FallbackFont,
                new Vector2(12, Size.Y - 12), _grund,
                HorizontalAlignment.Left, Size.X - 24, 12,
                new Color("e8a24a"));
        }
    }

    private ImageTexture? HoleTextur()
    {
        if (_painted == null)
        {
            return null;
        }

        // **Und  die  Signatur  ist  Breite  mal  Hoehe  und  die
        //  Laenge**, -- **und  ein  Puffer,  der  sich  aendert,
        //  ohne  seine  Form  zu  aendern,  laedt  trotzdem
        //  neu.**
        var signatur = $"{_painted.Width}x{_painted.Height}"
            + $":{_painted.Pixels.Length}";
        if (_textur != null && signatur == _signatur)
        {
            return _textur;
        }

        var bild = Image.CreateFromData(
            _painted.Width, _painted.Height, false,
            Image.Format.Rgba8, _painted.Pixels);
        if (bild == null)
        {
            return null;
        }

        _textur = ImageTexture.CreateFromImage(bild);
        _signatur = signatur;
        return _textur;
    }
}
