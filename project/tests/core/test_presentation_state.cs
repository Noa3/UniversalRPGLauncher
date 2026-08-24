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
        AssertTrue(presentation.ShowPicture(3, "Picture01", 10, 20, 100, 80));
        AssertTrue(presentation.ShowPicture(3, "Picture02", 11, 21, 90, 70));
        AssertEq(presentation.Pictures.Count, 1);
        AssertEq(presentation.Pictures[3].Name, "Picture02");
        AssertFalse(presentation.ShowPicture(0, "Invalid", 0, 0, 1, 1));
        AssertTrue(presentation.ErasePicture(3));
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
}
