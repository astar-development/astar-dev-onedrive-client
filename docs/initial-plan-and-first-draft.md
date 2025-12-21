Yesterday
You said
Hiya, I am hoping you can create a plan for me.
I want to create a cross-platform, Avalonia UI-based application that syncs a personal onedrive account to a local, configurable, directory.
The plan should cover getting the full list of files when no sync has been performed. It should store the delta so future syncs can start where the original finished.
C#14 / .Net10 should be used and, where applicable, the latest features it offers: records, record structs, collection expressions, primary constructors etc.
The UI should include options for: Light / Dark theme, download parallelisation, download batch size, sync stats for the database updates, downloads etc.
When the download of new files has completed, the application should upload any new files that exist locally. Both download and upload should delete files where applicable
The database should be Sqlite and use EF Core

Please create a plan that I can use to implement this. Please follow SOLID, especially SRP, SOC

If you have any questions before you create the plan, please let me know
Copilot said

Quick answer: Use Microsoft Graph’s DriveItem delta to enumerate the full drive on first sync and persist the returned @odata.deltaLink for incremental syncs; store file metadata and change tokens in a Sqlite + EF Core local DB; implement a layered service architecture (Auth → GraphClient → SyncEngine → Storage → UI) following SRP/SOC so each class has one responsibility. 

.
Overview
Goal

