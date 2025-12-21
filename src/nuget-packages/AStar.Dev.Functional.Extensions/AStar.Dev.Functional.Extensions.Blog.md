### AStar.Dev.Functional.Extensions — A friendly, practical guide to Results and Options in .NET (for junior devs) 🎯✨

Welcome! If you’ve ever written one of these…

```csharp
try
{
    var data = await api.GetStuffAsync();
    if (data is null)
    {
        ShowError("No data returned :(");
        return;
    }
    UpdateUi(data);
}
catch (Exception ex)
{
    ShowError(ex.Message);
}
```

…then you already know the pain this library tries to reduce: repetitive try/catch blocks, unclear null checks, and plumbing code that drowns real logic. `AStar.Dev.Functional.Extensions` gives you
small, focused building blocks so you can model success/failure and optional values explicitly — while keeping everyday code readable and testable.

This article is a from-zero-to-productive tour aimed at junior developers. We’ll go slow, use lots of examples, and connect each concept to real MVVM and service code you’ll actually write. By the end
you’ll be comfortable using `Result<TSuccess, TError>` and `Option<T>`, and you’ll know when to reach for helper extensions like `Apply`, `ApplyToCollectionAsync`, `ToStatus`, `GetOrThrow`,
`ToErrorResponse`, and the LINQ-style operators for `Option<T>`.

We’ll cover:

- What `Result<TSuccess, TError>` is and why it’s better than fragile `null`s and exceptions leaking everywhere
- How `Option<T>` models “maybe there’s a value, maybe there isn’t” without ambiguity
- Practical patterns for view-models: set status, show errors, update collections
- Small helpers you can memorize in minutes that save you hours over a month
- Asynchronous variants and why `ConfigureAwait(false)` appears in the source
- Testing: how these shapes make unit tests predictable and simple
- Pitfalls, trade-offs, and tips for team code reviews

All examples are C# 12/13 compatible. The post is pure Markdown (Medium/web friendly). Grab a snack. Let’s go. 🙌

---

### Quick index (bookmark this) 🧭

- Core types: `Result<TSuccess, TError>`, `Option<T>`
- View-model ergonomics: `Apply`, `ApplyAsync`
- UI collection and status: `ApplyToCollectionAsync`, `ToStatus`
- Convenience helpers: `GetOrThrow`, `GetOrThrowAsync`, `ToErrorMessage`
- Exception to UI mapping: `ToErrorResponse`, `ToErrorResponseAsync`
- LINQ for options: `Select`, `SelectMany`, `SelectAwait`
- End-to-end example: service → view-model → view
- FAQ, pitfalls, and checklists

Pro tip: Treat failure and absence as values, not surprises. Your future self (and teammates) will thank you. 🧑‍💻💚

---

## Part 1 — Meet `Result<TSuccess, TError>`

Think of `Result<TSuccess, TError>` as a box that contains either:

- a successful value of type `TSuccess`, or
- a failure reason of type `TError`.

It’s a small discriminated union with two nested classes: `Ok` and `Error`.

```csharp
// Success
var ok = new Result<int, Exception>.Ok(42);

// Failure
var err = new Result<int, Exception>.Error(new InvalidOperationException("Nope."));

// Match to handle each case explicitly
var message = ok.Match(
    onSuccess: v => $"Got {v}",
    onFailure: e => $"Failed: {e.Message}" // not called here
);
```

Why this is helpful for beginners:

- You don’t need to remember to throw exceptions to signal failure.
- You don’t rely on `null` (which could mean “didn’t find anything”, or “bug caused a null”, or “special case”… yikes).
- Your method signatures tell a story: “This can fail, and if it fails, here’s the error type.”

Async versions exist, too:

```csharp
Task<Result<User, Exception>> resultTask = userService.TryLoadAsync(id);

string status = await (await resultTask).MatchAsync(
    onSuccess: async user =>
    {
        await cache.SaveAsync(user);
        return $"Welcome back, {user.DisplayName}!";
    },
    onFailure: e => $"Load failed: {e.Message}"
);
```

