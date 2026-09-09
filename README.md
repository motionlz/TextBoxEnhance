# TextBox Enhance

Per-character text animation for TextMeshPro, driven by rich-text tags.

![Thai and Latin text animating in a dialogue box](docs/images/demo.gif)

## TL;DR

- Write `The <wave>sea</wave> was <shake>calm</shake>` and the words move. Tags nest,
  stack, and take attributes.
- Eleven effects built in, a typewriter with nine entrances, `<speed>` and `<pause>` for
  pacing.
- Build your own from layers in a visual editor — no code, no recompiling.
- Thai vowels and tone marks stay glued to the consonants they belong to, and can be
  animated on their own when that is the point.
- One component. TextMeshPro's own tags keep working alongside.

## Install

**Window > Package Manager > + > Install package from git URL**:

```
https://github.com/motionlz/TextBoxEnhance.git?path=/Assets/TextBoxEnhance
```

Unity 6000.0+. If the project has never used TextMeshPro, run **Tools > TextBox Enhance
> Import TextMeshPro Resources** once, or every label renders blank.

## Use it

**GameObject > UI > Animated Text Box**, then type into the component, or:

```csharp
var box = GetComponent<AnimatedTextBox>();

box.Play("You found the <rainbow>Sunstone</rainbow>!<pause=0.4> Careful.");
box.OnRevealCompleted.AddListener(() => showContinuePrompt = true);
box.SkipToEnd();   // the click-to-continue half of a dialogue box
```

Distances are in **em**, a fraction of the font size, so an effect looks the same on a
12pt label and a 120pt one.

## Make your own effects

**Tools > TextBox Enhance > Text Effect Editor.** Start from a preset, watch it move at
the size the game draws it, drag until it feels right, give it a tag and save. It works
in any text box straight away — `<sparkle>like this</sparkle>`.

<img src="docs/images/effect-editor.png" alt="The Text Effect Editor window" width="360">

An effect is a stack of **layers**. Each moves one thing — sideways, up and down,
rotation, size, opacity or colour — in one way: sine, bounce, drift, shake, jitter,
ramp, blink, or a curve you draw.

## Thai and other scripts with marks

Thai writes a syllable as a consonant plus vowels and tone marks with no width of their
own, and TextMeshPro lays each one out separately. Animated naively they drift apart.
Here they move as one letter, and the typewriter reveals them together.

When splitting them *is* the effect, aim it:

```
<shake>ทั้งคำสั่น</shake>            whole word
<shake marks>เฉพาะวรรณยุกต์</shake>    tone marks only, over a word that holds still
<shake base>เฉพาะพยัญชนะ</shake>      consonants only, sliding out from under them
```

The rule is Unicode's, so Devanagari matras and Latin accents behave the same way. You
still need a font with the glyphs — **Assets > TextBox Enhance > Create TMP Font Asset**
builds one from any font in the project.

## More

Full tag reference, attributes, API and how to write an effect in C#:
[`Assets/TextBoxEnhance/README.md`](Assets/TextBoxEnhance/README.md).

[MIT](Assets/TextBoxEnhance/LICENSE.md). No font is bundled — fonts are licensed
separately from code.
