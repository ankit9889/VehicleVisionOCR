# Deployment & Release Checklist
**Version:** 1.0.0

## 1. Pre-Build Validation
- [x] Codebase audited for static analysis warnings (CA/SA rules passed/suppressed).
- [x] `dotnet build -c Release` completes with 0 errors.
- [x] `npm run build` completes with 0 errors.
- [x] Clean Architecture layer constraints are intact.
- [x] All mock endpoints replaced with live EF Core persistence layers.

## 2. Configuration & Dependencies
- [x] `package.json` Semantic Versioning updated.
- [x] `.csproj` package references verified for vulnerable versions (none found).
- [x] Zebra CoreScanner SDK integration tested.
- [x] SQLite WAL mode pragmas applied to connection lifecycle.

## 3. Security Validation
- [x] CORS applied on local ASP.NET endpoints limiting React domains.
- [x] File paths (Logs/DB) relative to AppDomain or AppData securely scoped.
- [x] Zero external network endpoints exposed. Data remains offline.

## 4. Installer Generation
- [x] `electron-builder` configured for Windows (`nsis` target).
- [x] .NET Self-Contained execution enabled (`PublishSingleFile=true`, `IncludeNativeLibrariesForSelfExtract=true`).
- [x] Assets bundled (React `dist` folder + Backend `Release/publish` folder).
- [x] Uninstaller and shortcut rules configured.

## 5. Post-Deployment Verification (Manual)
1. Run `VehicleVisionOCR Setup 1.0.0.exe`.
2. Verify desktop shortcut creation.
3. Launch application via shortcut.
4. Verify backend process spins up invisibly via `electron/main.js` child_process.
5. Plug in Zebra scanner (USB/Bluetooth).
6. Verify UI Dashboard status shifts to `Connected`.
7. Scan vehicle document/tag.
8. Verify UI flashes loading OCR processing indicator.
9. Verify SQLite DB `VehicleScans` table increments by 1 row.
10. Uninstall application and verify cleanup of Program Files directory.
