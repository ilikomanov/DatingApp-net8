using System.Collections.Generic;
using System.Linq;
using Moq;
using MockQueryable.Moq;
using Xunit;

public class BuildMockTest
{
    [Fact]
    public void BuildMock_Works()
    {
        // Arrange
        var list = new List<string> { "one", "two", "three" };
        var queryable = list.AsQueryable();
        var mock = queryable.BuildMock(); // ✅ This returns IQueryable<string>

        // Act
        var result = mock.First(); // ✅ Use it directly — no '.Object'

        // Assert
        Assert.Equal("one", result);
    }
}
