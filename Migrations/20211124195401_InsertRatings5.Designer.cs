﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WJS_MovieLens.Services;

namespace WJS_MovieLens.Migrations
{
    [DbContext(typeof(DbMediaService))]
    [Migration("20211124195401_InsertRatings5")]
    partial class InsertRatings5
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.12")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("WJS_MovieLens.Model.Genre", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Genres");
                });

            modelBuilder.Entity("WJS_MovieLens.Model.Movie", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("ReleaseDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Title")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Movies");
                });

            modelBuilder.Entity("WJS_MovieLens.Model.MovieGenre", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<long?>("GenreId")
                        .HasColumnType("bigint");

                    b.Property<long?>("MovieId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("GenreId");

                    b.HasIndex("MovieId");

                    b.ToTable("MovieGenres");
                });

            modelBuilder.Entity("WJS_MovieLens.Model.Occupation", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Occupations");
                });

            modelBuilder.Entity("WJS_MovieLens.Model.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<long>("Age")
                        .HasColumnType("bigint");

                    b.Property<string>("Gender")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long?>("OccupationId")
                        .HasColumnType("bigint");

                    b.Property<string>("ZipCode")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("OccupationId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("WJS_MovieLens.Model.UserMovie", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<long?>("MovieId")
                        .HasColumnType("bigint");

                    b.Property<DateTime>("RatedAt")
                        .HasColumnType("datetime2");

                    b.Property<long>("Rating")
                        .HasColumnType("bigint");

                    b.Property<long?>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("MovieId");

                    b.HasIndex("UserId");

                    b.ToTable("UserMovies");
                });

            modelBuilder.Entity("WJS_MovieLens.Model.MovieGenre", b =>
                {
                    b.HasOne("WJS_MovieLens.Model.Genre", "Genre")
                        .WithMany("MovieGenres")
                        .HasForeignKey("GenreId");

                    b.HasOne("WJS_MovieLens.Model.Movie", "Movie")
                        .WithMany("MovieGenres")
                        .HasForeignKey("MovieId");

                    b.Navigation("Genre");

                    b.Navigation("Movie");
                });

            modelBuilder.Entity("WJS_MovieLens.Model.User", b =>
                {
                    b.HasOne("WJS_MovieLens.Model.Occupation", "Occupation")
                        .WithMany()
                        .HasForeignKey("OccupationId");

                    b.Navigation("Occupation");
                });

            modelBuilder.Entity("WJS_MovieLens.Model.UserMovie", b =>
                {
                    b.HasOne("WJS_MovieLens.Model.Movie", "Movie")
                        .WithMany("UserMovies")
                        .HasForeignKey("MovieId");

                    b.HasOne("WJS_MovieLens.Model.User", "User")
                        .WithMany("UserMovies")
                        .HasForeignKey("UserId");

                    b.Navigation("Movie");

                    b.Navigation("User");
                });

            modelBuilder.Entity("WJS_MovieLens.Model.Genre", b =>
                {
                    b.Navigation("MovieGenres");
                });

            modelBuilder.Entity("WJS_MovieLens.Model.Movie", b =>
                {
                    b.Navigation("MovieGenres");

                    b.Navigation("UserMovies");
                });

            modelBuilder.Entity("WJS_MovieLens.Model.User", b =>
                {
                    b.Navigation("UserMovies");
                });
#pragma warning restore 612, 618
        }
    }
}
