# Design Thinking, Styles, Themes, and Color Theory

Reusable guidance distilled from GistHub's `docs/design-style-theme-color-theory.md`. Its implementation details, catalog sizes, storage keys, and historical test results are project-specific, not requirements for other apps.

## 1. Understand the task and set priorities

- Identify the user's main activity, content density, existing components, appearance preferences, and required light/dark behavior before choosing decoration.
- Prioritize readability, then visual hierarchy, then consistent controls and interactions, then style expression.
- Keep content surfaces calm; use accents primarily for actions, links, selection, and focus. Preserve readable code fonts, useful wrapping, and normal interaction behavior.
- Treat reference material as design input. Record what was inspected and what was adapted; do not claim inaccessible sources were read or inspiration was an exact reproduction.

## 2. Separate visual style, color theme, and presets

| Concern | Owns |
| --- | --- |
| Visual style | Corners, borders, shadows, heading typography, texture, geometry, and control treatment |
| Color theme | Page and content surfaces, text, accents, and syntax colors |
| Preset, if needed | An intentional combination of style and theme |

- Changing style should preserve the selected palette; changing palette should preserve style. An explicit preset may change both.
- Adapt styles to usable UI treatments. Decoration must not rotate content, replace readable code fonts with ornamental fonts, or change control behavior.
- Preserve existing defaults and preferences. Do not introduce catalogs, selectors, or a theming framework for a one-off visual change.

## 3. Map the palette to semantic roles

Use existing theme tokens and helpers first. When role mapping is needed:

1. Collect the source UI and syntax colors; choose the source background as the page foundation.
2. Select readable foregrounds against their actual backgrounds.
3. Choose card and muted surfaces near the page background's luminance while maintaining readable text.
4. Select a primary accent distinguishable on page, card, and muted surfaces.
5. Choose button labels against their own fills, independently of the page text color; check secondary and destructive roles too.

When the user asks to preserve colors, select solid role colors from the source palette instead of inventing new colors. If no source pairing meets the required contrast, report the constraint and resolve it before claiming success. Palette remapping does not automatically generate complementary or analogous color harmonies.

Transparency, gradients, shadows, glow, and glass overlays blend rendered pixels. Preserving source solid colors does not mean every rendered pixel equals a source hex value.

## 4. Check contrast and meaning

- Use linearized sRGB relative luminance, not raw RGB distance: contrast = `(lighter luminance + 0.05) / (darker luminance + 0.05)`.
- Use 4.5:1 as the normal-text target for body/card/muted text, button labels on their fills, and links on their surfaces. Use 3:1 for required non-text UI cues against adjacent colors; check focus against every relevant surrounding surface.
- Check selected, hover, focus, disabled, error, and destructive states as applicable. Never communicate state or danger through color alone; retain labels, icons, or other clear cues.
- Test actual rendered combinations where overlays or style effects alter the background. A passing token-pair test is not a whole-app accessibility audit.
- If syntax highlighting is present, preserve source text for copy, use language metadata with filename fallback, and show unknown languages as plain text. Check syntax readability separately; passing UI token tests does not validate every syntax token.

## 5. Keep appearance preview reversible

When the product includes an appearance picker:

- Hover and keyboard focus may preview without persisting. Click or Enter commits the choice.
- Escape, dismissal, or leaving the preview should restore the committed appearance. Keep pointer and keyboard behavior consistent.
- Reuse existing preference storage, validate saved values, and handle unavailable storage without breaking the current page.
- Restore defaults according to the product's existing system/light/dark behavior. Browser preferences are origin-specific; keep them separate from content data.

## 6. Verify and report evidence

- Run the narrow existing checks for palette roles, valid/invalid preferences, storage failures, and preview commit/cancel behavior as relevant to the change.
- Inspect representative desktop/mobile views, dialogs, popup fit, long content, keyboard focus, and light/dark combinations. Check rendered overlays and syntax samples when touched.
- Distinguish automated token checks, browser observations, and untested areas such as physical devices, zoom/text scaling, screen readers, and color-vision simulation.
- Document source provenance, adaptation limits, checks actually run, and remaining gaps. Do not carry a source document's past test claims forward as fresh verification or describe all combinations as perfect.
