using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace PMS.Infrastructure.Users
{
    internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("tbl.ms_RefreshTokens");

            builder.HasKey(refreshToken => refreshToken.Id);

            builder.Property(refreshToken => refreshToken.Token).HasMaxLength(200);

            builder.HasIndex(refreshToken => refreshToken.Token).IsUnique();

            builder.HasOne(refreshToken => refreshToken.User)
                .WithMany()
                .HasForeignKey(refreshToken => refreshToken.UserId);
        }
    }
}
