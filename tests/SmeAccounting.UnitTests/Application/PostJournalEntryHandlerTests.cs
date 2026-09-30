using FluentAssertions;
using NSubstitute;
using SmeAccounting.Application.Features.JournalEntries.Commands;
using SmeAccounting.Application.Interfaces;

namespace SmeAccounting.UnitTests.Application;

public sealed class PostJournalEntryHandlerTests
{
    private static PostJournalEntryCommand BalancedCommand() => new(
        Guid.NewGuid(),
        "CT-2026-0001",
        new DateOnly(2026, 1, 15),
        "VND",
        [
            new PostJournalEntryLine("111", 1_000_000m, 0m, null),
            new PostJournalEntryLine("511", 0m, 1_000_000m, null)
        ]);

    private static PostJournalEntryHandler Handler(
        out IJournalEntryRepository repo,
        out IUnitOfWork uow,
        bool periodClosed = false)
    {
        repo = Substitute.For<IJournalEntryRepository>();
        uow = Substitute.For<IUnitOfWork>();
        var periods = Substitute.For<IAccountingPeriodChecker>();
        periods.IsClosedAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(periodClosed);
        return new PostJournalEntryHandler(repo, uow, periods);
    }

    [Fact]
    public async Task Post_When_Unbalanced_Then_ReturnsFailure()
    {
        var cmd = BalancedCommand() with
        {
            Lines = [
                new PostJournalEntryLine("111", 1_000_000m, 0m, null),
                new PostJournalEntryLine("511", 0m, 900_000m, null)
            ]
        };
        var handler = Handler(out _, out _);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("GL.UNBALANCED");
    }

    [Fact]
    public async Task Post_When_PeriodClosed_Then_ReturnsFailure()
    {
        var handler = Handler(out _, out _, periodClosed: true);

        var result = await handler.Handle(BalancedCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("ACCOUNT.CLOSED_PERIOD");
    }

    [Fact]
    public async Task Post_When_Balanced_Then_SuccessAndSavesOnce()
    {
        var handler = Handler(out _, out var uow);

        var result = await handler.Handle(BalancedCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be("Posted");
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
