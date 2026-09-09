# Changelog

All notable changes to this package are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the version numbers
follow [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-09-09

### Added

- `AnimatedTextBox`, which animates a TextMeshPro label per character from rich-text
  tags and reveals it with a typewriter.
- Eleven built-in effects: `<wave>`, `<shake>`, `<wobble>`, `<jitter>`, `<bounce>`,
  `<swing>`, `<pulse>`, `<rainbow>`, `<tint>`, `<fade>` and `<blink>`, plus `<speed>`
  and `<pause>` for pacing.
- Nine character entrances, from a plain fade to a spin.
- A visual effect editor: build an effect from layers, watch it animate at the size the
  game draws it, and save it as an asset with a tag of its own.
- Support for scripts that write marks above or below a base. A Thai vowel or tone mark
  animates with the consonant it sits on and is revealed at the same moment, and an
  `Applies to` setting can aim an effect at just the marks or just the base.
- A `DialogueSequence` sample and a demo scene.

### Notes

- Effects authored in the project are registered through a library asset, which the
  editor creates in `Assets/TextBoxEnhance Effects` -- outside the package, because a
  package installed from a git URL is read-only.
- Thai and other scripts need a font that has the glyphs. `Assets > TextBox Enhance >
  Create TMP Font Asset` builds one from any font in the project.
