# OneDrive Client - Dependency Flow Diagram

**Analysis Date**: 2025-01-XX  
**Status**: ? No Circular Dependencies Detected  
**Architecture**: Clean Layered Architecture with Dependency Inversion

---

## Executive Summary

This diagram maps all dependencies starting from `MainWindow.axaml` through all layers of the application. The analysis confirms **no circular dependencies** exist in the codebase.

### Key Findings
- ? Clean layered architecture (UI ? Services ? Infrastructure ? Core)
- ? Proper dependency inversion (all layers depend on Core interfaces)
- ? One-directional dependency flow
- ? No circular dependencies detected

---

## Full Dependency Flow Diagram

```mermaid
graph TD
    %% Layer 0: UI View
    MainWindow[MainWindow.axaml<br/>VIEW]
    
    %% Layer 1: ViewModel
    MainWindowVM[MainWindowViewModel<br/>UI LAYER]
    
    %% Layer 2: Service Interfaces
    IAuthService[IAuthService<br/>CORE INTERFACE]
    ISyncEngine[ISyncEngine<br/>SERVICES INTERFACE]
    ITransferService[ITransferService<br/>SERVICES INTERFACE]
    ISettingsService[ISettingsAndPreferencesService<br/>UI INTERFACE]
    
    %% Layer 3: Service Implementations
    SyncEngine[SyncEngine<br/>SERVICES LAYER]
    TransferService[TransferService<br/>SERVICES LAYER]
    
    %% Layer 4: Service Dependencies (Interfaces)
    ISyncRepo[ISyncRepository<br/>CORE INTERFACE]
    IGraphClient[IGraphClient<br/>CORE INTERFACE]
    IFileSystemAdapter[IFileSystemAdapter<br/>CORE INTERFACE]
    UserPrefs[UserPreferences<br/>SERVICES CONFIG]
    
    %% Layer 5: Infrastructure Implementations
    MsalAuth[MsalAuthService<br/>INFRASTRUCTURE]
    GraphWrapper[GraphClientWrapper<br/>INFRASTRUCTURE]
    EfRepo[EfSyncRepository<br/>INFRASTRUCTURE]
    FileAdapter[LocalFileSystemAdapter<br/>INFRASTRUCTURE]
    
    %% Layer 6: Infrastructure Dependencies
    AppDbContext[AppDbContext<br/>INFRASTRUCTURE]
    HttpClient[HttpClient<br/>SYSTEM]
    MSAL[Microsoft.Identity.Client<br/>EXTERNAL]
    
    %% Layer 7: Core Domain
    CoreEntities[Core Entities<br/>DriveItemRecord<br/>LocalFileRecord<br/>DeltaToken<br/>TransferLog]
    CoreDtos[Core DTOs<br/>DeltaPage<br/>SyncProgress<br/>UploadSessionInfo]
    
    %% Define connections - Layer 0 to 1
    MainWindow --> MainWindowVM
    
    %% Layer 1 to 2
    MainWindowVM --> IAuthService
    MainWindowVM --> ISyncEngine
    MainWindowVM --> ITransferService
    MainWindowVM --> ISettingsService
    
    %% Layer 2 to 3 (Interface to Implementation)
    ISyncEngine -.implements.-> SyncEngine
    ITransferService -.implements.-> TransferService
    IAuthService -.implements.-> MsalAuth
    
    %% Layer 3 (Services) dependencies
    SyncEngine --> ISyncRepo
    SyncEngine --> IGraphClient
    SyncEngine --> ITransferService
    
    TransferService --> IFileSystemAdapter
    TransferService --> IGraphClient
    TransferService --> ISyncRepo
    TransferService --> UserPrefs
    
    %% Layer 4 to 5 (Interface to Implementation)
    ISyncRepo -.implements.-> EfRepo
    IGraphClient -.implements.-> GraphWrapper
    IFileSystemAdapter -.implements.-> FileAdapter
    
    %% Layer 5 (Infrastructure) dependencies
    GraphWrapper --> IAuthService
    GraphWrapper --> HttpClient
    MsalAuth --> MSAL
    EfRepo --> AppDbContext
    
    %% All layers depend on Core
    SyncEngine --> CoreEntities
    SyncEngine --> CoreDtos
    TransferService --> CoreEntities
    TransferService --> CoreDtos
    EfRepo --> CoreEntities
    GraphWrapper --> CoreEntities
    GraphWrapper --> CoreDtos
    FileAdapter --> CoreDtos
    
    %% Styling
    classDef uiLayer fill:#e1f5ff,stroke:#0288d1,stroke-width:2px
    classDef serviceLayer fill:#fff3e0,stroke:#f57c00,stroke-width:2px
    classDef infraLayer fill:#f3e5f5,stroke:#7b1fa2,stroke-width:2px
    classDef coreLayer fill:#e8f5e9,stroke:#388e3c,stroke-width:2px
    classDef interfaceLayer fill:#fff9c4,stroke:#f9a825,stroke-width:2px
    classDef externalLayer fill:#ffebee,stroke:#c62828,stroke-width:2px
    
    class MainWindow,MainWindowVM uiLayer
    class SyncEngine,TransferService,UserPrefs serviceLayer
    class MsalAuth,GraphWrapper,EfRepo,FileAdapter,AppDbContext infraLayer
    class CoreEntities,CoreDtos coreLayer
    class IAuthService,ISyncEngine,ITransferService,ISettingsService,ISyncRepo,IGraphClient,IFileSystemAdapter interfaceLayer
    class HttpClient,MSAL externalLayer
```

