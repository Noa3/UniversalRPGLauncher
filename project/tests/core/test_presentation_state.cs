using UniversalRPG.Rm2k.Presentation;
using UniversalRPG.Tests.Framework;

namespace UniversalRPG.Tests.Core;

public partial class TestPresentationState : TestBase
{
    public void Test_MessageWindowStoresBoundedTextAndContinuationLines()
    {
        var presentation = new PresentationState();
        AssertTrue(presentation.ShowMessage("Hello\nWorld"));
        AssertTrue(presentation.MessageVisible);
        AssertEq(presentation.MessageText, "Hello\nWorld");
        AssertFalse(presentation.ShowMessage(new string('x', PresentationState.MaxMessageCharacters + 1)));
    }

    public void Test_MessageWindowCanBeDismissed()
    {
        var presentation = new PresentationState();
        presentation.ShowMessage("Hello");
        presentation.DismissMessage();
        AssertFalse(presentation.MessageVisible);
        AssertEq(presentation.MessageText, "");
    }

    public void Test_PicturesAreBoundedAndReplaceById()
    {
        var presentation = new PresentationState();
        // The natural size, which the magnification scales. A first draft
        // passed the two numbers as the picture's own width and height, and
        // the signature grew a magnification and eight more parameters to say
        // so.
        AssertTrue(presentation.ShowPicture(
            3, "Picture01", 10, 20,
            pFixedToMap: false, pMagnify: 100,
            pTopTransparency: 0, pUseTransparentColor: false,
            pRed: 255, pGreen: 255, pBlue: 255,
            pSaturation: 100, pEffectMode: 0, pEffectPower: 100,
            pNaturalWidth: 100, pNaturalHeight: 80));
        AssertTrue(presentation.ShowPicture(
            3, "Picture02", 11, 21,
            pFixedToMap: false, pMagnify: 100,
            pTopTransparency: 0, pUseTransparentColor: false,
            pRed: 255, pGreen: 255, pBlue: 255,
            pSaturation: 100, pEffectMode: 0, pEffectPower: 100,
            pNaturalWidth: 90, pNaturalHeight: 70));
        AssertEq(presentation.Pictures.Count, 1);
        AssertEq(presentation.Pictures[3].Name, "Picture02");
        AssertFalse(presentation.ShowPicture(
            0, "Invalid", 0, 0,
            pFixedToMap: false, pMagnify: 100,
            pTopTransparency: 0, pUseTransparentColor: false,
            pRed: 255, pGreen: 255, pBlue: 255,
            pSaturation: 100, pEffectMode: 0, pEffectPower: 100,
            pNaturalWidth: 1, pNaturalHeight: 1));
        AssertTrue(presentation.ErasePicture(3, out var hatte));
        AssertTrue(
            hatte,
            "and the erase reports that there was a picture, because a reader"
            + $" cannot tell an erase of nothing from a refusal otherwise; it was {hatte}");
        AssertEq(presentation.Pictures.Count, 0);
    }

    public void Test_ChoicesAreBoundedAndSelectable()
    {
        var presentation = new PresentationState();
        AssertTrue(presentation.ShowChoices(new[] { "Yes", "No" }));
        AssertTrue(presentation.SelectChoice(1));
        AssertEq(presentation.ActiveChoice!.SelectedIndex, 1);
        AssertFalse(presentation.SelectChoice(2));
        AssertFalse(presentation.ShowChoices(new[] { "1", "2", "3", "4", "5" }));
    }

    public void Test_ResetClearsAllPendingPresentationState()
    {
        var presentation = new PresentationState();
        AssertTrue(presentation.ShowMessage("stale"));
        AssertTrue(presentation.ShowChoices(new[] { "Yes", "No" }));
        AssertTrue(presentation.BeginInput(4));
        AssertTrue(presentation.SetInputValue(12));
        AssertTrue(presentation.ShowPicture(
            1, "Picture", 0, 0,
            pFixedToMap: false, pMagnify: 100,
            pTopTransparency: 0, pUseTransparentColor: false,
            pRed: 255, pGreen: 255, pBlue: 255,
            pSaturation: 100, pEffectMode: 0, pEffectPower: 100,
            pNaturalWidth: 16, pNaturalHeight: 16));

        presentation.Reset();

        AssertFalse(presentation.MessageVisible);
        AssertEq(presentation.MessageText, "");
        AssertTrue(presentation.ActiveChoice == null);
        AssertTrue(presentation.PendingInputVariableId == null);
        AssertTrue(presentation.InputValue == null);
        AssertEq(presentation.Pictures.Count, 0);
    }
}
