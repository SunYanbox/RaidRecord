# AGENTS.md - RaidRecord Mod Development Guide

This document contains essential information for AI assistants working on the RaidRecord mod project.

## Build Commands

### Primary Commands
From the project root (`RaidRecord/`):
- `dotnet build` - Build the project
- `dotnet build --configuration Release` - Build for release

### Post-Build Automation
The project includes MSBuild targets that automatically:
1. Copy the built DLL and database files to `bin/{Configuration}SPT/user/mods/{AssemblyName}/`
2. Create a 7z archive of the SPT folder with versioning (`{AssemblyName}-{Version}.7z`)
3. Optionally copy to a custom SPT installation path if `SPTPath` environment variable is set

To use the SPT copy feature, set the `SPTPath` environment variable to your SPT installation directory before building.

## Code Style

### Language Features
- **Target Framework**: .NET 9.0
- **Nullable References**: Enabled (`<Nullable>enable</Nullable>`)
- **Implicit Usings**: Enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- **Primary Constructors**: Used where appropriate
- **Dependency Injection**: Used throughout (SPTarkov.DI)

### Naming Conventions
- **Public Members**: PascalCase (`public class RaidInfo`)
- **Private Fields**: Underscore prefix with camelCase (`private readonly HttpClient _httpClient`)
- **Parameters & Locals**: camelCase (`string playerName`)
- **Constants**: PascalCase (`public const string DefaultConfigPath`)
- **Methods**: PascalCase (`public void Initialize()`)
- **Properties**: PascalCase (`public int RaidCount { get; set; }`)

### Using Directives Order
1. System namespaces
2. Internal/application namespaces
3. SPTarkov namespaces
4. SuntionCore namespaces

Example from `RaidRecordMod.cs`:
```csharp
using System.IO;
using SPTarkov.DI;
using SPTarkov.Server.Core;
using SPTarkov.Server.Web;
using SuntionCore.SPTExtensions;
```

### File Organization
- **Entry Point**: `RaidRecordMod.cs` - Main mod class
- **Services**: `DataGetterService.cs`, `RecordManager.cs` - Business logic
- **Models**: `RaidInfo.cs` - Data structures
- **Utilities**: `StringUtil.cs`, `Constants.cs` - Helper classes
- **Web UI**: `Home.razor` - Blazor component

### Common Patterns
1. **Service Registration**: Register services in `RaidRecordMod.RegisterServices()`
2. **Configuration Access**: Use `IConfiguration` injected via constructor
3. **Logging**: Use `ILogger` injected via constructor
4. **Async Operations**: Use `async`/`await` with proper cancellation tokens

## Testing

Currently no formal test projects exist in the repository. Testing is done manually through the SPTarkov mod system.

### Important Testing Guidelines for AI Assistants
1. **DO NOT automatically test or run the project** - AI assistants should never attempt to run `dotnet run` or automatically test functionality
2. **Wait for user feedback** - When testing is required, stop output and wait for the user to:
   - Build the mod with `dotnet build`
   - Copy to SPT installation (via `SPTPath` environment variable)
   - Launch SPTarkov server and test functionality
   - Provide feedback on test results
3. **Resume work only after user confirmation** - Continue development tasks only after receiving explicit user feedback on test outcomes

## Linting

No formal linting configuration or CI/CD pipelines are currently configured for this project.

### Code Quality Checks
- **Compiler Warnings**: Treat warnings as errors (not explicitly enabled)
- **Null Safety**: Rely on nullable reference types for null safety
- **Formatting**: Follow existing code conventions

## Development Environment

### JetBrains Rider Settings
The project includes `.idea` directory with:
- **Abbreviations**: SPT (for SPTarkov), UI (for user interface)
- **File Templates**: Standard C# templates

### Dependencies
- **SPTarkov Packages**: Common, DI, Server.Core, Server.Web (v4.0.8)
- **SuntionCore**: Referenced DLLs in `libs/` directory

## Project Structure

```
RaidRecord/
├── RaidRecord.csproj          # Project file
├── RaidRecordMod.cs           # Main mod entry point
├── DataGetterService.cs       # Service for retrieving raid data
├── RaidInfo.cs                # Raid information model
├── RecordManager.cs           # Manages raid record storage
├── StringUtil.cs              # String utility methods
├── Constants.cs               # Constant values
├── Home.razor                 # Blazor web UI component
├── db/                        # Database files
├── wwwroot/                   # Static web assets
└── libs/                      # External DLL dependencies
```

## Notes for AI Assistants

1. **Modding Context**: This is a mod for SPTarkov (Single Player Tarkov) - a fan-made single-player version of Escape from Tarkov
2. **Web UI**: Uses Blazor for the mod's web interface
3. **Database**: Includes JSON key-value pair database files in `db/` directory (stored as JSON files)
4. **Versioning**: Follows semantic versioning (currently v0.6.13)

When making changes:
- Ensure compatibility with SPTarkov v4.0.8 APIs
- Maintain backward compatibility with existing raid data
- Follow the established naming and organizational patterns
- Test with the SPTarkov environment before committing