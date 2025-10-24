using Application.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace Application.Tests.Services;

public class TransactionServiceTests
{
    private readonly Mock<ITransactionRepository> _mockRepository;
    private readonly TransactionService _sut;

    public TransactionServiceTests()
    {
        _mockRepository = new Mock<ITransactionRepository>();
        _sut = new TransactionService(_mockRepository.Object);
    }

    [Fact]
    public async Task UpdateTransactionStatusAsync_WithValidData_ShouldUpdateStatus()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var status = "Approved";
        var transaction = new Transaction(transactionId, Guid.NewGuid(), Guid.NewGuid(), 1, 100m);

        _mockRepository.Setup(x => x.GetByExternalIdAsync(transactionId))
            .ReturnsAsync(transaction);

        _mockRepository.Setup(x => x.UpdateStatusAsync(transactionId, TransactionStatus.Approved, null))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.UpdateTransactionStatusAsync(transactionId, status);

        // Assert
        _mockRepository.Verify(x => x.GetByExternalIdAsync(transactionId), Times.Once);
        _mockRepository.Verify(x => x.UpdateStatusAsync(transactionId, TransactionStatus.Approved, null), Times.Once);
    }

    [Fact]
    public async Task UpdateTransactionStatusAsync_WithReason_ShouldUpdateStatusWithReason()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var status = "Rejected";
        var reason = "Fraud detected";
        var transaction = new Transaction(transactionId, Guid.NewGuid(), Guid.NewGuid(), 1, 100m);

        _mockRepository.Setup(x => x.GetByExternalIdAsync(transactionId))
            .ReturnsAsync(transaction);

        _mockRepository.Setup(x => x.UpdateStatusAsync(transactionId, TransactionStatus.Rejected, reason))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.UpdateTransactionStatusAsync(transactionId, status, reason);

        // Assert
        _mockRepository.Verify(x => x.UpdateStatusAsync(transactionId, TransactionStatus.Rejected, reason), Times.Once);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("pending")]
    [InlineData("PENDING")]
    [InlineData("Approved")]
    [InlineData("approved")]
    [InlineData("APPROVED")]
    [InlineData("Rejected")]
    [InlineData("rejected")]
    [InlineData("REJECTED")]
    public async Task UpdateTransactionStatusAsync_WithDifferentCasing_ShouldHandleCaseInsensitively(string status)
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transaction = new Transaction(transactionId, Guid.NewGuid(), Guid.NewGuid(), 1, 100m);

        _mockRepository.Setup(x => x.GetByExternalIdAsync(transactionId))
            .ReturnsAsync(transaction);

        _mockRepository.Setup(x => x.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<TransactionStatus>(), null))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.UpdateTransactionStatusAsync(transactionId, status);

        // Assert
        _mockRepository.Verify(x => x.UpdateStatusAsync(
            transactionId, 
            It.IsAny<TransactionStatus>(), 
            null), Times.Once);
    }

    [Fact]
    public async Task UpdateTransactionStatusAsync_WhenTransactionNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var status = "Approved";

        _mockRepository.Setup(x => x.GetByExternalIdAsync(transactionId))
            .ReturnsAsync((Transaction?)null);

        // Act
        var act = async () => await _sut.UpdateTransactionStatusAsync(transactionId, status);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Transaction with external ID {transactionId} not found");

        _mockRepository.Verify(x => x.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<TransactionStatus>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("Invalid")]
    [InlineData("Unknown")]
    [InlineData("")]
    [InlineData("Processing")]
    [InlineData("Completed")]
    public async Task UpdateTransactionStatusAsync_WithInvalidStatus_ShouldThrowArgumentException(string invalidStatus)
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var transaction = new Transaction(transactionId, Guid.NewGuid(), Guid.NewGuid(), 1, 100m);

        _mockRepository.Setup(x => x.GetByExternalIdAsync(transactionId))
            .ReturnsAsync(transaction);

        // Act
        var act = async () => await _sut.UpdateTransactionStatusAsync(transactionId, invalidStatus);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Invalid transaction status: {invalidStatus}");

        _mockRepository.Verify(x => x.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<TransactionStatus>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateTransactionStatusAsync_WhenRepositoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var status = "Approved";
        var transaction = new Transaction(transactionId, Guid.NewGuid(), Guid.NewGuid(), 1, 100m);

        _mockRepository.Setup(x => x.GetByExternalIdAsync(transactionId))
            .ReturnsAsync(transaction);

        _mockRepository.Setup(x => x.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<TransactionStatus>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        var act = async () => await _sut.UpdateTransactionStatusAsync(transactionId, status);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }

    [Fact]
    public async Task UpdateTransactionStatusAsync_ToApproved_ShouldNotRequireReason()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var status = "Approved";
        var transaction = new Transaction(transactionId, Guid.NewGuid(), Guid.NewGuid(), 1, 100m);

        _mockRepository.Setup(x => x.GetByExternalIdAsync(transactionId))
            .ReturnsAsync(transaction);

        _mockRepository.Setup(x => x.UpdateStatusAsync(transactionId, TransactionStatus.Approved, null))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.UpdateTransactionStatusAsync(transactionId, status, null);

        // Assert
        _mockRepository.Verify(x => x.UpdateStatusAsync(transactionId, TransactionStatus.Approved, null), Times.Once);
    }

    [Fact]
    public async Task UpdateTransactionStatusAsync_ToRejected_ShouldAcceptReason()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var status = "Rejected";
        var reason = "Insufficient funds";
        var transaction = new Transaction(transactionId, Guid.NewGuid(), Guid.NewGuid(), 1, 100m);

        _mockRepository.Setup(x => x.GetByExternalIdAsync(transactionId))
            .ReturnsAsync(transaction);

        _mockRepository.Setup(x => x.UpdateStatusAsync(transactionId, TransactionStatus.Rejected, reason))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.UpdateTransactionStatusAsync(transactionId, status, reason);

        // Assert
        _mockRepository.Verify(x => x.UpdateStatusAsync(transactionId, TransactionStatus.Rejected, reason), Times.Once);
    }
}
