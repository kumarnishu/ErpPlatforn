namespace Platform.Api.Interfaces;

/// <summary>Marks a persisted model that must be retained when deleted.</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}