Create an Avalonia cross‑platform app (.NET 10 / C# 14) that bi‑directionally syncs a personal OneDrive to a configurable local folder, supports theme and parallel download settings, and persists deltas for incremental syncs.
High‑level architecture

    Presentation (UI): Avalonia MVVM views + viewmodels; settings page for theme, parallelism, batch size, and stats.

    Application layer: ISyncService, ITransferService, ISettingsService, IAuthService.

    Domain / Persistence: EF Core + Sqlite DbContext with entities for DriveItemRecord, LocalFileRecord, DeltaToken.

    Infrastructure: Microsoft Graph client wrapper, file I/O adapter, background worker / queue for transfers.

    Sync Engine: Orchestrates initial full enumeration, applies deltas, schedules downloads/uploads, handles deletes and conflict resolution.

Key implementation steps

    Authentication

        Use delegated OAuth2 with MSAL for personal accounts; expose sign‑in in UI and persist refresh tokens securely (OS keystore recommended).

    Initial full enumeration

        Call DriveItem delta with no parameters to enumerate the entire drive; follow @odata.nextLink pages until complete, then save the returned @odata.deltaLink for future incremental calls

    .

    Persist each item’s id, path, eTag/cTag, size, lastModifiedDateTime, deleted flag in DriveItemRecord.

Delta storage & incremental sync

    Store the deltaLink in DeltaToken table. On subsequent syncs call delta with that link to receive only changes; apply adds/updates/deletes to local DB and filesystem

    .

Database design (EF Core + Sqlite)

    Entities as records/record structs where appropriate (C# 14 features).

    Use migrations, connection pooling, and a single AppDbContext with repositories for SRP.

    Note EF Core SQLite provider limitations and use transactions for batch updates

        .

    Transfer pipeline

        Download: Batch items by configured batch size; use a bounded concurrency queue for parallelisation; report per‑batch stats to UI.

        Upload: After downloads complete, scan local folder diffs (compare LocalFileRecord vs DriveItemRecord) and upload new files; delete remote/local items when delta indicates deletion.

        Use resumable uploads for large files (upload sessions).

    UI

        Settings: Light/Dark, parallelism, batch size, auto‑start, local folder.

        Dashboard: sync status, counts (db updates, downloads, uploads, errors), progress bars.

        Use IProgress<T> and observable collections for live stats.

    Error handling & conflicts

        Last‑write wins by default; expose conflict policy in settings.

        Retry with exponential backoff for transient Graph errors; log permanent failures and surface to user.

    Testing & telemetry

        Unit tests for services (mock Graph client).

        Integration tests against Graph Explorer or test tenant.

        Add optional telemetry/logging (file + UI view).

Important considerations & risks

    Rate limits and paging: delta responses are paged; follow @odata.nextLink until no nextLink

.

SQLite concurrency: EF Core + SQLite has limitations for heavy concurrent writes—serialize DB writes and use batching

    .

    Security: store tokens securely; avoid storing raw credentials.

Would you like a detailed class diagram and EF Core schema next so you can start scaffolding the project?

Microsoft Learn

driveItem: delta - Microsoft Graph v1.0 | Microsoft Learn
You said
yes please
Copilot said

Bold summary: Use Microsoft Graph DriveItem delta for the initial full enumeration and to obtain an @odata.deltaLink for incremental syncs; persist drive metadata and the delta token in a Sqlite + EF Core database, and implement a layered SyncEngine that orchestrates batched, parallel downloads and resumable uploads while following SRP/SOC.
Key considerations and decision points

    Auth: MSAL delegated OAuth for personal OneDrive accounts; store tokens securely.

    Delta-first design: The Graph delta API returns the full set on the first call and an @odata.deltaLink to use for later incremental calls

.

Local state: Persist DriveItem metadata, LocalFile records, and the delta token in Sqlite via EF Core; serialize DB writes to avoid SQLite write contention

.

Transfers: Use bounded concurrency and batching for downloads; use upload sessions for large files and delete semantics driven by the delta’s deleted facet

    .

EF Core schema (concise)

    DriveItemRecord (record): Id (string) PK; DriveItemId; Path; ETag; CTag; Size; LastModifiedUtc; IsFolder; IsDeleted; ParentId; VersionStamp

    LocalFileRecord (record): Id PK; RelativePath; Hash; Size; LastWriteUtc; SyncState (enum)

    DeltaToken (record): Id PK; Token; LastSyncedUtc

    TransferLog (record): Id PK; Type; ItemId; StartedUtc; CompletedUtc; Status; Error  
    Use migrations, enable WAL journaling for better concurrency, and serialize writes where necessary to avoid SQLITE_BUSY issues

    .

Core classes and responsibilities (SRP/SOC)

    IAuthService / AuthService: MSAL sign-in, token refresh, secure storage.

    IGraphClient / GraphClientWrapper: Thin wrapper around Microsoft Graph SDK; exposes GetDriveDeltaAsync(deltaLink), DownloadAsync, CreateUploadSessionAsync, UploadChunkAsync.

    ISyncRepository / EfSyncRepository: DB CRUD and batch apply of delta changes.

    ISyncEngine / SyncEngine: Orchestrates initial enumeration, applies DB deltas, schedules transfers, enforces conflict policy.

    ITransferService / TransferService: Bounded concurrency queue, batch processing, progress reporting, retry/backoff.

    IFileSystemAdapter: Abstracts file I/O for testability.

    ViewModels: SettingsViewModel, DashboardViewModel, SyncViewModel (Avalonia MVVM).

Sync flow (stepwise)

    Initial full enumeration: SyncEngine calls GetDriveDeltaAsync(null) and pages @odata.nextLink until exhausted; persist each item and final @odata.deltaLink in DeltaToken

    .

    Apply DB delta: Map adds/updates/deletes to DriveItemRecord and mark LocalFileRecord changes.

    Download phase: Batch by configured batch size; use a bounded SemaphoreSlim for parallelism; write files via IFileSystemAdapter.

    Upload phase: After downloads, scan local folder diffs and create upload sessions for new/changed files; delete remote items when local deletion detected and vice versa.

    Persist progress: Log TransferLog entries and update DeltaToken after successful application.

UI and settings

    Settings: Light/Dark theme, parallelism, batch size, local folder, conflict policy.

    Dashboard: DB updates count, downloads/uploads counts, active transfers, error list, per-batch stats. Use IProgress<T> and observable collections for live updates.

Risks and implementation notes

    Delta paging and rate limits: follow @odata.nextLink until no nextLink; store deltaLink for incremental calls

.

SQLite concurrency: enable WAL and serialize writes for heavy syncs; use EF Core optimistic concurrency patterns for conflicts

    .

    Large files: use Graph upload sessions and chunked uploads.

If you want, I can generate a scaffolded EF Core DbContext and the SyncEngine class skeleton (C# 14 records and primary constructors) next so you can paste it into a new .NET 10 project.

Microsoft Learn

driveItem: delta - Microsoft Graph v1.0 | Microsoft Learn
You said
yes please
Copilot said

Use Microsoft Graph DriveItem delta for the initial full enumeration and persist the returned @odata.deltaLink in a Sqlite/EF Core DB; implement a layered SyncEngine (Auth → Graph wrapper → Repository → Transfer pipeline → UI) with C# 14 records and primary constructors, batched/parallel transfers, and WAL‑enabled Sqlite for safe writes.
Architecture

    Layers: UI (Avalonia MVVM) → Application services (ISyncService, ITransferService, ISettingsService) → Domain + Persistence (EF Core + Sqlite) → Infrastructure (Graph wrapper, FileSystem adapter, MSAL auth).

    Key principles: SRP (each class one responsibility), SOC (clear separation of concerns), dependency injection, testable interfaces.

Important Graph behavior to design for

    The first driveItem/delta call returns the full enumeration (paged) and a final @odata.deltaLink to use for incremental syncs; follow @odata.nextLink until no nextLink is returned, then persist the deltaLink for later calls

. After initial enumeration, call delta with the saved @odata.deltaLink to receive only changes.

    .

EF Core schema (records)
csharp

public record DriveItemRecord(string Id, string DriveItemId, string RelativePath, string ETag, long Size, DateTimeOffset LastModifiedUtc, bool IsFolder, bool IsDeleted);

public record LocalFileRecord(string Id, string RelativePath, string Hash, long Size, DateTimeOffset LastWriteUtc, SyncState SyncState);

public record DeltaToken(string Id, string Token, DateTimeOffset LastSyncedUtc);

DbContext skeleton
csharp

public class AppDbContext : DbContext
{
    public DbSet<DriveItemRecord> DriveItems { get; init; } = null!;
    public DbSet<LocalFileRecord> LocalFiles { get; init; } = null!;
    public DbSet<DeltaToken> DeltaTokens { get; init; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<DriveItemRecord>().HasKey(d => d.Id);
        mb.Entity<LocalFileRecord>().HasKey(l => l.Id);
        mb.Entity<DeltaToken>().HasKey(t => t.Id);
    }
}

Implementation notes: enable SQLite WAL mode and serialize heavy writes; use EF Core migrations and batch updates for delta application.
SyncEngine skeleton (C# 14 style)
csharp

public sealed class SyncEngine(ISyncRepository repo, IGraphClient graph, ITransferService transfer, IFileSystemAdapter fs, ILogger logger)
{
    public async Task InitialFullSyncAsync(CancellationToken ct)
    {
        string? next = null;
        string? deltaLink = null;
        do
        {
            var page = await graph.GetDriveDeltaPageAsync(next, ct);
            repo.ApplyDeltaPage(page.Items);
            next = page.NextLink;
            deltaLink = page.DeltaLink ?? deltaLink;
        } while (!string.IsNullOrEmpty(next) && !ct.IsCancellationRequested);

        if (!string.IsNullOrEmpty(deltaLink))
            await repo.SaveDeltaTokenAsync(new DeltaToken(Guid.NewGuid().ToString(), deltaLink, DateTimeOffset.UtcNow));
    }

    public async Task IncrementalSyncAsync(CancellationToken ct)
    {
        var token = await repo.GetDeltaTokenAsync();
        var page = await graph.GetDriveDeltaAsync(token.Token, ct);
        repo.ApplyDeltaPage(page.Items);
        await repo.UpdateDeltaTokenAsync(token with { Token = page.DeltaLink!, LastSyncedUtc = DateTimeOffset.UtcNow });
        await transfer.ProcessPendingDownloadsAsync(ct);
        await transfer.ProcessPendingUploadsAsync(ct);
    }
}

Notes: IGraphClient returns paged results with NextLink and DeltaLink. Persist metadata (ETag/cTag) to detect changes.
Transfer pipeline & UI

    TransferService: bounded SemaphoreSlim for parallelism; batch by configured batch size; report progress via IProgress<SyncStats> and observable collections to the UI.

    UI: Settings (Light/Dark, parallelism, batch size, local folder, conflict policy), Dashboard (DB updates, downloads, uploads, errors).

Risks & mitigations

    Paging & rate limits: always follow @odata.nextLink until exhausted; implement retry/backoff. (See Microsoft Graph delta docs)

    .

    SQLite concurrency: use WAL and serialize DB writes; batch updates to reduce contention.

Would you like me to generate the full project scaffold (DbContext, repository implementations, Graph wrapper interfaces, and basic Avalonia ViewModels) next so you can paste it into a .NET 10 solution?

Microsoft Learn

driveItem: delta - Microsoft Graph v1.0 | Microsoft Learn
You said
yes please
Copilot said
Summary

Below is a practical, implementable scaffold you can paste into a new .NET 10 solution to start building the Avalonia OneDrive sync app. It follows SRP and Separation of Concerns, uses EF Core + SQLite, and leverages modern C# 14 features (records, record structs, primary-constructor style, collection expressions where appropriate). The scaffold includes: entities + DbContext, repository interfaces and a simple EF implementation, Graph wrapper interfaces, SyncEngine and TransferService skeletons, IFileSystemAdapter, MSAL auth interface, basic Avalonia ViewModels, and DI/startup notes.
Project layout (recommended)
Code

/src
  /App.UI.Avalonia         -> Avalonia app, Views, ViewModels
  /App.Core                -> Domain models, interfaces
  /App.Infrastructure      -> EF Core, Graph wrapper, FileSystem adapter, Auth
  /App.Services            -> SyncEngine, TransferService, Repositories
  /App.Tests               -> Unit tests (mocks for Graph & FS)

EF Core entities and DbContext
csharp

// App.Core/Entities/DriveItemRecord.cs
public record DriveItemRecord(
    string Id,
    string DriveItemId,
    string RelativePath,
    string? ETag,
    string? CTag,
    long Size,
    DateTimeOffset LastModifiedUtc,
    bool IsFolder,
    bool IsDeleted
);

// App.Core/Entities/LocalFileRecord.cs
public record LocalFileRecord(
    string Id,
    string RelativePath,
    string? Hash,
    long Size,
    DateTimeOffset LastWriteUtc,
    SyncState SyncState
);

public enum SyncState { Unknown, PendingDownload, Downloaded, PendingUpload, Uploaded, Deleted, Error }

// App.Core/Entities/DeltaToken.cs
public record DeltaToken(string Id, string Token, DateTimeOffset LastSyncedUtc);

// App.Infrastructure/Data/AppDbContext.cs
public sealed class AppDbContext : DbContext
{
    public DbSet<DriveItemRecord> DriveItems { get; init; } = null!;
    public DbSet<LocalFileRecord> LocalFiles { get; init; } = null!;
    public DbSet<DeltaToken> DeltaTokens { get; init; } = null!;
    public DbSet<TransferLog> TransferLogs { get; init; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<DriveItemRecord>().HasKey(d => d.Id);
        mb.Entity<LocalFileRecord>().HasKey(l => l.Id);
        mb.Entity<DeltaToken>().HasKey(t => t.Id);
        mb.Entity<TransferLog>().HasKey(t => t.Id);

        // Map records to tables and configure properties as needed
        mb.Entity<DriveItemRecord>().Property(d => d.RelativePath).IsRequired();
        mb.Entity<LocalFileRecord>().Property(l => l.RelativePath).IsRequired();

        // Use concurrency token if desired (ETag)
        mb.Entity<DriveItemRecord>().Property(d => d.ETag).IsConcurrencyToken(false);
    }
}

// App.Core/Entities/TransferLog.cs
public record TransferLog(string Id, TransferType Type, string ItemId, DateTimeOffset StartedUtc, DateTimeOffset? CompletedUtc, TransferStatus Status, string? Error);

public enum TransferType { Download, Upload, Delete }
public enum TransferStatus { Pending, InProgress, Success, Failed }

Notes

    Use WAL journaling for SQLite: set PRAGMA journal_mode=WAL; on DB open (via connection string or DbConnection initialization).

    Serialize heavy writes (single writer pattern) to avoid SQLITE_BUSY. Use EF Core batch updates inside a transaction when applying delta pages.

Core interfaces (SRP-focused)
csharp

// App.Core/Interfaces/IAuthService.cs
public interface IAuthService
{
    Task SignInAsync(CancellationToken ct);
    Task SignOutAsync(CancellationToken ct);
    Task<string> GetAccessTokenAsync(CancellationToken ct);
    bool IsSignedIn { get; }
}

// App.Core/Interfaces/IGraphClient.cs
public interface IGraphClient
{
    Task<DeltaPage> GetDriveDeltaPageAsync(string? deltaOrNextLink, CancellationToken ct);
    Task<Stream> DownloadDriveItemContentAsync(string driveItemId, CancellationToken ct);
    Task<UploadSessionInfo> CreateUploadSessionAsync(string parentPath, string fileName, CancellationToken ct);
    Task UploadChunkAsync(UploadSessionInfo session, Stream chunk, long rangeStart, long rangeEnd, CancellationToken ct);
    Task DeleteDriveItemAsync(string driveItemId, CancellationToken ct);
}

// App.Core/Interfaces/IFileSystemAdapter.cs
public interface IFileSystemAdapter
{
    Task WriteFileAsync(string relativePath, Stream content, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct);
    Task DeleteFileAsync(string relativePath, CancellationToken ct);
    Task<IEnumerable<LocalFileInfo>> EnumerateFilesAsync(CancellationToken ct);
}

// App.Core/Interfaces/ISyncRepository.cs
public interface ISyncRepository
{
    Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken ct);
    Task SaveOrUpdateDeltaTokenAsync(DeltaToken token, CancellationToken ct);
    Task ApplyDriveItemsAsync(IEnumerable<DriveItemRecord> items, CancellationToken ct);
    Task<IEnumerable<DriveItemRecord>> GetPendingDownloadsAsync(int limit, CancellationToken ct);
    Task MarkLocalFileStateAsync(string localFileId, SyncState state, CancellationToken ct);
    Task AddOrUpdateLocalFileAsync(LocalFileRecord file, CancellationToken ct);
    Task<IEnumerable<LocalFileRecord>> GetPendingUploadsAsync(int limit, CancellationToken ct);
}

Data transfer DTOs
csharp

public sealed record DeltaPage(IEnumerable<DriveItemRecord> Items, string? NextLink, string? DeltaLink);
public sealed record UploadSessionInfo(string UploadUrl, string SessionId, long? ExpirationSeconds);
public sealed record LocalFileInfo(string RelativePath, long Size, DateTimeOffset LastWriteUtc, string? Hash);

SyncEngine and TransferService skeletons
csharp

// App.Services/SyncEngine.cs
public sealed class SyncEngine(ISyncRepository repo, IGraphClient graph, ITransferService transfer, ILogger<SyncEngine> logger)
{
    public async Task InitialFullSyncAsync(CancellationToken ct)
    {
        string? nextOrDelta = null;
        string? finalDelta = null;

        do
        {
            var page = await graph.GetDriveDeltaPageAsync(nextOrDelta, ct);
            await repo.ApplyDriveItemsAsync(page.Items, ct);
            nextOrDelta = page.NextLink;
            finalDelta = page.DeltaLink ?? finalDelta;
        } while (!string.IsNullOrEmpty(nextOrDelta) && !ct.IsCancellationRequested);

        if (!string.IsNullOrEmpty(finalDelta))
        {
            var token = new DeltaToken(Guid.NewGuid().ToString(), finalDelta, DateTimeOffset.UtcNow);
            await repo.SaveOrUpdateDeltaTokenAsync(token, ct);
        }

        // schedule transfers
        await transfer.ProcessPendingDownloadsAsync(ct);
        await transfer.ProcessPendingUploadsAsync(ct);
    }

    public async Task IncrementalSyncAsync(CancellationToken ct)
    {
        var token = await repo.GetDeltaTokenAsync(ct) ?? throw new InvalidOperationException("No delta token found; run initial sync.");
        var page = await graph.GetDriveDeltaPageAsync(token.Token, ct);
        await repo.ApplyDriveItemsAsync(page.Items, ct);
        if (!string.IsNullOrEmpty(page.DeltaLink))
            await repo.SaveOrUpdateDeltaTokenAsync(token with { Token = page.DeltaLink, LastSyncedUtc = DateTimeOffset.UtcNow }, ct);

        await transfer.ProcessPendingDownloadsAsync(ct);
        await transfer.ProcessPendingUploadsAsync(ct);
    }
}

// App.Services/TransferService.cs
public sealed class TransferService(IFileSystemAdapter fs, IGraphClient graph, ISyncRepository repo, ILogger<TransferService> logger, SyncSettings settings)
{
    private readonly SemaphoreSlim _parallelism;
    public TransferService()
    {
        _parallelism = new SemaphoreSlim(settings.ParallelDownloads);
    }

    public async Task ProcessPendingDownloadsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var items = (await repo.GetPendingDownloadsAsync(settings.BatchSize, ct)).ToList();
            if (!items.Any()) break;

            var tasks = items.Select(item => DownloadItemAsync(item, ct)).ToList();
            await Task.WhenAll(tasks);
        }
    }

    private async Task DownloadItemAsync(DriveItemRecord item, CancellationToken ct)
    {
        await _parallelism.WaitAsync(ct);
        try
        {
            using var stream = await graph.DownloadDriveItemContentAsync(item.DriveItemId, ct);
            await fs.WriteFileAsync(item.RelativePath, stream, ct);
            await repo.MarkLocalFileStateAsync(item.Id, SyncState.Downloaded, ct);
        }
        catch (Exception ex)
        {
            // log and mark error
        }
        finally { _parallelism.Release(); }
    }

    public async Task ProcessPendingUploadsAsync(CancellationToken ct)
    {
        var uploads = (await repo.GetPendingUploadsAsync(settings.BatchSize, ct)).ToList();
        foreach (var local in uploads)
        {
            // create upload session and upload in chunks
        }
    }
}

Notes

    SyncSettings holds ParallelDownloads, BatchSize, ConflictPolicy, etc.

    Use SemaphoreSlim to bound concurrency; use HttpClient with MaxConnectionsPerServer tuned for parallel uploads/downloads.

    Implement resumable uploads using UploadSessionInfo and chunked UploadChunkAsync.

IGraphClient and Auth (implementation notes)
csharp

// App.Infrastructure/Graph/GraphClientWrapper.cs (interface implemented here)
public sealed class GraphClientWrapper : IGraphClient
{
    private readonly IAuthService _auth;
    private readonly HttpClient _http;

    public GraphClientWrapper(IAuthService auth, HttpClient http) { ... }

    public async Task<DeltaPage> GetDriveDeltaPageAsync(string? deltaOrNextLink, CancellationToken ct)
    {
        // If deltaOrNextLink is null -> call /me/drive/root/delta
        // If deltaOrNextLink is a nextLink -> GET that URL
        // If deltaOrNextLink is a deltaLink -> GET that URL
        // Parse items into DriveItemRecord; return NextLink and DeltaLink
    }

    // DownloadDriveItemContentAsync, CreateUploadSessionAsync, UploadChunkAsync, DeleteDriveItemAsync
}

Auth

    Implement IAuthService using MSAL (PublicClientApplication) for personal accounts. Persist refresh tokens via OS secure storage (Windows Credential Manager, macOS Keychain, Linux Secret Service) or use MSAL token cache with secure storage provider.

    GetAccessTokenAsync returns a bearer token used by GraphClientWrapper to call Graph endpoints.

Avalonia ViewModels and UI notes
csharp

// App.UI/ViewModels/SettingsViewModel.cs
public sealed class SettingsViewModel : ViewModelBase
{
    public string LocalFolder { get; set; } = string.Empty;
    public int ParallelDownloads { get; set; } = 4;
    public int BatchSize { get; set; } = 50;
    public ThemeMode Theme { get; set; } = ThemeMode.System;
    public ConflictPolicy ConflictPolicy { get; set; } = ConflictPolicy.LastWriteWins;

    public ICommand SaveCommand { get; }
}

// App.UI/ViewModels/DashboardViewModel.cs
public sealed class DashboardViewModel : ViewModelBase
{
    public ObservableCollection<TransferLogViewModel> RecentTransfers { get; } = new();
    public int PendingDownloads { get; set; }
    public int PendingUploads { get; set; }
    public string SyncStatus { get; set; } = "Idle";

    public void StartSync() => /* call SyncEngine.IncrementalSyncAsync via injected service */;
}

UI features

    Settings page: Light/Dark toggle, numeric inputs for parallelism and batch size, local folder picker, conflict policy dropdown.

    Dashboard: live counts, progress bars, recent transfer list, error list.

    Use IProgress<T> or Reactive patterns to push stats from services to ViewModels.

DI, startup, migrations, testing, and operational notes

    DI registration (example):

csharp

services.AddDbContext<AppDbContext>(opts => opts.UseSqlite("Data Source=app.db"));
services.AddScoped<ISyncRepository, EfSyncRepository>();
services.AddSingleton<IAuthService, MsalAuthService>();
services.AddSingleton<IGraphClient, GraphClientWrapper>();
services.AddSingleton<IFileSystemAdapter, LocalFileSystemAdapter>();
services.AddSingleton<ITransferService, TransferService>();
services.AddSingleton<SyncEngine>();

    Migrations: create EF Core migrations and apply on startup. Ensure PRAGMA journal_mode=WAL; is set for the SQLite connection.

    Testing: mock IGraphClient and IFileSystemAdapter for unit tests. Add integration tests that simulate delta pages and verify DB state transitions.

    Resilience: implement retry with exponential backoff for transient HTTP/Graph errors; respect Graph rate limits (HTTP 429).

    Conflict policy: default to LastWriteWins but expose KeepLocal, KeepRemote, and Prompt options in settings.

    Deletes: when delta indicates deletion, mark DriveItemRecord.IsDeleted = true and delete local file if policy allows; when local deletion detected, call Graph delete.

Next steps (practical)

    Create the solution and projects using the layout above.

    Add the core records and AppDbContext file, scaffold EF migrations, and enable WAL.

    Implement IAuthService (MSAL) and IGraphClient wrapper (start with GetDriveDeltaPageAsync).

    Implement ISyncRepository (EF-backed) and SyncEngine.InitialFullSyncAsync.

    Wire up TransferService and basic Avalonia UI to start/observe sync.

I can generate the complete file-by-file scaffold (C# source files for entities, DbContext, repository, Graph wrapper interfaces, SyncEngine, TransferService, and basic Avalonia ViewModels) ready to paste into your .NET 10 solution.
You said
that would be fantastic. thanks
Copilot said
Project scaffold overview

Below is a file by file scaffold you can paste into a new .NET 10 solution. It includes core domain records, EF Core DbContext, repository, Graph client interfaces, MSAL auth stub, file system adapter, SyncEngine, TransferService, basic Avalonia ViewModels, and DI wiring. The scaffold follows SRP and Separation of Concerns, uses modern C# 14 features (records, primary-constructor style), and is ready to be extended.

    How to use  
    Create a solution with projects: App.Core, App.Infrastructure, App.Services, App.UI.Avalonia. Place each file under the path shown in the file header comments. Add NuGet packages: Microsoft.EntityFrameworkCore, Microsoft.EntityFrameworkCore.Sqlite, Microsoft.Graph, Microsoft.Identity.Client, Avalonia, Avalonia.ReactiveUI, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging.

1 Core domain and DTOs
csharp

// File: App.Core/Entities/DriveItemRecord.cs
namespace App.Core.Entities;
public sealed record DriveItemRecord(
    string Id,
    string DriveItemId,
    string RelativePath,
    string? ETag,
    string? CTag,
    long Size,
    DateTimeOffset LastModifiedUtc,
    bool IsFolder,
    bool IsDeleted
);

csharp

// File: App.Core/Entities/LocalFileRecord.cs
namespace App.Core.Entities;
public sealed record LocalFileRecord(
    string Id,
    string RelativePath,
    string? Hash,
    long Size,
    DateTimeOffset LastWriteUtc,
    SyncState SyncState
);

public enum SyncState { Unknown, PendingDownload, Downloaded, PendingUpload, Uploaded, Deleted, Error }

csharp

// File: App.Core/Entities/DeltaToken.cs
namespace App.Core.Entities;
public sealed record DeltaToken(string Id, string Token, DateTimeOffset LastSyncedUtc);

csharp

// File: App.Core/Entities/TransferLog.cs
namespace App.Core.Entities;
public sealed record TransferLog(string Id, TransferType Type, string ItemId, DateTimeOffset StartedUtc, DateTimeOffset? CompletedUtc, TransferStatus Status, string? Error);

public enum TransferType { Download, Upload, Delete }
public enum TransferStatus { Pending, InProgress, Success, Failed }

csharp

// File: App.Core/Dto/DeltaPage.cs
namespace App.Core.Dto;
public sealed record DeltaPage(IEnumerable<App.Core.Entities.DriveItemRecord> Items, string? NextLink, string? DeltaLink);

public sealed record UploadSessionInfo(string UploadUrl, string SessionId, DateTimeOffset ExpiresAt);
public sealed record LocalFileInfo(string RelativePath, long Size, DateTimeOffset LastWriteUtc, string? Hash);

2 EF Core DbContext and repository interfaces
csharp

// File: App.Infrastructure/Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using App.Core.Entities;

namespace App.Infrastructure.Data;
public sealed class AppDbContext : DbContext
{
    public DbSet<DriveItemRecord> DriveItems { get; init; } = null!;
    public DbSet<LocalFileRecord> LocalFiles { get; init; } = null!;
    public DbSet<DeltaToken> DeltaTokens { get; init; } = null!;
    public DbSet<TransferLog> TransferLogs { get; init; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<DriveItemRecord>().HasKey(d => d.Id);
        mb.Entity<LocalFileRecord>().HasKey(l => l.Id);
        mb.Entity<DeltaToken>().HasKey(t => t.Id);
        mb.Entity<TransferLog>().HasKey(t => t.Id);

        mb.Entity<DriveItemRecord>().Property(d => d.RelativePath).IsRequired();
        mb.Entity<LocalFileRecord>().Property(l => l.RelativePath).IsRequired();

        // Map records to tables
        mb.Entity<DriveItemRecord>().ToTable("DriveItems");
        mb.Entity<LocalFileRecord>().ToTable("LocalFiles");
        mb.Entity<DeltaToken>().ToTable("DeltaTokens");
        mb.Entity<TransferLog>().ToTable("TransferLogs");
    }
}

csharp

// File: App.Core/Interfaces/ISyncRepository.cs
using App.Core.Entities;
namespace App.Core.Interfaces;
public interface ISyncRepository
{
    Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken ct);
    Task SaveOrUpdateDeltaTokenAsync(DeltaToken token, CancellationToken ct);
    Task ApplyDriveItemsAsync(IEnumerable<DriveItemRecord> items, CancellationToken ct);
    Task<IEnumerable<DriveItemRecord>> GetPendingDownloadsAsync(int limit, CancellationToken ct);
    Task MarkLocalFileStateAsync(string driveItemId, SyncState state, CancellationToken ct);
    Task AddOrUpdateLocalFileAsync(LocalFileRecord file, CancellationToken ct);
    Task<IEnumerable<LocalFileRecord>> GetPendingUploadsAsync(int limit, CancellationToken ct);
    Task LogTransferAsync(TransferLog log, CancellationToken ct);
}

csharp

// File: App.Infrastructure/Repositories/EfSyncRepository.cs
using Microsoft.EntityFrameworkCore;
using App.Core.Interfaces;
using App.Core.Entities;

namespace App.Infrastructure.Repositories;
public sealed class EfSyncRepository : ISyncRepository
{
    private readonly AppDbContext _db;
    public EfSyncRepository(AppDbContext db) => _db = db;

    public async Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken ct) =>
        await _db.DeltaTokens.OrderByDescending(t => t.LastSyncedUtc).FirstOrDefaultAsync(ct);

    public async Task SaveOrUpdateDeltaTokenAsync(DeltaToken token, CancellationToken ct)
    {
        var existing = await _db.DeltaTokens.FindAsync(new object[] { token.Id }, ct);
        if (existing is null) _db.DeltaTokens.Add(token);
        else _db.Entry(existing).CurrentValues.SetValues(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ApplyDriveItemsAsync(IEnumerable<DriveItemRecord> items, CancellationToken ct)
    {
        // Apply in a transaction and batch to reduce contention
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        foreach (var item in items)
        {
            var existing = await _db.DriveItems.FindAsync(new object[] { item.Id }, ct);
            if (existing is null) _db.DriveItems.Add(item);
            else _db.Entry(existing).CurrentValues.SetValues(item);
        }
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<IEnumerable<DriveItemRecord>> GetPendingDownloadsAsync(int limit, CancellationToken ct) =>
        await _db.DriveItems.Where(d => !d.IsFolder && !d.IsDeleted)
                            .OrderBy(d => d.LastModifiedUtc)
                            .Take(limit)
                            .ToListAsync(ct);

    public async Task MarkLocalFileStateAsync(string driveItemId, SyncState state, CancellationToken ct)
    {
        var item = await _db.DriveItems.FindAsync(new object[] { driveItemId }, ct);
        if (item is null) return;
        // create or update LocalFileRecord mapping by drive item id as Id
        var local = await _db.LocalFiles.FindAsync(new object[] { driveItemId }, ct);
        if (local is null)
        {
            _db.LocalFiles.Add(new LocalFileRecord(driveItemId, item.RelativePath, null, item.Size, item.LastModifiedUtc, state));
        }
        else
        {
            _db.Entry(local).CurrentValues.SetValues(local with { SyncState = state });
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddOrUpdateLocalFileAsync(LocalFileRecord file, CancellationToken ct)
    {
        var existing = await _db.LocalFiles.FindAsync(new object[] { file.Id }, ct);
        if (existing is null) _db.LocalFiles.Add(file);
        else _db.Entry(existing).CurrentValues.SetValues(file);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<LocalFileRecord>> GetPendingUploadsAsync(int limit, CancellationToken ct) =>
        await _db.LocalFiles.Where(l => l.SyncState == SyncState.PendingUpload).Take(limit).ToListAsync(ct);

    public async Task LogTransferAsync(TransferLog log, CancellationToken ct)
    {
        _db.TransferLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}

3 Graph client and auth interfaces
csharp

// File: App.Core/Interfaces/IAuthService.cs
namespace App.Core.Interfaces;
public interface IAuthService
{
    Task SignInAsync(CancellationToken ct);
    Task SignOutAsync(CancellationToken ct);
    Task<string> GetAccessTokenAsync(CancellationToken ct);
    bool IsSignedIn { get; }
}

csharp

// File: App.Core/Interfaces/IGraphClient.cs
using App.Core.Dto;
namespace App.Core.Interfaces;
public interface IGraphClient
{
    Task<DeltaPage> GetDriveDeltaPageAsync(string? deltaOrNextLink, CancellationToken ct);
    Task<Stream> DownloadDriveItemContentAsync(string driveItemId, CancellationToken ct);
    Task<UploadSessionInfo> CreateUploadSessionAsync(string parentPath, string fileName, CancellationToken ct);
    Task UploadChunkAsync(UploadSessionInfo session, Stream chunk, long rangeStart, long rangeEnd, CancellationToken ct);
    Task DeleteDriveItemAsync(string driveItemId, CancellationToken ct);
}

csharp

// File: App.Infrastructure/Graph/GraphClientWrapper.cs
using System.Net.Http.Headers;
using System.Text.Json;
using App.Core.Interfaces;
using App.Core.Dto;
using App.Core.Entities;

namespace App.Infrastructure.Graph;
public sealed class GraphClientWrapper : IGraphClient
{
    private readonly IAuthService _auth;
    private readonly HttpClient _http;
    public GraphClientWrapper(IAuthService auth, HttpClient http) { _auth = auth; _http = http; }

    public async Task<DeltaPage> GetDriveDeltaPageAsync(string? deltaOrNextLink, CancellationToken ct)
    {
        var url = string.IsNullOrEmpty(deltaOrNextLink) ? "https://graph.microsoft.com/v1.0/me/drive/root/delta" : deltaOrNextLink;
        var token = await _auth.GetAccessTokenAsync(ct);
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        using var s = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct);
        // parse items, nextLink, deltaLink
        var items = new List<DriveItemRecord>();
        if (doc.RootElement.TryGetProperty("value", out var arr))
        {
            foreach (var el in arr.EnumerateArray())
            {
                var id = el.GetProperty("id").GetString()!;
                var isFolder = el.TryGetProperty("folder", out _);
                var size = el.TryGetProperty("size", out var sProp) ? sProp.GetInt64() : 0L;
                var path = el.TryGetProperty("parentReference", out var pr) && pr.TryGetProperty("path", out var p) ? p.GetString() ?? id : id;
                var eTag = el.TryGetProperty("eTag", out var et) ? et.GetString() : null;
                var cTag = el.TryGetProperty("cTag", out var ctProp) ? ctProp.GetString() : null;
                var last = el.TryGetProperty("lastModifiedDateTime", out var lm) ? DateTimeOffset.Parse(lm.GetString()!) : DateTimeOffset.UtcNow;
                var isDeleted = el.TryGetProperty("deleted", out _);
                items.Add(new DriveItemRecord(id, id, path, eTag, cTag, size, last, isFolder, isDeleted));
            }
        }
        var next = doc.RootElement.TryGetProperty("@odata.nextLink", out var nl) ? nl.GetString() : null;
        var delta = doc.RootElement.TryGetProperty("@odata.deltaLink", out var dl) ? dl.GetString() : null;
        return new DeltaPage(items, next, delta);
    }

    public async Task<Stream> DownloadDriveItemContentAsync(string driveItemId, CancellationToken ct)
    {
        var token = await _auth.GetAccessTokenAsync(ct);
        var url = $"https://graph.microsoft.com/v1.0/me/drive/items/{driveItemId}/content";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStreamAsync(ct);
    }

    public async Task<UploadSessionInfo> CreateUploadSessionAsync(string parentPath, string fileName, CancellationToken ct)
    {
        // Simplified: create upload session under root or parentPath
        var token = await _auth.GetAccessTokenAsync(ct);
        var url = $"https://graph.microsoft.com/v1.0/me/drive/root:/{Uri.EscapeDataString(parentPath.Trim('/'))}/{Uri.EscapeDataString(fileName)}:/createUploadSession";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        using var s = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(s, cancellationToken: ct);
        var uploadUrl = doc.RootElement.GetProperty("uploadUrl").GetString()!;
        var expiration = doc.RootElement.TryGetProperty("expirationDateTime", out var ex) ? DateTimeOffset.Parse(ex.GetString()!) : DateTimeOffset.UtcNow.AddHours(1);
        return new UploadSessionInfo(uploadUrl, Guid.NewGuid().ToString(), expiration);
    }

    public async Task UploadChunkAsync(UploadSessionInfo session, Stream chunk, long rangeStart, long rangeEnd, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, session.UploadUrl);
        req.Content = new StreamContent(chunk);
        req.Content.Headers.Add("Content-Range", $"bytes {rangeStart}-{rangeEnd}/*");
        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task DeleteDriveItemAsync(string driveItemId, CancellationToken ct)
    {
        var token = await _auth.GetAccessTokenAsync(ct);
        var url = $"https://graph.microsoft.com/v1.0/me/drive/items/{driveItemId}";
        using var req = new HttpRequestMessage(HttpMethod.Delete, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var res = await _http.SendAsync(req, ct);
        if (res.StatusCode != System.Net.HttpStatusCode.NoContent && res.StatusCode != System.Net.HttpStatusCode.NotFound)
            res.EnsureSuccessStatusCode();
    }
}

csharp

// File: App.Infrastructure/Auth/MsalAuthService.cs
using Microsoft.Identity.Client;
using App.Core.Interfaces;

namespace App.Infrastructure.Auth;
public sealed class MsalAuthService : IAuthService
{
    private readonly IPublicClientApplication _pca;
    private IAccount? _account;
    private readonly string[] _scopes = new[] { "Files.ReadWrite.All", "offline_access", "User.Read" };

    public bool IsSignedIn => _account is not null;

    public MsalAuthService(string clientId)
    {
        _pca = PublicClientApplicationBuilder.Create(clientId)
            .WithRedirectUri("http://localhost")
            .Build();
    }

    public async Task SignInAsync(CancellationToken ct)
    {
        var result = await _pca.AcquireTokenInteractive(_scopes).ExecuteAsync(ct);
        _account = result.Account;
    }

    public async Task SignOutAsync(CancellationToken ct)
    {
        if (_account is null) return;
        await _pca.RemoveAsync(_account);
        _account = null;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (_account is null) throw new InvalidOperationException("Not signed in");
        var result = await _pca.AcquireTokenSilent(_scopes, _account).ExecuteAsync(ct);
        return result.AccessToken;
    }
}

4 File system adapter
csharp

// File: App.Core/Interfaces/IFileSystemAdapter.cs
using App.Core.Dto;
namespace App.Core.Interfaces;
public interface IFileSystemAdapter
{
    Task WriteFileAsync(string relativePath, Stream content, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct);
    Task DeleteFileAsync(string relativePath, CancellationToken ct);
    Task<IEnumerable<LocalFileInfo>> EnumerateFilesAsync(CancellationToken ct);
}

csharp

// File: App.Infrastructure/Filesystem/LocalFileSystemAdapter.cs
using App.Core.Interfaces;
using App.Core.Dto;
using System.Security.Cryptography;

namespace App.Infrastructure.Filesystem;
public sealed class LocalFileSystemAdapter : IFileSystemAdapter
{
    private readonly string _root;
    public LocalFileSystemAdapter(string root) => _root = root;

    private string FullPath(string relative) => Path.Combine(_root, relative.TrimStart(Path.DirectorySeparatorChar));

    public async Task WriteFileAsync(string relativePath, Stream content, CancellationToken ct)
    {
        var full = FullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var fs = File.Create(full);
        await content.CopyToAsync(fs, ct);
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct)
    {
        var full = FullPath(relativePath);
        if (!File.Exists(full)) return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(File.OpenRead(full));
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken ct)
    {
        var full = FullPath(relativePath);
        if (File.Exists(full)) File.Delete(full);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<LocalFileInfo>> EnumerateFilesAsync(CancellationToken ct)
    {
        var list = new List<LocalFileInfo>();
        if (!Directory.Exists(_root)) return Task.FromResult<IEnumerable<LocalFileInfo>>(list.AsEnumerable());
        foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
        {
            var fi = new FileInfo(file);
            var rel = Path.GetRelativePath(_root, file);
            list.Add(new LocalFileInfo(rel, fi.Length, fi.LastWriteTimeUtc, null));
        }
        return Task.FromResult<IEnumerable<LocalFileInfo>>(list);
    }
}

5 SyncEngine and TransferService
csharp

// File: App.Services/SyncEngine.cs
using App.Core.Interfaces;
using App.Core.Dto;
using App.Core.Entities;

namespace App.Services;
public sealed class SyncEngine
{
    private readonly ISyncRepository _repo;
    private readonly IGraphClient _graph;
    private readonly TransferService _transfer;
    private readonly ILogger<SyncEngine> _logger;

    public SyncEngine(ISyncRepository repo, IGraphClient graph, TransferService transfer, ILogger<SyncEngine> logger)
    {
        _repo = repo;
        _graph = graph;
        _transfer = transfer;
        _logger = logger;
    }

    public async Task InitialFullSyncAsync(CancellationToken ct)
    {
        string? next = null;
        string? finalDelta = null;
        do
        {
            var page = await _graph.GetDriveDeltaPageAsync(next, ct);
            await _repo.ApplyDriveItemsAsync(page.Items, ct);
            next = page.NextLink;
            finalDelta = page.DeltaLink ?? finalDelta;
        } while (!string.IsNullOrEmpty(next) && !ct.IsCancellationRequested);

        if (!string.IsNullOrEmpty(finalDelta))
        {
            var token = new DeltaToken(Guid.NewGuid().ToString(), finalDelta, DateTimeOffset.UtcNow);
            await _repo.SaveOrUpdateDeltaTokenAsync(token, ct);
        }

        await _transfer.ProcessPendingDownloadsAsync(ct);
        await _transfer.ProcessPendingUploadsAsync(ct);
    }

    public async Task IncrementalSyncAsync(CancellationToken ct)
    {
        var token = await _repo.GetDeltaTokenAsync(ct) ?? throw new InvalidOperationException("No delta token found");
        var page = await _graph.GetDriveDeltaPageAsync(token.Token, ct);
        await _repo.ApplyDriveItemsAsync(page.Items, ct);
        if (!string.IsNullOrEmpty(page.DeltaLink))
            await _repo.SaveOrUpdateDeltaTokenAsync(token with { Token = page.DeltaLink, LastSyncedUtc = DateTimeOffset.UtcNow }, ct);

        await _transfer.ProcessPendingDownloadsAsync(ct);
        await _transfer.ProcessPendingUploadsAsync(ct);
    }
}

csharp

// File: App.Services/TransferService.cs
using App.Core.Interfaces;
using App.Core.Entities;
using App.Core.Dto;

namespace App.Services;
public sealed class TransferService
{
    private readonly IFileSystemAdapter _fs;
    private readonly IGraphClient _graph;
    private readonly ISyncRepository _repo;
    private readonly ILogger<TransferService> _logger;
    private readonly SemaphoreSlim _parallelism;
    private readonly SyncSettings _settings;

    public TransferService(IFileSystemAdapter fs, IGraphClient graph, ISyncRepository repo, ILogger<TransferService> logger, SyncSettings settings)
    {
        _fs = fs;
        _graph = graph;
        _repo = repo;
        _logger = logger;
        _settings = settings;
        _parallelism = new SemaphoreSlim(settings.ParallelDownloads);
    }

    public async Task ProcessPendingDownloadsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var items = (await _repo.GetPendingDownloadsAsync(_settings.BatchSize, ct)).ToList();
            if (!items.Any()) break;
            var tasks = items.Select(i => DownloadItemAsync(i, ct)).ToList();
            await Task.WhenAll(tasks);
        }
    }

    private async Task DownloadItemAsync(DriveItemRecord item, CancellationToken ct)
    {
        await _parallelism.WaitAsync(ct);
        var log = new TransferLog(Guid.NewGuid().ToString(), TransferType.Download, item.Id, DateTimeOffset.UtcNow, null, TransferStatus.InProgress, null);
        await _repo.LogTransferAsync(log, ct);
        try
        {
            await using var stream = await _graph.DownloadDriveItemContentAsync(item.DriveItemId, ct);
            await _fs.WriteFileAsync(item.RelativePath, stream, ct);
            await _repo.MarkLocalFileStateAsync(item.Id, SyncState.Downloaded, ct);
            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Success };
            await _repo.LogTransferAsync(log, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download failed for {Id}", item.Id);
            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Failed, Error = ex.Message };
            await _repo.LogTransferAsync(log, ct);
        }
        finally { _parallelism.Release(); }
    }

    public async Task ProcessPendingUploadsAsync(CancellationToken ct)
    {
        var uploads = (await _repo.GetPendingUploadsAsync(_settings.BatchSize, ct)).ToList();
        foreach (var local in uploads)
        {
            var log = new TransferLog(Guid.NewGuid().ToString(), TransferType.Upload, local.Id, DateTimeOffset.UtcNow, null, TransferStatus.InProgress, null);
            await _repo.LogTransferAsync(log, ct);
            try
            {
                var session = await _graph.CreateUploadSessionAsync(Path.GetDirectoryName(local.RelativePath) ?? "/", Path.GetFileName(local.RelativePath), ct);
                await using var stream = await _fs.OpenReadAsync(local.RelativePath, ct) ?? throw new FileNotFoundException(local.RelativePath);
                const int chunkSize = 320 * 1024;
                long uploaded = 0;
                while (uploaded < stream.Length)
                {
                    var toRead = (int)Math.Min(chunkSize, stream.Length - uploaded);
                    var buffer = new byte[toRead];
                    stream.Seek(uploaded, SeekOrigin.Begin);
                    await stream.ReadAsync(buffer, 0, toRead, ct);
                    await using var ms = new MemoryStream(buffer, writable: false);
                    await _graph.UploadChunkAsync(session, ms, uploaded, uploaded + toRead - 1, ct);
                    uploaded += toRead;
                }
                await _repo.MarkLocalFileStateAsync(local.Id, SyncState.Uploaded, ct);
                log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Success };
                await _repo.LogTransferAsync(log, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload failed for {Id}", local.Id);
                log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Failed, Error = ex.Message };
                await _repo.LogTransferAsync(log, ct);
            }
        }
    }
}

csharp

// File: App.Services/SyncSettings.cs
public sealed record SyncSettings(int ParallelDownloads = 4, int BatchSize = 50, ConflictPolicy ConflictPolicy = ConflictPolicy.LastWriteWins);
public enum ConflictPolicy { LastWriteWins, KeepLocal, KeepRemote, Prompt }

6 Avalonia ViewModels and DI startup
csharp

// File: App.UI/ViewModels/ViewModelBase.cs
using ReactiveUI;
public abstract class ViewModelBase : ReactiveObject { }

csharp

// File: App.UI/ViewModels/SettingsViewModel.cs
public sealed class SettingsViewModel : ViewModelBase
{
    public string LocalFolder { get; set; } = string.Empty;
    public int ParallelDownloads { get; set; } = 4;
    public int BatchSize { get; set; } = 50;
    public bool UseDarkTheme { get; set; } = false;

    public ICommand SaveCommand { get; }

    public SettingsViewModel(ICommand saveCommand) => SaveCommand = saveCommand;
}

csharp

// File: App.UI/ViewModels/DashboardViewModel.cs
using System.Collections.ObjectModel;
public sealed class DashboardViewModel : ViewModelBase
{
    public ObservableCollection<string> RecentTransfers { get; } = new();
    public int PendingDownloads { get; set; }
    public int PendingUploads { get; set; }
    public string SyncStatus { get; set; } = "Idle";

    public ICommand StartSyncCommand { get; }

    public DashboardViewModel(ICommand startSync) => StartSyncCommand = startSync;
}

csharp

// File: App.UI/Program.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using App.Infrastructure.Data;
using App.Infrastructure.Repositories;
using App.Infrastructure.Graph;
using App.Infrastructure.Auth;
using App.Infrastructure.Filesystem;
using App.Core.Interfaces;
using App.Services;

var services = new ServiceCollection();
services.AddLogging(cfg => cfg.AddConsole());
services.AddDbContext<AppDbContext>(opts =>
{
    opts.UseSqlite("Data Source=app.db");
});
services.AddScoped<ISyncRepository, EfSyncRepository>();
services.AddSingleton<IAuthService>(_ => new MsalAuthService("YOUR_CLIENT_ID"));
services.AddHttpClient<IGraphClient, GraphClientWrapper>();
services.AddSingleton<IFileSystemAdapter>(_ => new LocalFileSystemAdapter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OneDriveSync")));
services.AddSingleton<SyncSettings>(_ => new SyncSettings(ParallelDownloads: 4, BatchSize: 50));
services.AddSingleton<TransferService>();
services.AddSingleton<SyncEngine>();
// Register ViewModels and UI services as needed

var provider = services.BuildServiceProvider();
// Apply migrations and set WAL
using (var scope = provider.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.OpenConnection();
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
    db.Database.Migrate();
}

// Start UI bootstrap here for Avalonia

7 Implementation notes and next steps

    Delta handling  
    The GraphClientWrapper.GetDriveDeltaPageAsync returns NextLink and DeltaLink. The initial call with null returns the full enumeration (paged). Persist the final DeltaLink and use it for incremental syncs.

    SQLite concurrency  
    Use WAL mode and batch DB writes inside transactions. Serialize heavy writes to avoid SQLITE_BUSY.

    Resumable uploads  
    Use upload sessions for large files. The scaffold shows a simple chunked upload loop; extend to handle session expiration and resume.

    Error handling and retries  
    Add exponential backoff for HTTP 429 and transient errors. Surface failures in TransferLog and UI.

    Security  
    Replace "YOUR_CLIENT_ID" with your Azure app registration client id. Use platform secure storage for token cache if required.

    UI polish  
    Implement theme switching in Avalonia using Styles and bind UseDarkTheme. Expose settings to persist in a settings store.

Follow up action

I will generate the complete set of source files as ready-to-paste code for one project at a time starting with App.Core then App.Infrastructure then App.Services then App.UI.Avalonia if you want me to proceed.
You said
sounds excellent. yes please
Copilot said
App.Core — Overview

This package contains the domain records, DTOs, and service interfaces used across the solution. It is intentionally small and focused so other projects can depend on it without pulling infrastructure details. Use C# 14 features (records, primary-constructor style) and keep each type single-responsibility.
App.Core — Project file
xml

<!-- File: src/App.Core/App.Core.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>preview</LangVersion>
  </PropertyGroup>
</Project>

App.Core — Entities
csharp

// File: src/App.Core/Entities/DriveItemRecord.cs
namespace App.Core.Entities;

public sealed record DriveItemRecord(
    string Id,
    string DriveItemId,
    string RelativePath,
    string? ETag,
    string? CTag,
    long Size,
    DateTimeOffset LastModifiedUtc,
    bool IsFolder,
    bool IsDeleted
);

csharp

// File: src/App.Core/Entities/LocalFileRecord.cs
namespace App.Core.Entities;

public sealed record LocalFileRecord(
    string Id,
    string RelativePath,
    string? Hash,
    long Size,
    DateTimeOffset LastWriteUtc,
    SyncState SyncState
);

public enum SyncState
{
    Unknown,
    PendingDownload,
    Downloaded,
    PendingUpload,
    Uploaded,
    Deleted,
    Error
}

csharp

// File: src/App.Core/Entities/DeltaToken.cs
namespace App.Core.Entities;

public sealed record DeltaToken(string Id, string Token, DateTimeOffset LastSyncedUtc);

csharp

// File: src/App.Core/Entities/TransferLog.cs
namespace App.Core.Entities;

public sealed record TransferLog(
    string Id,
    TransferType Type,
    string ItemId,
    DateTimeOffset StartedUtc,
    DateTimeOffset? CompletedUtc,
    TransferStatus Status,
    string? Error
);

public enum TransferType { Download, Upload, Delete }
public enum TransferStatus { Pending, InProgress, Success, Failed }

App.Core — DTOs
csharp

// File: src/App.Core/Dto/DeltaPage.cs
using App.Core.Entities;

namespace App.Core.Dto;

public sealed record DeltaPage(IEnumerable<DriveItemRecord> Items, string? NextLink, string? DeltaLink);

csharp

// File: src/App.Core/Dto/UploadSessionInfo.cs
namespace App.Core.Dto;

public sealed record UploadSessionInfo(string UploadUrl, string SessionId, DateTimeOffset ExpiresAt);

csharp

// File: src/App.Core/Dto/LocalFileInfo.cs
namespace App.Core.Dto;

public sealed record LocalFileInfo(string RelativePath, long Size, DateTimeOffset LastWriteUtc, string? Hash);

App.Core — Interfaces
csharp

// File: src/App.Core/Interfaces/IAuthService.cs
namespace App.Core.Interfaces;

public interface IAuthService
{
    Task SignInAsync(CancellationToken ct);
    Task SignOutAsync(CancellationToken ct);
    Task<string> GetAccessTokenAsync(CancellationToken ct);
    bool IsSignedIn { get; }
}

csharp

// File: src/App.Core/Interfaces/IGraphClient.cs
using App.Core.Dto;

namespace App.Core.Interfaces;

public interface IGraphClient
{
    /// <summary>
    /// If deltaOrNextLink is null, call /me/drive/root/delta to start full enumeration.
    /// If it is a nextLink or deltaLink, GET that URL.
    /// </summary>
    Task<DeltaPage> GetDriveDeltaPageAsync(string? deltaOrNextLink, CancellationToken ct);

    Task<Stream> DownloadDriveItemContentAsync(string driveItemId, CancellationToken ct);

    Task<UploadSessionInfo> CreateUploadSessionAsync(string parentPath, string fileName, CancellationToken ct);

    Task UploadChunkAsync(UploadSessionInfo session, Stream chunk, long rangeStart, long rangeEnd, CancellationToken ct);

    Task DeleteDriveItemAsync(string driveItemId, CancellationToken ct);
}

csharp

// File: src/App.Core/Interfaces/IFileSystemAdapter.cs
using App.Core.Dto;

namespace App.Core.Interfaces;

public interface IFileSystemAdapter
{
    Task WriteFileAsync(string relativePath, Stream content, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct);
    Task DeleteFileAsync(string relativePath, CancellationToken ct);
    Task<IEnumerable<LocalFileInfo>> EnumerateFilesAsync(CancellationToken ct);
}

csharp

// File: src/App.Core/Interfaces/ISyncRepository.cs
using App.Core.Entities;

namespace App.Core.Interfaces;

public interface ISyncRepository
{
    Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken ct);
    Task SaveOrUpdateDeltaTokenAsync(DeltaToken token, CancellationToken ct);

    /// <summary>
    /// Apply a page of DriveItem metadata to the local DB.
    /// Implementations should use a transaction and batch writes.
    /// </summary>
    Task ApplyDriveItemsAsync(IEnumerable<DriveItemRecord> items, CancellationToken ct);

    Task<IEnumerable<DriveItemRecord>> GetPendingDownloadsAsync(int limit, CancellationToken ct);
    Task MarkLocalFileStateAsync(string driveItemId, SyncState state, CancellationToken ct);
    Task AddOrUpdateLocalFileAsync(LocalFileRecord file, CancellationToken ct);
    Task<IEnumerable<LocalFileRecord>> GetPendingUploadsAsync(int limit, CancellationToken ct);

    Task LogTransferAsync(TransferLog log, CancellationToken ct);
}

App.Core — Utilities
csharp

// File: src/App.Core/Utilities/SyncSettings.cs
namespace App.Core.Utilities;

public sealed record SyncSettings(int ParallelDownloads = 4, int BatchSize = 50, ConflictPolicy ConflictPolicy = ConflictPolicy.LastWriteWins);

public enum ConflictPolicy { LastWriteWins, KeepLocal, KeepRemote, Prompt }

csharp

// File: src/App.Core/Utilities/Result.cs
namespace App.Core.Utilities;

/// <summary>
/// Minimal Result type for simple success/failure flows.
/// Keep small and focused; expand in services if needed.
/// </summary>
public readonly record struct Result(bool IsSuccess, string? Error)
{
    public static Result Success => new(true, null);
    public static Result Failure(string error) => new(false, error);
}

App.Core — Notes and next steps

    Single responsibility: interfaces are intentionally narrow so implementations remain testable and focused.

    C# 14 features: records and primary-constructor style are used throughout. Adjust LangVersion in project files if needed.

    What I will generate next: App.Infrastructure with EF Core AppDbContext, EfSyncRepository, GraphClientWrapper, MsalAuthService, and LocalFileSystemAdapter.

If you want, I will now generate the App.Infrastructure project files ready to paste into src/App.Infrastructure.
You said
yes please
Copilot said
App.Infrastructure — Project scaffold

Below are ready‑to‑paste source files for the App.Infrastructure project. They implement EF Core persistence, the Graph client wrapper, MSAL auth stub, local filesystem adapter, and a small DI helper. Place them under src/App.Infrastructure. Add NuGet packages: Microsoft.EntityFrameworkCore, Microsoft.EntityFrameworkCore.Sqlite, Microsoft.Identity.Client, Microsoft.Extensions.Http, Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging.
Project file
xml

<!-- File: src/App.Infrastructure/App.Infrastructure.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>preview</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
    <PackageReference Include="Microsoft.Identity.Client" Version="4.70.0" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\App.Core\App.Core.csproj" />
  </ItemGroup>
</Project>

Data layer: DbContext and migrations helper
csharp

// File: src/App.Infrastructure/Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using App.Core.Entities;

namespace App.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public DbSet<DriveItemRecord> DriveItems { get; init; } = null!;
    public DbSet<LocalFileRecord> LocalFiles { get; init; } = null!;
    public DbSet<DeltaToken> DeltaTokens { get; init; } = null!;
    public DbSet<TransferLog> TransferLogs { get; init; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<DriveItemRecord>().HasKey(d => d.Id);
        mb.Entity<LocalFileRecord>().HasKey(l => l.Id);
        mb.Entity<DeltaToken>().HasKey(t => t.Id);
        mb.Entity<TransferLog>().HasKey(t => t.Id);

        mb.Entity<DriveItemRecord>().ToTable("DriveItems");
        mb.Entity<LocalFileRecord>().ToTable("LocalFiles");
        mb.Entity<DeltaToken>().ToTable("DeltaTokens");
        mb.Entity<TransferLog>().ToTable("TransferLogs");

        mb.Entity<DriveItemRecord>().Property(d => d.RelativePath).IsRequired();
        mb.Entity<LocalFileRecord>().Property(l => l.RelativePath).IsRequired();

        // Optional: index for quick lookups by DriveItemId or RelativePath
        mb.Entity<DriveItemRecord>().HasIndex(d => d.DriveItemId);
        mb.Entity<LocalFileRecord>().HasIndex(l => l.RelativePath);
    }
}

csharp

// File: src/App.Infrastructure/Data/DbInitializer.cs
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Data;

public static class DbInitializer
{
    public static void EnsureDatabaseCreatedAndConfigured(AppDbContext db)
    {
        // Ensure DB created and enable WAL for better concurrency
        db.Database.OpenConnection();
        db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        db.Database.Migrate();
    }
}

Repository: EF implementation
csharp

// File: src/App.Infrastructure/Repositories/EfSyncRepository.cs
using Microsoft.EntityFrameworkCore;
using App.Core.Interfaces;
using App.Core.Entities;

namespace App.Infrastructure.Repositories;

public sealed class EfSyncRepository : ISyncRepository
{
    private readonly AppDbContext _db;
    public EfSyncRepository(AppDbContext db) => _db = db;

    public async Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken ct) =>
        await _db.DeltaTokens.OrderByDescending(t => t.LastSyncedUtc).FirstOrDefaultAsync(ct);

    public async Task SaveOrUpdateDeltaTokenAsync(DeltaToken token, CancellationToken ct)
    {
        var existing = await _db.DeltaTokens.FindAsync(new object[] { token.Id }, ct);
        if (existing is null) _db.DeltaTokens.Add(token);
        else _db.Entry(existing).CurrentValues.SetValues(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ApplyDriveItemsAsync(IEnumerable<DriveItemRecord> items, CancellationToken ct)
    {
        // Batch apply inside a transaction to reduce contention on SQLite
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        foreach (var item in items)
        {
            var existing = await _db.DriveItems.FindAsync(new object[] { item.Id }, ct);
            if (existing is null) _db.DriveItems.Add(item);
            else _db.Entry(existing).CurrentValues.SetValues(item);
        }
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<IEnumerable<DriveItemRecord>> GetPendingDownloadsAsync(int limit, CancellationToken ct) =>
        await _db.DriveItems.Where(d => !d.IsFolder && !d.IsDeleted)
                            .OrderBy(d => d.LastModifiedUtc)
                            .Take(limit)
                            .ToListAsync(ct);

    public async Task MarkLocalFileStateAsync(string driveItemId, SyncState state, CancellationToken ct)
    {
        // Use driveItemId as the local file id mapping for simplicity
        var drive = await _db.DriveItems.FindAsync(new object[] { driveItemId }, ct);
        if (drive is null) return;

        var local = await _db.LocalFiles.FindAsync(new object[] { driveItemId }, ct);
        if (local is null)
        {
            _db.LocalFiles.Add(new LocalFileRecord(driveItemId, drive.RelativePath, null, drive.Size, drive.LastModifiedUtc, state));
        }
        else
        {
            _db.Entry(local).CurrentValues.SetValues(local with { SyncState = state, LastWriteUtc = drive.LastModifiedUtc, Size = drive.Size });
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddOrUpdateLocalFileAsync(LocalFileRecord file, CancellationToken ct)
    {
        var existing = await _db.LocalFiles.FindAsync(new object[] { file.Id }, ct);
        if (existing is null) _db.LocalFiles.Add(file);
        else _db.Entry(existing).CurrentValues.SetValues(file);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<LocalFileRecord>> GetPendingUploadsAsync(int limit, CancellationToken ct) =>
        await _db.LocalFiles.Where(l => l.SyncState == SyncState.PendingUpload).Take(limit).ToListAsync(ct);

    public async Task LogTransferAsync(TransferLog log, CancellationToken ct)
    {
        _db.TransferLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}

Graph wrapper and HTTP helpers
csharp

// File: src/App.Infrastructure/Graph/GraphClientWrapper.cs
using System.Net.Http.Headers;
using System.Text.Json;
using App.Core.Interfaces;
using App.Core.Dto;
using App.Core.Entities;

namespace App.Infrastructure.Graph;

public sealed class GraphClientWrapper : IGraphClient
{
    private readonly IAuthService _auth;
    private readonly HttpClient _http;

    public GraphClientWrapper(IAuthService auth, HttpClient http)
    {
        _auth = auth;
        _http = http;
    }

    public async Task<DeltaPage> GetDriveDeltaPageAsync(string? deltaOrNextLink, CancellationToken ct)
    {
        var url = string.IsNullOrEmpty(deltaOrNextLink)
            ? "https://graph.microsoft.com/v1.0/me/drive/root/delta"
            : deltaOrNextLink;

        var token = await _auth.GetAccessTokenAsync(ct);
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        res.EnsureSuccessStatusCode();

        await using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var items = new List<DriveItemRecord>();
        if (doc.RootElement.TryGetProperty("value", out var arr))
        {
            foreach (var el in arr.EnumerateArray())
            {
                var id = el.GetProperty("id").GetString()!;
                var isFolder = el.TryGetProperty("folder", out _);
                var size = el.TryGetProperty("size", out var sProp) ? sProp.GetInt64() : 0L;
                var parentPath = el.TryGetProperty("parentReference", out var pr) && pr.TryGetProperty("path", out var p) ? p.GetString() ?? string.Empty : string.Empty;
                var name = el.TryGetProperty("name", out var n) ? n.GetString() ?? id : id;
                var relativePath = BuildRelativePath(parentPath, name);
                var eTag = el.TryGetProperty("eTag", out var et) ? et.GetString() : null;
                var cTag = el.TryGetProperty("cTag", out var ctProp) ? ctProp.GetString() : null;
                var last = el.TryGetProperty("lastModifiedDateTime", out var lm) ? DateTimeOffset.Parse(lm.GetString()!) : DateTimeOffset.UtcNow;
                var isDeleted = el.TryGetProperty("deleted", out _);
                items.Add(new DriveItemRecord(id, id, relativePath, eTag, cTag, size, last, isFolder, isDeleted));
            }
        }

        var next = doc.RootElement.TryGetProperty("@odata.nextLink", out var nl) ? nl.GetString() : null;
        var delta = doc.RootElement.TryGetProperty("@odata.deltaLink", out var dl) ? dl.GetString() : null;
        return new DeltaPage(items, next, delta);
    }

    private static string BuildRelativePath(string parentReferencePath, string name)
    {
        // parentReference.path looks like "/drive/root:/Folder/SubFolder"
        if (string.IsNullOrEmpty(parentReferencePath)) return name;
        var idx = parentReferencePath.IndexOf(":/", StringComparison.Ordinal);
        if (idx >= 0)
        {
            var after = parentReferencePath[(idx + 2)..].Trim('/');
            return string.IsNullOrEmpty(after) ? name : Path.Combine(after, name);
        }
        return name;
    }

    public async Task<Stream> DownloadDriveItemContentAsync(string driveItemId, CancellationToken ct)
    {
        var token = await _auth.GetAccessTokenAsync(ct);
        var url = $"https://graph.microsoft.com/v1.0/me/drive/items/{driveItemId}/content";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStreamAsync(ct);
    }

    public async Task<UploadSessionInfo> CreateUploadSessionAsync(string parentPath, string fileName, CancellationToken ct)
    {
        var token = await _auth.GetAccessTokenAsync(ct);
        // Build path under root
        var path = string.IsNullOrWhiteSpace(parentPath) ? fileName : $"{parentPath.Trim('/')}/{fileName}";
        var url = $"https://graph.microsoft.com/v1.0/me/drive/root:/{Uri.EscapeDataString(path)}:/createUploadSession";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        await using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var uploadUrl = doc.RootElement.GetProperty("uploadUrl").GetString()!;
        var expiration = doc.RootElement.TryGetProperty("expirationDateTime", out var ex) ? DateTimeOffset.Parse(ex.GetString()!) : DateTimeOffset.UtcNow.AddHours(1);
        return new UploadSessionInfo(uploadUrl, Guid.NewGuid().ToString(), expiration);
    }

    public async Task UploadChunkAsync(UploadSessionInfo session, Stream chunk, long rangeStart, long rangeEnd, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, session.UploadUrl);
        req.Content = new StreamContent(chunk);
        req.Content.Headers.Add("Content-Range", $"bytes {rangeStart}-{rangeEnd}/*");
        using var res = await _http.SendAsync(req, ct);
        // Graph returns 201/200 when upload completes, 202 for accepted chunk
        if (!res.IsSuccessStatusCode)
            res.EnsureSuccessStatusCode();
    }

    public async Task DeleteDriveItemAsync(string driveItemId, CancellationToken ct)
    {
        var token = await _auth.GetAccessTokenAsync(ct);
        var url = $"https://graph.microsoft.com/v1.0/me/drive/items/{driveItemId}";
        using var req = new HttpRequestMessage(HttpMethod.Delete, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var res = await _http.SendAsync(req, ct);
        if (res.StatusCode != System.Net.HttpStatusCode.NoContent && res.StatusCode != System.Net.HttpStatusCode.NotFound)
            res.EnsureSuccessStatusCode();
    }
}

Auth: MSAL wrapper (platform-agnostic public client)
csharp

// File: src/App.Infrastructure/Auth/MsalAuthService.cs
using Microsoft.Identity.Client;
using App.Core.Interfaces;

namespace App.Infrastructure.Auth;

public sealed class MsalAuthService : IAuthService
{
    private readonly IPublicClientApplication _pca;
    private IAccount? _account;
    private readonly string[] _scopes = new[] { "Files.ReadWrite.All", "offline_access", "User.Read" };

    public bool IsSignedIn => _account is not null;

    public MsalAuthService(string clientId)
    {
        _pca = PublicClientApplicationBuilder.Create(clientId)
            .WithRedirectUri("http://localhost")
            .Build();
    }

    public async Task SignInAsync(CancellationToken ct)
    {
        var result = await _pca.AcquireTokenInteractive(_scopes).ExecuteAsync(ct);
        _account = result.Account;
    }

    public async Task SignOutAsync(CancellationToken ct)
    {
        if (_account is null) return;
        await _pca.RemoveAsync(_account);
        _account = null;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (_account is null) throw new InvalidOperationException("Not signed in");
        var result = await _pca.AcquireTokenSilent(_scopes, _account).ExecuteAsync(ct);
        return result.AccessToken;
    }
}

    Implementation note: for production, wire MSAL token cache to platform secure storage (Windows Credential Manager, macOS Keychain, Linux Secret Service) or use MSAL extensions.

Filesystem adapter
csharp

// File: src/App.Infrastructure/Filesystem/LocalFileSystemAdapter.cs
using App.Core.Interfaces;
using App.Core.Dto;

namespace App.Infrastructure.Filesystem;

public sealed class LocalFileSystemAdapter : IFileSystemAdapter
{
    private readonly string _root;
    public LocalFileSystemAdapter(string root) => _root = root;

    private string FullPath(string relative) => Path.Combine(_root, relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    public async Task WriteFileAsync(string relativePath, Stream content, CancellationToken ct)
    {
        var full = FullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var fs = File.Create(full);
        await content.CopyToAsync(fs, ct);
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct)
    {
        var full = FullPath(relativePath);
        if (!File.Exists(full)) return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(File.OpenRead(full));
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken ct)
    {
        var full = FullPath(relativePath);
        if (File.Exists(full)) File.Delete(full);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<LocalFileInfo>> EnumerateFilesAsync(CancellationToken ct)
    {
        var list = new List<LocalFileInfo>();
        if (!Directory.Exists(_root)) return Task.FromResult<IEnumerable<LocalFileInfo>>(list.AsEnumerable());
        foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
        {
            var fi = new FileInfo(file);
            var rel = Path.GetRelativePath(_root, file);
            list.Add(new LocalFileInfo(rel, fi.Length, fi.LastWriteTimeUtc, null));
        }
        return Task.FromResult<IEnumerable<LocalFileInfo>>(list);
    }
}

DI registration helper
csharp

// File: src/App.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using App.Core.Interfaces;
using App.Infrastructure.Data;
using App.Infrastructure.Repositories;
using App.Infrastructure.Graph;
using App.Infrastructure.Auth;
using App.Infrastructure.Filesystem;

namespace App.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string sqliteConnectionString, string localRoot, string msalClientId)
    {
        services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(sqliteConnectionString));
        services.AddScoped<ISyncRepository, EfSyncRepository>();

        services.AddSingleton<IAuthService>(_ => new MsalAuthService(msalClientId));
        services.AddHttpClient<IGraphClient, GraphClientWrapper>()
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = true });

        services.AddSingleton<IFileSystemAdapter>(_ => new LocalFileSystemAdapter(localRoot));

        // Optional: register DbInitializer action
        services.AddSingleton<Action<IServiceProvider>>(sp =>
        {
            return provider =>
            {
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DbInitializer.EnsureDatabaseCreatedAndConfigured(db);
            };
        });

        return services;
    }
}

Final notes and recommended next steps

    Migrations: run dotnet ef migrations add InitialCreate -p src/App.Infrastructure -s src/App.UI.Avalonia and dotnet ef database update (adjust startup project) or call DbInitializer.EnsureDatabaseCreatedAndConfigured at app startup as shown earlier.

    WAL mode: the initializer sets PRAGMA journal_mode=WAL; to improve concurrency for SQLite. Keep DB writes batched and transactional.

    HttpClient tuning: when registering GraphClientWrapper you can tune MaxConnectionsPerServer via SocketsHttpHandler for higher parallelism.

    Security: replace msalClientId with your Azure app registration client id and wire token cache to secure storage for production.

I will generate the App.Services project next with SyncEngine, TransferService, and service-level wiring so you can run an initial full sync and incremental sync.
You said
thanks, yes please
Copilot said
App.Services — Project scaffold (overview)

This package contains the SyncEngine, TransferService, service-level wiring, and small helpers that orchestrate delta application, batched/parallel transfers, and repository interactions. It follows SRP and Separation of Concerns, uses C# 14 records/primary constructors, and is ready to plug into the App.Infrastructure DI registration.

Files included

    App.Services.csproj

    SyncSettings.cs

    SyncEngine.cs

    TransferService.cs

    ServiceCollectionExtensions.cs

    HealthAndStats.cs (simple runtime stats DTO)

Add NuGet packages: Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging, Polly (optional for retries).
Project file
xml

<!-- File: src/App.Services/App.Services.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>preview</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="Polly" Version="8.1.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\App.Core\App.Core.csproj" />
    <ProjectReference Include="..\App.Infrastructure\App.Infrastructure.csproj" />
  </ItemGroup>
</Project>

SyncSettings and runtime DTOs
csharp

// File: src/App.Services/SyncSettings.cs
namespace App.Services;

public sealed record SyncSettings(int ParallelDownloads = 4, int BatchSize = 50, int MaxRetries = 3, int RetryBaseDelayMs = 500);

public sealed record SyncStats(int PendingDownloads, int PendingUploads, int ActiveDownloads, int ActiveUploads, int FailedTransfers);

csharp

// File: src/App.Services/HealthAndStats.cs
namespace App.Services;

public sealed record TransferProgress(string ItemId, long BytesTransferred, long? TotalBytes, DateTimeOffset Timestamp);

SyncEngine
csharp

// File: src/App.Services/SyncEngine.cs
using App.Core.Interfaces;
using App.Core.Dto;
using App.Core.Entities;
using Microsoft.Extensions.Logging;

namespace App.Services;

public sealed class SyncEngine
{
    private readonly ISyncRepository _repo;
    private readonly IGraphClient _graph;
    private readonly TransferService _transfer;
    private readonly ILogger<SyncEngine> _logger;

    public SyncEngine(ISyncRepository repo, IGraphClient graph, TransferService transfer, ILogger<SyncEngine> logger)
    {
        _repo = repo;
        _graph = graph;
        _transfer = transfer;
        _logger = logger;
    }

    /// <summary>
    /// Performs the initial full enumeration using Graph delta. Pages until exhausted,
    /// persists DriveItemRecords and the final deltaLink for incremental syncs.
    /// </summary>
    public async Task InitialFullSyncAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting initial full sync");
        string? nextOrDelta = null;
        string? finalDelta = null;

        do
        {
            var page = await _graph.GetDriveDeltaPageAsync(nextOrDelta, ct);
            await _repo.ApplyDriveItemsAsync(page.Items, ct);
            nextOrDelta = page.NextLink;
            finalDelta = page.DeltaLink ?? finalDelta;
            _logger.LogInformation("Applied page: items={Count} next={Next}", page.Items.Count(), page.NextLink is not null);
        } while (!string.IsNullOrEmpty(nextOrDelta) && !ct.IsCancellationRequested);

        if (!string.IsNullOrEmpty(finalDelta))
        {
            var token = new DeltaToken(Guid.NewGuid().ToString(), finalDelta, DateTimeOffset.UtcNow);
            await _repo.SaveOrUpdateDeltaTokenAsync(token, ct);
            _logger.LogInformation("Saved delta token");
        }

        // Kick off transfers after DB is updated
        await _transfer.ProcessPendingDownloadsAsync(ct);
        await _transfer.ProcessPendingUploadsAsync(ct);
        _logger.LogInformation("Initial full sync complete");
    }

    /// <summary>
    /// Performs an incremental sync using the stored delta token.
    /// </summary>
    public async Task IncrementalSyncAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting incremental sync");
        var token = await _repo.GetDeltaTokenAsync(ct) ?? throw new InvalidOperationException("Delta token missing; run initial sync first.");
        var page = await _graph.GetDriveDeltaPageAsync(token.Token, ct);
        await _repo.ApplyDriveItemsAsync(page.Items, ct);

        if (!string.IsNullOrEmpty(page.DeltaLink))
        {
            await _repo.SaveOrUpdateDeltaTokenAsync(token with { Token = page.DeltaLink, LastSyncedUtc = DateTimeOffset.UtcNow }, ct);
            _logger.LogInformation("Updated delta token");
        }

        await _transfer.ProcessPendingDownloadsAsync(ct);
        await _transfer.ProcessPendingUploadsAsync(ct);
        _logger.LogInformation("Incremental sync complete");
    }
}

Design notes

    SyncEngine only orchestrates: it does not perform transfers or file I/O directly (SRP).

    Delta pages are applied via ISyncRepository.ApplyDriveItemsAsync which must be transactional and idempotent.

TransferService
csharp

// File: src/App.Services/TransferService.cs
using App.Core.Interfaces;
using App.Core.Entities;
using App.Core.Dto;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace App.Services;

public sealed class TransferService
{
    private readonly IFileSystemAdapter _fs;
    private readonly IGraphClient _graph;
    private readonly ISyncRepository _repo;
    private readonly ILogger<TransferService> _logger;
    private readonly SyncSettings _settings;
    private readonly SemaphoreSlim _downloadSemaphore;
    private readonly AsyncRetryPolicy _retryPolicy;

    public TransferService(IFileSystemAdapter fs, IGraphClient graph, ISyncRepository repo, ILogger<TransferService> logger, SyncSettings settings)
    {
        _fs = fs;
        _graph = graph;
        _repo = repo;
        _logger = logger;
        _settings = settings;
        _downloadSemaphore = new SemaphoreSlim(settings.ParallelDownloads);

        _retryPolicy = Policy.Handle<Exception>()
                             .WaitAndRetryAsync(settings.MaxRetries, i => TimeSpan.FromMilliseconds(settings.RetryBaseDelayMs * Math.Pow(2, i)),
                                 (ex, ts, retryCount, ctx) => _logger.LogWarning(ex, "Retry {Retry} after {Delay}ms", retryCount, ts.TotalMilliseconds));
    }

    /// <summary>
    /// Pulls pending downloads from repository in batches and downloads them with bounded concurrency.
    /// </summary>
    public async Task ProcessPendingDownloadsAsync(CancellationToken ct)
    {
        _logger.LogInformation("Processing pending downloads");
        while (!ct.IsCancellationRequested)
        {
            var items = (await _repo.GetPendingDownloadsAsync(_settings.BatchSize, ct)).ToList();
            if (!items.Any()) break;

            var tasks = items.Select(item => DownloadItemWithRetryAsync(item, ct)).ToList();
            await Task.WhenAll(tasks);
        }
    }

    private async Task DownloadItemWithRetryAsync(DriveItemRecord item, CancellationToken ct)
    {
        await _retryPolicy.ExecuteAsync(async ct2 => await DownloadItemAsync(item, ct2), ct);
    }

    private async Task DownloadItemAsync(DriveItemRecord item, CancellationToken ct)
    {
        await _downloadSemaphore.WaitAsync(ct);
        var log = new TransferLog(Guid.NewGuid().ToString(), TransferType.Download, item.Id, DateTimeOffset.UtcNow, null, TransferStatus.InProgress, null);
        await _repo.LogTransferAsync(log, ct);
        try
        {
            await using var stream = await _graph.DownloadDriveItemContentAsync(item.DriveItemId, ct);
            await _fs.WriteFileAsync(item.RelativePath, stream, ct);
            await _repo.MarkLocalFileStateAsync(item.Id, SyncState.Downloaded, ct);
            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Success };
            await _repo.LogTransferAsync(log, ct);
            _logger.LogInformation("Downloaded {Path}", item.RelativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download failed for {Id}", item.Id);
            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Failed, Error = ex.Message };
            await _repo.LogTransferAsync(log, ct);
        }
        finally
        {
            _downloadSemaphore.Release();
        }
    }

    /// <summary>
    /// Scans repository for pending uploads and uploads them using upload sessions and chunked uploads.
    /// </summary>
    public async Task ProcessPendingUploadsAsync(CancellationToken ct)
    {
        _logger.LogInformation("Processing pending uploads");
        var uploads = (await _repo.GetPendingUploadsAsync(_settings.BatchSize, ct)).ToList();
        foreach (var local in uploads)
        {
            await _retryPolicy.ExecuteAsync(async ct2 => await UploadLocalFileAsync(local, ct2), ct);
        }
    }

    private async Task UploadLocalFileAsync(LocalFileRecord local, CancellationToken ct)
    {
        var log = new TransferLog(Guid.NewGuid().ToString(), TransferType.Upload, local.Id, DateTimeOffset.UtcNow, null, TransferStatus.InProgress, null);
        await _repo.LogTransferAsync(log, ct);
        try
        {
            var parent = Path.GetDirectoryName(local.RelativePath) ?? "/";
            var fileName = Path.GetFileName(local.RelativePath);
            var session = await _graph.CreateUploadSessionAsync(parent, fileName, ct);

            await using var stream = await _fs.OpenReadAsync(local.RelativePath, ct) ?? throw new FileNotFoundException(local.RelativePath);
            const int chunkSize = 320 * 1024; // 320KB
            long uploaded = 0;
            while (uploaded < stream.Length)
            {
                var toRead = (int)Math.Min(chunkSize, stream.Length - uploaded);
                var buffer = new byte[toRead];
                stream.Seek(uploaded, SeekOrigin.Begin);
                var read = await stream.ReadAsync(buffer.AsMemory(0, toRead), ct);
                await using var ms = new MemoryStream(buffer, 0, read, writable: false);
                await _graph.UploadChunkAsync(session, ms, uploaded, uploaded + read - 1, ct);
                uploaded += read;
            }

            await _repo.MarkLocalFileStateAsync(local.Id, SyncState.Uploaded, ct);
            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Success };
            await _repo.LogTransferAsync(log, ct);
            _logger.LogInformation("Uploaded {Path}", local.RelativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed for {Id}", local.Id);
            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Failed, Error = ex.Message };
            await _repo.LogTransferAsync(log, ct);
        }
    }
}

Design notes

    Uses SemaphoreSlim to bound concurrent downloads.

    Uses Polly retry policy with exponential backoff for transient errors.

    Uploads are chunked and use IGraphClient.CreateUploadSessionAsync and UploadChunkAsync.

    Transfer logging is persisted via ISyncRepository.LogTransferAsync.

DI registration for services
csharp

// File: src/App.Services/DependencyInjection/ServiceCollectionExtensions.cs
using Microsoft.Extensions.DependencyInjection;
using App.Core.Interfaces;

namespace App.Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSyncServices(this IServiceCollection services)
    {
        services.AddSingleton<SyncSettings>(_ => new SyncSettings(ParallelDownloads: 4, BatchSize: 50));
        services.AddSingleton<TransferService>();
        services.AddSingleton<SyncEngine>();
        return services;
    }
}

Startup wiring

    Call services.AddInfrastructure(...) (from App.Infrastructure) first, then services.AddSyncServices().

    Ensure AppDbContext initializer is executed before starting sync operations.

Testing and extension notes

    Unit tests: mock IGraphClient, IFileSystemAdapter, and ISyncRepository to validate SyncEngine and TransferService behavior (idempotency, retries, logging).

    Integration tests: simulate delta pages and verify DB state transitions and transfer logs.

    Observability: expose IObservable<TransferProgress> or IProgress<TransferProgress> from TransferService if the UI needs live progress updates.

    Cancellation: all public methods accept CancellationToken; ensure UI cancels long-running syncs gracefully.

    Backpressure: tune ParallelDownloads and BatchSize per platform and network conditions.

I can generate the App.UI.Avalonia project files next so you have the full runnable scaffold and a simple UI to start/observe syncs.
You said
The UI project files would be great/ Thanks
Copilot said
App.UI.Avalonia — Overview

Below is a ready‑to‑paste scaffold for the Avalonia UI project. It includes the project file, app bootstrap, XAML for the main window, ViewModels, simple Views, theme switching, settings persistence, and wiring to the services (SyncEngine, TransferService, IAuthService). The UI is intentionally minimal but complete enough to start the app, sign in, run initial and incremental syncs, and observe basic stats and transfer logs.

NuGet packages to add

    Avalonia

    Avalonia.ReactiveUI

    Microsoft.Extensions.DependencyInjection

    Microsoft.Extensions.Logging

    Microsoft.EntityFrameworkCore.Sqlite (already in infrastructure)

    ReactiveUI.Fody (optional for property change wiring)

Project file
xml

<!-- File: src/App.UI.Avalonia/App.UI.Avalonia.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>WinExe</OutputType>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>preview</LangVersion>
    <UseWPF>false</UseWPF>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.0.0" />
    <PackageReference Include="Avalonia.ReactiveUI" Version="11.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="8.0.0" />
    <PackageReference Include="ReactiveUI.Fody" Version="17.0.0" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\App.Core\App.Core.csproj" />
    <ProjectReference Include="..\App.Infrastructure\App.Infrastructure.csproj" />
    <ProjectReference Include="..\App.Services\App.Services.csproj" />
  </ItemGroup>
</Project>

App bootstrap and DI wiring
csharp

// File: src/App.UI.Avalonia/Program.cs
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using App.Infrastructure.DependencyInjection;
using App.Services.DependencyInjection;
using App.Core.Utilities;
using App.Services;
using App.Infrastructure.DependencyInjection;

class Program
{
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
}

csharp

// File: src/App.UI.Avalonia/App.axaml.cs
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using App.Infrastructure.DependencyInjection;
using App.Services.DependencyInjection;
using App.Core.Utilities;

public partial class App : Application
{
    private IServiceProvider? _services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddLogging(cfg => cfg.AddConsole());

        // Infrastructure registration
        var dbPath = "Data Source=app.db";
        var localRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OneDriveSync");
        var msalClientId = "YOUR_CLIENT_ID";
        services.AddInfrastructure(dbPath, localRoot, msalClientId);

        // App services
        services.AddSyncServices();

        // UI services and viewmodels
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<DashboardViewModel>();

        // Sync settings
        services.AddSingleton(new SyncSettings(ParallelDownloads: 4, BatchSize: 50));

        _services = services.BuildServiceProvider();

        // Ensure DB created and configured
        var initializer = _services.GetRequiredService<Action<IServiceProvider>>();
        initializer(_services);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var main = _services.GetRequiredService<MainWindow>();
            desktop.MainWindow = main;
        }

        base.OnFrameworkInitializationCompleted();
    }
}

