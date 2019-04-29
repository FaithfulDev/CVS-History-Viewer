# CVS History Viewer Changelog

## v1.0.1 (Released 2019/04/29)
### Bugfix
* (#1) Fixed a crash that were caused when analyzing diff with empty diff lines.
* (#2) Fixed a crash that were caused by files with a state of "dead", that were still present in the directory, when they are first added to the database.
* (#3) Fixed wrong diff colors (green/red) for diff blocks with a "addition" or "deletion" classification.