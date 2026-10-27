using Backend.Infrastructure.Conversions;
using Backend.Infrastructure.Enums;

namespace Backend.UnitTests.Infrastructure;

public sealed class JsonConversionsTests
{
    [Fact]
    public void ToJson_WithObject_ShouldUseCamelCaseAndEnumAsText()
    {
        var value = new SampleValue("Valor de teste", SortDirection.Ascending);

        var json = value.ToJson();

        Assert.Equal("""{"name":"Valor de teste","direction":"Ascending"}""", json);
    }

    [Fact]
    public void FromJson_WithJson_ShouldDeserializeIgnoringCase()
    {
        var value = """{"NAME":"Valor de teste","direction":"Descending"}""".FromJson<SampleValue>();

        Assert.Equal(new SampleValue("Valor de teste", SortDirection.Descending), value);
    }

    private sealed record SampleValue(string Name, SortDirection Direction);
}