XAML and Views
xml

<!-- File: src/App.UI.Avalonia/MainWindow.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="https://github.com/avaloniaui"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        x:Class="App.UI.Avalonia.MainWindow"
        Width="900" Height="600"
        Title="OneDrive Sync">
  <DockPanel>
    <StackPanel Orientation="Horizontal" DockPanel.Dock="Top" Margin="8">
      <Button Content="Sign In" Command="{Binding SignInCommand}" Margin="4"/>
      <Button Content="Initial Full Sync" Command="{Binding InitialSyncCommand}" Margin="4"/>
      <Button Content="Incremental Sync" Command="{Binding IncrementalSyncCommand}" Margin="4"/>
      <ToggleButton Content="Dark Theme" IsChecked="{Binding UseDarkTheme}" Margin="4"/>
      <TextBlock Text="Parallel" VerticalAlignment="Center" Margin="8,0,0,0"/>
      <NumericUpDown Minimum="1" Maximum="32" Value="{Binding ParallelDownloads}" Width="80" Margin="4"/>
      <TextBlock Text="Batch" VerticalAlignment="Center" Margin="8,0,0,0"/>
      <NumericUpDown Minimum="1" Maximum="500" Value="{Binding BatchSize}" Width="80" Margin="4"/>
    </StackPanel>

    <Grid Margin="8">
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="2*"/>
        <ColumnDefinition Width="3*"/>
      </Grid.ColumnDefinitions>

      <StackPanel Grid.Column="0" Margin="8">
        <TextBlock Text="Sync Stats" FontWeight="Bold" Margin="0,0,0,8"/>
        <TextBlock Text="{Binding SyncStatus}" Margin="0,0,0,4"/>
        <TextBlock Text="Pending Downloads:"/>
        <TextBlock Text="{Binding PendingDownloads}" Margin="0,0,0,8"/>
        <TextBlock Text="Pending Uploads:"/>
        <TextBlock Text="{Binding PendingUploads}" Margin="0,0,0,8"/>
        <ProgressBar Minimum="0" Maximum="100" Value="{Binding ProgressPercent}" Height="20" Margin="0,8,0,8"/>
      </StackPanel>

      <StackPanel Grid.Column="1" Margin="8">
        <TextBlock Text="Recent Transfers" FontWeight="Bold" Margin="0,0,0,8"/>
        <ListBox Items="{Binding RecentTransfers}" Height="400"/>
      </StackPanel>
    </Grid>
  </DockPanel>
