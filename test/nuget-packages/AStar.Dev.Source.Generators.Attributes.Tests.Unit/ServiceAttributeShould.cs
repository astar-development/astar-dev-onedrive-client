namespace AStar.Dev.Source.Generators.Attributes.Tests.Unit;

public class ServiceAttributeShould
{
    [Fact]
    public void HaveDefaultLifetimeAsScoped()
        => new ServiceAttribute().Lifetime.ShouldBe(ServiceLifetime.Scoped);

    [Fact]
    public void AllowSettingLifetimeViaConstructor()
        => new ServiceAttribute(ServiceLifetime.Singleton).Lifetime.ShouldBe(ServiceLifetime.Singleton);

    [Fact]
    public void AllowSettingAsProperty()
        => new ServiceAttribute { As = typeof(string) }.As.ShouldBe(typeof(string));

    [Fact]
    public void AllowSettingAsSelfProperty()
        => new ServiceAttribute { AsSelf = true }.AsSelf.ShouldBeTrue();

    [Fact]
    public void BeApplicableToClassesOnly()
        => typeof(ServiceAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().ValidOn.ShouldBe(AttributeTargets.Class);

    [Fact]
    public void NotBeInherited()
        => typeof(ServiceAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().Inherited.ShouldBeFalse();

    [Fact]
    public void NotAllowMultipleUsageOnSameClass()
        => typeof(ServiceAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .OfType<AttributeUsageAttribute>().Single().AllowMultiple.ShouldBeFalse();
}
