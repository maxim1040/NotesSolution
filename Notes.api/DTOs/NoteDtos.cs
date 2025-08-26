namespace Notes.Api.DTOs;
public record NoteCreateDto(string Title, string Content);
public record NoteUpdateDto(string Title, string Content, DateTime UpdatedAtUtc);
public record NoteDto(Guid Id, string Title, string Content, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);