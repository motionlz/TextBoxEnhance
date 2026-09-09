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
| `<wave>` | `a` amplitude 0.05, `f` speed 6, `w` radians per character 0.6 | Sine wave travelling along the text |
| `<shake>` | `a` amplitude 0.03, `f` steps per second 25 | Hard stepped rattle — shouting, damage numbers |
| `<wobble>` | `a` amplitude 0.033, `f` speed 1.5 | Smooth Perlin drift — drunk, underwater |
| `<jitter>` | `a` amplitude 0.03 | Re-rolled every frame; more frantic than shake |
| `<bounce>` | `a` height 0.083, `f` speed 6, `w` 0.6 | Characters hop upward in sequence |
| `<swing>` | `a` degrees 4, `f` speed 4, `w` 0.5 | Rocks around the baseline |
| `<pulse>` | `a` amount 0.05, `f` speed 5, `w` 0.4 | Breathes in and out |

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

## Making your own effects without code

**Tools > TextBox Enhance > Text Effect Editor.**

Pick a preset, watch it move, drag the sliders until it feels right, save. The effect
gets a tag of its own and works in any text box straight away.

### An effect is a stack of layers

Each layer moves **one thing** in **one way**:

| Moves | How |
| --- | --- |
| Left/right, up/down, rotation, size, opacity, colour | Sine, Bounce, Drift, Shake, Jitter, Ramp, Blink, a curve you draw, or nothing |

`<wave>` is one layer. `<wobble>` is two — one for each axis. Stack more and they add
together, so a layer that lifts and a layer that recolours become one effect.

Three controls carry most of the work:

- **Distance / Angle / Amount** — how far it travels
- **Speed** — how many times per second
- **Offset per letter** — how far each letter lags the one before it. This is what turns
  a nodding letter into a wave travelling along a word.

**Advanced** reveals the rest: resting position, phase, randomness seed, and whether the
layer runs off the clock or off the character's entrance. That last one is worth knowing
about — set a layer to **Reveal progress** and it plays once as the typewriter reaches
each character, instead of looping forever.

### Colour layers

Three modes: a flat **colour**, a **gradient** the motion sweeps through, or a **hue
sweep** for the rainbow. Gradients use Unity's own gradient editor, so a fire or ice
effect is a couple of colour stops rather than a formula.

### Using your effect

An effect named `sparkle` is written `<sparkle>like this</sparkle>`, and takes the same
attributes as a built-in — `<sparkle a=2>` for twice the travel, `f=` for speed, `w=` for
the per-letter offset. Those scale every layer at once.

Effects live in a **library asset** that the editor keeps up to date and adds to the
build's preloaded assets. If a tag works in the editor and not in a build, the library
is the first thing to check.

## Scripts with combining marks (Thai, and others)

Thai writes a syllable as a consonant plus vowels and tone marks that sit above or below
it with no width of their own. TextMeshPro lays each of those out as a separate
character, so animating per character would give a vowel its own phase and its own
random offset and let it drift off the consonant it belongs to — and the typewriter
would show a bare consonant for a moment before its vowel caught up.

TextBox Enhance groups them. A non-spacing mark animates with the letter it sits on,
pivots on that letter, and is revealed at the same moment. The rule comes from Unicode
rather than from Thai, so Devanagari matras, Arabic and Hebrew vowel points and Latin
combining accents all behave the same way. Text without marks is unaffected — every
character is its own letter.

`TotalCharacters` and `OnCharacterRevealed` count letters as a reader would count them,
not code points, so `"กิน"` is two rather than three.

### You still need a font that has the glyphs

The font TextMeshPro ships with covers Latin and nothing else, so Thai comes out as a
row of empty boxes. Put a font that covers the script into the project, select it, and
use **Assets > TextBox Enhance > Create TMP Font Asset**. It builds a dynamic font asset
— glyphs are rendered as they are first used, so there are no character ranges to pick.

Assign the result to your text, or set it as the default in **Project Settings >
TextMesh Pro > Settings**. The effect editor has its own **Font** field and warns when
the font cannot draw the sample text.

No font is bundled with this package on purpose: fonts are licensed separately from
code. Noto Sans Thai is a common choice and is licensed for redistribution; the Windows
system fonts are not, so copying one into a project you ship is worth checking first.

### Animating the marks on their own

Each layer has an **Applies to** dropdown, and built-in tags take the same choice as a
bare flag:

| Applies to | Tag | What moves |
| --- | --- | --- |
| Whole letter | `<shake>` | The base and its marks together. The default. |
| Base letter only | `<shake base>` | The word slides out from under its marks. |
| Marks only | `<shake marks>` | Tone marks rattle over a word that holds still. |

With **Marks only** each mark gets a phase and a random offset of its own, so two marks
stacked on one consonant come apart rather than moving as a block.

Marks still pivot on the letter they belong to, so a rotation swings a mark in an arc
around its consonant rather than spinning it in place. Stack two layers to combine —
one on the whole letter, one on the marks — and the marks move relative to a word that
is itself moving.

Worth saying plainly: this is for a single word, a title or a damage number. In running
text a tone mark that drifts off its consonant reads as broken rather than as style, and
one that drifts towards the next consonant can be read as belonging to that one instead.

### Why an effect can look different in the tool and in the game

Offsets are measured in **em** — a fraction of the font size — so the same effect moves
further on bigger text. A travel of 0.05 em is 1.8 points on 36pt text and 0.9 points on
18pt text.

The effect editor draws its sample at actual size and prints the travel in points
underneath, so set **Text size** to whatever the game uses and the preview matches.

Two things on the label itself will shrink the motion without the number in the effect
changing:

- **Auto Size** on the TMP component. The effective point size is whatever auto-sizing
  settled on, which in a tight rect can be far below the size shown in the inspector.
- A `<size>` tag inside the text, which changes the point size for the characters it
  covers.

Both are working as intended — the motion stays proportional to the letters — but they
are the usual answer when an effect feels weaker in the game than it did in the tool.
