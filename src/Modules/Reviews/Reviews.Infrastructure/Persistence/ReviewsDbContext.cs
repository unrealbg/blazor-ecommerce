using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Reviews.Domain.Questions;
using Reviews.Domain.Reports;
using Reviews.Domain.Reviews;

namespace Reviews.Infrastructure.Persistence;

public sealed class ReviewsDbContext(
    DbContextOptions<ReviewsDbContext> options,
    IEventSerializer eventSerializer)
    : ModuleDbContext(options, eventSerializer)
{
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();

    public DbSet<ReviewVote> ReviewVotes => Set<ReviewVote>();

    public DbSet<ReviewReport> ReviewReports => Set<ReviewReport>();

    public DbSet<ProductQuestion> ProductQuestions => Set<ProductQuestion>();

    public DbSet<ProductAnswer> ProductAnswers => Set<ProductAnswer>();

    public DbSet<ReviewAggregateSnapshot> ReviewAggregateSnapshots => Set<ReviewAggregateSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("reviews");
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReviewsDbContext).Assembly);
    }
}