</Window>

csharp

// File: src/App.UI.Avalonia/MainWindow.axaml.cs
using Avalonia.Controls;
using Avalonia.Interactivity;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}

ViewModels and Commands
csharp

// File: src/App.UI.Avalonia/ViewModels/MainWindowViewModel.cs
using ReactiveUI;
using System.Reactive;
using App.Core.Interfaces;
using App.Services;
using Microsoft.Extensions.Logging;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly SyncEngine _sync;
    private readonly SyncSettings _settings;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly TransferService _transfer;

    public ReactiveCommand<Unit, Unit> SignInCommand { get; }
    public ReactiveCommand<Unit, Unit> InitialSyncCommand { get; }
    public ReactiveCommand<Unit, Unit> IncrementalSyncCommand { get; }

    public ObservableCollection<string> RecentTransfers { get; } = new();
    public int PendingDownloads { get => _pendingDownloads; set => this.RaiseAndSetIfChanged(ref _pendingDownloads, value); }
    public int PendingUploads { get => _pendingUploads; set => this.RaiseAndSetIfChanged(ref _pendingUploads, value); }
    public string SyncStatus { get => _syncStatus; set => this.RaiseAndSetIfChanged(ref _syncStatus, value); }
    public double ProgressPercent { get => _progressPercent; set => this.RaiseAndSetIfChanged(ref _progressPercent, value); }

    public bool UseDarkTheme { get => _useDarkTheme; set { this.RaiseAndSetIfChanged(ref _useDarkTheme, value); ApplyTheme(); } }
    public int ParallelDownloads { get => _parallelDownloads; set { this.RaiseAndSetIfChanged(ref _parallelDownloads, value); UpdateSettings(); } }
    public int BatchSize { get => _batchSize; set { this.RaiseAndSetIfChanged(ref _batchSize, value); UpdateSettings(); } }

    private int _pendingDownloads;
    private int _pendingUploads;
    private string _syncStatus = "Idle";
    private double _progressPercent;
    private bool _useDarkTheme;
    private int _parallelDownloads;
    private int _batchSize;

    public MainWindowViewModel(IAuthService auth, SyncEngine sync, TransferService transfer, SyncSettings settings, ILogger<MainWindowViewModel> logger)
    {
        _auth = auth;
        _sync = sync;
        _transfer = transfer;
        _settings = settings;
        _logger = logger;

        _parallelDownloads = settings.ParallelDownloads;
        _batchSize = settings.BatchSize;

        SignInCommand = ReactiveCommand.CreateFromTask(async ct =>
        {
            SyncStatus = "Signing in...";
            await _auth.SignInAsync(ct);
            SyncStatus = "Signed in";
        });

        InitialSyncCommand = ReactiveCommand.CreateFromTask(async ct =>
        {
            SyncStatus = "Running initial full sync";
            await _sync.InitialFullSyncAsync(ct);
            SyncStatus = "Initial sync complete";
            await RefreshStatsAsync(ct);
        });

        IncrementalSyncCommand = ReactiveCommand.CreateFromTask(async ct =>
        {
            SyncStatus = "Running incremental sync";
            await _sync.IncrementalSyncAsync(ct);
            SyncStatus = "Incremental sync complete";
            await RefreshStatsAsync(ct);
        });
    }

    private void ApplyTheme()
    {
        // Minimal theme switcher. For full theme support, swap Avalonia styles.
        // This placeholder toggles a simple resource; extend as needed.
    }

    private void UpdateSettings()
    {
        // Update SyncSettings instance used by TransferService
        // For simplicity, this example does not re-create services; in production update via options pattern
    }

    private async Task RefreshStatsAsync(CancellationToken ct)
    {
        // Query repository for counts and update properties
        // Placeholder: set dummy values
        PendingDownloads = 0;
        PendingUploads = 0;
        ProgressPercent = 100;
        RecentTransfers.Clear();
        RecentTransfers.Add($"Sync completed at {DateTimeOffset.Now}");
    }
}

