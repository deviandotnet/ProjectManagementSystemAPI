using FluentAssertions;
using FluentValidation.TestHelper;
using PMS.Application.Features.UserFeatures.CreateUser;

namespace PMS.UnitTests.UserFeatures;

public class CreateUserValidatorTests
{
    private readonly CreateUserValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new CreateUserRequest(
            FirstName: "John",
            MiddleName: "Alexander",
            LastName: "Doe",
            Email: "john.doe@example.com",
            Password: "Password123!"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNullMiddleName_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new CreateUserRequest(
            FirstName: "Jane",
            MiddleName: null,
            LastName: "Smith",
            Email: "jane.smith@example.com",
            Password: "Password123!"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyFirstName_ShouldHaveValidationError(string? firstName)
    {
        // Arrange
        var request = new CreateUserRequest(
            FirstName: firstName!,
            MiddleName: "Middle",
            LastName: "Doe",
            Email: "test@example.com",
            Password: "Password123!"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
              .WithErrorMessage("First name is required.");
    }

    [Fact]
    public void Validate_WithFirstNameExceeding100Characters_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateUserRequest(
            FirstName: new string('A', 101),
            MiddleName: "Middle",
            LastName: "Doe",
            Email: "test@example.com",
            Password: "Password123!"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
              .WithErrorMessage("First name must not exceed 100 characters.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyLastName_ShouldHaveValidationError(string? lastName)
    {
        // Arrange
        var request = new CreateUserRequest(
            FirstName: "John",
            MiddleName: "Middle",
            LastName: lastName!,
            Email: "test@example.com",
            Password: "Password123!"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
              .WithErrorMessage("Last name is required.");
    }

    [Fact]
    public void Validate_WithLastNameExceeding100Characters_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateUserRequest(
            FirstName: "John",
            MiddleName: "Middle",
            LastName: new string('B', 101),
            Email: "test@example.com",
            Password: "Password123!"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
              .WithErrorMessage("Last name must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithMiddleNameExceeding100Characters_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateUserRequest(
            FirstName: "John",
            MiddleName: new string('C', 101),
            LastName: "Doe",
            Email: "test@example.com",
            Password: "Password123!"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MiddleName)
              .WithErrorMessage("Middle name must not exceed 100 characters.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyEmail_ShouldHaveValidationError(string? email)
    {
        // Arrange
        var request = new CreateUserRequest(
            FirstName: "John",
            MiddleName: null,
            LastName: "Doe",
            Email: email!,
            Password: "Password123!"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email address is required.");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("user@")]
    [InlineData("@domain.com")]
    [InlineData("plainaddress")]
    public void Validate_WithInvalidEmailFormat_ShouldHaveValidationError(string invalidEmail)
    {
        // Arrange
        var request = new CreateUserRequest(
            FirstName: "John",
            MiddleName: null,
            LastName: "Doe",
            Email: invalidEmail,
            Password: "Password123!"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("A valid email address is required.");
    }

    [Fact]
    public void Validate_WithEmailExceeding256Characters_ShouldHaveValidationError()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@test.com";
        var request = new CreateUserRequest(
            FirstName: "John",
            MiddleName: null,
            LastName: "Doe",
            Email: longEmail,
            Password: "Password123!"
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorMessage("Email address must not exceed 256 characters.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_WithEmptyPassword_ShouldHaveValidationError(string? password)
    {
        // Arrange
        var request = new CreateUserRequest(
            FirstName: "John",
            MiddleName: null,
            LastName: "Doe",
            Email: "john@example.com",
            Password: password!
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password is required.");
    }

    [Theory]
    [InlineData("1")]
    [InlineData("12345")]
    public void Validate_WithShortPassword_ShouldHaveValidationError(string shortPassword)
    {
        // Arrange
        var request = new CreateUserRequest(
            FirstName: "John",
            MiddleName: null,
            LastName: "Doe",
            Email: "john@example.com",
            Password: shortPassword
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must be at least 6 characters long.");
    }

    [Fact]
    public void Validate_WithPasswordExceeding100Characters_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateUserRequest(
            FirstName: "John",
            MiddleName: null,
            LastName: "Doe",
            Email: "john@example.com",
            Password: new string('P', 101)
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorMessage("Password must not exceed 100 characters.");
    }
}
