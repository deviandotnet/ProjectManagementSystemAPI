using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.HolidayCalendars;

namespace PMS.Infrastructure.Database.Configurations
{
    /// <summary>
    /// EF Core fluent configuration for the HolidayCalendar entity.
    /// Table: HolidayCalendar
    /// Global table — used by CalendarEngine across all projects.
    /// Pre-seeded with Philippine national holidays by HolidaySeeder on startup.
    /// HolidayDate maps to SQL DATE (DateOnly in C#).
    /// </summary>
    public class HolidayCalendarConfiguration : IEntityTypeConfiguration<HolidayCalendar>
    {
        public void Configure(EntityTypeBuilder<HolidayCalendar> builder)
        {
            builder.ToTable("tbl.ms_HolidayCalendar");

            builder.HasKey(h => h.Id);

            builder.Property(h => h.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            // DateOnly → SQL DATE — no time component for a holiday date
            builder.Property(h => h.HolidayDate)
                .HasColumnType("date")
                .IsRequired();

            builder.Property(h => h.Name)
                .IsRequired()
                .HasMaxLength(200);

            // Enum stored as tinyint
            builder.Property(h => h.Type)
                .IsRequired()
                .HasConversion<byte>();

            builder.Property(h => h.IsRecurringAnnually)
                .HasDefaultValue(false);

            // Year is null when IsRecurringAnnually = true
            builder.Property(h => h.Year);

            builder.Property(h => h.CreatedAt)
                .HasDefaultValueSql("now() at time zone 'utc'");

            // Prevent duplicate holiday entries for the same date and year combination
            builder.HasIndex(h => new { h.HolidayDate, h.Year })
                .IsUnique();
        }
    }
}
