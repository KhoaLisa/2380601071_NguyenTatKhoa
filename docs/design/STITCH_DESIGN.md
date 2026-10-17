---
name: Botanical Focus
colors:
  surface: '#e7fff2'
  surface-dim: '#c7e0d2'
  surface-bright: '#e7fff2'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#e1f9ec'
  surface-container: '#dbf4e6'
  surface-container-high: '#d5eee0'
  surface-container-highest: '#d0e8db'
  on-surface: '#0a1f17'
  on-surface-variant: '#414944'
  inverse-surface: '#20352c'
  inverse-on-surface: '#def7e9'
  outline: '#717974'
  outline-variant: '#c1c8c2'
  surface-tint: '#3e6654'
  primary: '#0a3626'
  on-primary: '#ffffff'
  primary-container: '#244d3c'
  on-primary-container: '#91bda7'
  inverse-primary: '#a4d0ba'
  secondary: '#476550'
  on-secondary: '#ffffff'
  secondary-container: '#c9ebd0'
  on-secondary-container: '#4d6b55'
  tertiary: '#402b00'
  on-tertiary: '#ffffff'
  tertiary-container: '#5d4000'
  on-tertiary-container: '#d7ac62'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#bfedd5'
  primary-fixed-dim: '#a4d0ba'
  on-primary-fixed: '#002115'
  on-primary-fixed-variant: '#254e3d'
  secondary-fixed: '#c9ebd0'
  secondary-fixed-dim: '#aecfb4'
  on-secondary-fixed: '#042110'
  on-secondary-fixed-variant: '#304d39'
  tertiary-fixed: '#ffdeaa'
  tertiary-fixed-dim: '#ecc073'
  on-tertiary-fixed: '#271900'
  on-tertiary-fixed-variant: '#5f4100'
  background: '#e7fff2'
  on-background: '#0a1f17'
  surface-variant: '#d0e8db'
typography:
  display-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 40px
    fontWeight: '700'
    lineHeight: 48px
    letterSpacing: -0.02em
  display-lg-mobile:
    fontFamily: Plus Jakarta Sans
    fontSize: 30px
    fontWeight: '700'
    lineHeight: 38px
    letterSpacing: -0.01em
  headline-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 28px
    fontWeight: '600'
    lineHeight: 36px
    letterSpacing: -0.015em
  headline-md:
    fontFamily: Plus Jakarta Sans
    fontSize: 22px
    fontWeight: '600'
    lineHeight: 30px
    letterSpacing: -0.01em
  headline-sm:
    fontFamily: Plus Jakarta Sans
    fontSize: 18px
    fontWeight: '600'
    lineHeight: 26px
    letterSpacing: -0.005em
  body-reading-lg:
    fontFamily: Inter
    fontSize: 17px
    fontWeight: '400'
    lineHeight: 28px
    letterSpacing: -0.005em
  body-md:
    fontFamily: Inter
    fontSize: 15px
    fontWeight: '400'
    lineHeight: 24px
    letterSpacing: 0em
  body-sm:
    fontFamily: Inter
    fontSize: 13px
    fontWeight: '400'
    lineHeight: 20px
    letterSpacing: 0.005em
  label-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '500'
    lineHeight: 20px
    letterSpacing: 0.01em
  label-sm:
    fontFamily: Inter
    fontSize: 12px
    fontWeight: '600'
    lineHeight: 16px
    letterSpacing: 0.02em
  caption:
    fontFamily: Inter
    fontSize: 11px
    fontWeight: '500'
    lineHeight: 14px
    letterSpacing: 0.03em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  gutter: 1.5rem
  gutter-mobile: 1rem
  margin: 2rem
  margin-mobile: 1rem
  space-xs: 0.25rem
  space-sm: 0.5rem
  space-md: 1rem
  space-lg: 1.5rem
  space-xl: 2.25rem
---

## Brand & Style

The design system is engineered for prolonged cognitive endurance and reduced exam anxiety during rigorous 75-minute standardized reading assessments. Rooted in biophilic design principles and refined modern minimalism, it transforms high-stakes test preparation into an intentional, grounding study sanctuary.

