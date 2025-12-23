# OneDrive Client - .NET 10 Development Instructions

> **Extends**: [copilot-instructions-starter.md](./copilot-instructions-starter.md)  
> **Project**: Avalonia UI OneDrive Sync Client

## Quick Reference

| Aspect | Standard |
|--------|----------|
| Framework | .NET 10, C# 14 |
| UI | Avalonia UI with ReactiveUI |
| Testing | xUnit V3, Shouldly, NSubstitute |
| Architecture | Folder-based (Auth, Views, ViewModels, Services) |
| Code Style | `var` for locals, warnings as errors, nullable enabled |

---

## Project-Specific Guidelines

### Architecture
- Single entry point: `MainWindow.axaml`
- Folder-based structure: Authentication, Views, ViewModels, OneDriveServices, SettingsAndPreferences
- NuGet packages for cross-cutting concerns
- Apply DDD/Repository/CQRS patterns where applicable

### Coding Standards
Inherits all standards from `copilot-instructions-starter.md`, plus:
- **ReactiveUI patterns**: Property notifications with `this.RaiseAndSetIfChanged`
- **Observable subscriptions**: Always dispose with `.DisposeWith(_disposables)`

---

## Testing: ReactiveUI & Avalonia Specifics

### Priority Guide
1. **HIGH**: Business logic, services, data mappers, file I/O, ViewModels property notifications
2. **MEDIUM**: UI coordination, configuration loading
3. **LOW**: UI controls, `Application.Current` dependencies, complex reactive streams

### ReactiveUI ViewModels

**Testing Property Notifications**
- Verify notifications fire when values change
- Verify notifications DON'T fire when setting same value
- Test ObservableCollections independently

**Complex Constructors Pattern**
```csharp
private static MainWindowViewModel CreateTestViewModel()
{
    IAuthService mockAuth = Substitute.For<IAuthService>();
    ISyncEngine mockSync = Substitute.For<ISyncEngine>();
    ILogger<MainWindowViewModel> mockLogger = Substitute.For<ILogger<MainWindowViewModel>>();
    
    // CRITICAL: Stub observables to prevent NullReferenceException
    Subject<SyncProgress> syncProgress = new();
    mockSync.Progress.Returns(syncProgress);
    
    return new MainWindowViewModel(mockAuth, mockSync, mockLogger);
}
```

**Key Pattern**: ViewModels subscribing to observables in constructors need all `IObservable<T>` dependencies stubbed with `Subject<T>`.

### MockFileSystem Pattern
```csharp
var fileSystem = new MockFileSystem();
fileSystem.AddDirectory(@"C:\Path\To\Dir");
fileSystem.AddFile(@"C:\Path\To\File.txt", new MockFileData("content"));
```
?? Always create directories before adding files.

### Efficient Test Editing
For bulk changes (e.g., converting types to `var`):
```csharp
// Use multi_replace_string_in_file with 3-5 lines context
[
  {"explanation": "Convert result to var", 
   "oldString": "string result = GetValue();\n...", 
   "newString": "var result = GetValue();\n..."}
]
```

---

## Test Execution

### Standard Commands
```bash
# Bypass Fine Code Coverage issues
dotnet test --settings .runsettings --no-build

# Specific test class
dotnet test --filter "FullyQualifiedName~ClassName"
```

### Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| FCC path caching | Create `.runsettings` with empty DataCollectors |
| NullRef in ViewModel constructor | Stub all `IObservable<T>` with `Subject<T>` |
| File duplication | Check for duplicate closing braces |
| Build ?, tests ? | Run `dotnet build` then `dotnet test --no-build` |

---

## SonarLint Suppressions

```csharp
#pragma warning disable S1075 // URIs should not be hardcoded - Required by MSAL for OAuth
private const string RedirectUri = "http://localhost";
#pragma warning restore S1075
```

Common project suppressions:
- **S1075**: OAuth redirect URIs
- **S6667**: Intentional exception non-logging

---

## Documentation Requirements

- **XML comments**: ALL public APIs in production code
- **NO documentation**: Test classes/methods
- **Interface implementation**: Use `/// <inheritdoc/>`

Example:
```csharp
/// <summary>
/// Manages synchronization between local storage and OneDrive.
/// </summary>
/// <remarks>
/// This service coordinates file transfers, handles conflict resolution,
/// and maintains sync state persistence.
/// </remarks>
public interface ISyncEngine { }

// Implementation
/// <inheritdoc/>
public sealed class SyncEngine : ISyncEngine { }
```

---

## Test Examples Repository

**See**: [Testing Examples](./testing-examples.md) for comprehensive patterns including:
- ReactiveUI ViewModel patterns
- Observable subscription testing
- MockFileSystem usage
- Theory/InlineData consolidation
- Helper method patterns

---

## Commit Standards

- Semantic format: `feature:`, `fix:`, `refactor:`, `test:`
- Ensure tests pass before pushing
- Meaningful commit messages (not "updates" or "fixes")

---

## Quick Troubleshooting

### Test Failures
1. Check observable stubbing in ViewModel tests
2. Verify MockFileSystem directory creation
3. Confirm using statements match namespaces
4. Check for file content duplication

### Build Warnings
1. Convert explicit types to `var` (except test mocks)
2. Add `#pragma` suppressions with justification
3. Prefix private fields with `_`
4. Use discard `_` for unused variables

### Performance
- Build frequently during test writing
- Use `--no-build` for faster test iterations
- Run specific test classes when debugging
