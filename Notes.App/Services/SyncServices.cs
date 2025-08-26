using Notes.App.Models;
using System.Net;

namespace Notes.App.Services;
public class SyncService
{
    private readonly LocalDb _db;
    private readonly ApiClient _api;
    public SyncService(LocalDb db, ApiClient api) { _db = db; _api = api; }

    public async Task SyncAsync()
    {
        if (Connectivity.NetworkAccess != NetworkAccess.Internet) return;

        //Push
        var dirty = await _db.GetDirtyAsync();
        foreach (var n in dirty)
        {
            if (n.IsDeleted)
            {
                var del = await _api.DeleteAsync(n.Id);
                if (del.IsSuccessStatusCode || del.StatusCode == HttpStatusCode.NotFound)
                {
                    await _db.DeleteAsync(n);
                }
                continue;
            }

            //proberen updaten
            var upd = await _api.UpdateAsync(n);
            if (upd.IsSuccessStatusCode)
            {
                n.IsDirty = false;
                n.UpdatedAtUtc = DateTime.UtcNow;
                await _db.UpsertAsync(n);
                continue;
            }

            // 404 op update? Dan bestond dat Id niet op de server → aanmaken
            if (upd.StatusCode == HttpStatusCode.NotFound)
            {
                var created = await _api.CreateAsync(n);
                if (created != null)
                {
                    var oldId = n.Id;

                    // Map server-waarden terug in lokaal item
                    n.Id = created.Id;
                    n.Title = created.Title;
                    n.Content = created.Content;
                    n.CreatedAtUtc = created.CreatedAtUtc;
                    n.UpdatedAtUtc = created.UpdatedAtUtc;
                    n.IsDirty = false;
                    n.IsDeleted = false;

                    // Bewaar onder nieuwe server-ID
                    await _db.UpsertAsync(n);

                    // Oude lokaal record met oldId opruimen als het bestaat
                    if (oldId != created.Id)
                        await _db.DeleteByIdAsync(oldId);

                    continue;
                }
            }

        }

        //Pull
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