### Emotional Demeanor & Personality
- **Serene & Grounded:** Natural forest and sage tones displace standard institutional blues to minimize eye strain and test-taking fatigue.
- **Precision-Driven:** Academic rigor is preserved through disciplined typographic hierarchy, crisp layout structures, and uncluttered reading columns.
- **Calmly Affirming:** Feedback patterns offer clear guidance without alarming the student; urgency is balanced with composure.

### Design Movement: Modern Biophilic Minimalism
The visual language pairs tactile, paper-like warmth with clean digital execution. High-density text surfaces adopt natural ivory backgrounds that mimic book stock, accented by organic, soft-cornered geometry and diffused ambient shadows that evoke gentle, indirect canopy light.

## Colors

The palette balances deep botanical pigments with gentle ivory and sand foundations, ensuring optimal contrast ratios compliant with WCAG AAA standards for sustained reading tasks.

### Functional Palette Applications
- **Surfaces & Canvases:**
  - Base Viewport Background: Warm Ivory (`#F7F5ED`)
  - Exam Reading Paper / Elevated Cards: Pristine Cream (`#FFFEFA`)
  - Subtle Framing / Section Dividers: Natural Sand (`#EDE8DB`)
  - Soft Selection Fills / Highlighting: Mint Mist (`#EDF5EF`) and Soft Sage (`#D9E7DB`)
- **Primary & Deep Accents:**
  - Active States, Primary Actions & Strong Focus: Primary Forest Green (`#244D3C`)
  - High-Contrast Headers & Deep Surface Accents: Deep Forest Green (`#193C30`)
- **Interactive Accents:**
  - Bookmarks, Timer Milestones & Badges: Sunlight Gold (`#E3B76B`)
  - Flagged Questions & Review Prompts: Soft Terracotta (`#C87962`)
- **Typography & Communication:**
  - Primary Content / Reading Passages: `#20352C` (provides an organic, low-glare alternative to pure black)
  - Secondary Guidance / Metadata: `#6C7C72`
- **Feedback & Validation:**
  - Correct Answer Affirmation: Success Green (`#368660`)
  - Warning / Pacing Notice: Warning Orange (`#D49343`)
  - Incorrect Answer / Time Expired: Error Red (`#C75D53`)

## Typography

The typographic strategy pairs the structural elegance of **Plus Jakarta Sans** for section markers, headers, and UI anchors with the neutral clarity of **Inter** for passages and questions.

### Reading Column Optimization
- **Body Reading (`body-reading-lg`):** Configured with an expanded 1.65 line-height ratio to preserve vertical scan tracking during Part 6 and Part 7 multi-paragraph and multi-passage texts.
- **Paragraph Measure:** Reading passage widths are capped between 60 to 72 characters per line on desktop viewports to prevent eye fatigue.
- **Numerical Distinction:** Tabular lining figures are applied globally for score indicators, question indices, and exam stopwatches.

## Layout & Spacing

The layout is built upon an adaptive split-pane framework designed specifically for double and triple reading comprehension passages.

### Responsive Structure & Breakpoints
- **Desktop (1024px+):** Synchronized dual-pane layout. The left pane renders passages on an ivory paper sheet with independent vertical scrolling. The right pane presents fixed-width question sets and multiple-choice trays.
- **Tablet (768px – 1023px):** Collapsible split-pane with a segmented toggle or adjustable draggable split ratio.
- **Mobile (< 768px):** Single-column stacked mode with a sticky question navigator and persistent progress footer. Reading cards utilize edge-to-edge margins with generous internal padding (`space-md`) to maximize vertical real estate.

### Spacing Principles
All spacing adheres strictly to an 8px modular baseline (`space-xs` = 4px, `space-sm` = 8px, `space-md` = 16px, `space-lg` = 24px, `space-xl` = 36px). Component interiors maintain generous white space around dense textual questions to keep cognitive load manageable.

