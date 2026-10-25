using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Coworking.Models;

public partial class CoworkingDbContext : DbContext
{
    public CoworkingDbContext()
    {
    }

    public CoworkingDbContext(DbContextOptions<CoworkingDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CategoriaEspacio> CategoriasEspacio { get; set; }

    public virtual DbSet<Espacio> Espacios { get; set; }

    public virtual DbSet<LogAuditoria> LogsAuditoria { get; set; }

    public virtual DbSet<Pago> Pagos { get; set; }

    public virtual DbSet<Reserva> Reservas { get; set; }

    public virtual DbSet<Rol> Roles { get; set; }

    public virtual DbSet<Servicio> Servicios { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rol>(entity =>
        {
            entity.ToTable("Roles");
            entity.Property(e => e.Nombre).HasMaxLength(50);
            entity.Property(e => e.Descripcion).HasMaxLength(200);
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("Usuarios");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Apellido).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(120);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Telefono).HasMaxLength(20);
            entity.Property(e => e.Estado).HasMaxLength(20).HasDefaultValue("Activo");
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");
            entity.HasIndex(e => e.Email).IsUnique();

            entity.HasOne(d => d.Rol).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuarios_Roles");
        });

        modelBuilder.Entity<CategoriaEspacio>(entity =>
        {
            entity.ToTable("CategoriasEspacio");
            entity.Property(e => e.Nombre).HasMaxLength(50);
            entity.Property(e => e.Descripcion).HasMaxLength(200);
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        modelBuilder.Entity<Servicio>(entity =>
        {
            entity.ToTable("Servicios");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Descripcion).HasMaxLength(200);
            entity.HasIndex(e => e.Nombre).IsUnique();
        });

        modelBuilder.Entity<Espacio>(entity =>
        {
            entity.ToTable("Espacios");
            entity.Property(e => e.Nombre).HasMaxLength(100);
            entity.Property(e => e.Descripcion).HasMaxLength(500);
            entity.Property(e => e.PrecioPorHora).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.PisoUbicacion).HasMaxLength(20);
            entity.Property(e => e.Estado).HasMaxLength(20).HasDefaultValue(EstadoEspacio.Disponible);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne(d => d.Categoria).WithMany(p => p.Espacios)
                .HasForeignKey(d => d.CategoriaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Espacios_CategoriasEspacio");

            entity.HasMany(d => d.Servicios).WithMany(p => p.Espacios)
                .UsingEntity<Dictionary<string, object>>(
                    "EspacioServicios",
                    r => r.HasOne<Servicio>().WithMany()
                        .HasForeignKey("ServicioId")
                        .HasConstraintName("FK_EspacioServicios_Servicios"),
                    l => l.HasOne<Espacio>().WithMany()
                        .HasForeignKey("EspacioId")
                        .HasConstraintName("FK_EspacioServicios_Espacios"),
                    j => j.HasKey("EspacioId", "ServicioId"));
        });

        modelBuilder.Entity<Reserva>(entity =>
        {
            entity.ToTable("Reservas");
            entity.Property(e => e.MontoTotal).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Estado).HasMaxLength(20).HasDefaultValue(EstadoReserva.Pendiente);
            entity.Property(e => e.NotasAdicionales).HasMaxLength(500);
            entity.Property(e => e.FechaCreacion).HasDefaultValueSql("(sysdatetime())");
            entity.HasIndex(e => new { e.EspacioId, e.FechaInicio, e.FechaFin });

            entity.HasOne(d => d.Espacio).WithMany(p => p.Reservas)
                .HasForeignKey(d => d.EspacioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reservas_Espacios");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Reservas)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reservas_Usuarios");
        });

        modelBuilder.Entity<Pago>(entity =>
        {
            entity.ToTable("Pagos");
            entity.Property(e => e.Monto).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.MetodoPago).HasMaxLength(50);
            entity.Property(e => e.Estado).HasMaxLength(20).HasDefaultValue("Aprobado");
            entity.Property(e => e.FechaPago).HasDefaultValueSql("(sysdatetime())");
            entity.HasIndex(e => e.ReservaId).IsUnique();

            entity.HasOne(d => d.Reserva).WithOne(p => p.Pago)
                .HasForeignKey<Pago>(d => d.ReservaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pagos_Reservas");
        });

        modelBuilder.Entity<LogAuditoria>(entity =>
        {
            entity.ToTable("LogsAuditoria");
            entity.Property(e => e.MetodoHttp).HasMaxLength(10);
            entity.Property(e => e.RutaEndpoint).HasMaxLength(255);
            entity.Property(e => e.IpOrigen).HasMaxLength(45);
            entity.Property(e => e.FechaRegistro).HasDefaultValueSql("(sysdatetime())");

            entity.HasOne<Usuario>().WithMany()
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_LogsAuditoria_Usuarios");
        });
    }
}
