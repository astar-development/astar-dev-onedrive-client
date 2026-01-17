
using AStar.Dev.OneDrive.Client.FromV3;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.FromV3.Services;

public class SyncEngineFormatScanningFolderForDisplayShould
{
    [Theory]
    [InlineData("/drives/abc123/root:/Desktop/Email Attachments", "OneDrive: /Desktop/Email Attachments")]
    [InlineData("/drive/root:/Documents/Work", "OneDrive: /Documents/Work")]
    [InlineData("/me/drive/root:/Pictures", "OneDrive: /me/drive/root:/Pictures")]
    [InlineData("/drives/xyz/root:", "OneDrive: ")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void RemoveGraphApiPrefixesCorrectly(string? input, string? expected)
    {
        var result = SyncEngine.FormatScanningFolderForDisplay(input);
        result.ShouldBe(expected);
    }
}