## Elevation & Depth

Visual hierarchy relies on organic tonal layering combined with soft, warm-tinted ambient shadows, rejecting stark artificial drop-shadows.

### Layering Hierarchy
1. **Foundation Level (0):** Natural background canvas (`#F7F5ED`) representing the desk surface.
2. **Reading Paper Level (1):** Clean reading sheets (`#FFFEFA`) resting on the background with a 1px border of `#EDE8DB` and an ambient shadow: `0 4px 20px -2px rgba(32, 53, 44, 0.05)`.
3. **Selected / Interactive Cards Level (2):** Focused answer options and active modal panels elevate with an expanded soft blur: `0 8px 30px -4px rgba(32, 53, 44, 0.08)`.
4. **Floating Overlays & Tooltips Level (3):** Exam status bar, vocabulary lookups, and timer badges float cleanly above content with: `0 12px 36px -6px rgba(25, 60, 48, 0.12)`.

## Shapes

The design system uses a medium-curved corner philosophy (`roundedness: 2`) that balances modern software structure with organic botanical contours.

### Shape Application Guidelines
- **Exam Content Containers & Reading Cards:** Standardized to 16px–20px (`rounded-lg` to `rounded-xl`) to establish a tactile, card-like sensation.
- **Buttons, Option Tiles & Input Fields:** Uniformly shaped at 12px–14px (`rounded-md` to `rounded-lg`) to provide comfortable touch targets on mobile devices.
- **Status Pills, Question Index Circles & Badges:** Fully rounded (`rounded-full`) to contrast against rectangular text blocks.

## Components

### Buttons
- **Primary Action (e.g., Next Section, Submit):** Deep Forest Green (`#193C30`) background, `#FFFEFA` typography, 12px border radius, 48px height for touch ergonomics. Active states transition to `#244D3C`.
- **Secondary / Utility Action (e.g., Flag for Review):** Transparent surface with `#EDE8DB` border, `#20352C` text. Hover introduces Mint Mist (`#EDF5EF`) fill.
- **Accent Action (e.g., Bookmark, Hint):** Soft Ivory background with Sunlight Gold (`#E3B76B`) outline and text accents.

### Answer Selection Cards (TOEIC Multiple Choice)
- **Resting State:** `#FFFEFA` background, 1.5px solid `#EDE8DB` border, 16px corner radius, padding `space-md`. Typography uses `body-md` in `#20352C`. Letter identifiers (A, B, C, D) are enclosed in 32px circular sand discs (`#EDE8DB`).
- **Selected State:** Mint Mist (`#EDF5EF`) background, 2px solid `#244D3C` border. The circular indicator transitions to solid `#244D3C` with crisp white lettering.
- **Review / Post-Exam States:**
  - Correct: Light sage tinted background (`#D9E7DB`), `#368660` border and indicator.
  - Incorrect: Soft muted red wash, `#C75D53` border and indicator.

### Question Index Palette / Question Grid
- Compact 36px circular or rounded-square tokens displaying question numbers 101–200.
  - *Unanswered:* Natural Sand background (`#EDE8DB`) with `#6C7C72` text.
  - *Answered:* Soft Sage Green (`#D9E7DB`) with `#193C30` text.
  - *Flagged for Review:* Soft Terracotta (`#C87962`) top-right dot indicator.
  - *Current Question:* 2px Primary Forest Green ring highlight.

### Text Highlighting & Annotations
- Dual-mode highlighter for long passages:
  - Botanical Sage Highlight: `rgba(143, 175, 150, 0.35)`
  - Amber Focus Highlight: `rgba(227, 183, 107, 0.35)`
- Rounded highlight terminals along line breaks for clean visual rhythm.

### Exam Timer & Progress Ribbon
- Fixed header component housing an unobtrusive countdown clock.
- Digital counter set in `label-md` with monospace tabular lining. Pacing bar transitions through three hues based on remaining time: Primary Forest Green (> 20m), Sunlight Gold (5–20m), and Soft Terracotta (< 5m).