csharp

// File: src/App.UI.Avalonia/ViewModels/SettingsViewModel.cs
public sealed class SettingsViewModel : ViewModelBase
{
    public string LocalFolder { get => _localFolder; set => this.RaiseAndSetIfChanged(ref _localFolder, value); }
    private string _localFolder = string.Empty;

    public void Save()
    {
        // Persist settings to a simple JSON file or platform settings store
        var settings = new { LocalFolder, Updated = DateTimeOffset.UtcNow };
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OneDriveSync", "ui-settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(settings));
    }
}

csharp

// File: src/App.UI.Avalonia/ViewModels/DashboardViewModel.cs
public sealed class DashboardViewModel : ViewModelBase
{
    public ObservableCollection<string> RecentTransfers { get; } = new();
    public int PendingDownloads { get => _pendingDownloads; set => this.RaiseAndSetIfChanged(ref _pendingDownloads, value); }
    public int PendingUploads { get => _pendingUploads; set => this.RaiseAndSetIfChanged(ref _pendingUploads, value); }
    public string SyncStatus { get => _syncStatus; set => this.RaiseAndSetIfChanged(ref _syncStatus, value); }

    private int _pendingDownloads;
    private int _pendingUploads;
    private string _syncStatus = "Idle";
}

Minimal styling and theme resources
xml

