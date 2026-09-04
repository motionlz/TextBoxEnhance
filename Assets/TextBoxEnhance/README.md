# TextBox Enhance

Per-character text animation for TextMeshPro, driven by rich-text tags.

Write `The <wave>sea</wave> was <shake>calm</shake>` into an `AnimatedTextBox` and
the words animate — no timeline, no per-character GameObjects, no separate label
for each effect.

## Quick start

1. **GameObject > UI > Animated Text Box.** This creates a canvas if the scene has
   none, adds a `TextMeshProUGUI` and an `AnimatedTextBox`, and imports the
   TextMeshPro resources if the project is missing them.
2. Type into the component's **Text** field. Effects preview live in the scene view.
3. From code:

```csharp
var box = GetComponent<AnimatedTextBox>();
box.Play("You found the <rainbow>Sunstone</rainbow>!<pause=0.4> Careful.");
box.OnRevealCompleted.AddListener(() => showContinuePrompt = true);
```

## Tag reference

Attributes take either form — `<wave=0.3>` sets the primary attribute,
`<wave a=0.3 f=8 w=0.6>` sets them by name. All distances are in **em**, where 1
is one font size, so an effect looks the same on a 12pt and a 120pt label.

### Motion

| Tag | Attributes | What it does |
| --- | --- | --- |
| `<wave>` | `a` amplitude 0.15, `f` speed 6, `w` radians per character 0.6 | Sine wave travelling along the text |
| `<shake>` | `a` amplitude 0.06, `f` steps per second 25 | Hard stepped rattle — shouting, damage numbers |
| `<wobble>` | `a` amplitude 0.1, `f` speed 1.5 | Smooth Perlin drift — drunk, underwater |
| `<jitter>` | `a` amplitude 0.05 | Re-rolled every frame; more frantic than shake |
| `<bounce>` | `a` height 0.25, `f` speed 6, `w` 0.6 | Characters hop upward in sequence |
| `<swing>` | `a` degrees 12, `f` speed 4, `w` 0.5 | Rocks around the baseline. Alias: `<rotate>` |
| `<pulse>` | `a` amount 0.15, `f` speed 5, `w` 0.4 | Breathes in and out |

`<wiggle>` is an alias for `<wobble>`.

### Colour

| Tag | Attributes | What it does |
| --- | --- | --- |
| `<rainbow>` | `f` cycles/sec 0.35, `w` hue shift per char 0.06, `s` saturation 1, `v` value 1 | Cycles hue along the text |
| `<tint>` | `c` colour, `w` weight 1 | Flat colour. `<tint=#FF8800>` or `<tint c=red>` |
| `<fade>` | `min` 0.25, `max` 1, `f` speed 2, `w` 0.4 | Breathes opacity |
| `<blink>` | `f` blinks/sec 3, `duty` 0.5, `min` off-alpha 0 | Hard on/off |

Colour effects change hue only; alpha is left to the typewriter and `<fade>`, so
they never fight each other.

### Timing

| Tag | What it does |
| --- | --- |
| `<speed=2>fast</speed>` | Multiplies the reveal rate over a span |
| `<pause=0.4>` | Holds for 0.4 s before revealing the next character |

Tags nest and stack: `<wave><rainbow>both at once</rainbow></wave>`.
TextMeshPro's own tags (`<b>`, `<color>`, `<size>`, `<sprite>`) keep working
alongside these.

## Component reference

**Typewriter** — `Characters Per Second` sets the base rate that `<speed>` scales.
Turn `Typewriter` off to show everything at once and use the tags purely as
decoration.

**Character entrance** — how each character arrives: `Fade`, `Pop`, `Grow`, four
slide directions, or `Spin`. `Reveal Duration` is how long one character takes,
`Reveal Ease` shapes it, `Reveal Distance` is the travel for the slides.

**Timing** — `Use Unscaled Time` keeps text moving while `Time.timeScale` is 0,
which is what you want for a pause menu.

### API

```csharp
box.Play("new text");   // replace the text and start revealing
box.Play();             // restart the current text
box.SkipToEnd();        // reveal everything now
box.Rewind();           // hide everything, ready to Play again
box.Pause();            // hold the typewriter; effects keep animating
box.Resume();

box.IsRevealing;        // still typing?
box.RevealedCharacters; // and how far along
box.TotalCharacters;
```

Events: `OnRevealStarted`, `OnCharacterRevealed(int index)` — wire a typing click
to this one — and `OnRevealCompleted`.

## Writing your own effect

Subclass `TextEffect`, tag it, and it is available in every text box. There is no
list to register it in and no asset to create.

```csharp
using TextBoxEnhance;
using UnityEngine;

[TextEffectTag("lean")]
public sealed class LeanEffect : TextEffect
{
    public override void Apply(in TextEffectContext context, TagParams parameters, ref CharacterMod mod)
    {
        float angle = parameters.GetFloat(15f, "a", "angle");
        mod.Rotation += angle;
    }
}
```

`<lean=25>` now works. Effects are stateless singletons — one instance serves
every label — so read per-character state from `context` rather than storing it
on the class. `context.Seed` gives a stable per-character random value, and
`context.RevealProgress` lets an effect react to the typewriter arriving.

## Notes

- Works with both `TextMeshProUGUI` and `TextMeshPro`.
- The component never regenerates the mesh. It restores TextMeshPro's own
  geometry each frame and writes offsets on top, so TMP stays in charge of
  layout and errors cannot compound frame to frame.
- Character ranges are computed by predicting TextMeshPro's character count.
  `<br>` and `<sprite>` count as one character each; other rich-text tags count
  as none. If you need a literal `<` in the text, wrap it in `<noparse>`.
- Setting `.text` on the TMP component directly bypasses the tag parser. Assign
  `AnimatedTextBox.Text` or call `Play(string)` instead.
