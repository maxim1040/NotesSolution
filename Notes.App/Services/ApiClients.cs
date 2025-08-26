using System.Net.Http.Json;
using Notes.App.Models;


namespace Notes.App.Services;
public class ApiClient
{
    private readonly HttpClient _http;
    public ApiClient(HttpClient http) { _http = http; }


    public record NoteCreateDto(string Title, string Content);
    public record NoteUpdateDto(string Title, string Content, DateTime UpdatedAtUtc);
    public record NoteDto(Guid Id, string Title, string Content, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);


    public Task<List<NoteDto>?> GetNotesAsync() => _http.GetFromJsonAsync<List<NoteDto>>($"{Constants.ApiBase}/api/notes");
    public Task<HttpResponseMessage> CreateAsync(NoteModel n) =>
    _http.PostAsJsonAsync($"{Constants.ApiBase}/api/notes", new NoteCreateDto(n.Title, n.Content));
    public Task<HttpResponseMessage> UpdateAsync(NoteModel n) =>
    _http.PutAsJsonAsync($"{Constants.ApiBase}/api/notes/{n.Id}", new NoteUpdateDto(n.Title, n.Content, n.UpdatedAtUtc));
    public Task<HttpResponseMessage> DeleteAsync(Guid id) => _http.DeleteAsync($"{Constants.ApiBase}/api/notes/{id}");
}