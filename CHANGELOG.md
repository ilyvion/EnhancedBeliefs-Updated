# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed

- Error in translation string invocation.
- Removed accidentally left in debug logging.

## [0.3.0] - 2025-08-09

### Added

- Added job fail reasons to the 'complete religious book' work giver so you get an explanation why a given pawn can't do the work to complete the book.

### Changed

- Moved ideoligion opinion information into its own pawn tab.
- The finishing a book job is now much improved; there's an effecter active while the book is being written, providing sound and visual effects and the book is now placed onto the lectern to be worked on.
- Replaced hard-coded English text with translatable text.

### Fixed

- Add a null checks for when calculating a pawn's mood.
- When Royalty was active, base-game patches were not applied due to accidental filename clobbering.
- Bug with being able to access the correct defs.

## [0.2.0] - 2025-08-08

### Fixed

- Conversion ability now actually causes reduction in certainty on targeted pawn.

## [0.1.1] - 2025-08-04

### Changed

- Changed back to original packageId for better future compatibility with original.

## [0.1.0] - 2025-08-04

### Added

- Update mod to work on RimWorld 1.6.

### Changed

- Project layout changed to match how ilyvion works.

[Unreleased]: https://github.com/ilyvion/EnhancedBeliefs-Updated/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/ilyvion/EnhancedBeliefs-Updated/compare/v0.2.0..v0.3.0
[0.2.0]: https://github.com/ilyvion/EnhancedBeliefs-Updated/compare/v0.1.1..v0.2.0
[0.1.1]: https://github.com/ilyvion/EnhancedBeliefs-Updated/compare/v0.1.0..v0.1.1
[0.1.0]: https://github.com/ilyvion/EnhancedBeliefs-Updated/releases/tag/v0.1.0
