# .Net10 / C# 14 Development Standards

**Universal coding standards for modern C# projects targeting .NET 10+**

## Quick Reference

### Code Style
- Use var for all local variables except:
    - For test mocks, always use the explicit type.
    - When the type is not obvious from the right-hand side, use the explicit type (e.g., for async calls or methods returning interface types).
    - If IDE0008 or similar analyzer suggests explicit type, prefer explicit type to avoid warnings.
- Always use explicit types for test mocks and for parameters in test helpers.
- Nullable reference types enabled
- Warnings as errors
- Expression-bodied members for simple operations
- Primary constructors where appropriate
- No blank lines between method signature and expression body / implementation
- No multiple blank lines
- No trailing whitespace
- No unnecessary usings
- No regions

```csharp
// Correct
  IMyService mockService = Substitute.For<IMyService>();

// Incorrect
  var mockService = Substitute.For<IMyService>();
```

### Testing
- Framework: xUnit V3
- Assertions: Shouldly
- Mocking: NSubstitute
- Naming: `<ClassName>Should` with `<Action><ExpectedBehavior>` methods. Example class name: ```HealthCheckShould```. Example Test name: ```ReturnHealthyStatusWhenUserIsSignedInAndTokenAcquired()```
- For all async test methods, always pass TestContext.Current.CancellationToken to any method that accepts a CancellationToken.
- AAA pattern (Arrange, Act, Assert) with blank lines as separators
- All tests must be deterministic
- Avoid redundant tests
- All tests must have at least one assertion

Example:
```csharp
[Fact]
  public async Task MyAsyncTest()
  {
      var sut = new MyService();

      var result = await sut.DoSomethingAsync(TestContext.Current.CancellationToken);

      result.ShouldBe(expected);
  }

```
- **Rationale**: This ensures tests are cancellable and compliant with xUnit V3 best practices.
---

## Coding Guidelines

### Type System & Nullability
- Enable nullable reference types and strict null checks
- Use `var` for local variable declarations (IDE0007)
  - **Exception**: Use explicit types for mock declarations in tests
- Treat warnings as errors - ensure clean builds
- Fix all warnings and errors before committing, including IDE suggestions (e.g., IDE0008 for explicit/var).

### Async Programming
- Only use async for truly awaitable operations (I/O-bound, parallelizable CPU-bound)
- Production async methods end with "Async" suffix (e.g., `GetUserAsync()`)
- Always accept and pass `CancellationToken` to awaitable operations
- Use `ConfigureAwait(false)` in library code

### Clean Code Principles
- Follow SOLID principles
- Meaningful names, small methods, single responsibility
- Methods: generally ~30 lines
- Classes: generally ~300 lines
- Method parameters: ~3 preferred (consider parameter objects if more)
- Constructor parameters: ~3 preferred (may indicate class doing too much)

### Modern C# Features
- **Primary constructors**: Use for concise class definitions
  ```csharp
  public class Person(string Name, int Age) { }
  ```
- **Collection expressions**: Cleaner initialization
  ```csharp
  var numbers = [1, 2, 3];
  ```
- **Pattern matching**: Use for cleaner control flow
- **Record types**: Use for immutable data models

### Dependency Injection
- Use appropriate service lifetimes (`AddScoped`, `AddSingleton`, `AddTransient`)
- Prefer constructor injection
- Keep constructors focused on dependency assignment

---

## Testing Standards

### Test Organization
- Mirror production code structure exactly - e.g. in the correct namespace matching the folder structure of the production code
- One test class per production class and each test file must only contain one test class
- Place in same relative path within test project
- Example:
  ```
  src/MyApp/Services/Calculator.cs
  test/MyApp.Tests.Unit/Services/CalculatorShould.cs
  ```

### Test Naming
- **Class**: `<ClassName>Should`
- **Method**: `<Action><ExpectedBehavior>`
- Creates grammatically correct sentences, no underscores, e.g., `CalculateTotalReturnsCorrectSum`

Examples:
```csharp
public class CalculatorShould
{
    [Fact]
    public void AddTwoNumbersAndReturnTheSum()
    {
        var calculator = new Calculator();
        
        var result = calculator.Add(2, 3);
        
        result.ShouldBe(5);
    }
    
    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(5, 5, 10)]
    public void AddTwoNumbersCorrectly(int a, int b, int expected)
    {
        var result = new Calculator().Add(a, b);

        result.ShouldBe(expected);
    }
}
```

