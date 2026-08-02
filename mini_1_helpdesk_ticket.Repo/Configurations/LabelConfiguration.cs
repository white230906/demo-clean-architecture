using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using mini_1_helpdesk_ticket.Repo.Entity;

namespace mini_1_helpdesk_ticket.Repo.Configurations;

public class LabelConfiguration: IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        builder.ToTable("labels", table =>
        {
            table.HasCheckConstraint(
                "ck_labels_name_not_blank",
                "length(btrim(name)) > 0");

            table.HasCheckConstraint(
                "ck_labels_slug_format",
                "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");

            table.HasCheckConstraint(
                "ck_labels_color_hex",
                "color ~ '^#[0-9A-Fa-f]{6}$'");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(7).IsRequired();

        builder.HasIndex(x => x.Slug)
            .IsUnique()
            .HasDatabaseName("ux_labels_slug");
    }
}