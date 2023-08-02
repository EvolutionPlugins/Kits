﻿// <auto-generated />
using System;
using Kits.Databases.MySql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Kits.Migrations
{
    [DbContext(typeof(KitsDbContext))]
    [Migration("20210623164103_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.16")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("Kits.API.Kit", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<float>("Cooldown")
                        .HasColumnType("float");

                    b.Property<decimal>("Cost")
                        .HasColumnType("decimal(18,2)");

                    b.Property<byte[]>("Items")
                        .HasColumnType("blob")
                        .HasMaxLength(65535);

                    b.Property<decimal>("Money")
                        .HasColumnType("decimal(18,2)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(25) CHARACTER SET utf8mb4")
                        .HasMaxLength(25);

                    b.Property<string>("VehicleId")
                        .HasColumnType("varchar(5) CHARACTER SET utf8mb4")
                        .HasMaxLength(5);

                    b.HasKey("Id");

                    b.ToTable("Kits_Kits");
                });
#pragma warning restore 612, 618
        }
    }
}