---

## Layered Architecture View

```mermaid
graph TB
    subgraph Layer0[" UI LAYER (Avalonia) "]
        A1[MainWindow.axaml]
        A2[MainWindowViewModel]
    end
    
    subgraph Layer1[" SERVICES LAYER "]
        B1[SyncEngine]
        B2[TransferService]
        B3[SyncSettings]
        B4[SyncProgress]
    end
    
    subgraph Layer2[" INFRASTRUCTURE LAYER "]
        C1[MsalAuthService]
        C2[GraphClientWrapper]
        C3[EfSyncRepository]
        C4[LocalFileSystemAdapter]
        C5[AppDbContext]
    end
    
    subgraph Layer3[" CORE LAYER (Domain) "]
        D1[Interfaces:<br/>IAuthService<br/>ISyncEngine<br/>ITransferService<br/>IGraphClient<br/>ISyncRepository<br/>IFileSystemAdapter]
        D2[Entities:<br/>DriveItemRecord<br/>LocalFileRecord<br/>DeltaToken<br/>TransferLog]
        D3[DTOs:<br/>DeltaPage<br/>SyncProgress<br/>UploadSessionInfo]
    end
    
    Layer0 --> Layer1
    Layer1 --> Layer2
    Layer0 --> Layer3
    Layer1 --> Layer3
    Layer2 --> Layer3
    
    style Layer0 fill:#e1f5ff,stroke:#0288d1,stroke-width:3px
    style Layer1 fill:#fff3e0,stroke:#f57c00,stroke-width:3px
    style Layer2 fill:#f3e5f5,stroke:#7b1fa2,stroke-width:3px
    style Layer3 fill:#e8f5e9,stroke:#388e3c,stroke-width:3px
```

---

## Dependency Inversion Pattern

```mermaid
graph LR
    subgraph UI["UI Layer"]
        VM[MainWindowViewModel]
    end
    
    subgraph Services["Services Layer"]
        SE[SyncEngine]
        TS[TransferService]
    end
    
    subgraph Core["Core Layer (Interfaces)"]
        IA[IAuthService]
        ISE[ISyncEngine]
        IT[ITransferService]
        IG[IGraphClient]
        IR[ISyncRepository]
        IF[IFileSystemAdapter]
    end
    
    subgraph Infrastructure["Infrastructure Layer"]
        Auth[MsalAuthService]
        Graph[GraphClientWrapper]
        Repo[EfSyncRepository]
        FS[LocalFileSystemAdapter]
    end
    
    %% Dependencies point UP to interfaces
    VM --> IA
    VM --> ISE
    VM --> IT
    
    SE --> IR
    SE --> IG
    SE --> IT
    
    TS --> IF
    TS --> IG
    TS --> IR
    
    %% Implementations realize interfaces
    IA -.-> Auth
    ISE -.-> SE
    IT -.-> TS
    IG -.-> Graph
    IR -.-> Repo
    IF -.-> FS
    
    %% Infrastructure depends on Core interfaces
    Graph --> IA
    
    style Core fill:#e8f5e9,stroke:#388e3c,stroke-width:4px
    style UI fill:#e1f5ff,stroke:#0288d1,stroke-width:2px
    style Services fill:#fff3e0,stroke:#f57c00,stroke-width:2px
    style Infrastructure fill:#f3e5f5,stroke:#7b1fa2,stroke-width:2px
```

---

## Key Observations

### ? No Circular Dependencies

**Validation Results:**
- ? UI ? Services (one-way)
- ? Services ? Infrastructure (one-way)
- ? All layers ? Core Interfaces (one-way, dependency inversion)
- ? No back-references detected

### ?? Notable Dependencies (Not Circular)

#### 1. SyncEngine ? ITransferService
- **Location**: Both in Services layer
- **Pattern**: Service orchestration
- **Direction**: SyncEngine depends on ITransferService (interface)
- **Reverse**: TransferService does NOT depend on SyncEngine
- **Verdict**: ? Valid one-directional dependency

