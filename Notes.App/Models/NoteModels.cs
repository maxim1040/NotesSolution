using SQLite;


namespace Notes.App.Models;
public class NoteModel
{
    [PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsDirty { get; set; } = true; // lokaal gewijzigd
    public bool IsDeleted { get; set; } = false; // soft delete tot sync
}
