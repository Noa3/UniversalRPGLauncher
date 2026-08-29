using System;
using System.Collections.Generic;

namespace UniversalRPG.Rm2k.Presentation;

public sealed class ChoiceState
{
    public IReadOnlyList<string> Options { get; init; } = Array.Empty<string>();
    public int SelectedIndex { get; private set; } = -1;

    public bool Select(int pIndex)
    {
        if (pIndex < 0 || pIndex >= Options.Count) return false;
        SelectedIndex = pIndex;
        return true;
    }
}

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
    public const int MaxChoices = 4;
    public const int MaxChoiceCharacters = 128;
    public const int MaxPictureNameCharacters = 256;
    public const int MaxPictureDimension = 8192;

    public bool MessageVisible { get; private set; }
    public string MessageText { get; private set; } = "";
    public ChoiceState? ActiveChoice { get; private set; }
    public int? PendingInputVariableId { get; private set; }
    public int? InputValue { get; private set; }
    public Dictionary<int, PictureState> Pictures { get; } = new();

    public void Reset()
    {
        MessageVisible = false;
        MessageText = "";
        ActiveChoice = null;
        PendingInputVariableId = null;
        InputValue = null;
        Pictures.Clear();
    }

    public bool BeginInput(int pVariableId)
    {
        if (pVariableId <= 0) return false;
        PendingInputVariableId = pVariableId;
        InputValue = null;
        return true;
    }

    public bool SetInputValue(int pValue)
    {
        if (PendingInputVariableId == null) return false;
        InputValue = pValue;
        return true;
    }

    public bool TryConsumeInput(out int pVariableId, out int pValue)
    {
        if (PendingInputVariableId is not int variableId || InputValue is not int value)
        {
            pVariableId = 0;
            pValue = 0;
            return false;
        }
        pVariableId = variableId;
        pValue = value;
        PendingInputVariableId = null;
        InputValue = null;
        return true;
    }

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
        ActiveChoice = null;
    }

    public bool ShowChoices(IEnumerable<string> pOptions)
    {
        var options = new List<string>();
        foreach (var option in pOptions)
        {
            if (options.Count >= MaxChoices || string.IsNullOrWhiteSpace(option) || option.Length > MaxChoiceCharacters)
            {
                return false;
            }
            options.Add(option);
        }
        if (options.Count == 0) return false;
        ActiveChoice = new ChoiceState { Options = options };
        return true;
    }

    public bool SelectChoice(int pIndex) => ActiveChoice?.Select(pIndex) == true;

    public void ClearChoice()
    {
        ActiveChoice = null;
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
