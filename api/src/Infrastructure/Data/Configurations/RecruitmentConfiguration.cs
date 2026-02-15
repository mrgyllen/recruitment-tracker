using api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Infrastructure.Data.Configurations;

public class RecruitmentConfiguration : IEntityTypeConfiguration<Recruitment>
{
    public void Configure(EntityTypeBuilder<Recruitment> builder)
    {
        builder.ToTable("Recruitments");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Description)
            .HasMaxLength(2000);

        builder.Property(r => r.JobRequisitionId)
            .HasMaxLength(100);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasMany(r => r.Steps)
            .WithOne()
            .HasForeignKey(s => s.RecruitmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Members)
            .WithOne()
            .HasForeignKey(m => m.RecruitmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(r => r.DomainEvents);

        builder.Navigation(r => r.Steps).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(r => r.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