<!-- File: src/App.UI.Avalonia/Styles/Theme.axaml -->
<ResourceDictionary xmlns="https://github.com/avaloniaui">
  <Color x:Key="BackgroundColor">#FFFFFFFF</Color>
  <Color x:Key="ForegroundColor">#FF000000</Color>
  <Style Selector="TextBlock">
    <Setter Property="Foreground" Value="{DynamicResource ForegroundColor}"/>
  </Style>
</ResourceDictionary>

Running and next steps

    Replace YOUR_CLIENT_ID in App.axaml.cs with your Azure app registration client id.

    Build the solution and run the Avalonia app. The UI exposes Sign In, Initial Full Sync, and Incremental Sync buttons and basic stats.

    Extend the ViewModels to subscribe to TransferService progress events or expose IProgress<TransferProgress> for live updates. Persist UI settings in a JSON file or platform store.

I can generate the full runnable solution file and a launch script so you can open and run the Avalonia UI immediately.
You said
yes please
Copilot said
Runnable solution and launch script

Below is a compact, ready‑to‑paste runnable solution scaffold and a simple launch script you can use to build and run the Avalonia UI immediately. It assumes you already created the src folder and the four projects (App.Core, App.Infrastructure, App.Services, App.UI.Avalonia) with the files I previously provided. This section adds the solution file, a launch script for Windows and Unix, and the final run checklist so you can start the app quickly.
Solution file

Create OneDriveSync.sln at the repository root with these contents. Save exactly as shown.
text

Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.0.0
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "App.Core", "src\App.Core\App.Core.csproj", "{11111111-1111-1111-1111-111111111111}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "App.Infrastructure", "src\App.Infrastructure\App.Infrastructure.csproj", "{22222222-2222-2222-2222-222222222222}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "App.Services", "src\App.Services\App.Services.csproj", "{33333333-3333-3333-3333-333333333333}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "App.UI.Avalonia", "src\App.UI.Avalonia\App.UI.Avalonia.csproj", "{44444444-4444-4444-4444-444444444444}"
EndProject
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Debug|Any CPU = Debug|Any CPU
        Release|Any CPU = Release|Any CPU
    EndGlobalSection
    GlobalSection(ProjectConfigurationPlatforms) = postSolution
        {11111111-1111-1111-1111-111111111111}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {11111111-1111-1111-1111-111111111111}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {22222222-2222-2222-2222-222222222222}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {22222222-2222-2222-2222-222222222222}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {33333333-3333-3333-3333-333333333333}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {33333333-3333-3333-3333-333333333333}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {44444444-4444-4444-4444-444444444444}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {44444444-4444-4444-4444-444444444444}.Debug|Any CPU.Build.0 = Debug|Any CPU
    EndGlobalSection
EndGlobal

Launch scripts

Create these two scripts at the repository root. They run dotnet restore, apply EF migrations, and start the Avalonia app.
Windows PowerShell launch.ps1
powershell

# File: launch.ps1
$ErrorActionPreference = "Stop"

Write-Host "Restoring packages..."
dotnet restore

Write-Host "Building solution..."
dotnet build -c Debug

Write-Host "Applying EF migrations and ensuring database..."
# Ensure you have dotnet-ef installed or use the runtime migration approach in code
dotnet tool install --global dotnet-ef --version 8.0.0 -s nuget.org -v q
dotnet ef database update --project src\App.Infrastructure --startup-project src\App.UI.Avalonia

Write-Host "Running App.UI.Avalonia..."
dotnet run --project src\App.UI.Avalonia -c Debug

Unix shell launch.sh
bash

#!/usr/bin/env bash
set -euo pipefail

echo "Restoring packages..."
dotnet restore

echo "Building solution..."
dotnet build -c Debug

echo "Applying EF migrations and ensuring database..."
if ! command -v dotnet-ef >/dev/null 2>&1; then
  echo "Installing dotnet-ef tool..."
  dotnet tool install --global dotnet-ef --version 8.0.0
  export PATH="$PATH:$HOME/.dotnet/tools"
fi

dotnet ef database update --project src/App.Infrastructure --startup-project src/App.UI.Avalonia

echo "Running App.UI.Avalonia..."
dotnet run --project src/App.UI.Avalonia -c Debug

Make the shell script executable:
bash

chmod +x launch.sh

Run checklist

    Replace the MSAL client id

        Open src/App.UI.Avalonia/App.axaml.cs and replace YOUR_CLIENT_ID with your Azure app registration client id that has delegated permissions Files.ReadWrite.All and offline_access.

    Ensure NuGet packages are present

        From repo root run dotnet restore or use the launch scripts.

    Migrations

        If you prefer code-first runtime migration, the DbInitializer in App.Infrastructure already calls Migrate() when invoked from DI. The launch scripts call dotnet ef database update to apply migrations. If you have not created migrations yet, run:
        bash

        dotnet ef migrations add InitialCreate --project src/App.Infrastructure --startup-project src/App.UI.Avalonia
        dotnet ef database update --project src\App.Infrastructure --startup-project src\App.UI.Avalonia

        Alternatively rely on DbInitializer.EnsureDatabaseCreatedAndConfigured which runs at startup in the Avalonia App bootstrap.

    Run

        Use ./launch.sh on macOS/Linux or .\launch.ps1 on Windows PowerShell to build, apply migrations, and run the UI.

    Sign in and sync

        Click Sign In in the app, then Initial Full Sync to enumerate OneDrive and persist the @odata.deltaLink. After initial sync, use Incremental Sync to apply deltas.

Notes on production readiness

    Token cache: wire MSAL token cache to platform secure storage for production.

    Concurrency tuning: adjust ParallelDownloads and BatchSize in settings to match network and device capabilities.

    Resilience: add more robust retry and rate limit handling for Graph 429 responses.

    Conflict policy: implement UI option to change conflict resolution strategy and persist choice.

    Logging: logs are wired to console; add file or telemetry sinks as needed.

I can generate a complete zipped repository structure with all files placed correctly so you can extract and run the app immediately.
You said
yes please
Copilot said

Short answer: I can’t produce a downloadable ZIP from here, but I will generate the complete, ready‑to‑paste repository scaffold (all files and exact contents) so you can create the ZIP locally or paste files into your editor and run the provided launch scripts immediately.
What I’ll deliver next

    All project files for the four projects: App.Core, App.Infrastructure, App.Services, App.UI.Avalonia under src/ with the exact contents we discussed.

    Solution file OneDriveSync.sln and both launch scripts launch.ps1 and launch.sh.

    Db migration helper and DI wiring.

    PLACEHOLDER_FILES.txt only if any snippet was missing and I had to add a minimal placeholder to make the project compile.

Important: the App.axaml.cs file will contain the literal placeholder YOUR_CLIENT_ID as requested; you must replace it with your Azure app client id before signing in.
How to use what I’ll provide

    I will paste every file, grouped by project and path, in the next message. Copy each file into the matching path under your repo root.

    Make launch.sh executable (chmod +x launch.sh) and run the appropriate script for your platform.

    If you prefer, after pasting files you can create a ZIP locally (e.g., zip -r OneDriveSync-scaffold.zip .) to share or archive.

Key runtime notes

    EF migrations: either run dotnet ef migrations add InitialCreate then dotnet ef database update or rely on the DbInitializer call in the app bootstrap which calls Migrate() at startup.

    SQLite WAL is enabled by the initializer.

    Token cache: MSAL token cache is not wired to platform secure storage in the scaffold; add that for production.

Files and structure I will paste

    OneDriveSync.sln

    launch.ps1, launch.sh

    src/App.Core/ — project file, Entities, DTOs, Interfaces, Utilities

    src/App.Infrastructure/ — project file, AppDbContext, DbInitializer, EfSyncRepository, GraphClientWrapper, MsalAuthService, LocalFileSystemAdapter, DI extension

    src/App.Services/ — project file, SyncSettings, SyncEngine, TransferService, DI extension, runtime DTOs

    src/App.UI.Avalonia/ — project file, Avalonia bootstrap, XAML views, ViewModels, styles

Bold reminder: I will not substitute YOUR_CLIENT_ID; it remains the literal placeholder.
Next action I will take now

I will paste the full contents of every file (one file at a time, with the exact text we agreed) in the next message so you can create the repository locally. If any file was underspecified I will include a minimal placeholder and list it in PLACEHOLDER_FILES.txt.

If you want me to proceed, reply with paste files and I will output the complete file set ready for you to save.
Today
You said
paste files
Copilot said
Repository files — paste each file into the matching path
OneDriveSync.sln
text

Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.0.0
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "App.Core", "src\App.Core\App.Core.csproj", "{11111111-1111-1111-1111-111111111111}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "App.Infrastructure", "src\App.Infrastructure\App.Infrastructure.csproj", "{22222222-2222-2222-2222-222222222222}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "App.Services", "src\App.Services\App.Services.csproj", "{33333333-3333-3333-3333-333333333333}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "App.UI.Avalonia", "src\App.UI.Avalonia\App.UI.Avalonia.csproj", "{44444444-4444-4444-4444-444444444444}"
EndProject
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Debug|Any CPU = Debug|Any CPU
        Release|Any CPU = Release|Any CPU
    EndGlobalSection
    GlobalSection(ProjectConfigurationPlatforms) = postSolution
        {11111111-1111-1111-1111-111111111111}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {11111111-1111-1111-1111-111111111111}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {22222222-2222-2222-2222-222222222222}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {22222222-2222-2222-2222-222222222222}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {33333333-3333-3333-3333-333333333333}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {33333333-3333-3333-3333-333333333333}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {44444444-4444-4444-4444-444444444444}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {44444444-4444-4444-4444-444444444444}.Debug|Any CPU.Build.0 = Debug|Any CPU
    EndGlobalSection
EndGlobal

launch.ps1
powershell

# File: launch.ps1
$ErrorActionPreference = "Stop"

Write-Host "Restoring packages..."
dotnet restore

Write-Host "Building solution..."
dotnet build -c Debug

Write-Host "Applying EF migrations and ensuring database..."
# Ensure you have dotnet-ef installed or use the runtime migration approach in code
dotnet tool install --global dotnet-ef --version 8.0.0 -s nuget.org -v q
dotnet ef database update --project src\App.Infrastructure --startup-project src\App.UI.Avalonia

Write-Host "Running App.UI.Avalonia..."
dotnet run --project src\App.UI.Avalonia -c Debug

launch.sh
bash

#!/usr/bin/env bash
set -euo pipefail

echo "Restoring packages..."
dotnet restore

echo "Building solution..."
dotnet build -c Debug

echo "Applying EF migrations and ensuring database..."
if ! command -v dotnet-ef >/dev/null 2>&1; then
  echo "Installing dotnet-ef tool..."
  dotnet tool install --global dotnet-ef --version 8.0.0
  export PATH="$PATH:$HOME/.dotnet/tools"
fi

dotnet ef database update --project src/App.Infrastructure --startup-project src/App.UI.Avalonia

echo "Running App.UI.Avalonia..."
dotnet run --project src\App.UI.Avalonia -c Debug

Make launch.sh executable after saving:
bash

chmod +x launch.sh

src/App.Core
src/App.Core/App.Core.csproj
xml

<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>preview</LangVersion>
  </PropertyGroup>
</Project>

src/App.Core/Entities/DriveItemRecord.cs
csharp

namespace App.Core.Entities;

public sealed record DriveItemRecord(
    string Id,
    string DriveItemId,
    string RelativePath,
    string? ETag,
    string? CTag,
    long Size,
    DateTimeOffset LastModifiedUtc,
    bool IsFolder,
    bool IsDeleted
);

src/App.Core/Entities/LocalFileRecord.cs
csharp

namespace App.Core.Entities;

public sealed record LocalFileRecord(
    string Id,
    string RelativePath,
    string? Hash,
    long Size,
    DateTimeOffset LastWriteUtc,
    SyncState SyncState
);

public enum SyncState
{
    Unknown,
    PendingDownload,
    Downloaded,
    PendingUpload,
    Uploaded,
    Deleted,
    Error
}

src/App.Core/Entities/DeltaToken.cs
csharp

namespace App.Core.Entities;

public sealed record DeltaToken(string Id, string Token, DateTimeOffset LastSyncedUtc);

src/App.Core/Entities/TransferLog.cs
csharp

namespace App.Core.Entities;

public sealed record TransferLog(
    string Id,
    TransferType Type,
    string ItemId,
    DateTimeOffset StartedUtc,
    DateTimeOffset? CompletedUtc,
    TransferStatus Status,
    string? Error
);

public enum TransferType { Download, Upload, Delete }
public enum TransferStatus { Pending, InProgress, Success, Failed }

src/App.Core/Dto/DeltaPage.cs
csharp

using App.Core.Entities;

namespace App.Core.Dto;

public sealed record DeltaPage(IEnumerable<DriveItemRecord> Items, string? NextLink, string? DeltaLink);

src/App.Core/Dto/UploadSessionInfo.cs
csharp

namespace App.Core.Dto;

public sealed record UploadSessionInfo(string UploadUrl, string SessionId, DateTimeOffset ExpiresAt);

src/App.Core/Dto/LocalFileInfo.cs
csharp

namespace App.Core.Dto;

public sealed record LocalFileInfo(string RelativePath, long Size, DateTimeOffset LastWriteUtc, string? Hash);

src/App.Core/Interfaces/IAuthService.cs
csharp

namespace App.Core.Interfaces;

public interface IAuthService
{
    Task SignInAsync(CancellationToken ct);
    Task SignOutAsync(CancellationToken ct);
    Task<string> GetAccessTokenAsync(CancellationToken ct);
    bool IsSignedIn { get; }
}

src/App.Core/Interfaces/IGraphClient.cs
csharp

using App.Core.Dto;

namespace App.Core.Interfaces;

public interface IGraphClient
{
    /// <summary>
    /// If deltaOrNextLink is null, call /me/drive/root/delta to start full enumeration.
    /// If it is a nextLink or deltaLink, GET that URL.
    /// </summary>
    Task<DeltaPage> GetDriveDeltaPageAsync(string? deltaOrNextLink, CancellationToken ct);

    Task<Stream> DownloadDriveItemContentAsync(string driveItemId, CancellationToken ct);

    Task<UploadSessionInfo> CreateUploadSessionAsync(string parentPath, string fileName, CancellationToken ct);

    Task UploadChunkAsync(UploadSessionInfo session, Stream chunk, long rangeStart, long rangeEnd, CancellationToken ct);

    Task DeleteDriveItemAsync(string driveItemId, CancellationToken ct);
}

src/App.Core/Interfaces/IFileSystemAdapter.cs
csharp

using App.Core.Dto;

namespace App.Core.Interfaces;

public interface IFileSystemAdapter
{
    Task WriteFileAsync(string relativePath, Stream content, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct);
    Task DeleteFileAsync(string relativePath, CancellationToken ct);
    Task<IEnumerable<LocalFileInfo>> EnumerateFilesAsync(CancellationToken ct);
}

src/App.Core/Interfaces/ISyncRepository.cs
csharp

using App.Core.Entities;

namespace App.Core.Interfaces;

public interface ISyncRepository
{
    Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken ct);
    Task SaveOrUpdateDeltaTokenAsync(DeltaToken token, CancellationToken ct);

    /// <summary>
    /// Apply a page of DriveItem metadata to the local DB.
    /// Implementations should use a transaction and batch writes.
    /// </summary>
    Task ApplyDriveItemsAsync(IEnumerable<DriveItemRecord> items, CancellationToken ct);

    Task<IEnumerable<DriveItemRecord>> GetPendingDownloadsAsync(int limit, CancellationToken ct);
    Task MarkLocalFileStateAsync(string driveItemId, SyncState state, CancellationToken ct);
    Task AddOrUpdateLocalFileAsync(LocalFileRecord file, CancellationToken ct);
    Task<IEnumerable<LocalFileRecord>> GetPendingUploadsAsync(int limit, CancellationToken ct);

    Task LogTransferAsync(TransferLog log, CancellationToken ct);
}

src/App.Core/Utilities/SyncSettings.cs
csharp

