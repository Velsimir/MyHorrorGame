# Rules for images inside text

An icon in a line of text is a glyph, like a letter. It must sit on the same baseline as the
letters next to it and must not "jump" when the set changes (keyboard → gamepad). The rules
below are about exactly that.

The Typecraft window is in Russian; button names are given in Russian with an English gloss.

## Canvas

- Standard: a **128×128 px** square, PNG with transparency (8 bits per channel, no premultiplied alpha).
- The artwork goes into the central **104×104 px** area: a 12 px margin on each side stays empty.
  No outline, shadow or glow may reach the margin (the check treats a pixel as used when its
  alpha is above ~0.05).
- The same canvas for every icon in a set. Different sizes within a set = different heights in the line.
- The package's base sets are drawn on other canvases (100 px and larger) — that is fine:
  the size is compared with the prevailing one in the set, not with 128.
- Template: `Documentation~/IconTemplate_128.png` — a blue outline of the margin, a red line
  for the bottom of the artwork, a sample shape inside.

## Anchor and baseline

- The anchor of an icon in text is the **lower-left corner of the canvas**. The bottom of the canvas
  sits exactly on the baseline, the icon starts where the previous character ended and takes
  the canvas width in the line. The sprite pivot does not affect the position in text.
- TMP fits the canvas height to the font's ascent line (the top of the line above the baseline), so
  the 12 px margin gives the same air around the icon at any font size.
- If the artwork should drop below the line (like the letters `g`, `y`), that is a manual fit,
  not an offset of the artwork inside the canvas.

## Check on add

When a PNG is dropped into a set (the «Библиотека» / Library tab), the image is checked: is it
square, does its size match the prevailing one in the set, does the artwork reach the margin.
If something is off, a window opens with the reasons and a preview of the icon inside a line of text:

- «Всё равно добавить» (Add anyway) — add as is;
- «Подогнать» (Fit) — add and open the fit sliders for this icon right away;
- «Отмена» (Cancel) — do not add.

When a set is rebuilt («Пересобрать» / Rebuild, `Rebuild Atlas`, `Rebuild Base Prompt Sets`) the same
checks only write a warning to the console.

## Manual fit

Without rebuilding the atlas, in the set panel (Library tab → set → icon):

- **Offset X / Y** of the icon — a fraction of the icon height; plus moves right and up.
- **Scale** of the icon — from the lower-left corner, 1 by default. The space the icon takes
  in the line grows with it.
- **Set Y offset** — shared by all icons of the set (`IconSet → Build Settings → Vertical Offset`),
  added to the icon offset.

The preview under the sliders shows a line with the icon: the red line is the baseline, the grey
ones are the top and bottom of the line.

## Right and wrong

- Right: a 128×128 canvas, the button drawn in the centre and filling almost the whole 104×104 area,
  12 px of transparent margin around it. In the line the bottom of the button sits on the baseline,
  next to the letters, without overlapping the neighbouring words.
- Wrong: the artwork pressed against the canvas edges, a shadow running into the margin — the icon
  sticks to the neighbouring letters in the line.
- Wrong: a 128×96 canvas — the icon is stretched to the line height and looks squashed
  or elongated next to the others.
- Wrong: the artwork drawn in the lower half of the canvas "so it sits on the line" — the icon
  ends up lower than its neighbours. The position in the line is fixed with the fit, not the image.

## Colour

Two kinds of icons, and each set holds only one kind:

- **Tintable** — a white silhouette on transparent. The colour comes from the text (`<color>`,
  a style). The `Tintable` flag on the entry.
- **Full colour** — a finished image, the text colour does not affect it.

## Names and keys

- File name = glyph name in the atlas. Latin letters, digits, `_`. No spaces or Cyrillic.
- The key in text is built from the name automatically: the prefix and suffix shared by the set
  are cut off, the rest is lower-cased. `PS5_Cross.png` → `cross`, `A_Key_Dark.png` → `a`.
- A key can be added ahead of time, without an image: a placeholder entry in `IconSet`. Text with it
  still builds, and the Typecraft window highlights it as "no art".
- Renaming a file does not break texts as long as the entry key stays the same.

## Where things live

- Sources go into a folder whose name ends with `~` (Unity does not import such folders):
  a source never ends up in a build and costs no import time.
- The generated atlas and `TMP_SpriteAsset` sit next to the `IconSet` asset.
- Rebuild: right-click the `IconSet` → `Rebuild Atlas`. The package's base sets —
  `Tools → Typecraft → Rebuild Base Prompt Sets`.

## Mobile

- The atlas has no mipmaps and no compression: the icons are small, and compression artefacts
  show on them more than the memory saving is worth.
- One set — one atlas, up to 2048 px. Larger — split the set.
