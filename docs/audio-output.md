# Audio Output

Global audio output switching is not implemented in the first milestone.

Planned behavior:

- Enumerate active Windows playback endpoints.
- Highlight the default output.
- Switch the global default output with one tap.
- Control master volume and mute.
- Show a lightweight activity meter.

Implementation should isolate Windows Core Audio and default-device switching behind small interfaces. If `IPolicyConfig` is used, it must remain behind an adapter that can be replaced later.
