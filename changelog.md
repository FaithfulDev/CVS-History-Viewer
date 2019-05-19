# CVS History Viewer Changelog

## v1.0.2 (Unreleased)
### Improvements
* (#12) File checking logic is now faster.
* (#14) Diff view now shows "Whitespace" (unchanged lines) in between diff blocks.
* (#16) Line numbers in Diff view are now padded, so that they right-align with each other.
* (#17) Diff view now uses the "Consolas" font, which is monospaced, meaning that each character takes up the same space.
* (#18) App will now start-up a lot faster on big repositories, but commits will be limited to **220**. This limit can be overwritten by using the "limit" keyword (e.g. "limit:400").
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