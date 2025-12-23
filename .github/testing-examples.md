# Testing Patterns Reference

**Companion to**: copilot-instructions-condensed.md

## ReactiveUI ViewModel Testing

### Basic Property Notification
```csharp
public class ThemeServiceShould
{
    [Fact]
    public void RaisePropertyChangedWhenThemeChanges()
    {
        var sut = new ThemeService();
        var propertyChanged = false;
        sut.PropertyChanged += (s, e) => 
        {
            if (e.PropertyName == nameof(ThemeService.CurrentTheme))
                propertyChanged = true;
        };

        sut.CurrentTheme = "Dark";

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void NotRaisePropertyChangedWhenSettingSameValue()
    {
        var sut = new ThemeService { CurrentTheme = "Light" };
        var changeCount = 0;
        sut.PropertyChanged += (s, e) => changeCount++;

        sut.CurrentTheme = "Light";

        changeCount.ShouldBe(0);
    }
}
```

### Complex ViewModel with Observables
```csharp
public class MainWindowViewModelShould
{
    private static MainWindowViewModel CreateTestViewModel()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        ISyncEngine mockSync = Substitute.For<ISyncEngine>();
        ITransferService mockTransfer = Substitute.For<ITransferService>();
        ISettingsService mockSettings = Substitute.For<ISettingsService>();
        ILogger<MainWindowViewModel> mockLogger = Substitute.For<ILogger<MainWindowViewModel>>();
        
        // CRITICAL: Stub observables
        Subject<SyncProgress> syncProgress = new();
        Subject<SyncProgress> transferProgress = new();
        
        mockSettings.Load().Returns(new UserPreferences());
        mockSync.Progress.Returns(syncProgress);
        mockTransfer.Progress.Returns(transferProgress);
        
        return new MainWindowViewModel(mockAuth, mockSync, mockTransfer, mockSettings, mockLogger);
    }

    [Fact]
    public void UpdateSyncStatusFromProgress()
    {
        var vm = CreateTestViewModel();
        // Test implementation...
    }
}
```

## Theory/InlineData Consolidation

### Before (Redundant)
```csharp
[Fact]
public void MapLightThemeTo1() 
{
    var result = _mapper.MapTheme("Light");
    result.ShouldBe(1);
}

[Fact]
public void MapDarkThemeTo2()
{
    var result = _mapper.MapTheme("Dark");
    result.ShouldBe(2);
}
```

### After (Consolidated)
```csharp
[Theory]
[InlineData("Light", 1)]
[InlineData("Dark", 2)]
[InlineData("Auto", 0)]
public void MapThemeToCorrectIndex(string theme, int expectedIndex)
{
    var result = _mapper.MapTheme(theme);
    result.ShouldBe(expectedIndex);
}
```

## MockFileSystem Patterns

### File Operations
```csharp
public class FileServiceShould
{
    [Fact]
    public void SaveDataToFile()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory(@"C:\Data");
        var sut = new FileService(fileSystem);

        sut.Save("test content", @"C:\Data\file.txt");

        var savedContent = fileSystem.GetFile(@"C:\Data\file.txt").TextContents;
        savedContent.ShouldBe("test content");
    }

    [Fact]
    public void ThrowWhenDirectoryDoesNotExist()
    {
        var fileSystem = new MockFileSystem();
        var sut = new FileService(fileSystem);

        Exception? ex = Record.Exception(() => 
            sut.Save("content", @"C:\NonExistent\file.txt"));

        ex.ShouldNotBeNull();
        ex.ShouldBeOfType<DirectoryNotFoundException>();
    }
}
```

## Type Converter Testing

### Round-Trip Pattern
```csharp
public class SqliteTypeConvertersShould
{
    [Fact]
    public void ConvertDateTimeOffsetToTicksAndBack()
    {
        DateTimeOffset original = new(2024, 12, 23, 15, 30, 45, TimeSpan.FromHours(5));

        var ticks = (long)SqliteTypeConverters.DateTimeOffsetToTicks.ConvertToProvider(original)!;
        var roundTrip = (DateTimeOffset)SqliteTypeConverters.DateTimeOffsetToTicks.ConvertFromProvider(ticks)!;

        roundTrip.ToUniversalTime().ShouldBe(original.ToUniversalTime());
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1.00, 100)]
    [InlineData(99.99, 9999)]
    public void ConvertDecimalsToCents(decimal value, long expectedCents)
    {
        var cents = (long)SqliteTypeConverters.DecimalToCents.ConvertToProvider(value)!;
        cents.ShouldBe(expectedCents);
    }
}
```

## Configuration Testing

### Settings Update Pattern
```csharp
public class WindowSettingsShould
{
    [Fact]
    public void UpdateAllPropertiesFromOtherInstance()
    {
        var sut = new WindowSettings();
        var source = new WindowSettings 
        { 
            WindowX = 100, 
            WindowY = 200, 
            WindowWidth = 1920, 
            WindowHeight = 1080 
        };

        var result = sut.Update(source);

        result.WindowX.ShouldBe(100);
        result.WindowY.ShouldBe(200);
        result.ShouldBe(sut); // Fluent API
    }
}
```

## Coordinator/Handler Testing

### Initialization Pattern
```csharp
public class MainWindowCoordinatorShould
{
    [Fact]
    public void LoadAndApplyUserPreferencesWhenInitializing()
    {
        ISettingsService mockSettings = Substitute.For<ISettingsService>();
        IThemeService mockTheme = Substitute.For<IThemeService>();
        IWindowValidator mockValidator = Substitute.For<IWindowValidator>();
        var prefs = new UserPreferences();
        mockSettings.Load().Returns(prefs);
        var sut = new MainWindowCoordinator(mockSettings, mockTheme, mockValidator);
        
        IWindow mockWindow = Substitute.For<IWindow>();
        var vm = CreateTestViewModel();

        sut.Initialize(mockWindow, vm);

        mockSettings.Received(1).Load();
        mockTheme.Received(1).ApplyTheme(prefs);
        vm.UserPreferences.ShouldBe(prefs);
    }
}
```

## Async Testing

### Basic Async
```csharp
[Fact]
public async Task SaveDataAsync()
{
    var sut = new DataService();

    await sut.SaveAsync("data");

    // Assertions...
}
```

### With Cancellation
```csharp
[Fact]
public async Task CancelOperationWhenTokenCancelled()
{
    var sut = new SyncService();
    using var cts = new CancellationTokenSource();
    var task = sut.SyncAsync(cts.Token);
    
    cts.Cancel();

    await Should.ThrowAsync<OperationCanceledException>(async () => await task);
}
```

## Multi-Replace Examples

### Converting Types to var
```csharp
// Use multi_replace_string_in_file for efficiency:
[
  {
    "explanation": "Convert result to var in Test1",
    "filePath": "test/MyTests.cs",
    "oldString": "    public void Test1()\n    {\n        string result = GetValue();",
    "newString": "    public void Test1()\n    {\n        var result = GetValue();"
  },
  {
    "explanation": "Convert result to var in Test2",
    "filePath": "test/MyTests.cs",
    "oldString": "    public void Test2()\n    {\n        int count = GetCount();",
    "newString": "    public void Test2()\n    {\n        var count = GetCount();"
  }
]
```

### Updating Mock Patterns
```csharp
[
  {
    "explanation": "Update mock setup to use Subject",
    "filePath": "test/ViewModelTests.cs",
    "oldString": "        mockSync.Progress.Returns(null);",
    "newString": "        Subject<SyncProgress> subject = new();\n        mockSync.Progress.Returns(subject);"
  }
]
```
