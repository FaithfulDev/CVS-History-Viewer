# CVS History Viewer Changelog

# v1.2.0 (Unreleased)
### New Features
* (#9) Revision list entries now have a context menu that allows more ways of interacting with the revision.
### Bugfix
* (#42) Fixed issue that prevented correct scans when using a directory referenced by UNC-paths.
* (#45) Commit time in revision head area now shows local time instead of UTC.
* (#47) Changes in file path/name capitalization now no longer causes the file to be added as a new file.

# v1.1.1 (Released 2019/10/21)
### Bugfix
* (#43) Fixed a crash that would appear when the app tries to merge 2 or more diff blocks.

## v1.1.0 (Released 2019/10/20)
### New Features
* (#30) Quick Diff View now got syntax highlighting!
### Improvements
* (#27) Tabs are now converted to spaces (default 4). This can be modified via setting file.
* (#31) UI will now restore the previous selected revision when going back to a previous selected commit.
* (#35) If nothing was found using the search and the search term could be a commit hash without the "commit:" keyword, an automatic follow up search is triggered, treating the search term as a commit hash.
* (#37) Revision entries (bottom-left list) now show the file path in the tooltip.
* (#40) Big search performance improvements due to some database optimizations.
### Bugfix
* (#22) Whitespace setting is now read from settings file. At some point in time there will be a way to change it via the UI, but for now you have to change it in the settings file (see local appdata folder).
* (#23) Search limit is now always applied, not just in the initial load. This should improve search performance on general searches (e.g. no or very few filter) **drastically**!
* (#28) Commit time is now converted to local time. Before it was shown in UTC, the way CVS logs it.
* (#29) Fixed a bug that would reverse diff lines whenever 2 blocks are merged.
* (#38) Double-click in the revision list will now only respond if a revision was targeted. Before it would use whatever revision was previously selected.
* (#39) Fixed a crash that would happened, whenever trying to refresh from a folder that was deleted.

## v1.0.2 (Released 2019/05/21)
### Improvements
* (#7) Ignored files (any CVS related ones) will now only be checked once and then never again.
* (#12) File checking logic is now faster.
* (#14) Diff view now shows "Whitespace" (unchanged lines) in between diff blocks.
* (#16) Line numbers in Diff view are now padded, so that they right-align with each other.
* (#17) Diff view now uses the "Consolas" font, which is monospaced, meaning that each character takes up the same space.
* (#18) App will now start-up a lot faster on big repositories, but commits will be limited to **220**. This limit can be overwritten by using the "limit" keyword (e.g. "limit:400").
* (#19) Commit, author and date are now selectable.
* (#20) Files that have been changed, but have no new commits, will now be checked again in the next refresh (even if no new change was made).
### Bugfix
* (#5) Progress bar is now filling up correctly.
* (#6) "&"-Sign is now also being URL encoded and will no longer break the "Report Issue" URL.
* (#13) Revision/Diff view is now reset, when no search results are found.
* (#15) File, author and commit filter are now connected via "OR".

## v1.0.1 (Released 2019/04/29)
### Bugfix
* (#1) Fixed a crash that were caused when analyzing diff with empty diff lines.
* (#2) Fixed a crash that were caused by files with a state of "dead", that were still present in the directory, when they are first added to the database.
* (#3) Fixed wrong diff colors (green/red) for diff blocks with a "addition" or "deletion" classification.