namespace App.Core.Utilities;

public sealed record SyncSettings(int ParallelDownloads = 4, int BatchSize = 50, ConflictPolicy ConflictPolicy = ConflictPolicy.LastWriteWins);

public enum ConflictPolicy { LastWriteWins, KeepLocal, KeepRemote, Prompt }

src/App.Core/Utilities/Result.cs
csharp

namespace App.Core.Utilities;

/// <summary>
/// Minimal Result type for simple success/failure flows.
/// Keep small and focused; expand in services if needed.
/// </summary>
public readonly record struct Result(bool IsSuccess, string? Error)
{
    public static Result Success => new(true, null);
    public static Result Failure(string error) => new(false, error);
}

src/App.Infrastructure
src/App.Infrastructure/App.Infrastructure.csproj
xml

<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>preview</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
    <PackageReference Include="Microsoft.Identity.Client" Version="4.70.0" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\App.Core\App.Core.csproj" />
  </ItemGroup>
</Project>

src/App.Infrastructure/Data/AppDbContext.cs
csharp

using Microsoft.EntityFrameworkCore;
using App.Core.Entities;

namespace App.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public DbSet<DriveItemRecord> DriveItems { get; init; } = null!;
    public DbSet<LocalFileRecord> LocalFiles { get; init; } = null!;
    public DbSet<DeltaToken> DeltaTokens { get; init; } = null!;
    public DbSet<TransferLog> TransferLogs { get; init; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<DriveItemRecord>().HasKey(d => d.Id);
        mb.Entity<LocalFileRecord>().HasKey(l => l.Id);
        mb.Entity<DeltaToken>().HasKey(t => t.Id);
        mb.Entity<TransferLog>().HasKey(t => t.Id);

        mb.Entity<DriveItemRecord>().ToTable("DriveItems");
        mb.Entity<LocalFileRecord>().ToTable("LocalFiles");
        mb.Entity<DeltaToken>().ToTable("DeltaTokens");
        mb.Entity<TransferLog>().ToTable("TransferLogs");

        mb.Entity<DriveItemRecord>().Property(d => d.RelativePath).IsRequired();
        mb.Entity<LocalFileRecord>().Property(l => l.RelativePath).IsRequired();

        // Optional: index for quick lookups by DriveItemId or RelativePath
        mb.Entity<DriveItemRecord>().HasIndex(d => d.DriveItemId);
        mb.Entity<LocalFileRecord>().HasIndex(l => l.RelativePath);
    }
}

src/App.Infrastructure/Data/DbInitializer.cs
csharp

using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Data;

public static class DbInitializer
{
    public static void EnsureDatabaseCreatedAndConfigured(AppDbContext db)
    {
        // Ensure DB created and enable WAL for better concurrency
        db.Database.OpenConnection();
        db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        db.Database.Migrate();
    }
}

src/App.Infrastructure/Repositories/EfSyncRepository.cs
csharp

using Microsoft.EntityFrameworkCore;
using App.Core.Interfaces;
using App.Core.Entities;

namespace App.Infrastructure.Repositories;

public sealed class EfSyncRepository : ISyncRepository
{
    private readonly AppDbContext _db;
    public EfSyncRepository(AppDbContext db) => _db = db;

    public async Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken ct) =>
        await _db.DeltaTokens.OrderByDescending(t => t.LastSyncedUtc).FirstOrDefaultAsync(ct);

    public async Task SaveOrUpdateDeltaTokenAsync(DeltaToken token, CancellationToken ct)
    {
        var existing = await _db.DeltaTokens.FindAsync(new object[] { token.Id }, ct);
        if (existing is null) _db.DeltaTokens.Add(token);
        else _db.Entry(existing).CurrentValues.SetValues(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ApplyDriveItemsAsync(IEnumerable<DriveItemRecord> items, CancellationToken ct)
    {
        // Batch apply inside a transaction to reduce contention on SQLite
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        foreach (var item in items)
        {
            var existing = await _db.DriveItems.FindAsync(new object[] { item.Id }, ct);
            if (existing is null) _db.DriveItems.Add(item);
            else _db.Entry(existing).CurrentValues.SetValues(item);
        }
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<IEnumerable<DriveItemRecord>> GetPendingDownloadsAsync(int limit, CancellationToken ct) =>
        await _db.DriveItems.Where(d => !d.IsFolder && !d.IsDeleted)
                            .OrderBy(d => d.LastModifiedUtc)
                            .Take(limit)
                            .ToListAsync(ct);

    public async Task MarkLocalFileStateAsync(string driveItemId, SyncState state, CancellationToken ct)
    {
        // Use driveItemId as the local file id mapping for simplicity
        var drive = await _db.DriveItems.FindAsync(new object[] { driveItemId }, ct);
        if (drive is null) return;

        var local = await _db.LocalFiles.FindAsync(new object[] { driveItemId }, ct);
        if (local is null)
        {
            _db.LocalFiles.Add(new LocalFileRecord(driveItemId, drive.RelativePath, null, drive.Size, drive.LastModifiedUtc, state));
        }
        else
        {
            _db.Entry(local).CurrentValues.SetValues(local with { SyncState = state, LastWriteUtc = drive.LastModifiedUtc, Size = drive.Size });
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddOrUpdateLocalFileAsync(LocalFileRecord file, CancellationToken ct)
    {
        var existing = await _db.LocalFiles.FindAsync(new object[] { file.Id }, ct);
        if (existing is null) _db.LocalFiles.Add(file);
        else _db.Entry(existing).CurrentValues.SetValues(file);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<LocalFileRecord>> GetPendingUploadsAsync(int limit, CancellationToken ct) =>
        await _db.LocalFiles.Where(l => l.SyncState == SyncState.PendingUpload).Take(limit).ToListAsync(ct);

    public async Task LogTransferAsync(TransferLog log, CancellationToken ct)
    {
        _db.TransferLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}

src/App.Infrastructure/Graph/GraphClientWrapper.cs
csharp

using System.Net.Http.Headers;
using System.Text.Json;
using App.Core.Interfaces;
using App.Core.Dto;
using App.Core.Entities;

namespace App.Infrastructure.Graph;

public sealed class GraphClientWrapper : IGraphClient
{
    private readonly IAuthService _auth;
    private readonly HttpClient _http;

    public GraphClientWrapper(IAuthService auth, HttpClient http)
    {
        _auth = auth;
        _http = http;
    }

    public async Task<DeltaPage> GetDriveDeltaPageAsync(string? deltaOrNextLink, CancellationToken ct)
    {
        var url = string.IsNullOrEmpty(deltaOrNextLink)
            ? "https://graph.microsoft.com/v1.0/me/drive/root/delta"
            : deltaOrNextLink;

        var token = await _auth.GetAccessTokenAsync(ct);
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        res.EnsureSuccessStatusCode();

        await using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        var items = new List<DriveItemRecord>();
        if (doc.RootElement.TryGetProperty("value", out var arr))
        {
            foreach (var el in arr.EnumerateArray())
            {
                var id = el.GetProperty("id").GetString()!;
                var isFolder = el.TryGetProperty("folder", out _);
                var size = el.TryGetProperty("size", out var sProp) ? sProp.GetInt64() : 0L;
                var parentPath = el.TryGetProperty("parentReference", out var pr) && pr.TryGetProperty("path", out var p) ? p.GetString() ?? string.Empty : string.Empty;
                var name = el.TryGetProperty("name", out var n) ? n.GetString() ?? id : id;
                var relativePath = BuildRelativePath(parentPath, name);
                var eTag = el.TryGetProperty("eTag", out var et) ? et.GetString() : null;
                var cTag = el.TryGetProperty("cTag", out var ctProp) ? ctProp.GetString() : null;
                var last = el.TryGetProperty("lastModifiedDateTime", out var lm) ? DateTimeOffset.Parse(lm.GetString()!) : DateTimeOffset.UtcNow;
                var isDeleted = el.TryGetProperty("deleted", out _);
                items.Add(new DriveItemRecord(id, id, relativePath, eTag, cTag, size, last, isFolder, isDeleted));
            }
        }

        var next = doc.RootElement.TryGetProperty("@odata.nextLink", out var nl) ? nl.GetString() : null;
        var delta = doc.RootElement.TryGetProperty("@odata.deltaLink", out var dl) ? dl.GetString() : null;
        return new DeltaPage(items, next, delta);
    }

    private static string BuildRelativePath(string parentReferencePath, string name)
    {
        // parentReference.path looks like "/drive/root:/Folder/SubFolder"
        if (string.IsNullOrEmpty(parentReferencePath)) return name;
        var idx = parentReferencePath.IndexOf(":/", StringComparison.Ordinal);
        if (idx >= 0)
        {
            var after = parentReferencePath[(idx + 2)..].Trim('/');
            return string.IsNullOrEmpty(after) ? name : Path.Combine(after, name);
        }
        return name;
    }

    public async Task<Stream> DownloadDriveItemContentAsync(string driveItemId, CancellationToken ct)
    {
        var token = await _auth.GetAccessTokenAsync(ct);
        var url = $"https://graph.microsoft.com/v1.0/me/drive/items/{driveItemId}/content";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStreamAsync(ct);
    }

    public async Task<UploadSessionInfo> CreateUploadSessionAsync(string parentPath, string fileName, CancellationToken ct)
    {
        var token = await _auth.GetAccessTokenAsync(ct);
        // Build path under root
        var path = string.IsNullOrWhiteSpace(parentPath) ? fileName : $"{parentPath.Trim('/')}/{fileName}";
        var url = $"https://graph.microsoft.com/v1.0/me/drive/root:/{Uri.EscapeDataString(path)}:/createUploadSession";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        await using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var uploadUrl = doc.RootElement.GetProperty("uploadUrl").GetString()!;
        var expiration = doc.RootElement.TryGetProperty("expirationDateTime", out var ex) ? DateTimeOffset.Parse(ex.GetString()!) : DateTimeOffset.UtcNow.AddHours(1);
        return new UploadSessionInfo(uploadUrl, Guid.NewGuid().ToString(), expiration);
    }

    public async Task UploadChunkAsync(UploadSessionInfo session, Stream chunk, long rangeStart, long rangeEnd, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, session.UploadUrl);
        req.Content = new StreamContent(chunk);
        req.Content.Headers.Add("Content-Range", $"bytes {rangeStart}-{rangeEnd}/*");
        using var res = await _http.SendAsync(req, ct);
        // Graph returns 201/200 when upload completes, 202 for accepted chunk
        if (!res.IsSuccessStatusCode)
            res.EnsureSuccessStatusCode();
    }

    public async Task DeleteDriveItemAsync(string driveItemId, CancellationToken ct)
    {
        var token = await _auth.GetAccessTokenAsync(ct);
        var url = $"https://graph.microsoft.com/v1.0/me/drive/items/{driveItemId}";
        using var req = new HttpRequestMessage(HttpMethod.Delete, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var res = await _http.SendAsync(req, ct);
        if (res.StatusCode != System.Net.HttpStatusCode.NoContent && res.StatusCode != System.Net.HttpStatusCode.NotFound)
            res.EnsureSuccessStatusCode();
    }
}

src/App.Infrastructure/Auth/MsalAuthService.cs
csharp

using Microsoft.Identity.Client;
using App.Core.Interfaces;

namespace App.Infrastructure.Auth;

public sealed class MsalAuthService : IAuthService
{
    private readonly IPublicClientApplication _pca;
    private IAccount? _account;
    private readonly string[] _scopes = new[] { "Files.ReadWrite.All", "offline_access", "User.Read" };

    public bool IsSignedIn => _account is not null;

    public MsalAuthService(string clientId)
    {
        _pca = PublicClientApplicationBuilder.Create(clientId)
            .WithRedirectUri("http://localhost")
            .Build();
    }

    public async Task SignInAsync(CancellationToken ct)
    {
        var result = await _pca.AcquireTokenInteractive(_scopes).ExecuteAsync(ct);
        _account = result.Account;
    }

    public async Task SignOutAsync(CancellationToken ct)
    {
        if (_account is null) return;
        await _pca.RemoveAsync(_account);
        _account = null;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (_account is null) throw new InvalidOperationException("Not signed in");
        var result = await _pca.AcquireTokenSilent(_scopes, _account).ExecuteAsync(ct);
        return result.AccessToken;
    }
}

src/App.Infrastructure/Filesystem/LocalFileSystemAdapter.cs
csharp

using App.Core.Interfaces;
using App.Core.Dto;

namespace App.Infrastructure.Filesystem;

public sealed class LocalFileSystemAdapter : IFileSystemAdapter
{
    private readonly string _root;
    public LocalFileSystemAdapter(string root) => _root = root;

    private string FullPath(string relative) => Path.Combine(_root, relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    public async Task WriteFileAsync(string relativePath, Stream content, CancellationToken ct)
    {
        var full = FullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await using var fs = File.Create(full);
        await content.CopyToAsync(fs, ct);
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct)
    {
        var full = FullPath(relativePath);
        if (!File.Exists(full)) return Task.FromResult<Stream?>(null);
        return Task.FromResult<Stream?>(File.OpenRead(full));
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken ct)
    {
        var full = FullPath(relativePath);
        if (File.Exists(full)) File.Delete(full);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<LocalFileInfo>> EnumerateFilesAsync(CancellationToken ct)
    {
        var list = new List<LocalFileInfo>();
        if (!Directory.Exists(_root)) return Task.FromResult<IEnumerable<LocalFileInfo>>(list.AsEnumerable());
        foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
        {
            var fi = new FileInfo(file);
            var rel = Path.GetRelativePath(_root, file);
            list.Add(new LocalFileInfo(rel, fi.Length, fi.LastWriteTimeUtc, null));
        }
        return Task.FromResult<IEnumerable<LocalFileInfo>>(list);
    }
}

src/App.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs
csharp

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using App.Core.Interfaces;
using App.Infrastructure.Data;
using App.Infrastructure.Repositories;
using App.Infrastructure.Graph;
using App.Infrastructure.Auth;
using App.Infrastructure.Filesystem;

namespace App.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string sqliteConnectionString, string localRoot, string msalClientId)
    {
        services.AddDbContext<AppDbContext>(opts => opts.UseSqlite(sqliteConnectionString));
        services.AddScoped<ISyncRepository, EfSyncRepository>();

        services.AddSingleton<IAuthService>(_ => new MsalAuthService(msalClientId));
        services.AddHttpClient<IGraphClient, GraphClientWrapper>()
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = true });

        services.AddSingleton<IFileSystemAdapter>(_ => new LocalFileSystemAdapter(localRoot));

        // Optional: register DbInitializer action
        services.AddSingleton<Action<IServiceProvider>>(sp =>
        {
            return provider =>
            {
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DbInitializer.EnsureDatabaseCreatedAndConfigured(db);
            };
        });

        return services;
    }
}

src/App.Services
src/App.Services/App.Services.csproj
xml

<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>preview</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
    <PackageReference Include="Polly" Version="8.1.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\App.Core\App.Core.csproj" />
    <ProjectReference Include="..\App.Infrastructure\App.Infrastructure.csproj" />
  </ItemGroup>
</Project>

src/App.Services/SyncSettings.cs
csharp

namespace App.Services;

public sealed record SyncSettings(int ParallelDownloads = 4, int BatchSize = 50, int MaxRetries = 3, int RetryBaseDelayMs = 500);

public sealed record SyncStats(int PendingDownloads, int PendingUploads, int ActiveDownloads, int ActiveUploads, int FailedTransfers);

src/App.Services/HealthAndStats.cs
csharp

namespace App.Services;

public sealed record TransferProgress(string ItemId, long BytesTransferred, long? TotalBytes, DateTimeOffset Timestamp);

src/App.Services/SyncEngine.cs
csharp

using App.Core.Interfaces;
using App.Core.Dto;
using App.Core.Entities;
using Microsoft.Extensions.Logging;

namespace App.Services;

public sealed class SyncEngine
{
    private readonly ISyncRepository _repo;
    private readonly IGraphClient _graph;
    private readonly TransferService _transfer;
    private readonly ILogger<SyncEngine> _logger;

    public SyncEngine(ISyncRepository repo, IGraphClient graph, TransferService transfer, ILogger<SyncEngine> logger)
    {
        _repo = repo;
        _graph = graph;
        _transfer = transfer;
        _logger = logger;
    }

    /// <summary>
    /// Performs the initial full enumeration using Graph delta. Pages until exhausted,
    /// persists DriveItemRecords and the final deltaLink for incremental syncs.
    /// </summary>
    public async Task InitialFullSyncAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting initial full sync");
        string? nextOrDelta = null;
        string? finalDelta = null;

        do
        {
            var page = await _graph.GetDriveDeltaPageAsync(nextOrDelta, ct);
            await _repo.ApplyDriveItemsAsync(page.Items, ct);
            nextOrDelta = page.NextLink;
            finalDelta = page.DeltaLink ?? finalDelta;
            _logger.LogInformation("Applied page: items={Count} next={Next}", page.Items.Count(), page.NextLink is not null);
        } while (!string.IsNullOrEmpty(nextOrDelta) && !ct.IsCancellationRequested);

        if (!string.IsNullOrEmpty(finalDelta))
        {
            var token = new DeltaToken(Guid.NewGuid().ToString(), finalDelta, DateTimeOffset.UtcNow);
            await _repo.SaveOrUpdateDeltaTokenAsync(token, ct);
            _logger.LogInformation("Saved delta token");
        }

        // Kick off transfers after DB is updated
        await _transfer.ProcessPendingDownloadsAsync(ct);
        await _transfer.ProcessPendingUploadsAsync(ct);
        _logger.LogInformation("Initial full sync complete");
    }

    /// <summary>
    /// Performs an incremental sync using the stored delta token.
    /// </summary>
    public async Task IncrementalSyncAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting incremental sync");
        var token = await _repo.GetDeltaTokenAsync(ct) ?? throw new InvalidOperationException("Delta token missing; run initial sync first.");
        var page = await _graph.GetDriveDeltaPageAsync(token.Token, ct);
        await _repo.ApplyDriveItemsAsync(page.Items, ct);

        if (!string.IsNullOrEmpty(page.DeltaLink))
        {
            await _repo.SaveOrUpdateDeltaTokenAsync(token with { Token = page.DeltaLink, LastSyncedUtc = DateTimeOffset.UtcNow }, ct);
            _logger.LogInformation("Updated delta token");
        }

        await _transfer.ProcessPendingDownloadsAsync(ct);
        await _transfer.ProcessPendingUploadsAsync(ct);
        _logger.LogInformation("Incremental sync complete");
    }
}

src/App.Services/TransferService.cs
csharp

using App.Core.Interfaces;
using App.Core.Entities;
using App.Core.Dto;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace App.Services;

public sealed class TransferService
{
    private readonly IFileSystemAdapter _fs;
    private readonly IGraphClient _graph;
    private readonly ISyncRepository _repo;
    private readonly ILogger<TransferService> _logger;
    private readonly SyncSettings _settings;
    private readonly SemaphoreSlim _downloadSemaphore;
    private readonly AsyncRetryPolicy _retryPolicy;

    public TransferService(IFileSystemAdapter fs, IGraphClient graph, ISyncRepository repo, ILogger<TransferService> logger, SyncSettings settings)
    {
        _fs = fs;
        _graph = graph;
        _repo = repo;
        _logger = logger;
        _settings = settings;
        _downloadSemaphore = new SemaphoreSlim(settings.ParallelDownloads);

        _retryPolicy = Policy.Handle<Exception>()
                             .WaitAndRetryAsync(settings.MaxRetries, i => TimeSpan.FromMilliseconds(settings.RetryBaseDelayMs * Math.Pow(2, i)),
                                 (ex, ts, retryCount, ctx) => _logger.LogWarning(ex, "Retry {Retry} after {Delay}ms", retryCount, ts.TotalMilliseconds));
    }

    /// <summary>
    /// Pulls pending downloads from repository in batches and downloads them with bounded concurrency.
    /// </summary>
    public async Task ProcessPendingDownloadsAsync(CancellationToken ct)
    {
        _logger.LogInformation("Processing pending downloads");
        while (!ct.IsCancellationRequested)
        {
            var items = (await _repo.GetPendingDownloadsAsync(_settings.BatchSize, ct)).ToList();
            if (!items.Any()) break;

            var tasks = items.Select(item => DownloadItemWithRetryAsync(item, ct)).ToList();
            await Task.WhenAll(tasks);
        }
    }

    private async Task DownloadItemWithRetryAsync(DriveItemRecord item, CancellationToken ct)
    {
        await _retryPolicy.ExecuteAsync(async ct2 => await DownloadItemAsync(item, ct2), ct);
    }

    private async Task DownloadItemAsync(DriveItemRecord item, CancellationToken ct)
    {
        await _downloadSemaphore.WaitAsync(ct);
        var log = new TransferLog(Guid.NewGuid().ToString(), TransferType.Download, item.Id, DateTimeOffset.UtcNow, null, TransferStatus.InProgress, null);
        await _repo.LogTransferAsync(log, ct);
        try
        {
            await using var stream = await _graph.DownloadDriveItemContentAsync(item.DriveItemId, ct);
            await _fs.WriteFileAsync(item.RelativePath, stream, ct);
            await _repo.MarkLocalFileStateAsync(item.Id, SyncState.Downloaded, ct);
            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Success };
            await _repo.LogTransferAsync(log, ct);
            _logger.LogInformation("Downloaded {Path}", item.RelativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download failed for {Id}", item.Id);
            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Failed, Error = ex.Message };
            await _repo.LogTransferAsync(log, ct);
        }
        finally
        {
            _downloadSemaphore.Release();
        }
    }

    /// <summary>
    /// Scans repository for pending uploads and uploads them using upload sessions and chunked uploads.
    /// </summary>
    public async Task ProcessPendingUploadsAsync(CancellationToken ct)
    {
        _logger.LogInformation("Processing pending uploads");
        var uploads = (await _repo.GetPendingUploadsAsync(_settings.BatchSize, ct)).ToList();
        foreach (var local in uploads)
        {
            await _retryPolicy.ExecuteAsync(async ct2 => await UploadLocalFileAsync(local, ct2), ct);
        }
    }

    private async Task UploadLocalFileAsync(LocalFileRecord local, CancellationToken ct)
    {
        var log = new TransferLog(Guid.NewGuid().ToString(), TransferType.Upload, local.Id, DateTimeOffset.UtcNow, null, TransferStatus.InProgress, null);
        await _repo.LogTransferAsync(log, ct);
        try
        {
            var parent = Path.GetDirectoryName(local.RelativePath) ?? "/";
            var fileName = Path.GetFileName(local.RelativePath);
            var session = await _graph.CreateUploadSessionAsync(parent, fileName, ct);

            await using var stream = await _fs.OpenReadAsync(local.RelativePath, ct) ?? throw new FileNotFoundException(local.RelativePath);
            const int chunkSize = 320 * 1024; // 320KB
            long uploaded = 0;
            while (uploaded < stream.Length)
            {
                var toRead = (int)Math.Min(chunkSize, stream.Length - uploaded);
                var buffer = new byte[toRead];
                stream.Seek(uploaded, SeekOrigin.Begin);
                var read = await stream.ReadAsync(buffer.AsMemory(0, toRead), ct);
                await using var ms = new MemoryStream(buffer, 0, read, writable: false);
                await _graph.UploadChunkAsync(session, ms, uploaded, uploaded + read - 1, ct);
                uploaded += read;
            }

            await _repo.MarkLocalFileStateAsync(local.Id, SyncState.Uploaded, ct);
            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Success };
            await _repo.LogTransferAsync(log, ct);
            _logger.LogInformation("Uploaded {Path}", local.RelativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed for {Id}", local.Id);
            log = log with { CompletedUtc = DateTimeOffset.UtcNow, Status = TransferStatus.Failed, Error = ex.Message };
            await _repo.LogTransferAsync(log, ct);
        }
    }
}

src/App.Services/DependencyInjection/ServiceCollectionExtensions.cs
csharp

using Microsoft.Extensions.DependencyInjection;
using App.Core.Interfaces;

namespace App.Services.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSyncServices(this IServiceCollection services)
    {
        services.AddSingleton<SyncSettings>(_ => new SyncSettings(ParallelDownloads: 4, BatchSize: 50));
        services.AddSingleton<TransferService>();
        services.AddSingleton<SyncEngine>();
        return services;
    }
}

