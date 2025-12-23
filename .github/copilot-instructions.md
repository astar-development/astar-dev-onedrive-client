# .NET 10 Development Instructions

**Status**

�	The project targets .NET 10 as required.

## Project Context
- Target Framework: .NET 10
- Architecture: As this solution implements a single entry point (the MainWindow.axaml), it does not require multiple projects for different layers at this time. Instead, separate folders exist for each separate feature: Authentication, Views, ViewModels, OneDriveServices, SettingsAndPreferences, etc.
- Language Features: C# 14 (e.g., primary constructors, collection expressions)
- Testing: Unit and Integration Testing with xUnit V3. Shouldly for assertions. NSubstitute for mocking.
- Documentation: English-only, XML comments for ALL public APIs - methods and classes but NOT Tests / Test Projects. If the class implements an interface, the interface should be documented and the method annotated with the ```/// <inheritdoc/>``` XML Tag. Below is an example of XML documentation for a public property:

```
    /// <summary>
    /// Gets or sets the logging configuration for the application.
    /// </summary>
    /// <remarks>Use this property to customize logging behavior, such as log levels, output destinations, and
    /// formatting options. Changes to the logging configuration take effect immediately and may impact how diagnostic
    /// information is recorded.</remarks>
    public Logging Logging { get; set; } = new();
```

## Development Standards

### Architecture
- Follow a simple, folder-based structure for the core solution project, with NuGet packages for cross-cutting concerns
- Use Domain-Driven Design (DDD) for complex business logic if applicable
- Implement the Repository pattern for data access abstraction if applicable
- Apply CQRS (Command Query Responsibility Segregation) for read/write separation if applicable

### Coding Guidelines
- Use nullable reference types and enable strict null checks
- Prefer `async/await` for asynchronous programming. Do not make methods Async "just because" - only use async when there is a truly awaitable operation, such as I/O-bound work or CPU-bound work that can be parallelized. When creating an async method, ensure a cancellation token is accepted and passed to all awaitable operations within the method.
- Production async methods should end with the "Async" suffix. E.g., `GetUserAsync()`.
- Use `ConfigureAwait(false)` in library code to avoid deadlocks in certain synchronization contexts
- Follow SOLID principles for maintainable and extensible code
- Adhere to Clean Code principles: meaningful names, small methods, single responsibility, single level of abstraction per method
- Use extension methods judiciously to enhance readability without over-complicating the codebase
- Use dependency injection (DI) for service lifetimes (e.g., `AddScoped`, `AddSingleton`)
- Write immutable classes where possible, using `record` types for data models
- Use pattern matching and switch expressions for cleaner code
- Use expression-bodied members for simple properties and methods
- Methods should generally not exceed 30 lines of code. If a method exceeds this, consider refactoring by extracting smaller methods or simplifying logic.
- Classes should generally not exceed 300 lines of code. If a class exceeds this, consider refactoring by splitting it into smaller, more focused classes.
- Projects are set to treat warnings as errors. Ensure code compiles without warnings. E.g.: use discard `_` for unused variables, prefix private fields with `_`, etc.
- Minimise the number of parameters in methods. If a method has more than 3 parameters consider refactoring by grouping related parameters into a class or struct.
- Minimise the number of parameters in a constructor. If a constructor has more than 3 parameters consider refactoring the class as it may be doing too many things. If necessary, refactor by grouping related parameters into a class or struct. (Avoid using the "Parameter Object" pattern excessively as it can lead to an explosion of small classes that are only used in one place.)

### Language Features
- Use primary constructors for concise class definitions:
 ```csharp
 public class Person(string Name, int Age) { }
 ```
- Leverage collection expressions for cleaner initialization:
```csharp
 
var numbers = [1, 2, 3];
```
- Use ref readonly parameters for performance-sensitive APIs.

## Testing

- Write unit tests for all business logic in the Domain layer.
- Use Shouldly for expressive assertions:
```csharp
 result.ShouldBe(expectedValue);
```
- Use NSubstitute for mocking dependencies in unit tests:
```csharp
 var mockService = Substitute.For<IMyService>();
```

- Use integration tests for API endpoints and database interactions.

- Follow Test-Driven Development (TDD) principles: ALWAYS write Unit tests before implementation.

- Test classes should follow the naming convention: `<ClassName>Should`. Test methods should follow the naming convention: `<Action><ExpectedBehavior>` so the combination of the test class and the test method creates a grammatically correct, English, sentence.
- Example:
```csharp
 public class CalculatorShould
 {
     [Fact]
     public void AddTwoNumbersAndReturnTheExpectedSum()
     {
         var calculator = new Calculator();

         var result = calculator.Add(2, 3);

         result.ShouldBe(5);
     }
 }
```

- When adding tests, ensure they are deterministic and do not rely on external state or timing.
- When adding tests, remember the "Law of Dimishing Returns" - each additional test should provide meaningful coverage and value. Avoid redundant tests that do not add new insights.
- Tests should use the AAA pattern: Arrange, Act, Assert. BUT, avoid unnecessary comments that state the obvious. Use blank lines to separate the Arrange, Act, and Assert sections instead of comments.
- Test async methods should NOT end with the "Async" suffix as that will affect the readability of the test in the runner / test report etc.
- The tests should exist in the same folder structure as the production code, but within a separate test project.
- Use xUnit V3 as the test framework.
- Ensure code coverage is at least 80% for all new features.
- Use code coverage tools like Coverlet or dotCover to measure coverage.
- Mock external dependencies (e.g., databases, web services) to isolate unit tests.
- Run tests automatically in CI/CD pipelines using GitHub Actions.
- Ensure tests run in parallel where possible to reduce execution time.
- Use test fixtures for shared setup and teardown logic.
- Unit Test Projects should be named with the suffix `.Tests.Unit`. E.g., if the production project is named `MyApp`, the corresponding test project should be named `MyApp.Tests.Unit`.

**Commit Conventions**

- Use semantic commit messages (e.g., feature:, fix:, refactor:).

- Ensure all commits pass linting and tests before pushing.

## AI-Assisted Development with GitHub Copilot

**Prompt Guidelines**

- Use prompts to enforce TDD workflows:

```csharp
 Write a failing test for the `CalculateTax` method in the `TaxService` class
```

Request clarifications from Copilot when generating code:

```csharp
 What assumptions are you making about the `Order` class?
```

## Chat Modes

- Use the "Architect" chat mode for planning and documentation tasks.

- Use the "Code Reviewer" chat mode to identify potential issues in pull requests.

**Reusable Prompts**

- Save reusable prompts in .github/prompts/. Example:

```csharp
 ---
mode: agent
tools: ['codebase', 'editFiles', 'runTests']
description: "Generate unit tests for the `OrderService` class."
---
```

## Additional Notes

- Always specify encoding="utf-8" when working with text files.

- Use System.Text.Json for JSON serialization/deserialization.

- Enable logging with Microsoft.Extensions.Logging and configure structured logs.

## Contribution Guidelines

- Follow the repository's coding standards and architectural rules.

- Submit pull requests with detailed descriptions and linked issues.

- Ensure all new features include tests and documentation.
