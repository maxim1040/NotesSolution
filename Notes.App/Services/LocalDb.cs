using SQLite;
using Notes.App.Models;

namespace Notes.App.Services;

public class LocalDb
{
    private SQLiteAsyncConnection? _db;
    private string? _userKey;

    public async Task UseUserAsync(string userKey)
    {
        if (!string.IsNullOrWhiteSpace(_userKey) && _userKey == userKey && _db != null)
            return;

        _userKey = userKey;
        var safe = userKey.Replace(":", "_").Replace("/", "_");
        var path = Path.Combine(FileSystem.AppDataDirectory, $"notes_{safe}.db3");

        _db = new SQLiteAsyncConnection(path);
        await _db.CreateTableAsync<NoteModel>();
    }

    public bool IsReady => _db != null;

    private SQLiteAsyncConnection EnsureDb() =>
        _db ?? throw new InvalidOperationException("LocalDb is not initialized. Call UseUserAsync(userId) first.");

    public Task<List<NoteModel>> GetNotesAsync() =>
        EnsureDb().Table<NoteModel>().Where(n => !n.IsDeleted).OrderByDescending(n => n.UpdatedAtUtc).ToListAsync();

    public Task<NoteModel?> GetAsync(Guid id) => EnsureDb().FindAsync<NoteModel>(id);
    public Task<int> UpsertAsync(NoteModel n) => EnsureDb().InsertOrReplaceAsync(n);
    public Task<int> DeleteAsync(NoteModel n) => EnsureDb().DeleteAsync(n);
    public Task<int> DeleteByIdAsync(Guid id) => EnsureDb().DeleteAsync<NoteModel>(id);

    public Task<List<NoteModel>> GetDirtyAsync() =>
        EnsureDb().Table<NoteModel>().Where(n => n.IsDirty || n.IsDeleted).ToListAsync();

    public void Reset()
    {
        _db = null;
        _userKey = null;
    }
}
