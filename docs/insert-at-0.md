User: Everything compiles and works correctly - thanks 

Question on the latest updates:
RecentTransfers.Insert(0, message);
Where
public ObservableCollection<string> RecentTransfers { get; } = [];

Whilst the list is restricted in length, is this the best way? Inserting at the beginning seems suboptimal from a performance point of view? 
Please do not change the code but can you explain why this is the best approach or offer other options?

GitHub Copilot: Great question! You're absolutely right to be concerned about performance. Let me explain the trade-offs:

## Current Approach: `Insert(0, message)`

**Pros:**
- Simple and readable
- Shows newest items first (natural for a "recent activity" list)
- For small collections (15 items), performance impact is negligible

**Cons:**
- O(n) operation - shifts all existing elements
- For each insert, the underlying array must move every element one position
- With frequent updates (e.g., every 100 files), this could be called thousands of times

## Alternative Options:

### Option 1: **Append + Reverse Display (Recommended)**
```csharp
// In ViewModel:
RecentTransfers.Add(message);  // O(1) - fast!
while(RecentTransfers.Count > MaxRecentTransfers)
{
    RecentTransfers.RemoveAt(0);  // Remove oldest
}

// In XAML/View:
// Bind with a reverse converter or use FlowDirection="RightToLeft" with reversal
```
**Trade-off:** Requires view-level handling to display newest-first.

### Option 2: **Circular Buffer Pattern**
Use a specialized collection like `Queue<string>` or `LinkedList<string>` internally, then expose as `ObservableCollection` when changed.

### Option 3: **Batched Updates**
Instead of inserting every progress update:
```csharp
private readonly List<string> _pendingTransfers = new();

private void AddRecentTransfer(string message)
{
    _pendingTransfers.Add(message);
    
    // Only update UI every N items or every X seconds
    if (_pendingTransfers.Count >= 10)
    {
        FlushPendingTransfers();
    }
}
```

### Option 4: **Deque (Double-Ended Queue)**
If .NET had a native `Deque`, you'd get O(1) insertions at both ends. You could use `LinkedList<T>` for similar benefits:
```csharp
private readonly LinkedList<string> _transfers = new();
// AddFirst() is O(1), no shifting needed
```

## My Recommendation:

**For your use case (15 items max, throttled updates), the current approach is fine.** Here's why:

1. **Scale**: 15 items × O(n) per insert = trivial cost on modern hardware
2. **Throttling**: You're already throttling to 500ms and every 100 files
3. **Simplicity**: The code is clear and maintainable
4. **UI Thread**: The real bottleneck is likely UI marshalling, not the Insert(0)

**When to optimize:**
- If `MaxRecentTransfers` grows to 100+
- If updates happen more frequently (no throttling)
- If profiling shows this as a hotspot

**Best alternative if you must optimize:** Option 3 (batched updates) gives you the most benefit with minimal code change - accumulate multiple messages and update the UI collection in one go.

Does this help clarify the trade-offs?