using System.Reactive.Subjects;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using AStar.Dev.OneDrive.Client.Services;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.SettingsAndPreferences;
using AStar.Dev.OneDrive.Client.Theme;
using AStar.Dev.OneDrive.Client.ViewModels;
using Avalonia;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.ViewModels;

public class MainWindowCoordinatorShould
{
    private readonly ISettingsAndPreferencesService _mockSettingsService;
    private readonly IThemeService _mockThemeService;
    private readonly IWindowPositionValidator _mockValidator;
    private readonly MainWindowCoordinator _sut;

    public MainWindowCoordinatorShould()
    {
        _mockSettingsService = Substitute.For<ISettingsAndPreferencesService>();
        _mockThemeService = Substitute.For<IThemeService>();
        _mockValidator = Substitute.For<IWindowPositionValidator>();
        _sut = new MainWindowCoordinator(_mockSettingsService, _mockThemeService, _mockValidator);
    }

    private static MainWindowViewModel CreateMockViewModel()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        ISyncEngine mockSync = Substitute.For<ISyncEngine>();
        ISyncRepository mockRepo = Substitute.For<ISyncRepository>();
        ITransferService mockTransfer = Substitute.For<ITransferService>();
        ISettingsAndPreferencesService mockSettings = Substitute.For<ISettingsAndPreferencesService>();
        ILogger<MainWindowViewModel> mockLogger = Substitute.For<ILogger<MainWindowViewModel>>();

        Subject<SyncProgress> syncProgressSubject = new();
        Subject<SyncProgress> transferProgressSubject = new();

        mockSettings.Load().Returns(new UserPreferences());
        mockSync.Progress.Returns(syncProgressSubject);
        mockTransfer.Progress.Returns(transferProgressSubject);

