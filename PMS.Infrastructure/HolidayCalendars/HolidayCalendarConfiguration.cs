using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.HolidayCalendars;

namespace PMS.Infrastructure.HolidayCalendars;

/// <summary>
/// EF Core fluent configuration for the HolidayCalendar entity.
/// Table: tbl.ms_HolidayCalendar
/// Global table — used by CalendarEngine across all projects.
/// Pre-seeded with Philippine national holidays by HolidaySeeder on startup.
/// </summary>
internal sealed class HolidayCalendarConfiguration : IEntityTypeConfiguration<HolidayCalendar>
{
    public void Configure(EntityTypeBuilder<HolidayCalendar> builder)
    {
        builder.ToTable("tbl.ms_HolidayCalendar");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(h => h.HolidayDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(h => h.Type)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(h => h.IsRecurringAnnually)
            .HasDefaultValue(false);

        builder.Property(h => h.Year);

        builder.Property(h => h.CreatedAt)
            .HasDefaultValueSql("now() at time zone 'utc'");

        builder.HasIndex(h => new { h.HolidayDate, h.Year })
            .IsUnique();
    }
}