Note: In the real library, `MatchAsync` comes in multiple overloads so you can mix sync/async handlers. This keeps your code succinct even when only one side is asynchronous.

### When should you return `Result<T, Exception>`?

- You’re calling I/O (HTTP, file system, DB) and errors are expected sometimes.
- You want the caller to decide how to display or log errors.
- You want to unit test both success and failure flows without throwing exceptions.

When not to use it:

- For internal helper methods where exceptions are truly exceptional and you intend to catch them one level up. Don’t overuse it; be pragmatic.

---

## Part 2 — `Option<T>`: None is not an error; it’s just… none

`Option<T>` models “maybe there’s a value”. It has two cases:

- `Option<T>.Some(value)` — we have a value
- `Option<T>.None` — no value

Perfect for lookups and parsing.

```csharp
Option<string> FindNickname(User user)
    => string.IsNullOrWhiteSpace(user.Nickname)
        ? Option.None<string>()
        : new Option<string>.Some(user.Nickname);

// Using it later
string label = FindNickname(user) is Option<string>.Some some
    ? some.Value
    : "(no nickname)";
```

With the LINQ-style methods you’ll see later (`Select`, `SelectMany`, `SelectAwait`), you can transform/compose options fluidly.

### Why not use `null` instead?

Because `null` doesn’t communicate intent. Is `null` a bug? A missing record? Did the repository timeout? With `Option<T>`, the “no value” is expected and explicit. Your API becomes self-documenting.

---

## Part 3 — ViewModel helpers: `Apply` and `ApplyAsync`

Most apps have a view-model that updates properties like `Status`, `ErrorMessage`, and collections bound to lists. You’ll constantly write “if success then set A, else set B”. `Apply` makes this a
one-liner.

Namespace: `AStar.Dev.Functional.Extensions`

### `Apply`

```csharp
Result<string, Exception> userName = await TryLoadUserNameAsync();

userName.Apply(
    onSuccess: name => vm.Status = $"Hello, {name}!",
    onError:   ex   => vm.ErrorMessage = ex.Message
);
```

Tips:

- Keep handlers tiny. If they’re getting big, that logic probably belongs in a method.
- You can pass only `onSuccess` if you want errors to no-op (e.g., you’ll handle them elsewhere).

### `ApplyAsync` (Task<Result<...>>) with sync handlers

```csharp
Task<Result<int, Exception>> getCountTask = repo.GetUnreadCountAsync();
await getCountTask.ApplyAsync(
    onSuccess: count => vm.Status = $"{count} unread",
    onError:   ex     => vm.ErrorMessage = $"Oops: {ex.Message}"
);
```

### `ApplyAsync` with async handlers

```csharp
await repo.GetProfileAsync().ApplyAsync(
    onSuccessAsync: async profile =>
    {
        await avatarCache.SaveAsync(profile.AvatarBytes);
        vm.Status = $"Welcome back, {profile.DisplayName}!";
    },
    onErrorAsync: async ex =>
    {
        await telemetry.TrackAsync("profile_load_failed", ex);
        vm.ErrorMessage = ex.Message;
    }
);
```

Why you’ll love it: It removes the ceremony around branching, and your result handling becomes literally the shape of what you want to do. It’s hard to accidentally forget handling a failure.

Common mistake: Doing heavy work inside UI-updating handlers. Keep `onSuccess` mostly about assigning properties; delegate long operations to other async methods.

---

## Part 4 — Collections and status messages

Two highly practical helpers live in `CollectionAndStatusExtensions`.

### `ApplyToCollectionAsync`

Signature: `Task ApplyToCollectionAsync<T>(this Task<Result<IEnumerable<T>, Exception>> resultTask, ObservableCollection<T> target, Action<Exception>? onError = null)`

What it does:

- Awaits your `Task<Result<IEnumerable<T>, Exception>>`.
- On success, clears the target `ObservableCollection<T>` and adds new items.
- On error, calls `onError` with the exception and leaves the collection unchanged.
- If the success value is `null`, it clears the collection.

Use it whenever you load lists for listboxes/grids.

