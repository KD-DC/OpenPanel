# Hardware Notes

Primary target:

- ASUS ProArt Display PA147CDV.
- 1920 x 550 resolution.
- 14-inch 32:9 touch display.
- 60 Hz refresh rate.
- HDMI 1.4 and USB-C DisplayPort Alt Mode.

The display service currently prefers any connected display reporting 1920 x 550. If no matching display is found, the app falls back to the primary display so the window does not get lost.

Manual display selection, persisted display hints, and disconnected-display recovery are planned after the shell is stable.
