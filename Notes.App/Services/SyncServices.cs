using Notes.App.Models;


namespace Notes.App.Services;
public class SyncService
{
    private readonly LocalDb _db;
    private readonly ApiClient _api;
    public SyncService(LocalDb db, ApiClient api) { _db = db; _api = api; }


    public async Task SyncAsync()
    {
        if (Connectivity.NetworkAccess != NetworkAccess.Internet) return;


        //Push lokale wijzigingen
        var dirty = await _db.GetDirtyAsync();
        foreach (var n in dirty)
        {
            HttpResponseMessage res;
            if (n.IsDeleted)
            {
                res = await _api.DeleteAsync(n.Id);
                if (res.IsSuccessStatusCode) await _db.DeleteAsync(n);
                continue;
            }
           
            res = await _api.UpdateAsync(n);
            if (!res.IsSuccessStatusCode)
                res = await _api.CreateAsync(n);
            if (res.IsSuccessStatusCode)
            {
                n.IsDirty = false; n.UpdatedAtUtc = DateTime.UtcNow;
                await _db.UpsertAsync(n);
            }
        }


        //Pull server data (eenvoudig: alles)
        var server = await _api.GetNotesAsync();
        if (server == null) return;
        foreach (var s in server)
        {
            var local = await _db.GetAsync(s.Id);
            if (local == null || s.UpdatedAtUtc > local.UpdatedAtUtc)
            {
                await _db.UpsertAsync(new NoteModel
                {
                    Id = s.Id,
                    Title = s.Title,
                    Content = s.Content,
                    CreatedAtUtc = s.CreatedAtUtc,
                    UpdatedAtUtc = s.UpdatedAtUtc,
                    IsDirty = false,
                    IsDeleted = false
                });
            }
        }
    }
}