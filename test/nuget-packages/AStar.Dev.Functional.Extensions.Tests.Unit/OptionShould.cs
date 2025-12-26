namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class OptionShould
{
    private static readonly int[] ExpectedArrayOfInts = [1, 2, 3];
    private static readonly int[] ExpectedArrayOfInts2 = [20, 40];

    [Fact]
    public void MatchToSomeHandlerWhenOptionIsSome()
    {
        var option = Option.Some(42);

        var matched = option.Match(
            some => $"Some: {some}",
            () => "None");

        matched.ShouldBe("Some: 42");
    }

    [Fact]
    public void MatchToNoneHandlerWhenOptionIsNone()
    {
        var option = Option.None<int>();

        var matched = option.Match(
            some => $"Some: {some}",
            () => "None");

        matched.ShouldBe("None");
    }

    [Fact]
    public async Task MatchAsyncToSomeAsyncHandlerWhenOptionIsSomeAsync()
    {
        var option = Option.Some(42);

        var matched = await option.MatchAsync(
            some => Task.FromResult($"Some: {some}"),
            () => "None");

        matched.ShouldBe("Some: 42");
    }

    [Fact]
    public async Task MatchAsyncToNoneHandlerWhenOptionIsNoneAsync()
    {
        var option = Option.None<int>();

        var matched = await option.MatchAsync(
            some => Task.FromResult($"Some: {some}"),
            () => "None");

        matched.ShouldBe("None");
    }

    [Fact]
    public async Task MatchAsyncToSomeHandlerAndAsyncNoneHandlerWhenOptionIsSomeAsync()
    {
        var option = Option.Some(42);

        var matched = await option.MatchAsync(
            some => $"Some: {some}",
            () => Task.FromResult("None"));

        matched.ShouldBe("Some: 42");
    }

    [Fact]
    public async Task MatchAsyncToSomeHandlerAndAsyncNoneHandlerWhenOptionIsNoneAsync()
    {
        var option = Option.None<int>();

        var matched = await option.MatchAsync(
            some => $"Some: {some}",
            () => Task.FromResult("None"));

        matched.ShouldBe("None");
    }

    [Fact]
    public async Task MatchAsyncToAsyncSomeHandlerAndAsyncNoneHandlerWhenOptionIsSomeAsync()
    {
        var option = Option.Some(42);

        var matched = await option.MatchAsync(
            some => Task.FromResult($"Some: {some}"),
            () => Task.FromResult("None"));

        matched.ShouldBe("Some: 42");
    }

    [Fact]
    public async Task MatchAsyncToAsyncSomeHandlerAndAsyncNoneHandlerWhenOptionIsNoneAsync()
    {
        var option = Option.None<int>();

        var matched = await option.MatchAsync(
            some => Task.FromResult($"Some: {some}"),
            () => Task.FromResult("None"));

        matched.ShouldBe("None");
    }

    [Fact]
    public void CreateSomeOptionWithCorrectValue()
    {
        var value = 42;

        var option = Option.Some(value);

        ((Option<int>.Some)option).Value.ShouldBe(value);
    }

    [Fact]
    public void ThrowWhenCreatingSomeWithNullValue() => Should.Throw<ArgumentNullException>(() => Option.Some<string>(null!));

    [Fact]
    public void ReturnTrueForIsSomeWhenOptionIsSome()
    {
        var option = Option.Some(42);

        option.IsSome().ShouldBeTrue();
    }

    [Fact]
    public void ReturnFalseForIsSomeWhenOptionIsNone()
    {
        var option = Option.None<int>();

        option.IsSome().ShouldBeFalse();
    }

    [Fact]
    public void ReturnTrueForIsNoneWhenOptionIsNone()
    {
        var option = Option.None<int>();

        option.IsNone().ShouldBeTrue();
    }

    [Fact]
    public void ReturnFalseForIsNoneWhenOptionIsSome()
    {
        var option = Option.Some(42);

        option.IsNone().ShouldBeFalse();
    }

    [Fact]
    public void ConvertValueToSomeImplicitly()
    {
        var value = "test";

        Option<string> option = value;

        _ = option.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)option).Value.ShouldBe("test");
    }

    [Fact]
    public void ConvertNullToNoneImplicitly()
    {
        string? value = null;

        Option<string> option = value!;

        _ = option.ShouldBeOfType<Option<string>.None>();
    }

    [Fact]
    public void ExtractValueWithTryGetValueWhenOptionIsSome()
    {
        var option = Option.Some(42);

        var success = option.TryGetValue(out var value);

        success.ShouldBeTrue();
        value.ShouldBe(42);
    }

    [Fact]
    public void ReturnFalseAndDefaultWithTryGetValueWhenOptionIsNone()
    {
        var option = Option.None<int>();

        var success = option.TryGetValue(out var value);

        success.ShouldBeFalse();
        value.ShouldBe(default);
    }

    [Fact]
    public void ConvertValueToSomeWithToOption()
    {
        var value = "test";

        var option = value.ToOption();

        _ = option.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)option).Value.ShouldBe("test");
    }

    [Fact]
    public void ConvertNullToNoneWithToOption()
    {
        string? value = null;

        var option = value!.ToOption();

        _ = option.ShouldBeOfType<Option<string>.None>();
    }

    [Fact]
    public void ConvertValueToSomeWithToOptionWhenPredicateIsTrue()
    {
        var value = 42;

        var option = value.ToOption(x => x > 0);

        _ = option.ShouldBeOfType<Option<int>.Some>();
        ((Option<int>.Some)option).Value.ShouldBe(42);
    }

    [Fact]
    public void ConvertValueToNoneWithToOptionWhenPredicateIsFalse()
    {
        var value = 42;

        var option = value.ToOption(x => x < 0);

        _ = option.ShouldBeOfType<Option<int>.None>();
    }

    [Fact]
    public void ConvertNullableToSomeWhenHasValue()
    {
        int? value = 42;

        var option = value.ToOption();

        _ = option.ShouldBeOfType<Option<int>.Some>();
        ((Option<int>.Some)option).Value.ShouldBe(42);
    }

    [Fact]
    public void ConvertNullableToNoneWhenNull()
    {
        int? value = null;

        var option = value.ToOption();

        _ = option.ShouldBeOfType<Option<int>.None>();
    }

    [Fact]
    public void MapValueToNewValueWhenOptionIsSome()
    {
        var option = Option.Some(42);

        Option<string> result = option.Map(x => x.ToString());

        _ = result.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)result).Value.ShouldBe("42");
    }

    [Fact]
    public void ReturnNoneWhenMappingNoneOption()
    {
        var option = Option.None<int>();

        Option<string> result = option.Map(x => x.ToString());

        _ = result.ShouldBeOfType<Option<string>.None>();
    }

    [Fact]
    public void BindValueToNewOptionWhenOptionIsSome()
    {
        var option = Option.Some(42);

        Option<string> result = option.Bind(x => Option.Some(x.ToString()));

        _ = result.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)result).Value.ShouldBe("42");
    }

    [Fact]
    public void ReturnNoneWhenBindingNoneOption()
    {
        var option = Option.None<int>();

        Option<string> result = option.Bind(x => Option.Some(x.ToString()));

        _ = result.ShouldBeOfType<Option<string>.None>();
    }

    [Fact]
    public void ReturnNoneWhenBindFunctionReturnsNone()
    {
        var option = Option.Some(42);

        Option<string> result = option.Bind(_ => Option.None<string>());

        _ = result.ShouldBeOfType<Option<string>.None>();
    }

    [Fact]
    public void ConvertSomeToOkResult()
    {
        var option = Option.Some(42);

        var result = option.ToResult(() => "Error");

        _ = result.ShouldBeOfType<Result<int, string>.Ok>();
        ((Result<int, string>.Ok)result).Value.ShouldBe(42);
    }

    [Fact]
    public void ConvertNoneToErrorResult()
    {
        var option = Option.None<int>();

        var result = option.ToResult(() => "Error Message");

        _ = result.ShouldBeOfType<Result<int, string>.Error>();
        ((Result<int, string>.Error)result).Reason.ShouldBe("Error Message");
    }

    [Fact]
    public void ConvertSomeToNullableValue()
    {
        var option = Option.Some(42);

        var result = option.ToNullable();

        _ = result.ShouldNotBeNull();
        result.ShouldBe(42);
    }

    [Fact]
    public void ConvertNoneToNullNullable()
    {
        var option = Option.None<int>();

        var result = option.ToNullable();

        result.ShouldBeNull();
    }

    [Fact]
    public void ConvertSomeToSingleItemEnumerable()
    {
        var option = Option.Some(42);

        var result = option.ToEnumerable().ToList();

        result.Count.ShouldBe(1);
        result[0].ShouldBe(42);
    }

    [Fact]
    public void ConvertNoneToEmptyEnumerable()
    {
        var option = Option.None<int>();

        var result = option.ToEnumerable().ToList();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void ReturnValueWithOrElseWhenOptionIsSome()
    {
        var option = Option.Some(42);

        var result = option.OrElse(-1);

        result.ShouldBe(42);
    }

    [Fact]
    public void ReturnFallbackWithOrElseWhenOptionIsNone()
    {
        var option = Option.None<int>();

        var result = option.OrElse(-1);

        result.ShouldBe(-1);
    }

    [Fact]
    public void ReturnValueWithOrThrowWhenOptionIsSome()
    {
        var option = Option.Some(42);

        var result = option.OrThrow();

        result.ShouldBe(42);
    }

    [Fact]
    public void ThrowDefaultExceptionWithOrThrowWhenOptionIsNone()
    {
        var option = Option.None<int>();

        InvalidOperationException ex = Should.Throw<InvalidOperationException>(() => option.OrThrow());
        ex.Message.ShouldBe("No value present");
    }

    [Fact]
    public void ThrowCustomExceptionWithOrThrowWhenOptionIsNone()
    {
        var option = Option.None<int>();
        var customEx = new ArgumentException("Custom message");

        ArgumentException ex = Should.Throw<ArgumentException>(() => option.OrThrow(customEx));
        ex.Message.ShouldBe("Custom message");
    }

    [Fact]
    public void DeconstructCorrectlyWhenOptionIsSome()
    {
        var option = Option.Some(42);

        (var isSome, var value) = option;

        isSome.ShouldBeTrue();
        value.ShouldBe(42);
    }

    [Fact]
    public void DeconstructCorrectlyWhenOptionIsNone()
    {
        var option = Option.None<int>();

        (var isSome, var value) = option;

        isSome.ShouldBeFalse();
        value.ShouldBe(default);
    }

    [Fact]
    public void HaveSameHashCodeForEqualSomeValues()
    {
        var option1 = Option.Some(42);
        var option2 = Option.Some(42);

        option1.GetHashCode().ShouldBe(option2.GetHashCode());
    }

    [Fact]
    public void HaveDifferentHashCodeForDifferentSomeValues()
    {
        var option1 = Option.Some(42);
        var option2 = Option.Some(43);

        option1.GetHashCode().ShouldNotBe(option2.GetHashCode());
    }

    [Fact]
    public void HaveSameHashCodeForNone()
    {
        var option1 = Option.None<int>();
        var option2 = Option.None<int>();

        option1.GetHashCode().ShouldBe(option2.GetHashCode());
    }

    [Fact]
    public void BeEqualForSameValues()
    {
        var option1 = Option.Some(42);
        var option2 = Option.Some(42);

        option1.ShouldBe(option2);
        (option1 == option2).ShouldBeTrue();
        (option1 != option2).ShouldBeFalse();
    }

    [Fact]
    public void NotBeEqualForDifferentValues()
    {
        var option1 = Option.Some(42);
        var option2 = Option.Some(43);

        option1.ShouldNotBe(option2);
        (option1 == option2).ShouldBeFalse();
        (option1 != option2).ShouldBeTrue();
    }

    [Fact]
    public void BeEqualForNone()
    {
        var option1 = Option.None<int>();
        var option2 = Option.None<int>();

        option1.ShouldBe(option2);
        (option1 == option2).ShouldBeTrue();
        (option1 != option2).ShouldBeFalse();
    }

    [Fact]
    public void NotBeEqualForSomeAndNone()
    {
        var option1 = Option.Some(42);
        var option2 = Option.None<int>();

        option1.ShouldNotBe(option2);
        (option1 == option2).ShouldBeFalse();
        (option1 != option2).ShouldBeTrue();
    }

    [Fact]
    public void HaveCorrectStringRepresentationForSome()
    {
        var option = Option.Some(42);

        var result = option.ToString();

        result.ShouldBe("Some(42)");
    }

    [Fact]
    public void HaveCorrectStringRepresentationForNone()
    {
        var option = Option.None<int>();

        var result = option.ToString();

        result.ShouldBe("None");
    }

    [Fact]
    public void ExecuteSideEffectWithTapWhenOptionIsSome()
    {
        var option = Option.Some(42);
        var sideEffectExecuted = false;
        var capturedValue = 0;

        Option<int> result = option.Tap(x =>
        {
            sideEffectExecuted = true;
            capturedValue = x;
        });

        sideEffectExecuted.ShouldBeTrue();
        capturedValue.ShouldBe(42);
        result.ShouldBeSameAs(option);
    }

    [Fact]
    public void NotExecuteSideEffectWithTapWhenOptionIsNone()
    {
        var option = Option.None<int>();
        var sideEffectExecuted = false;

        Option<int> result = option.Tap(_ => sideEffectExecuted = true);

        sideEffectExecuted.ShouldBeFalse();
        result.ShouldBeSameAs(option);
    }

    [Fact]
    public void FilterToSameOptionWhenPredicateIsTrue()
    {
        var option = Option.Some(42);

        Option<int> result = option.Filter(x => x > 0);

        _ = result.ShouldBeOfType<Option<int>.Some>();
        ((Option<int>.Some)result).Value.ShouldBe(42);
    }

    [Fact]
    public void FilterToNoneWhenPredicateIsFalse()
    {
        var option = Option.Some(42);

        Option<int> result = option.Filter(x => x < 0);

        _ = result.ShouldBeOfType<Option<int>.None>();
    }

    [Fact]
    public void MapToTransformedValueWithMapOrDefaultWhenOptionIsSome()
    {
        var option = Option.Some(42);

        var result = option.MapOrDefault(x => x * 2, -1);

        result.ShouldBe(84);
    }

    [Fact]
    public void ReturnDefaultValueWithMapOrDefaultWhenOptionIsNone()
    {
        var option = Option.None<int>();

        var result = option.MapOrDefault(x => x * 2, -1);

        result.ShouldBe(-1);
    }

    [Fact]
    public void MapToTransformedValueWithMapOrElseWhenOptionIsSome()
    {
        var option = Option.Some(42);
        var factoryCalled = false;

        var result = option.MapOrElse(
            x => x * 2,
            () =>
            {
                factoryCalled = true;

                return -1;
            }
        );

        result.ShouldBe(84);
        factoryCalled.ShouldBeFalse();
    }

    [Fact]
    public void UseFactoryWithMapOrElseWhenOptionIsNone()
    {
        var option = Option.None<int>();
        var factoryCalled = false;

        var result = option.MapOrElse(
            x => x * 2,
            () =>
            {
                factoryCalled = true;

                return -1;
            }
        );

        result.ShouldBe(-1);
        factoryCalled.ShouldBeTrue();
    }

    [Fact]
    public void ExtractValuesFromCollectionOfOptions()
    {
        Option<int>[] options = [Option.Some(1), Option.None<int>(), Option.Some(2), Option.None<int>(), Option.Some(3)];

        var result = options.Values().ToList();

        result.Count.ShouldBe(3);
        result.ShouldBe(ExpectedArrayOfInts);
    }

    [Fact]
    public void FilterCollectionWithChoosePredicate()
    {
        var numbers = new[] { 1, 2, 3, 4, 5 };

        var result = numbers.Choose(x => x % 2 == 0).ToList();

        result.Count.ShouldBe(2);
        result.All(option => option is Option<int>.Some).ShouldBeTrue();
        ((Option<int>.Some)result[0]).Value.ShouldBe(2);
        ((Option<int>.Some)result[1]).Value.ShouldBe(4);
    }

    [Fact]
    public void TransformCollectionWithChooser()
    {
        var numbers = new[] { 1, 2, 3, 4, 5 };

        var result = numbers.Choose(x => x % 2 == 0
            ? Option.Some(x * 10)
            : Option.None<int>()).ToList();

        result.Count.ShouldBe(2);
        result.ShouldBe(ExpectedArrayOfInts2);
    }

    [Fact]
    public async Task MapAsyncWithSomeOptionAsync()
    {
        var option = Option.Some(42);

        Option<string> result = await option.MapAsync(x => Task.FromResult(x.ToString()));

        _ = result.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)result).Value.ShouldBe("42");
    }

    [Fact]
    public async Task MapAsyncWithNoneOptionAsync()
    {
        var option = Option.None<int>();

        Option<string> result = await option.MapAsync(x => Task.FromResult(x.ToString()));

        _ = result.ShouldBeOfType<Option<string>.None>();
    }

    [Fact]
    public async Task MapAsyncWithTaskOptionAsync()
    {
        Task<Option<int>> optionTask = Task.FromResult(Option.Some(42));

        Option<string> result = await optionTask.MapAsync(x => x.ToString());

        _ = result.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)result).Value.ShouldBe("42");
    }

    [Fact]
    public async Task MapAsyncWithTaskOptionAndAsyncMapperAsync()
    {
        Task<Option<int>> optionTask = Task.FromResult(Option.Some(42));

        Option<string> result = await optionTask.MapAsync(x => Task.FromResult(x.ToString()));

        _ = result.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)result).Value.ShouldBe("42");
    }

    [Fact]
    public async Task BindAsyncWithSomeOptionAsync()
    {
        var option = Option.Some(42);

        Option<string> result = await option.BindAsync(x => Task.FromResult(Option.Some(x.ToString())));

        _ = result.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)result).Value.ShouldBe("42");
    }

    [Fact]
    public async Task BindAsyncWithNoneOptionAsync()
    {
        var option = Option.None<int>();

        Option<string> result = await option.BindAsync(x => Task.FromResult(Option.Some(x.ToString())));

        _ = result.ShouldBeOfType<Option<string>.None>();
    }

    [Fact]
    public async Task ToResultAsyncWithSomeOptionAsync()
    {
        var option = Option.Some(42);

        Result<int, string> result = await option.ToResultAsync(() => Task.FromResult("Error"));

        _ = result.ShouldBeOfType<Result<int, string>.Ok>();
        ((Result<int, string>.Ok)result).Value.ShouldBe(42);
    }

    [Fact]
    public async Task ToResultAsyncWithNoneOptionAsync()
    {
        var option = Option.None<int>();

        Result<int, string> result = await option.ToResultAsync(() => Task.FromResult("Error"));

        _ = result.ShouldBeOfType<Result<int, string>.Error>();
        ((Result<int, string>.Error)result).Reason.ShouldBe("Error");
    }

    [Fact]
    public async Task TapAsyncWithSomeOptionAsync()
    {
        var option = Option.Some(42);
        var sideEffectExecuted = false;
        var capturedValue = 0;

        Option<int> result = await option.TapAsync(x =>
        {
            sideEffectExecuted = true;
            capturedValue = x;

            return Task.CompletedTask;
        });

        sideEffectExecuted.ShouldBeTrue();
        capturedValue.ShouldBe(42);
        result.ShouldBe(option);
    }

    [Fact]
    public async Task TapAsyncWithNoneOptionAsync()
    {
        var option = Option.None<int>();
        var sideEffectExecuted = false;

        Option<int> result = await option.TapAsync(_ =>
        {
            sideEffectExecuted = true;

            return Task.CompletedTask;
        });

        sideEffectExecuted.ShouldBeFalse();
        result.ShouldBe(option);
    }

    [Fact]
    public async Task OrElseAsyncWithSomeOptionAsync()
    {
        var option = Option.Some(42);
        var fallbackCalled = false;

        var result = await option.OrElseAsync(() =>
        {
            fallbackCalled = true;

            return Task.FromResult(-1);
        });

        result.ShouldBe(42);
        fallbackCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task OrElseAsyncWithNoneOptionAsync()
    {
        var option = Option.None<int>();
        var fallbackCalled = false;

        var result = await option.OrElseAsync(() =>
        {
            fallbackCalled = true;

            return Task.FromResult(-1);
        });

        result.ShouldBe(-1);
        fallbackCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task BindAsyncWithTaskOptionAndSyncBinderAsync()
    {
        Task<Option<int>> optionTask = Task.FromResult(Option.Some(42));

        Option<string> result = await optionTask.BindAsync(x => Option.Some(x.ToString()));

        _ = result.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)result).Value.ShouldBe("42");
    }

    [Fact]
    public async Task ToResultAsyncWithTaskOptionAndSyncErrorFactoryAsync()
    {
        Task<Option<int>> optionTask = Task.FromResult(Option.None<int>());

        Result<int, string> result = await optionTask.ToResultAsync(() => "Error");

        _ = result.ShouldBeOfType<Result<int, string>.Error>();
        ((Result<int, string>.Error)result).Reason.ShouldBe("Error");
    }

    [Fact]
    public async Task ToResultAsyncWithTaskOptionAndAsyncErrorFactoryAsync()
    {
        Task<Option<int>> optionTask = Task.FromResult(Option.None<int>());

        Result<int, string> result = await optionTask.ToResultAsync(() => Task.FromResult("Error"));

        _ = result.ShouldBeOfType<Result<int, string>.Error>();
        ((Result<int, string>.Error)result).Reason.ShouldBe("Error");
    }

    [Fact]
    public async Task TapAsyncWithTaskOptionAndSyncActionAsync()
    {
        Task<Option<int>> optionTask = Task.FromResult(Option.Some(42));
        var sideEffectExecuted = false;
        var capturedValue = 0;

        Option<int> result = await optionTask.TapAsync(x =>
        {
            sideEffectExecuted = true;
            capturedValue = x;
        });

        sideEffectExecuted.ShouldBeTrue();
        capturedValue.ShouldBe(42);
        _ = result.ShouldBeOfType<Option<int>.Some>();
        ((Option<int>.Some)result).Value.ShouldBe(42);
    }

    [Fact]
    public async Task TapAsyncWithTaskOptionAndAsyncActionAsync()
    {
        Task<Option<int>> optionTask = Task.FromResult(Option.Some(42));
        var sideEffectExecuted = false;
        var capturedValue = 0;

        Option<int> result = await optionTask.TapAsync(x =>
        {
            sideEffectExecuted = true;
            capturedValue = x;

            return Task.CompletedTask;
        });

        sideEffectExecuted.ShouldBeTrue();
        capturedValue.ShouldBe(42);
        _ = result.ShouldBeOfType<Option<int>.Some>();
        ((Option<int>.Some)result).Value.ShouldBe(42);
    }

    [Fact]
    public async Task OrElseAsyncWithTaskOptionAndSyncFallbackAsync()
    {
        Task<Option<int>> optionTask = Task.FromResult(Option.None<int>());

        var result = await optionTask.OrElseAsync(-1);

        result.ShouldBe(-1);
    }

    [Fact]
    public async Task OrElseAsyncWithTaskOptionAndAsyncFallbackAsync()
    {
        Task<Option<int>> optionTask = Task.FromResult(Option.None<int>());
        var fallbackCalled = false;

        var result = await optionTask.OrElseAsync(() =>
        {
            fallbackCalled = true;

            return Task.FromResult(-1);
        });

        result.ShouldBe(-1);
        fallbackCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ReturnSomeValueWithTaskOrElseAsyncWhenOptionIsSomeAsync()
    {
        Task<Option<int>> optionTask = Task.FromResult(Option.Some(42));
        var fallbackCalled = false;

        var result = await optionTask.OrElseAsync(() =>
        {
            fallbackCalled = true;

            return Task.FromResult(-1);
        });

        result.ShouldBe(42);
        fallbackCalled.ShouldBeFalse();
    }

    [Fact]
    public void ConvertValueToSomeWithToOptionWhenValueIsNotDefault()
    {
        var value = "test";

        var option = value.ToOption();

        _ = option.ShouldBeOfType<Option<string>.Some>();
        ((Option<string>.Some)option).Value.ShouldBe("test");
    }

    [Fact]
    public void ConvertDefaultValueToNoneWithToOption()
    {
        int value = default;

        var option = value.ToOption();

        _ = option.ShouldBeOfType<Option<int>.None>();
    }

    [Fact]
    public void GenerateCorrectToStringForSome()
    {
        var some = new Option<int>.Some(42);

        var result = some.ToString();

        result.ShouldBe("Some(42)");
    }

    [Fact]
    public void GenerateCorrectToStringForNone()
    {
        Option<int>.None none = Option<int>.None.Instance;

        var result = none.ToString();

        result.ShouldBe("None");
    }

    [Fact]
    public void CheckEqualityWithDifferentTypes()
    {
        var option = Option.Some(42);
        var notOption = "not an option";

        var result = option.Equals(notOption);

        result.ShouldBeFalse();
    }

    [Fact]
    public void FilterNoneToNone()
    {
        var option = Option.None<int>();

        Option<int> result = option.Filter(x => x > 0);

        _ = result.ShouldBeOfType<Option<int>.None>();
    }
}
