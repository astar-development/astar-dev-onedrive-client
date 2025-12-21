namespace AStar.Dev.Source.Generators.Sample;

/// <summary>
/// Example StrongId types demonstrating the generator.
/// </summary>
/// 
// ✅ Valid: String-based ID
[StrongId(typeof(string))]
public readonly partial record struct UserId;

// ✅ Valid: Integer-based ID
[StrongId(typeof(int))]
public readonly partial record struct OrderNumber;

// ✅ Valid: GUID-based ID
[StrongId(typeof(System.Guid))]
public readonly partial record struct EntityGuid;

// ❌ Invalid: Missing 'partial' keyword
// Uncommenting this will cause STRONGID001 error
// [StrongId(typeof(string))]
// public readonly record struct InvalidId1;

// ❌ Invalid: Not a struct (it's a class)
// Uncommenting this will cause STRONGID002 error
// [StrongId(typeof(string))]
// public partial record InvalidId2;

/// <summary>
/// Examples demonstrating usage of generated StrongIds.
/// </summary>
public static class StrongIdExamples
{
    public static void DemonstrateUsage()
    {
        // Creating instances
        var userId = new UserId("user-12345");
        var orderNumber = new OrderNumber(1001);
        var entityGuid = new EntityGuid(System.Guid.NewGuid());

        // Accessing underlying values
        _ = userId.Value;

        _ = orderNumber.Value;

        _ = entityGuid.Value;

        // Implicit conversion to underlying type
        _ = userId;

        _ = orderNumber;

        _ = entityGuid;

        // String representation
        _ = userId.ToString();

        _ = orderNumber.ToString();

        _ = entityGuid.ToString();
    }

    public static void DemonstrateValidation()
    {
        try
        {
            // ❌ Throws ArgumentException: UserId cannot be null or empty
            var invalid1 = new UserId("");
        }
        catch(System.ArgumentException)
        {
            // Validation caught the error
        }

        try
        {
            // ❌ Throws ArgumentException: OrderNumber must be greater than zero
            var invalid2 = new OrderNumber(0);
        }
        catch(System.ArgumentException)
        {
            // Validation caught the error
        }

        try
        {
            // ❌ Throws ArgumentException: EntityGuid cannot be an empty GUID
            var invalid3 = new EntityGuid(System.Guid.Empty);
        }
        catch(System.ArgumentException)
        {
            // Validation caught the error
        }
    }

    /// <summary>
    /// Demonstrates type safety - these won't compile if uncommented.
    /// </summary>
    public static void DemonstrateTypeSafety()
    {
        var userId = new UserId("user-123");
        var orderNumber = new OrderNumber(42);

        // ❌ Won't compile: Cannot convert UserId to OrderNumber
        // OrderNumber invalid = userId;

        // ❌ Won't compile: Cannot compare different types
        // bool areEqual = userId == orderNumber;

        // ✅ Type-safe method calls
        ProcessUser(userId);
        ProcessOrder(orderNumber);

        // ❌ Won't compile: Wrong parameter type
        // ProcessUser(orderNumber);
    }

    private static void ProcessUser(UserId id) { }
    private static void ProcessOrder(OrderNumber number) { }
}
