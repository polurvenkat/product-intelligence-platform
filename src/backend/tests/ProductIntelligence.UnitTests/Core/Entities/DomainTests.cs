using FluentAssertions;
using ProductIntelligence.Core.Entities;
using ProductIntelligence.Core.Enums;
using Xunit;

namespace ProductIntelligence.UnitTests.Core.Entities;

public class DomainTests
{
    [Fact]
    public void Domain_ShouldCreateWithValidParameters()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var name = "Customer Management";
        var description = "All customer-related features";

        // Act
        var domain = new Domain(organizationId, name, description);

        // Assert
        domain.Id.Should().NotBeEmpty();
        domain.OrganizationId.Should().Be(organizationId);
        domain.Name.Should().Be(name);
        domain.Description.Should().Be(description);
        domain.Path.Should().Be("customer_management");
        domain.IsRoot().Should().BeTrue();
        domain.GetLevel().Should().Be(1);
    }

    [Fact]
    public void Domain_ShouldCreateSubDomainWithCorrectPath()
    {
        // Arrange
        var organizationId = Guid.NewGuid();
        var parentDomain = new Domain(organizationId, "Customer Management");
        
        // Act
        var subDomain = new Domain(
            organizationId,
            "User Authentication",
            null,
            parentDomain.Id,
            parentDomain.Path);

        // Assert
        subDomain.Path.Should().Be("customer_management.user_authentication");
        subDomain.ParentDomainId.Should().Be(parentDomain.Id);
        subDomain.IsRoot().Should().BeFalse();
        subDomain.GetLevel().Should().Be(2);
    }

    [Fact]
    public void Domain_ShouldThrowWhenNameIsEmpty()
    {
        // Arrange
        var organizationId = Guid.NewGuid();

        // Act & Assert
        var act = () => new Domain(organizationId, "");
        act.Should().Throw<ArgumentException>();
    }
}
