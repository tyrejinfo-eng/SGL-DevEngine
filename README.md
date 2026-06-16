# SGL DevEngine Beta 1.1.6 - INSTALLER INSTRUCTIONS

## Quick Deploy Version (Recommended for Testing)

**Location**: `SGL DevEngine Windows Quick Deploy Version/`

This folder contains all runtime binaries ready to run immediately:

1. Navigate to the folder
2. Run: `SGLDevEngine.Studio.exe`

**Requirements**:
- Windows 10 (build 19041) or Windows 11
- .NET 8.0 Runtime (download from https://dotnet.microsoft.com/download/dotnet/8.0)
- 500 MB free disk space

## Windows Installer Version

### Building the Installer

The Inno Setup 6 script `SGLDevEngine-Setup-1.1.6.iss` is provided for creating a professional installer.

**Prerequisites**:
- Inno Setup 6 (download from https://jrsoftware.org/isdl.php)

**Build Steps**:

1. **Install Inno Setup 6**
   - Download from https://jrsoftware.org/isdl.php
   - Run installer and complete setup

2. **Compile the Installer**
   ```
   "C:\Program Files (x86)\Inno Setup 6\iscc.exe" "SGLDevEngine-Setup-1.1.6.iss"
   ```

3. **Output**
   - Generated file: `SGLDevEngine-Setup-1.1.6.exe`
   - Location: Same directory as .iss script
   - Size: ~50-100 MB

4. **Distribution**
   - Distribute `SGLDevEngine-Setup-1.1.6.exe` to end users
   - Users run installer and select installation directory
   - Application creates Start Menu shortcuts and desktop icon (optional)
   - Uninstaller available via Control Panel > Programs

### Installer Features

✅ **Automated Installation**
- Single-click setup
- Automatic .NET dependency check (future: integration)
- Start Menu and Desktop shortcuts

✅ **Professional UI**
- Inno Setup modern wizard interface
- System requirements verification
- Post-install launch option

✅ **Uninstall Support**
- Clean uninstallation via Control Panel
- Registry cleanup
- Shortcut removal

✅ **Documentation**
- Embedded audit documentation
- Business plan and technical reference
- Quick start guide

## File Structure

```
Beta 1.1.6/
├── Audit Review/
│   ├── BETA-1.1.6-COMPREHENSIVE-AUDIT.md  (15+ sections)
│   ├── BETA-1.1.6-INDEX.md
│   ├── BETA-1.1.6-REVIEW.md
│   └── BETA-1.1.6-PLAN.md
├── Documents/
│   └── BUSINESS-PLAN.md
├── SGL DevEngine Windows Quick Deploy Version/
│   ├── SGLDevEngine.Studio.exe            (Main application)
│   ├── SGLDevEngine.Studio.dll            (UI assembly)
│   ├── SGLDevEngine.BlueprintRuntime.dll  (Runtime engine)
│   ├── SGLDevEngine.Core.dll              (Core services)
│   ├── Npgsql.dll                         (PostgreSQL driver)
│   ├── MySqlConnector.dll                 (MySQL driver)
│   ├── System.Data.SQLite.dll             (SQLite driver)
│   └── *.pdb                              (Debug symbols)
├── SGL DevEngine Windows Install Version/
│   └── SGLDevEngine-Setup-1.1.6.exe       (Installer - after compilation)
└── SGLDevEngine-Setup-1.1.6.iss           (Inno Setup script source)
```

## System Requirements

### Minimum
- **OS**: Windows 10 (build 19041) or Windows 11
- **RAM**: 4 GB
- **Disk**: 500 MB free
- **Runtime**: .NET 8.0

### Recommended
- **OS**: Windows 11 Pro or Enterprise
- **RAM**: 8+ GB
- **Disk**: 2 GB free
- **GPU**: Dedicated GPU for faster blueprint rendering

## Support

**Documentation**: See `Audit Review/` folder for:
- Comprehensive audit report
- System architecture
- Feature specifications
- API documentation

**Issues**: https://github.com/sgldevengine/sgldevengine/issues

**Email**: support@sgldevengine.dev

---

**Version**: Beta 1.1.6  
**Build Date**: April 9, 2026  
**Status**: Ready for Distribution
