using System;
using System.Collections.Generic;
using AStar.Dev.OneDrive.Client.Services;
using Xunit;
using Shouldly;

public class SyncErrorLoggerShould
{
    [Fact]
    public void LogError_StoresErrorWithPath()
    {
        var logger = new SyncErrorLogger();
        var ex = new InvalidOperationException("fail");
        logger.LogError(ex, "file.txt");
        var errors = logger.GetErrors();
        errors.Count.ShouldBe(1);
        errors[0].Exception.ShouldBe(ex);
        errors[0].Path.ShouldBe("file.txt");
    }

    [Fact]
    public void GetErrors_ReturnsEmptyListWhenNoErrors()
    {
        var logger = new SyncErrorLogger();
        var errors = logger.GetErrors();
        errors.ShouldBeEmpty();
    }
}
