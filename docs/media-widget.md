# Media Widget

Media session integration is not implemented in the first milestone.

Planned source:

- `GlobalSystemMediaTransportControlsSessionManager`.

Planned behavior:

- Prefer an actively playing Spotify session.
- Fall back to any active Windows media session.
- Retain the last controlled session when nothing is playing.
- Show app source, title, artist, album, playback state, duration, position, and artwork.
- Provide play/pause, previous, next, and seek when supported by the session.

The MVP should not use Spotify Web API or require OAuth.
