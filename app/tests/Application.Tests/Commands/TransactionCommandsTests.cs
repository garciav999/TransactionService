using Application.Commands;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Events;
using FluentAssertions;
using Moq;
using Xunit;

namespace Application.Tests.Commands;

public class TransactionCommandsTests
{
    private readonly Mock<ITransactionRepository> _mockRepository;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly TransactionCommands _sut;

    public TransactionCommandsTests()
    {
        _mockRepository = new Mock<ITransactionRepository>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _sut = new TransactionCommands(_mockRepository.Object, _mockEventPublisher.Object);
    }

    [Fact]
    public async Task InsertAsync_WithValidData_ShouldCreateTransactionAndPublishEvent()
    {
        // Arrange
        var sourceAccountId = Guid.NewGuid();
        var targetAccountId = Guid.NewGuid();
        var transferTypeId = 1;
        var value = 100.50m;

        _mockRepository.Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .Returns(Task.CompletedTask);

        _mockEventPublisher.Setup(x => x.PublishAsync(It.IsAny<TransactionCreatedEvent>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.InsertAsync(sourceAccountId, targetAccountId, transferTypeId, value);

        // Assert
        result.Should().NotBe(Guid.Empty);
        
        _mockRepository.Verify(x => x.AddAsync(It.Is<Transaction>(t =>
            t.SourceAccountId == sourceAccountId &&
            t.TargetAccountId == targetAccountId &&
            t.TransferTypeId == transferTypeId &&
            t.Value == value &&
            t.Status == TransactionStatus.Pending
        )), Times.Once);

        _mockEventPublisher.Verify(x => x.PublishAsync(
            It.Is<TransactionCreatedEvent>(e =>
                e.TransactionExternalId == result &&
                e.SourceAccountId == sourceAccountId &&
                e.TargetAccountId == targetAccountId &&
                e.TransferTypeId == transferTypeId &&
                e.Value == value
            ), default), Times.Once);
    }

    [Fact]
    public async Task InsertAsync_WithCustomStatus_ShouldCreateTransactionWithSpecifiedStatus()
    {
        // Arrange
        var sourceAccountId = Guid.NewGuid();
        var targetAccountId = Guid.NewGuid();
        var transferTypeId = 1;
        var value = 100.50m;
        var status = TransactionStatus.Approved;

        _mockRepository.Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .Returns(Task.CompletedTask);

        _mockEventPublisher.Setup(x => x.PublishAsync(It.IsAny<TransactionCreatedEvent>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.InsertAsync(sourceAccountId, targetAccountId, transferTypeId, value, status);

        // Assert
        _mockRepository.Verify(x => x.AddAsync(It.Is<Transaction>(t =>
            t.Status == TransactionStatus.Approved
        )), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public async Task InsertAsync_WithInvalidValue_ShouldThrowArgumentException(decimal invalidValue)
    {
        // Arrange
        var sourceAccountId = Guid.NewGuid();
        var targetAccountId = Guid.NewGuid();
        var transferTypeId = 1;

        // Act
        var act = async () => await _sut.InsertAsync(sourceAccountId, targetAccountId, transferTypeId, invalidValue);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Value must be greater than zero.*")
            .WithParameterName("value");
    }

    [Fact]
    public async Task InsertAsync_WithEmptySourceAccountId_ShouldThrowArgumentException()
    {
        // Arrange
        var sourceAccountId = Guid.Empty;
        var targetAccountId = Guid.NewGuid();
        var transferTypeId = 1;
        var value = 100.50m;

        // Act
        var act = async () => await _sut.InsertAsync(sourceAccountId, targetAccountId, transferTypeId, value);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid sourceAccountId.*")
            .WithParameterName("sourceAccountId");
    }

    [Fact]
    public async Task InsertAsync_WithEmptyTargetAccountId_ShouldThrowArgumentException()
    {
        // Arrange
        var sourceAccountId = Guid.NewGuid();
        var targetAccountId = Guid.Empty;
        var transferTypeId = 1;
        var value = 100.50m;

        // Act
        var act = async () => await _sut.InsertAsync(sourceAccountId, targetAccountId, transferTypeId, value);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid targetAccountId.*")
            .WithParameterName("targetAccountId");
    }

    [Fact]
    public async Task InsertAsync_ShouldGenerateUniqueExternalId()
    {
        // Arrange
        var sourceAccountId = Guid.NewGuid();
        var targetAccountId = Guid.NewGuid();
        var transferTypeId = 1;
        var value = 100.50m;

        _mockRepository.Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .Returns(Task.CompletedTask);

        _mockEventPublisher.Setup(x => x.PublishAsync(It.IsAny<TransactionCreatedEvent>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var result1 = await _sut.InsertAsync(sourceAccountId, targetAccountId, transferTypeId, value);
        var result2 = await _sut.InsertAsync(sourceAccountId, targetAccountId, transferTypeId, value);

        // Assert
        result1.Should().NotBe(result2);
        result1.Should().NotBe(Guid.Empty);
        result2.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task InsertAsync_WhenRepositoryFails_ShouldPropagateException()
    {
        // Arrange
        var sourceAccountId = Guid.NewGuid();
        var targetAccountId = Guid.NewGuid();
        var transferTypeId = 1;
        var value = 100.50m;

        _mockRepository.Setup(x => x.AddAsync(It.IsAny<Transaction>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var act = async () => await _sut.InsertAsync(sourceAccountId, targetAccountId, transferTypeId, value);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");

        _mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<TransactionCreatedEvent>(), default), Times.Never);
    }
}
