# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-07-08
### Added
- **Electron Shell:** Packaged the React frontend and ASP.NET Core backend into a single unified desktop application installer.
- **Auto Update Framework:** Base architecture prepared for offline update packages and MSI installers via `electron-builder` NSIS configuration.
- **Scanner Discovery:** Zero-configuration Zebra scanner plug-and-play capability implemented via CoreScanner SDK COM interoperability.
- **Tesseract OCR:** Configured OpenCV pre-processing and Tesseract extraction.
- **Diagnostic Export:** Automatic zip file log archival capabilities via Serilog rolling files.
- **SQLite Database:** Local persistence with WAL journaling optimization for zero-contention UI reads during background hardware operations.
- **React Frontend:** Zustand state management and React Query integrations for real-time local polling.

### Changed
- Replaced mocked hardware scanner triggers with real `CoreScanner_PNPEvent` handlers.
- Integrated background queue decoupling for image processing.

### Fixed
- Addressed database locking mechanisms (enabled Write-Ahead Logging).
- Secured local app configuration scopes.
