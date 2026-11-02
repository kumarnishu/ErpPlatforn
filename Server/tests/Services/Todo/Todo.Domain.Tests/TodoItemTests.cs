using Todo.Domain;

namespace Todo.Domain.Tests;

public sealed class TodoItemTests
{
    [Fact]
    public void Create_trims_title_and_keeps_tenant_and_owner()
    {
        var item = TodoItem.Create("tenant-1", "user-7", "  Review invoice  ");

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal("tenant-1", item.TenantId);
        Assert.Equal("user-7", item.OwnerUserId);
        Assert.Equal("Review invoice", item.Title);
        Assert.False(item.IsCompleted);
        Assert.Null(item.CompletedAtUtc);
    }

    [Fact]
    public void Mark_completed_then_reopen_updates_completion_state()
    {
        var item = TodoItem.Create("tenant-1", "user-7", "Review invoice");
        var completedAt = DateTimeOffset.UtcNow;

        item.MarkCompleted(completedAt);

        Assert.True(item.IsCompleted);
        Assert.Equal(completedAt, item.CompletedAtUtc);

        item.Reopen();

        Assert.False(item.IsCompleted);
        Assert.Null(item.CompletedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_blank_title(string title)
    {
        Assert.Throws<ArgumentException>(() => TodoItem.Create("tenant-1", "user-7", title));
    }
}