#### 2. GraphClientWrapper ? IAuthService
- **Location**: Both implementations in Infrastructure
- **Pattern**: Dependency on Core interface
- **Direction**: GraphClientWrapper depends on IAuthService (interface)
- **Implementation**: MsalAuthService implements IAuthService
- **Reverse**: MsalAuthService does NOT depend on GraphClientWrapper
- **Verdict**: ? Valid dependency through interface

---

## Dependency Rules Applied

### ? Followed Rules
1. **Dependency Inversion Principle**
   - All high-level modules depend on abstractions (interfaces in Core)
   - Infrastructure implements Core interfaces

2. **Single Direction Flow**
   - UI ? Services ? Infrastructure ? Core
   - No reverse dependencies

3. **Interface Segregation**
   - Small, focused interfaces (IAuthService, ISyncEngine, etc.)
   - Clients depend only on interfaces they use

4. **Layered Architecture**
   - Clear separation of concerns
   - Each layer has distinct responsibility

### ?? Dependency Statistics

| Layer | Depends On | Depended By | Interface Count |
|-------|------------|-------------|-----------------|
| UI | Services, Core | None | 0 |
| Services | Infrastructure, Core | UI | 2 (ISyncEngine, ITransferService) |
| Infrastructure | Core | Services, UI | 0 |
| Core | None | All | 6 (IAuthService, ISyncEngine, ITransferService, IGraphClient, ISyncRepository, IFileSystemAdapter) |

---

## Design Patterns Identified

### 1. Repository Pattern
```
ISyncRepository (Core) ? EfSyncRepository (Infrastructure)
```

### 2. Adapter Pattern
```
IFileSystemAdapter (Core) ? LocalFileSystemAdapter (Infrastructure)
```

### 3. Wrapper Pattern
```
IGraphClient (Core) ? GraphClientWrapper (Infrastructure)
```

### 4. Service Layer Pattern
```
ISyncEngine (Core) ? SyncEngine (Services)
ITransferService (Core) ? TransferService (Services)
```

### 5. MVVM Pattern
```
MainWindow.axaml ? MainWindowViewModel
```

---

## Potential Architectural Improvements

### 1. Extract ITransferService to Core (Optional)
**Current**: ITransferService is in Services layer  
**Rationale**: If TransferService is a core domain concept, move interface to Core  
**Benefit**: More explicit dependency inversion  
**Impact**: Low - current design is acceptable for orchestration services

### 2. Consider CQRS for Repository (Future)
**Current**: ISyncRepository handles all data operations  
**Enhancement**: Separate read and write operations  
**Benefit**: Better scalability and testability  
**Impact**: Medium - refactor when needed

### 3. Event-Driven Architecture (Future)
**Current**: Direct service calls  
**Enhancement**: Use domain events for cross-service communication  
**Benefit**: Decoupling, better testability  
**Impact**: High - significant architectural change

---

## Testing Implications

### ? Highly Testable Architecture

**Benefits of Clean Dependencies:**
1. **Unit Testing**: Each layer can be tested in isolation
2. **Integration Testing**: Clear boundaries for integration tests
3. **Mocking**: All dependencies are interfaces (easy to mock)
4. **Test Pyramid**: Clear separation supports proper test distribution

**Example Test Setup:**
```csharp
// Unit Test - ViewModel
MainWindowViewModel vm = new(
    mockAuthService,      // IAuthService
    mockSyncEngine,       // ISyncEngine
    mockTransferService,  // ITransferService
    mockSettingsService,  // ISettingsAndPreferencesService
    mockLogger
);

// Integration Test - Services
SyncEngine engine = new(
    realRepository,       // Real EfSyncRepository with in-memory DB
    mockGraphClient,      // Mock IGraphClient
    mockTransferService,  // Mock ITransferService
    mockLogger
);
```

---

## Conclusion

### Architecture Quality: ????? Excellent

**Strengths:**
- ? No circular dependencies
- ? Clean layered architecture
- ? Proper dependency inversion
- ? High testability
- ? Clear separation of concerns
- ? Interface segregation

**Recommendations:**
- ? Current architecture is production-ready
- ?? Minor: Consider moving ITransferService to Core (optional)
- ?? Future: CQRS and event-driven patterns as system grows

---

## Appendix: Interface Locations

### Core Interfaces (AStar.Dev.OneDrive.Client.Core)
- `IAuthService` - Authentication abstraction
- `IGraphClient` - Microsoft Graph API abstraction
- `ISyncRepository` - Data persistence abstraction
- `IFileSystemAdapter` - File system operations abstraction

### Services Interfaces (AStar.Dev.OneDrive.Client.Services)
- `ISyncEngine` - Sync orchestration
- `ITransferService` - File transfer operations

### UI Interfaces (AStar.Dev.OneDrive.Client)
- `ISettingsAndPreferencesService` - User preferences management

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-XX  
**Analysis Tool**: Manual code review + static analysis  
**Related Docs**: [Test Coverage Summary](./Test-Coverage-Summary.md)