```csharp
ObservableCollection<FileItem> Items { get; } = new();

Task<Result<IEnumerable<FileItem>, Exception>> task = fileService.LoadAsync(folderId);
await task.ApplyToCollectionAsync(
    Items,
    onError: ex => vm.ErrorMessage = $"Load failed: {ex.Message}"
);
```

Testing tip: Because this method is deterministic, it’s easy to assert that the collection now contains exactly your items or that `onError` was invoked.

### `ToStatus`

Signature: `string ToStatus<T>(this Result<T, Exception> result, Func<T, string> successFormatter, Func<Exception, string>? errorFormatter = null)`

This turns a `Result<T, Exception>` into a friendly string. On success, it runs your `successFormatter(value)`. On failure, it uses `errorFormatter(ex)` or just `ex.Message`.

```csharp
Result<int, Exception> r = await stats.CountNewDocsAsync();
vm.Status = r.ToStatus(
    successFormatter: n => $"Indexed {n} docs",
    errorFormatter:   e => $"Indexing failed: {e.Message}"
);
```

Why it’s nice: centralize status formatting; keep your view-model clean.

---

## Part 5 — Convenience helpers

Sometimes you just need the value, or a quick UI error. These do exactly that.

### `GetOrThrow`

```csharp
var user = repo.GetUser("42");                  // Result<User, Exception>
var u = user.GetOrThrow();                       // Returns User or throws the captured Exception
```

Great for tests or when you know failures are already validated elsewhere.

### `GetOrThrowAsync`

```csharp
var profile = await repo.GetProfileAsync().GetOrThrowAsync();
```

### `ToErrorMessage`

```csharp
Result<bool, Exception> saved = await repo.SaveAsync(model);
string message = saved.ToErrorMessage(); // "" on success, ex.Message on failure
if (!string.IsNullOrEmpty(message))
    vm.ErrorMessage = message;
```

Pitfall: Don’t use `GetOrThrow` in UI code where an exception would crash the experience. Prefer `Apply` or `ToStatus` there.

---

## Part 6 — Mapping exceptions to UI errors (`TryExtensions`)

It’s common to catch exceptions at the boundary (e.g., service layer) and turn them into a UI-friendly shape. `ToErrorResponse` does this by mapping the base exception message to your app’s
`ErrorResponse` type.

```csharp
Result<Order, Exception> r = await orderService.PlaceAsync(cart);
Result<Order, ErrorResponse> uiResult = r.ToErrorResponse();

vm.Error = uiResult.Match(
    onSuccess: _ => null,
    onFailure: err => err.Message
);
```

Async variant:

```csharp
Result<bool, ErrorResponse> ok = await repo.DeleteAsync(id).ToErrorResponseAsync();
```

When to use:

- You’re preparing data for the UI; you don’t want raw exceptions there.
- You want to centralize error message extraction and optionally localize later.

---

## Part 7 — LINQ for optional values (`OptionLinqExtensions`)

These helpers let you compose `Option<T>` like you compose `IEnumerable<T>` with LINQ.

### `Select` (map)

```csharp
Option<int> maybe = TryParseInt(userInput);
Option<string> doubled = maybe.Select(n => (n * 2).ToString());
```

### `SelectMany` (bind)

Great for chaining lookups without deeply nested `if`/`else`.

```csharp
Option<User> user = repo.TryFindUser(handle);
Option<string> email = user.SelectMany(
    u => repo.TryFindPrimaryEmail(u.Id),
    (u, e) => e
);
```

### `SelectAwait` (async map)

```csharp
Task<Option<Project>> task = repo.TryGetProjectAsync(key);
Option<string> summary = await task.SelectAwait(async p =>
{
    var stats = await repo.GetStatsAsync(p.Id);
    return $"{p.Name}: {stats.OpenIssues} open issues";
});
```

Mental model: If you have a box that might have a value, `Select` lets you transform the value without opening the box. If the box is empty (`None`), the transformation is skipped.

---

## Part 8 — End-to-end example (service → VM → view)

Let’s put several pieces together. Suppose we need to load items from an API, display them in a list, and show a status.

