using FluentAssertions;
using SmeAccounting.Domain.Entities;
using SmeAccounting.Domain.Exceptions;
using SmeAccounting.Domain.ValueObjects;

namespace SmeAccounting.UnitTests.Domain;

public sealed class JournalEntryTests
{
    private static JournalEntry BalancedEntry() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        new VoucherNo("CT-2026-0001"),
        new DateOnly(2026, 1, 15),
        [
            new JournalLine(new AccountCode("111"), new Money(1_000_000m, "VND"), Money.Zero("VND")),
            new JournalLine(new AccountCode("511"), Money.Zero("VND"), new Money(1_000_000m, "VND"))
        ]);

    [Fact]
    public void Post_When_Balanced_Then_Posted()
    {
        var entry = BalancedEntry();

        entry.Post(isPeriodClosed: false);

        entry.Status.Should().Be(JournalEntryStatus.Posted);
        entry.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void Post_When_Unbalanced_Then_Throws()
    {
        var entry = new JournalEntry(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new VoucherNo("CT-2026-0002"),
            new DateOnly(2026, 1, 15),
            [
                new JournalLine(new AccountCode("111"), new Money(1_000_000m, "VND"), Money.Zero("VND")),
                new JournalLine(new AccountCode("511"), Money.Zero("VND"), new Money(900_000m, "VND"))
            ]);

        var act = () => entry.Post(isPeriodClosed: false);

        act.Should().Throw<UnbalancedEntryException>()
            .Where(e => e.ErrorCode == "GL.UNBALANCED");
    }

    [Fact]
    public void Post_When_PeriodClosed_Then_Throws()
    {
        var entry = BalancedEntry();

        var act = () => entry.Post(isPeriodClosed: true);

        act.Should().Throw<ClosedPeriodException>()
            .Where(e => e.ErrorCode == "ACCOUNT.CLOSED_PERIOD");
        entry.Status.Should().Be(JournalEntryStatus.Draft);
    }

    [Fact]
    public void Void_When_Posted_Then_Voided()
    {
        var entry = BalancedEntry();
        entry.Post(isPeriodClosed: false);

        entry.Void();

        entry.Status.Should().Be(JournalEntryStatus.Voided);
    }

    [Fact]
    public void Void_When_Draft_Then_Throws()
    {
        var entry = BalancedEntry();

        var act = () => entry.Void();

        act.Should().Throw<DomainException>();
    }
}
