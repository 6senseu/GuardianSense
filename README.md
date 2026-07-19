<p align="center">
  <img src="assets/banner.png" width="100%">
</p>

<p align="center">

<img src="https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet">

<img src="https://img.shields.io/badge/Platform-Windows-0078D4?style=for-the-badge&logo=windows">

<img src="https://img.shields.io/badge/Status-Early_Development-orange?style=for-the-badge">

</p>

## Features

### Current

* Provider-based analysis pipeline
* SHA-256 file hashing
* Magic byte file type detection
* Authenticode signature verification (publisher, issuer, validity)
* VirusTotal cloud hash reputation lookup
* Local + cloud risk assessment
* Download folder monitoring
* Automatic quarantine of high-risk downloads before they can run
* Quarantine management (restore, permanently delete, history)
* Safe inspection of quarantined files inside Windows Sandbox (never auto-executed)
* Tray notifications when a threat is quarantined
* WPF dashboard (status, scan reports, quarantine, settings)
* Structured JSON analysis reports
* Modular architecture with dependency injection
* Detailed logging

### Planned

* MalwareBazaar integration
* ClamAV support
* YARA rule scanning
* Behavioral analysis / process monitoring
* Machine learning-based detection
* Plugin system
* Installer, auto-updates, GitHub Releases

---

## Project Structure

```text
GuardianSense
│
├── Guardian.Analysis     # File analysis engine (providers, hashing, signatures, risk, cloud reputation)
├── Guardian.Service      # Background watcher (download monitoring, quarantine, reports)
├── Guardian.Shared       # Shared models, settings and storage contracts
├── Guardian.Tray         # System tray application
├── Guardian.Dashboard    # WPF dashboard (status, reports, quarantine, settings)
└── Guardian.Tests        # Unit tests
```

---

## Architecture

```text
DownloadWatcher
        │
        ▼
AnalysisPipeline
        │
        ▼
Providers
 ├── HashProvider
 ├── MagicByteProvider
 ├── CloudReputationProvider (VirusTotal)
 ├── AuthenticodeProvider
 └── RiskProvider
        │
        ▼
Analysis Report ──► ReportStore
        │
        ▼
QuarantineManager (isolates high-risk files before they can run)
        │
        ▼
QuarantineStore ──► Tray notification / Dashboard
```

GuardianSense uses a provider-based architecture, allowing new analysis modules to be added without modifying the core pipeline. The Service, Tray and Dashboard exchange data through a shared `%ProgramData%\Guardian` folder structure (reports, quarantine records, logs, settings) rather than a dedicated IPC channel.

---

## Roadmap

### Phase 1 – Foundation ✅

* Download monitoring
* Provider architecture
* SHA-256 hashing
* Magic byte detection
* Risk assessment
* Logging

### Phase 2 – Trust Verification ✅

* Authenticode
* Certificate validation
* Publisher detection

### Phase 3 – Cloud Intelligence 🚧

* VirusTotal hash reputation ✅
* MalwareBazaar integration

### Phase 4 – Quarantine 🚧

* Automatic quarantine of high-risk files ✅
* Restore / delete / history ✅
* Manual quarantine action for medium-risk files

### Phase 9 – User Interface 🚧

* Tray notifications ✅
* Dashboard (status, reports, quarantine, settings) ✅
* Windows Sandbox inspection ✅

### Future

* Local malware engines (ClamAV, YARA)
* Sandboxed behavioral analysis / process monitoring
* Machine learning
* Plugin ecosystem
* Installer & auto-updates

---

## Building

Requirements:

* .NET 9 SDK
* Windows 10 or Windows 11
* Visual Studio Code
* or
* Visual Studio 2022 or newer
  

Clone the repository:

```bash
git clone https://github.com/<your-username>/GuardianSense.git
```

Build the solution:

```bash
dotnet build
```

Run the tests:

```bash
dotnet test
```

### Running it as one program

During development, each project builds into its own separate `bin/` folder, but
`Guardian.Tray` looks for `Guardian.Service.exe` and `Guardian.Dashboard.exe` right next
to itself (the same layout an installer would produce). Run `publish.ps1` once to collect
all three into a shared `dist/` folder, then start `Guardian.Tray.exe` - it launches
Guardian.Service in the background automatically, shows the tray icon, and offers
"Open Dashboard" and "Start with Windows" from its context menu:

```powershell
.\publish.ps1
.\dist\Guardian.Tray.exe
```

### Running the pieces individually (for development)

```bash
dotnet run --project Guardian.Service      # background watcher only
dotnet run --project Guardian.Dashboard    # UI only
```

To enable VirusTotal cloud reputation checks, get a free API key at
[virustotal.com](https://www.virustotal.com/) and either set it in the Dashboard's
Settings tab or add it to `Guardian.Service/appsettings.json` (`Guardian:VirusTotalApiKey`),
then set `Guardian:CloudReputationEnabled` to `true` and restart the service.

---

## Contributing

Contributions, feature requests, and bug reports are welcome.

If you would like to contribute:

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Open a Pull Request

---

## License

This project is licensed under the Apache License 2.0.

See the `LICENSE` file for details.