        return new MainWindowViewModel(mockAuth, mockSync, mockRepo, mockTransfer, mockSettings, mockLogger);
    }

    [Fact]
    public void LoadUserPreferencesWhenInitializing()
    {
        IWindowPositionable mockWindow = Substitute.For<IWindowPositionable>();
        MainWindowViewModel mockViewModel = CreateMockViewModel();
        UserPreferences testPreferences = new();
        _mockSettingsService.Load().Returns(testPreferences);
        _mockValidator.IsValidSize(Arg.Any<double>(), Arg.Any<double>()).Returns(false);
        _mockValidator.IsValidPosition(Arg.Any<int>(), Arg.Any<int>()).Returns(false);

        _sut.Initialize(mockWindow, mockViewModel);

        _mockSettingsService.Received(1).Load();
    }

    [Fact]
    public void ApplyThemePreferenceWhenInitializing()
    {
        IWindowPositionable mockWindow = Substitute.For<IWindowPositionable>();
        MainWindowViewModel mockViewModel = CreateMockViewModel();
        UserPreferences testPreferences = new();
        _mockSettingsService.Load().Returns(testPreferences);
        _mockValidator.IsValidSize(Arg.Any<double>(), Arg.Any<double>()).Returns(false);
        _mockValidator.IsValidPosition(Arg.Any<int>(), Arg.Any<int>()).Returns(false);

        _sut.Initialize(mockWindow, mockViewModel);

        _mockThemeService.Received(1).ApplyThemePreference(testPreferences);
    }

    [Fact]
    public void SetViewModelUserPreferencesWhenInitializing()
    {
        IWindowPositionable mockWindow = Substitute.For<IWindowPositionable>();
        MainWindowViewModel mockViewModel = CreateMockViewModel();
        UserPreferences testPreferences = new();
        _mockSettingsService.Load().Returns(testPreferences);
        _mockValidator.IsValidSize(Arg.Any<double>(), Arg.Any<double>()).Returns(false);
        _mockValidator.IsValidPosition(Arg.Any<int>(), Arg.Any<int>()).Returns(false);

        _sut.Initialize(mockWindow, mockViewModel);

        mockViewModel.UserPreferences.ShouldBe(testPreferences);
    }

    [Fact]
    public void SetViewModelSyncStatusFromLastActionWhenInitializing()
    {
        IWindowPositionable mockWindow = Substitute.For<IWindowPositionable>();
        MainWindowViewModel mockViewModel = CreateMockViewModel();
        UserPreferences testPreferences = new();
        testPreferences.UiSettings.Update(new UiSettings { LastAction = "Test Action" });
        _mockSettingsService.Load().Returns(testPreferences);
        _mockValidator.IsValidSize(Arg.Any<double>(), Arg.Any<double>()).Returns(false);
        _mockValidator.IsValidPosition(Arg.Any<int>(), Arg.Any<int>()).Returns(false);

        _sut.Initialize(mockWindow, mockViewModel);

        mockViewModel.SyncStatusMessage.ShouldBe("Test Action");
    }

    [Fact]
    public void ApplyWindowSizeWhenValidationPasses()
    {
        IWindowPositionable mockWindow = Substitute.For<IWindowPositionable>();
        MainWindowViewModel mockViewModel = CreateMockViewModel();
        UserPreferences testPreferences = new();
        testPreferences.WindowSettings.Update(new WindowSettings { WindowWidth = 1024, WindowHeight = 768 });
        _mockSettingsService.Load().Returns(testPreferences);
        _mockValidator.IsValidSize(1024, 768).Returns(true);
        _mockValidator.IsValidPosition(Arg.Any<int>(), Arg.Any<int>()).Returns(false);

        _sut.Initialize(mockWindow, mockViewModel);

        mockWindow.Width.ShouldBe(1024);
        mockWindow.Height.ShouldBe(768);
    }

    [Fact]
    public void NotApplyWindowSizeWhenValidationFails()
    {
        IWindowPositionable mockWindow = Substitute.For<IWindowPositionable>();
        mockWindow.Width = 800;
        mockWindow.Height = 600;
        MainWindowViewModel mockViewModel = CreateMockViewModel();
        UserPreferences testPreferences = new();
        testPreferences.WindowSettings.Update(new WindowSettings { WindowWidth = -100, WindowHeight = -100 });
        _mockSettingsService.Load().Returns(testPreferences);
        _mockValidator.IsValidSize(-100, -100).Returns(false);
        _mockValidator.IsValidPosition(Arg.Any<int>(), Arg.Any<int>()).Returns(false);

        _sut.Initialize(mockWindow, mockViewModel);

        mockWindow.Width.ShouldBe(800);
        mockWindow.Height.ShouldBe(600);
    }

    [Fact]
    public void ApplyWindowPositionWhenValidationPasses()
    {
        IWindowPositionable mockWindow = Substitute.For<IWindowPositionable>();
        MainWindowViewModel mockViewModel = CreateMockViewModel();
        UserPreferences testPreferences = new();
        testPreferences.WindowSettings.Update(new WindowSettings { WindowX = 100, WindowY = 200 });
        _mockSettingsService.Load().Returns(testPreferences);
        _mockValidator.IsValidSize(Arg.Any<double>(), Arg.Any<double>()).Returns(false);
        _mockValidator.IsValidPosition(100, 200).Returns(true);

        _sut.Initialize(mockWindow, mockViewModel);

        mockWindow.Position.ShouldBe(new PixelPoint(100, 200));
    }

    [Fact]
    public void NotApplyWindowPositionWhenValidationFails()
    {
        IWindowPositionable mockWindow = Substitute.For<IWindowPositionable>();
        PixelPoint originalPosition = new(50, 75);
        mockWindow.Position = originalPosition;
        MainWindowViewModel mockViewModel = CreateMockViewModel();
        UserPreferences testPreferences = new();
        testPreferences.WindowSettings.Update(new WindowSettings { WindowX = -500, WindowY = -500 });
        _mockSettingsService.Load().Returns(testPreferences);
        _mockValidator.IsValidSize(Arg.Any<double>(), Arg.Any<double>()).Returns(false);
        _mockValidator.IsValidPosition(-500, -500).Returns(false);

        _sut.Initialize(mockWindow, mockViewModel);

        mockWindow.Position.ShouldBe(originalPosition);
    }

    [Fact]
    public void UpdateWindowSettingsWhenPersistingPreferences()
    {
        IWindowPositionable mockWindow = Substitute.For<IWindowPositionable>();
        mockWindow.Position = new PixelPoint(150, 250);
        mockWindow.Width = 1280;
        mockWindow.Height = 720;
        MainWindowViewModel mockViewModel = CreateMockViewModel();
        mockViewModel.UserPreferences = new UserPreferences();

        _sut.PersistUserPreferences(mockWindow, mockViewModel);

        mockViewModel.UserPreferences.WindowSettings.WindowX.ShouldBe(150);
        mockViewModel.UserPreferences.WindowSettings.WindowY.ShouldBe(250);
        mockViewModel.UserPreferences.WindowSettings.WindowWidth.ShouldBe(1280);
        mockViewModel.UserPreferences.WindowSettings.WindowHeight.ShouldBe(720);
    }

    [Fact]
    public void SaveUserPreferencesWhenPersisting()
    {
        IWindowPositionable mockWindow = Substitute.For<IWindowPositionable>();
        mockWindow.Position = new PixelPoint(100, 200);
        mockWindow.Width = 1024;
        mockWindow.Height = 768;
        MainWindowViewModel mockViewModel = CreateMockViewModel();
        UserPreferences testPreferences = new();
        mockViewModel.UserPreferences = testPreferences;

        _sut.PersistUserPreferences(mockWindow, mockViewModel);

        _mockSettingsService.Received(1).Save(testPreferences);
    }

    [Fact]
    public void ValidatePositionWithCorrectCoordinatesWhenApplyingWindowSettings()
    {
        IWindowPositionable mockWindow = Substitute.For<IWindowPositionable>();
        MainWindowViewModel mockViewModel = CreateMockViewModel();
        UserPreferences testPreferences = new();
        testPreferences.WindowSettings.Update(new WindowSettings { WindowX = 300, WindowY = 400 });
        _mockSettingsService.Load().Returns(testPreferences);
        _mockValidator.IsValidSize(Arg.Any<double>(), Arg.Any<double>()).Returns(false);
        _mockValidator.IsValidPosition(300, 400).Returns(true);

        _sut.Initialize(mockWindow, mockViewModel);

        _mockValidator.Received(1).IsValidPosition(300, 400);
    }
}