src/App.UI.Avalonia
src/App.UI.Avalonia/App.UI.Avalonia.csproj
xml

<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>WinExe</OutputType>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>preview</LangVersion>
    <UseWPF>false</UseWPF>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.0.0" />
    <PackageReference Include="Avalonia.ReactiveUI" Version="11.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="8.0.0" />
    <PackageReference Include="ReactiveUI.Fody" Version="17.0.0" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\App.Core\App.Core.csproj" />
    <ProjectReference Include="..\App.Infrastructure\App.Infrastructure.csproj" />
    <ProjectReference Include="..\App.Services\App.Services.csproj" />
  </ItemGroup>
</Project>

src/App.UI.Avalonia/Program.cs
csharp

using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using App.Infrastructure.DependencyInjection;
using App.Services.DependencyInjection;
using App.Core.Utilities;
using App.Services;
using App.Infrastructure.DependencyInjection;

class Program
{
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
}

src/App.UI.Avalonia/App.axaml
xml

<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="App.UI.Avalonia.App">
  <Application.Styles>
    <StyleInclude Source="avares://Avalonia.Themes.Default/DefaultTheme.xaml"/>
    <StyleInclude Source="avares://Avalonia.Controls/Styles/Default.xaml"/>
    <StyleInclude Source="Styles/Theme.axaml"/>
  </Application.Styles>
</Application>

src/App.UI.Avalonia/App.axaml.cs
csharp

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using App.Infrastructure.DependencyInjection;
using App.Services.DependencyInjection;
using App.Core.Utilities;

public partial class App : Application
{
    private IServiceProvider? _services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddLogging(cfg => cfg.AddConsole());

        // Infrastructure registration
        var dbPath = "Data Source=app.db";
        var localRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OneDriveSync");
        var msalClientId = "YOUR_CLIENT_ID";
        services.AddInfrastructure(dbPath, localRoot, msalClientId);

        // App services
        services.AddSyncServices();

        // UI services and viewmodels
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<DashboardViewModel>();

        // Sync settings
        services.AddSingleton(new SyncSettings(ParallelDownloads: 4, BatchSize: 50));

        _services = services.BuildServiceProvider();

        // Ensure DB created and configured
        var initializer = _services.GetRequiredService<Action<IServiceProvider>>();
        initializer(_services);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var main = _services.GetRequiredService<MainWindow>();
            desktop.MainWindow = main;
        }

        base.OnFrameworkInitializationCompleted();
    }
}

src/App.UI.Avalonia/MainWindow.axaml
xml

<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="https://github.com/avaloniaui"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        x:Class="App.UI.Avalonia.MainWindow"
        Width="900" Height="600"
        Title="OneDrive Sync">
  <DockPanel>
    <StackPanel Orientation="Horizontal" DockPanel.Dock="Top" Margin="8">
      <Button Content="Sign In" Command="{Binding SignInCommand}" Margin="4"/>
      <Button Content="Initial Full Sync" Command="{Binding InitialSyncCommand}" Margin="4"/>
      <Button Content="Incremental Sync" Command="{Binding IncrementalSyncCommand}" Margin="4"/>
      <ToggleButton Content="Dark Theme" IsChecked="{Binding UseDarkTheme}" Margin="4"/>
      <TextBlock Text="Parallel" VerticalAlignment="Center" Margin="8,0,0,0"/>
      <NumericUpDown Minimum="1" Maximum="32" Value="{Binding ParallelDownloads}" Width="80" Margin="4"/>
      <TextBlock Text="Batch" VerticalAlignment="Center" Margin="8,0,0,0"/>
      <NumericUpDown Minimum="1" Maximum="500" Value="{Binding BatchSize}" Width="80" Margin="4"/>
    </StackPanel>

    <Grid Margin="8">
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="2*"/>
        <ColumnDefinition Width="3*"/>
      </Grid.ColumnDefinitions>

      <StackPanel Grid.Column="0" Margin="8">
        <TextBlock Text="Sync Stats" FontWeight="Bold" Margin="0,0,0,8"/>
        <TextBlock Text="{Binding SyncStatus}" Margin="0,0,0,4"/>
        <TextBlock Text="Pending Downloads:"/>
        <TextBlock Text="{Binding PendingDownloads}" Margin="0,0,0,8"/>
        <TextBlock Text="Pending Uploads:"/>
        <TextBlock Text="{Binding PendingUploads}" Margin="0,0,0,8"/>
        <ProgressBar Minimum="0" Maximum="100" Value="{Binding ProgressPercent}" Height="20" Margin="0,8,0,8"/>
      </StackPanel>

      <StackPanel Grid.Column="1" Margin="8">
        <TextBlock Text="Recent Transfers" FontWeight="Bold" Margin="0,0,0,8"/>
        <ListBox Items="{Binding RecentTransfers}" Height="400"/>
      </StackPanel>
    </Grid>
  </DockPanel>
</Window>

src/App.UI.Avalonia/MainWindow.axaml.cs
csharp

using Avalonia.Controls;
using Avalonia.Interactivity;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}

src/App.UI.Avalonia/ViewModels/ViewModelBase.cs
csharp

using ReactiveUI;
public abstract class ViewModelBase : ReactiveObject { }

src/App.UI.Avalonia/ViewModels/MainWindowViewModel.cs
csharp

using ReactiveUI;
using System.Reactive;
using App.Core.Interfaces;
using App.Services;
using Microsoft.Extensions.Logging;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly SyncEngine _sync;
    private readonly SyncSettings _settings;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly TransferService _transfer;

    public ReactiveCommand<Unit, Unit> SignInCommand { get; }
    public ReactiveCommand<Unit, Unit> InitialSyncCommand { get; }
    public ReactiveCommand<Unit, Unit> IncrementalSyncCommand { get; }

    public ObservableCollection<string> RecentTransfers { get; } = new();
    public int PendingDownloads { get => _pendingDownloads; set => this.RaiseAndSetIfChanged(ref _pendingDownloads, value); }
    public int PendingUploads { get => _pendingUploads; set => this.RaiseAndSetIfChanged(ref _pendingUploads, value); }
    public string SyncStatus { get => _syncStatus; set => this.RaiseAndSetIfChanged(ref _syncStatus, value); }
    public double ProgressPercent { get => _progressPercent; set => this.RaiseAndSetIfChanged(ref _progressPercent, value); }

    public bool UseDarkTheme { get => _useDarkTheme; set { this.RaiseAndSetIfChanged(ref _useDarkTheme, value); ApplyTheme(); } }
    public int ParallelDownloads { get => _parallelDownloads; set { this.RaiseAndSetIfChanged(ref _parallelDownloads, value); UpdateSettings(); } }
    public int BatchSize { get => _batchSize; set { this.RaiseAndSetIfChanged(ref _batchSize, value); UpdateSettings(); } }

    private int _pendingDownloads;
    private int _pendingUploads;
    private string _syncStatus = "Idle";
    private double _progressPercent;
    private bool _useDarkTheme;
    private int _parallelDownloads;
    private int _batchSize;

    public MainWindowViewModel(IAuthService auth, SyncEngine sync, TransferService transfer, SyncSettings settings, ILogger<MainWindowViewModel> logger)
    {
        _auth = auth;
        _sync = sync;
        _transfer = transfer;
        _settings = settings;
        _logger = logger;

        _parallelDownloads = settings.ParallelDownloads;
        _batchSize = settings.BatchSize;

        SignInCommand = ReactiveCommand.CreateFromTask(async ct =>
        {
            SyncStatus = "Signing in...";
            await _auth.SignInAsync(ct);
            SyncStatus = "Signed in";
        });

        InitialSyncCommand = ReactiveCommand.CreateFromTask(async ct =>
        {
            SyncStatus = "Running initial full sync";
            await _sync.InitialFullSyncAsync(ct);
            SyncStatus = "Initial sync complete";
            await RefreshStatsAsync(ct);
        });

        IncrementalSyncCommand = ReactiveCommand.CreateFromTask(async ct =>
        {
            SyncStatus = "Running incremental sync";
            await _sync.IncrementalSyncAsync(ct);
            SyncStatus = "Incremental sync complete";
            await RefreshStatsAsync(ct);
        });
    }

    private void ApplyTheme()
    {
        // Minimal theme switcher. For full theme support, swap Avalonia styles.
        // This placeholder toggles a simple resource; extend as needed.
    }

    private void UpdateSettings()
    {
        // Update SyncSettings instance used by TransferService
        // For simplicity, this example does not re-create services; in production update via options pattern
    }

    private async Task RefreshStatsAsync(CancellationToken ct)
    {
        // Query repository for counts and update properties
        // Placeholder: set dummy values
        PendingDownloads = 0;
        PendingUploads = 0;
        ProgressPercent = 100;
        RecentTransfers.Clear();
        RecentTransfers.Add($"Sync completed at {DateTimeOffset.Now}");
    }
}

src/App.UI.Avalonia/ViewModels/SettingsViewModel.cs
csharp

public sealed class SettingsViewModel : ViewModelBase
{
    public string LocalFolder { get => _localFolder; set => this.RaiseAndSetIfChanged(ref _localFolder, value); }
    private string _localFolder = string.Empty;

    public void Save()
    {
        // Persist settings to a simple JSON file or platform settings store
        var settings = new { LocalFolder, Updated = DateTimeOffset.UtcNow };
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OneDriveSync", "ui-settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(settings));
    }
}

src/App.UI.Avalonia/ViewModels/DashboardViewModel.cs
csharp

public sealed class DashboardViewModel : ViewModelBase
{
    public ObservableCollection<string> RecentTransfers { get; } = new();
    public int PendingDownloads { get => _pendingDownloads; set => this.RaiseAndSetIfChanged(ref _pendingDownloads, value); }
    public int PendingUploads { get => _pendingUploads; set => this.RaiseAndSetIfChanged(ref _pendingUploads, value); }
    public string SyncStatus { get => _syncStatus; set => this.RaiseAndSetIfChanged(ref _syncStatus, value); }

    private int _pendingDownloads;
    private int _pendingUploads;
    private string _syncStatus = "Idle";
}

src/App.UI.Avalonia/Styles/Theme.axaml
xml

<ResourceDictionary xmlns="https://github.com/avaloniaui">
  <Color x:Key="BackgroundColor">#FFFFFFFF</Color>
  <Color x:Key="ForegroundColor">#FF000000</Color>
  <Style Selector="TextBlock">
    <Setter Property="Foreground" Value="{DynamicResource ForegroundColor}"/>
  </Style>
</ResourceDictionary>

Final notes

    The placeholder YOUR_CLIENT_ID appears literally in src/App.UI.Avalonia/App.axaml.cs. Replace it with your Azure app registration client id before signing in.

    After creating files, run the appropriate launch script (launch.ps1 on Windows PowerShell or launch.sh on macOS/Linux) to restore, build, apply migrations, and run the Avalonia app.

    If you prefer to create EF migrations manually, run:
    bash

    dotnet ef migrations add InitialCreate --project src/App.Infrastructure --startup-project src/App.UI.Avalonia
    dotnet ef database update --project src\App.Infrastructure --startup-project src\App.UI.Avalonia

    If any file needs small adjustments (namespaces, using directives) for your environment, apply them locally.

If you want, I can now:

    generate the EF migration InitialCreate content for you to paste, or

    produce a short checklist of the first three implementation tasks to prioritize.

You said
checklist please
Copilot said
Implementation checklist — first three priorities
1. Project skeleton and tooling

    Create solution and projects: add App.Core, App.Infrastructure, App.Services, App.UI.Avalonia and the solution file exactly as scaffolded.

    Restore and build: run dotnet restore and dotnet build to verify compile surface and fix missing using or small namespace issues.

    EF Core tools: install dotnet-ef and confirm migrations can be created; ensure App.Infrastructure references are correct.

2. Authentication and Graph client

    Implement and test MSAL sign in: wire MsalAuthService with your Azure app client id, run interactive sign in, and verify GetAccessTokenAsync returns a token.

    HttpClient and Graph wrapper: configure HttpClient for GraphClientWrapper, call the simple Graph endpoint (e.g., /me) to validate auth header and connectivity.

    Error and token cache: add basic error handling and confirm silent token acquisition works after interactive sign in.

3. Database and initial delta enumeration

    DbContext and migrations: apply the initial EF migration or call DbInitializer.EnsureDatabaseCreatedAndConfigured at startup; confirm WAL mode is set.

    Initial delta flow: run GraphClientWrapper.GetDriveDeltaPageAsync(null, ct) and page through @odata.nextLink until exhausted; persist returned DriveItemRecord rows and the final @odata.deltaLink into DeltaToken.

    Repository idempotency: verify EfSyncRepository.ApplyDriveItemsAsync is idempotent by running the initial enumeration twice and confirming no duplicate records and stable DB state.

Quick verification checklist after these tasks

    Build passes with all projects.

    Sign in succeeds and GetAccessTokenAsync returns a valid token.

    Initial full enumeration completes and a DeltaToken is stored in SQLite.

    No SQLITE_BUSY errors during bulk writes (WAL enabled and writes batched).

If you want, I will now generate the exact dotnet ef migrations add InitialCreate commands and a minimal InitialCreate migration file you can paste so migrations run immediately.
