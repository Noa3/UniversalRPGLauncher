using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Presentation;

public sealed class PictureState
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
}

public sealed class PresentationState
{
    public const int MaxMessageCharacters = 4096;
    public const int MaxPictures = 100;
    public const int MaxPictureNameCharacters = 256;
    public const int MaxPictureDimension = 8192;

    public bool MessageVisible { get; private set; }
    public string MessageText { get; private set; } = "";
    public Dictionary<int, PictureState> Pictures { get; } = new();

    public bool ShowMessage(string pText)
    {
        if (pText == null || pText.Length > MaxMessageCharacters)
        {
            return false;
        }
        MessageText = pText;
        MessageVisible = true;
        return true;
    }

    public void DismissMessage()
    {
        MessageText = "";
        MessageVisible = false;
    }

    public bool ShowPicture(int pId, string pName, int pX, int pY, int pWidth, int pHeight)
    {
        if (pId <= 0 || pId > MaxPictures || string.IsNullOrWhiteSpace(pName) ||
            pName.Length > MaxPictureNameCharacters || pWidth <= 0 || pWidth > MaxPictureDimension ||
            pHeight <= 0 || pHeight > MaxPictureDimension)
        {
            return false;
        }
        Pictures[pId] = new PictureState { Id = pId, Name = pName, X = pX, Y = pY, Width = pWidth, Height = pHeight };
        return true;
    }

    public bool ErasePicture(int pId)
    {
        return pId > 0 && Pictures.Remove(pId);
    }
}

public sealed class PresentationResult
{
    private PresentationResult(bool pSuccess, string pError)
    {
        Success = pSuccess;
        Error = pError;
    }

    public bool Success { get; }
    public string Error { get; }
    public static PresentationResult Succeeded() => new(true, "");
    public static PresentationResult Failed(string pError) => new(false, pError);
}

public sealed class PresentationAdapter
{
    private readonly PresentationState _state;

    public PresentationAdapter(PresentationState pState)
    {
        _state = pState ?? throw new ArgumentNullException(nameof(pState));
    }

    public PresentationResult ShowMessage(string pText) =>
        _state.ShowMessage(pText) ? PresentationResult.Succeeded() : PresentationResult.Failed("Message exceeds presentation bounds.");

    public PresentationResult ShowPicture(int pId, string pName, int pX, int pY, int pWidth, int pHeight) =>
        _state.ShowPicture(pId, pName, pX, pY, pWidth, pHeight)
            ? PresentationResult.Succeeded()
            : PresentationResult.Failed("Picture data exceeds presentation bounds.");

    public PresentationResult ErasePicture(int pId) =>
        _state.ErasePicture(pId) ? PresentationResult.Succeeded() : PresentationResult.Failed("Picture ID is not present.");
}

public sealed class PresentationStatePlaceholder { }
