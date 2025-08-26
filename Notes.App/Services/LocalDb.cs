using SQLite;
using Notes.App.Models;


namespace Notes.App.Services;
public class LocalDb
{
    private readonly SQLiteAsyncConnection _db;
    public LocalDb()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "notes.db3");
        _db = new SQLiteAsyncConnection(path);
        _db.CreateTableAsync<NoteModel>().Wait();
    }


    public Task<List<NoteModel>> GetNotesAsync() => _db.Table<NoteModel>().Where(n => !n.IsDeleted).OrderByDescending(n => n.UpdatedAtUtc).ToListAsync();
    public Task<NoteModel?> GetAsync(Guid id) => _db.FindAsync<NoteModel>(id);
    public Task<int> UpsertAsync(NoteModel n) => _db.InsertOrReplaceAsync(n);
    public Task<int> DeleteAsync(NoteModel n) => _db.DeleteAsync(n);
    public Task<List<NoteModel>> GetDirtyAsync() => _db.Table<NoteModel>().Where(n => n.IsDirty || n.IsDeleted).ToListAsync();
    public Task<int> DeleteByIdAsync(Guid id) => _db.DeleteAsync<NoteModel>(id);
}