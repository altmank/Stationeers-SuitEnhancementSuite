# Release check

Do these in order. Stop at the first one that fails.

1. Versions match: the csproj `<Version>`, `Plugin.PluginVersion` and About.xml `<Version>` all say the same number.
2. CHANGELOG.md has an entry for the release. About.xml `<ChangeLog>` holds only the newest version.
3. The protected suites pass (see `tools/tests/protected.txt`).
4. A read-only reviewer reads every player-facing string the release adds or changes, cold, as a player would. That
   covers settings text, slot labels and refusals, About.xml, the README and the CHANGELOG lines. Each string must say
   what the player sees or does, in the game's own terms. Apply the reviewer's rewrites before the tag.
5. Tag and push.
6. Deploy only with the game closed and the game lock held.
7. Publish to the Workshop only with the owner's go. The item is public.
