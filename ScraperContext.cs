using Microsoft.EntityFrameworkCore;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

internal class ScraperContext : DbContext
{
    public DbSet<HTMLDocument> HTMLDocuments { get; init; }

    public static ScraperContext Create(IMongoDatabase database) =>
        new(new DbContextOptionsBuilder<ScraperContext>()
            .UseMongoDB(database.Client, database.DatabaseNamespace.DatabaseName)
            .Options);

    public ScraperContext(DbContextOptions options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<HTMLDocument>().ToCollection("htmldocuments");
    }
}

internal class HTMLDocument
{
    public Guid Id { get; set; }
    public string FullPath { get; set; }
    public string RelativePath { get; set; }
    public string FileName { get; set; }
    public string FullHTML { get; set; }
    public string ArticleHTML { get; set; }
    public string ArticleText { get; set; }
    public string ExtractedArticleText { get; set; }
    public string ExtractedArticleTextInMarkdownFormat { get; set; }
}