### Test Guidelines
- **AAA Pattern**: Arrange, Act, Assert (separate with blank lines, not comments)
- **Test async methods**: Do NOT use "Async" suffix (affects test runner output)
- **Deterministic**: No external state or timing dependencies
- **Meaningful coverage**: Avoid redundant tests (Law of Diminishing Returns)
- **Target coverage**: ~80% for new features
- Use var for all local variables except for test mocks and when the type is not obvious.
- Always use explicit types for test mocks and parameters in test helpers.
- All test classes must be placed in the correct namespace matching the folder structure.
- Each test file must contain only one public test class.
- Each test file must start with all required using statements, followed by the namespace, then the test class.
- No documentation comments are required for test classes or methods.
- Run ```dotnet build``` and ```dotnet test --no-build``` after any test or code change.
- Fix all warnings and errors before committing.

### Variable Declaration in Tests
```csharp
// Use explicit types for mocks (avoids warnings-as-errors issues)
IMyService mockService = Substitute.For<IMyService>();

// Use explicit types for exceptions
Exception? exception = Record.Exception(() => sut.DoSomething());
```

### Consolidating Similar Tests
Use `[Theory]` with `[InlineData]` to reduce duplication:
```csharp
[Theory]
[InlineData("Light", 1)]
[InlineData("Dark", 2)]
public void MapThemeToCorrectIndex(string theme, int expectedIndex)
{
    var result = sut.MapTheme(theme);
    result.ShouldBe(expectedIndex);
}
```

---

## Code Quality & Static Analysis

### Warning Suppression
When warnings must be suppressed (rare), use `#pragma` with clear justification:
```csharp
#pragma warning disable S1075 // URIs should not be hardcoded - Required by OAuth library
private const string RedirectUri = "http://localhost";
#pragma warning restore S1075
```

Common suppressible warnings:
- **S1075**: Hardcoded URIs (OAuth redirects, required API endpoints)
- **S6667**: Logging in catch (when exception intentionally not logged)

---

## Test Execution

### Running Tests
```bash
# Full test suite
dotnet test

# Without rebuilding
dotnet test --no-build

# Specific test class
dotnet test --filter "FullyQualifiedName~ClassName"

# Verbose output
dotnet test --logger "console;verbosity=detailed"
```

### Common Issues
- **Build passes, tests fail**: Run `dotnet build` first, then `dotnet test --no-build`
- **CS0246 type not found**: Check using statements and namespaces

---

## Interface Design for Testability

### When to Create Interfaces
- Classes that are dependencies of other classes
- Sealed classes used as dependencies MUST have interfaces

### Interface Conventions
- **Naming**: `I` + class name (e.g., `ThemeService` ? `IThemeService`)
- **Placement**: Same namespace as implementation
- **Documentation**: Document interface, use `/// <inheritdoc/>` in implementation

### Refactoring Steps
1. Create interface
2. Update class to implement interface
3. Update consumers to use interface
4. Update DI registrations
5. Write tests with mocked interface

---

## Documentation

### XML Comments
- Document ALL public APIs (methods, properties, classes)
- Do NOT document tests or test projects
- Use `/// <inheritdoc/>` when implementing interfaces

Example:
```csharp
/// <summary>
/// Gets or sets the logging configuration for the application.
/// </summary>
/// <remarks>
/// Use this property to customize logging behavior, such as log levels, 
/// output destinations, and formatting options.
/// </remarks>
public LoggingConfig Logging { get; set; } = new();
```

---

## Additional Notes

- Use `System.Text.Json` for JSON serialization
- Use `Microsoft.Extensions.Logging` for structured logging
- Specify `encoding="utf-8"` when working with text files
- Use semantic commit messages (e.g., `feature:`, `fix:`, `refactor:`)
- Ensure commits pass linting and tests before pushing

### Examples of Correct `var` Usage
```csharp
// Use explicit type for mocks
IAuthService auth = Substitute.For<IAuthService>();

// Use explicit type when type is not obvious (e.g., async/await)
HealthCheckResult result = await check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

// Use var when type is obvious
var check = new GraphApiHealthCheck(auth);
