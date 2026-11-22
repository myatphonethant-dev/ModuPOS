# /meta-tags — metadata preview and implementation

Use [metatags.io](https://metatags.io/) as a development tool to inspect a live page, preview proposed copy and export metadata. Do not add an app editor, runtime service dependency or database table for this workflow.

`/meta-tags` is a named workflow inside `$dotnet-collin`, not a separately registered native slash command. Example requests:

```text
Use $dotnet-collin /meta-tags for https://example.com/. Show the live preview and suggested tags first; do not change code or DB.
```

```text
Use $dotnet-collin /meta-tags to apply the approved metadata, preserve the URL and logo, commit/push, deploy and verify the live preview.
```

## Inspect and propose

- Read existing metadata and live HTML. For standalone Blazor WASM, crawler-readable tags normally belong in `wwwroot/index.html`; for MVC, Razor Pages or Blazor Web Apps, use the existing server-rendered head/layout and page metadata conventions. Inspect `PageTitle`/`HeadContent` overrides; avoid duplicate tags.
- Collect the real project name, title, description, canonical URL, locale and image URL/MIME/dimensions/alt text. Preserve the shared URL, logo and palette unless a change is requested. Do not add query parameters as an unrequested preview workaround.
- Parse the exact public URL in metatags.io. Check parsing completed and the image loaded. Distinguish imported live metadata from edits made only in the preview tool. If scraping fails, report it and inspect live HTML directly; do not label a manually filled mockup as a successful scrape.
- Show the preview and proposed tags in the user's requested language. Prefer consistent primary/Open Graph/Twitter title and description unless intentionally different copy is needed. Reuse existing image assets when appropriate.
- Inspect generated code: its image URL can still be a metatags.io placeholder even when the preview shows the site's image. Use the verified hosted asset and retain useful existing locale, site-name, image-size, alt-text, canonical, favicon and manifest metadata. Do not invent social account handles or ratings.
- A review-only request ends with the proposal, without editing code, DB or deployment settings. When implementation is already authorized, proceed without adding another approval gate.

## HTML template

Adapt [assets/meta-tags.html](../assets/meta-tags.html). It is HTML head content, not a Razor component or rendering engine. Replace every token and HTML-escape text/attribute values. Prefer absolute public HTTPS canonical/image URLs. Use actual image dimensions and MIME type; 1200×630 JPEG is a useful starting point, not a requirement to convert working assets.

| Token | Value |
|---|---|
| `PROJECT_NAME` | Actual site/app name |
| `PAGE_TITLE` | Concise page title |
| `DESCRIPTION` | Accurate page/app summary |
| `CANONICAL_URL` | Existing canonical URL with intended path/trailing slash |
| `OG_LOCALE` | Actual locale, e.g. `my_MM` or `en_US` |
| `IMAGE_URL` | Public hosted image retaining requested branding |
| `IMAGE_MIME_TYPE` | Served type, e.g. `image/jpeg` or `image/png` |
| `IMAGE_WIDTH`, `IMAGE_HEIGHT` | Actual pixel dimensions |
| `IMAGE_ALT` | Meaningful preview-image description |

The approved KyarKwat example uses `KyarKwat — မြန်မာ့ရိုးရာ ကျားကွက်`, canonical `https://kyarkwat.vercel.app/`, locale `my_MM`, and the existing Tiger + ဒိုးပြား JPEG at `/branding/social-card-tiger.jpg` (1200×630). Its description is:

> မြန်မာ့ရိုးရာ ကျားနိုင် / ကျားကုန်ကို Browser မှာ ကစားပါ။ Easy / Medium / Hard AI နဲ့ဖြစ်စေ၊ နှစ်ယောက်ဖြစ်စေ ကစားနိုင်ပါတယ်။

These substitutions are an example, not defaults for other apps.

## Apply and verify

- Replace tags in their current owner, keeping them in initial HTML without requiring WASM/JavaScript startup. Do not change unrelated behavior, DB, security settings or branding.
- Verify consistent title/description, canonical URL, image URL/MIME/dimensions/alt text, no generator placeholders, and favicon/manifest references. Use the project's build/deploy workflow when authorized; use Conventional Commits and report actual push/deployment results.
- After deployment, fetch the live HTML/image, then reparse the exact URL in metatags.io without manually filling the proposed values. Confirm live title/description and image are displayed; report the checks actually run.
- metatags.io previews are simulations, not evidence of actual Facebook/Telegram rendering or cache resets. Distinguish user-agent probes from real platform requests.
- For persistent platform-only failures, use a coordinated recrawl and available request/security logs with a positive control. A refresh-success message, absent runtime rows or CDN `HIT` does not prove success or a cause. Stop speculative metadata/cache/security changes when evidence no longer supports an origin fix; report the unresolved boundary. Do not enable paid monitoring or contact support on the user's behalf without authorization.
