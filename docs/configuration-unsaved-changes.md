# Unsaved configuration changes

Task configuration forms, notification settings, today's automatic recovery settings, and administrator settings confirm navigation when their draft differs from the saved configuration.

- **Save and leave** validates the current form and uses its existing save workflow. Navigation continues only after a successful save with no remaining draft changes.
- **Discard changes** leaves the page without saving the draft.
- **Continue editing** keeps the current page and draft. Closing the dialog with Escape has the same effect.

Unchanged controls and controls reverted to their saved values do not prompt. Navigation during an active save remains on the current page. Repeated navigation attempts share a single dialog and a single save. A failed save or validation error retains the draft and displays a message.

Refreshing, closing the tab, and leaving the application use the browser's native unsaved-changes prompt. Browsers control its wording and require prior user interaction. No configuration values, credentials, or destination URLs are included in the application dialog.

Independently saved medal exclusions do not count as unsaved edits. Medal pins, whitelist selections, interaction limits, task times and other form changes remain drafts until explicitly saved.
