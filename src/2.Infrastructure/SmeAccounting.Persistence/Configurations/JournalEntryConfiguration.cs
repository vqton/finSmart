using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmeAccounting.Domain.Entities;
using SmeAccounting.Domain.ValueObjects;

namespace SmeAccounting.Persistence.Configurations;

public sealed class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("journal_entries", "gl");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.Property(x => x.VoucherNo)
            .HasColumnName("voucher_no")
            .HasMaxLength(30)
            .IsRequired()
            .HasConversion(v => v.Value, s => new VoucherNo(s));

        builder.Property(x => x.PostingDate)
            .HasColumnName("posting_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion<string>();

        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.TotalDebits);
        builder.Ignore(x => x.TotalCredits);

        builder.Navigation(x => x.Lines)
            .HasField("_lines")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(x => x.Lines, lines =>
        {
            lines.ToTable("journal_lines", "gl");
            lines.WithOwner().HasForeignKey("EntryId");
            lines.Property<int>("Id").HasColumnName("id");
            lines.HasKey("Id");
            lines.Property("EntryId").HasColumnName("entry_id");

            lines.Property(l => l.AccountCode)
                .HasColumnName("account_code")
                .HasMaxLength(20)
                .IsRequired()
                .HasConversion(c => c.Value, s => new AccountCode(s));

            lines.OwnsOne(l => l.Debit, money =>
            {
                money.Property(m => m.Amount)
                    .HasColumnName("debit_amount")
                    .HasColumnType("numeric(19,4)")
                    .IsRequired();
                money.Property(m => m.Currency)
                    .HasColumnName("debit_currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            lines.OwnsOne(l => l.Credit, money =>
            {
                money.Property(m => m.Amount)
                    .HasColumnName("credit_amount")
                    .HasColumnType("numeric(19,4)")
                    .IsRequired();
                money.Property(m => m.Currency)
                    .HasColumnName("credit_currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

            lines.Property(l => l.Description)
                .HasColumnName("description")
                .HasMaxLength(500);
        });

        builder.HasIndex(x => new { x.CompanyId, x.PostingDate })
            .HasDatabaseName("ix_gl_entries_company_posting");

        builder.HasIndex(x => new { x.CompanyId, x.VoucherNo })
            .IsUnique()
            .HasDatabaseName("ux_gl_entries_company_voucher");
    }
}