```csharp
// Service method returns a Result wrapped in a Task
Task<Result<IEnumerable<Item>, Exception>> LoadItemsAsync() =>
    TryAsync(async () =>
    {
        var items = await api.GetItemsAsync();
        return new Result<IEnumerable<Item>, Exception>.Ok(items);
    });

// ViewModel has an observable collection bound to the UI
public ObservableCollection<Item> Items { get; } = new();
public string Status { get; private set; } = string.Empty;
public string ErrorMessage { get; private set; } = string.Empty;

public async Task RefreshAsync()
{
    await LoadItemsAsync().ApplyToCollectionAsync(
        Items,
        onError: ex => ErrorMessage = ex.Message
    );

    var status = (await LoadItemsAsync()).ToStatus(
        successFormatter: xs => $"Loaded {xs.Count()} items",
        errorFormatter:   e  => $"Couldn’t load items: {e.Message}"
    );

    Status = status;
}
```

If an item has optional metadata, use `Option<T>` to compose it:

```csharp
Option<User> maybeOwner = repo.TryFindOwner(item.Id);
Option<string> ownerDisplay = maybeOwner.Select(o => o.DisplayName);
string display = ownerDisplay is Option<string>.Some some ? some.Value : "(no owner)";
```

This keeps both loading and formatting logic explicit and testable.

---

## Part 9 — Testing guidance

You’ll love testing with these shapes:

- To test success flows, construct `new Result<T, Exception>.Ok(value)`.
- To test failures, construct `new Result<T, Exception>.Error(new Exception("message"))`.
- For `ApplyToCollectionAsync`, pass a `Task.FromResult<Result<IEnumerable<T>, Exception>>(...)`.
- For `Option<T>`, use `new Option<T>.Some(value)` or `Option.None<T>()`.

Examples:

```csharp
[Fact]
public async Task Refresh_Sets_Items_On_Success()
{
    var vm = new MyVm();
    Task<Result<IEnumerable<int>, Exception>> task()
        => Task.FromResult<Result<IEnumerable<int>, Exception>>(new Result<IEnumerable<int>, Exception>.Ok(new[] {1,2,3}));

    await task().ApplyToCollectionAsync(vm.Items);

    vm.Items.ShouldBe([1,2,3]);
}
```

```csharp
[Fact]
public void ToStatus_Uses_Error_Message_When_Failure()
{
    var err = new Result<int, Exception>.Error(new Exception("boom"));
    err.ToStatus(i => i.ToString()).ShouldBe("boom");
}
```

Because everything is just data (no hidden global state), tests are predictable and quick.

---

## Part 10 — Pitfalls and pro tips

- Don’t wrap everything in `Result`. For pure, internal computations, throwing and catching might be simpler.
- Prefer `ToStatus` for status bars and small messages; don’t do heavy work inside formatters.
- For UI code, avoid `GetOrThrow` — use `Apply` so you don’t crash the UI thread.
- With `Option<T>`, don’t convert back to `null` unless you must interact with legacy APIs. Keep the clarity.
- When composing async options with `SelectAwait`, keep the operations small and focused.

Code review checklist:

- Does the method signature clearly communicate success/failure (`Result`) or optionality (`Option`)?
- Are error messages coming from a single place (`ToErrorResponse` or formatter)?
- Are view-model updates concise and done via `Apply`/`ApplyAsync`?
- Are collection updates using `ApplyToCollectionAsync` instead of manual clear/add loops?

---

## Part 11 — FAQ

Q: Is this a full functional framework?  
A: No. It’s pragmatic sugar over patterns you already use.

Q: Do I have to change my architecture?  
A: Nope. Use what helps. Ignore the rest.

Q: What about performance?  
A: The types are small wrappers. In typical app code (I/O, UI), overhead is negligible compared to clarity gains.

Q: Is it test-friendly?  
A: Extremely. Every helper is deterministic; the repo ships with unit tests.

Q: Can I map exceptions to localized messages?  
A: Yes — `ToErrorResponse` centralizes extraction. Localize `ErrorResponse.Message` later.

---

### Bonus: Deep dive for the curious (and for interviews) 🧠

#### Why model failure as a value instead of throwing exceptions?

