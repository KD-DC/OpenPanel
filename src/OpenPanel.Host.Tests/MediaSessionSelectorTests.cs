using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPanel.Host.Services;

namespace OpenPanel.Host.Tests;

[TestClass]
public sealed class MediaSessionSelectorTests
{
    [TestMethod]
    public void PrefersPlayingSpotifyOverOtherPlayingSessions()
    {
        MediaSessionCandidate[] candidates =
        [
            new("chrome.exe", true),
            new("Spotify.exe", true)
        ];

        var selected = MediaSessionSelector.SelectIndex(candidates, null, "chrome.exe");

        Assert.AreEqual(1, selected);
    }

    [TestMethod]
    public void PrefersAnyPlayingSessionWhenSpotifyIsPaused()
    {
        MediaSessionCandidate[] candidates =
        [
            new("Spotify.exe", false),
            new("chrome.exe", true)
        ];

        var selected = MediaSessionSelector.SelectIndex(candidates, "Spotify.exe", null);

        Assert.AreEqual(1, selected);
    }

    [TestMethod]
    public void RetainsLastSessionWhenNothingIsPlaying()
    {
        MediaSessionCandidate[] candidates =
        [
            new("Spotify.exe", false),
            new("chrome.exe", false)
        ];

        var selected = MediaSessionSelector.SelectIndex(
            candidates,
            "chrome.exe",
            "Spotify.exe");

        Assert.AreEqual(1, selected);
    }
}
