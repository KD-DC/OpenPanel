# Media Widget

OpenPanel uses Windows global system media sessions. It does not require Spotify OAuth or the Spotify Web API.

## Session Selection

1. A currently playing Spotify session.
2. Any currently playing session.
3. The last session controlled by OpenPanel.
4. A paused Spotify session.
5. Windows' current session, then the first remaining session.

The widget displays source, title, artist, album artwork, playback status, timeline, and only the controls supported by the selected session. When the source publishes them, it also shows album, album artist, subtitle, genre, track number and count, media type, shuffle, repeat, and nonstandard playback rate. Previous, play/pause, next, and seek commands use the corresponding Windows session methods.

These fields come from the existing Windows media session snapshot. They add no dependency, network request, or polling loop. Optional fields remain hidden when a media application does not publish them.

When the selected session reports shuffle control support, the widget shows a shuffle toggle. OpenPanel sends the requested state through Windows GSMTC and updates the active state from the next session snapshot. A rejected command is logged and leaves the displayed state unchanged.

Artwork is limited to 2 MB, cached by track identity, and transmitted to WebView2 only on a track change or a 30-second refresh. The UI retains cached artwork between ordinary state updates.

Applications that do not publish a Windows system media session appear as unavailable. Spotify and supported browsers normally expose sessions while media is loaded.