- Exceptions are great for truly exceptional situations, not routine “the network was down” or “the user cancelled.” Exceptions escape normal control flow, can be expensive if used for control logic,
  and are easy to forget to handle in distant call sites. By contrast, a `Result<T, TError>` puts success and failure in the type signature. The compiler will nudge you to handle both.

- With `Result`, failures are test data. You can construct an error case in one line and write a test that asserts your UI responds properly, without arranging a crazy environment to force a real
  exception.

#### “Railway oriented” programming in 2 minutes

Imagine your workflow as train tracks. Success continues straight; failure diverts to the side. With `Result`, each function either returns an `Ok` (keep going) or `Error` (handle or convert). The
library’s helpers (`Apply`, `ToStatus`, `ToErrorResponse`) are like switches that make the tracks easy to follow. This pattern keeps long workflows readable.

#### What about logging? Where should it live?

Log at boundaries. Have the service that talks to HTTP/DB catch exceptions and return `Result<T, Exception>`, logging details once. Downstream, display succinct messages (maybe via `ToStatus` or
`ToErrorResponse`) and avoid re‑logging the same error over and over.

#### Threading and UI frameworks (Avalonia/WPF/WinUI)

Updating UI must happen on the UI thread. The extensions in this library don’t marshal threads for you — they help with branching. If you’re in an event handler already on the UI thread, you’re fine.
Otherwise, in Avalonia use `Dispatcher.UIThread.Post` or `InvokeAsync` around your UI updates. Keep your handlers short; do I/O work in services.

#### Performance notes

These types mainly add tiny wrappers and switch expressions; overhead is negligible in I/O‑bound apps. If you’re writing hot loops in high‑frequency code (e.g., real‑time graphics), prefer direct
methods that avoid allocations. In normal app/service code, readability wins by a mile.

#### Migration guide: from exceptions and nulls to `Result` and `Option`

1) Identify a service method that sometimes throws or returns `null` on failure.
2) Change signature to `Task<Result<T, Exception>>` for failure or `Result<Option<T>, Exception>` for “found or not”.
3) At the boundary, wrap exceptions: `catch (Exception ex) => new Result<T, Exception>.Error(ex)`.
4) Update callers to use `Apply`, `ApplyAsync`, `ToStatus`, or `Match` to branch explicitly.
5) Remove now‑redundant try/catch at call sites.

#### Style guide for team consistency

- Use `Result<T, Exception>` for recoverable failures.
- Use `Option<T>` when absence is normal, not an error.
- In view‑models, prefer `Apply`/`ApplyAsync` over explicit `switch` unless logic is complex.
- Centralize error message mapping with `ToErrorResponse` or dedicated formatters.
- Name your formatters clearly: `successFormatter`, `errorFormatter`.

#### Glossary

- Result: A discriminated union of `Ok` or `Error`.
- Option: A discriminated union of `Some` or `None`.
- Match: A pattern‑matching helper to branch on variants.
- Bind/SelectMany: Compose operations while preserving the container shape.

#### Practice exercises

1) Wrap a file‑read method to return `Result<string, Exception>`. Display a status using `ToStatus`.
2) Write a repository `TryFindByEmail` returning `Option<User>`. Compose with `Select` to get `DisplayName` if present.
3) Convert an existing try/catch in your VM into `ApplyAsync` handlers.
4) Write tests that cover `Ok` and `Error` flows without throwing.

#### Common interview question prompts

- “How would you model an operation that may fail without throwing?”
- “When would you choose `Option<T>` over `null`?”
- “Show how to map a `Result<T, Exception>` to UI messages.”

Being able to write clean code with these types makes great interview material.

---

### Final thoughts

You don’t need to “go functional” to get real benefits. Start small: return `Result<T, Exception>` from a service. Use `Apply` in your view-model. Replace `null` with `Option<T>` where absence is
normal. Sprinkle in collection and status helpers. Enjoy clearer intent, simpler tests, and fewer surprises. 🚀

If this saved you a try/catch today, consider leaving a star. ⭐ If it saved you two, drop a dad joke in your codebase comments. Your future self will groan appreciatively. 😄

— Updated on 2025-12-02 at 20:03 local time
