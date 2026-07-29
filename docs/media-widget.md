# Media Widget

OpenPanel uses Windows global system media sessions. It does not require Spotify OAuth or the Spotify Web API.

## Session Selection

1. A currently playing Spotify session.
2. Any currently playing session.
3. The last session controlled by OpenPanel.
4. A paused Spotify session.
5. Windows' current session, then the first remaining session.

The widget displays source, title, artist, album artwork, playback status, timeline, and only the controls supported by the selected session. Previous, play/pause, next, and seek commands use the corresponding Windows session methods.

Artwork is limited to 2 MB, cached by track identity, and transmitted to WebView2 only on a track change or a 30-second refresh. The UI retains cached artwork between ordinary state updates.

Applications that do not publish a Windows system media session appear as unavailable. Spotify and supported browsers normally expose sessions while media is loaded.
