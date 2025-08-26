using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notes.Api.Data;
using Notes.Api.DTOs;
using Notes.Api.Models;
using System.Security.Claims;

namespace Notes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly AppDbContext _db;
    public NotesController(AppDbContext db) => _db = db;

    private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    // GET: api/notes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<NoteDto>>> GetAll(CancellationToken ct)
    {
        var uid = GetUserId();
        if (uid is null) return Unauthorized();

        var notes = await _db.Notes
            .Where(n => n.UserId == uid)
            .OrderByDescending(n => n.UpdatedAtUtc)
            .Select(n => new NoteDto(n.Id, n.Title, n.Content, n.CreatedAtUtc, n.UpdatedAtUtc))
            .ToListAsync(ct);

        return Ok(notes);
    }

    // GET: api/notes/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NoteDto>> Get(Guid id, CancellationToken ct)
    {
        var uid = GetUserId();
        if (uid is null) return Unauthorized();

        var n = await _db.Notes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid, ct);

        if (n is null) return NotFound();

        return Ok(new NoteDto(n.Id, n.Title, n.Content, n.CreatedAtUtc, n.UpdatedAtUtc));
    }

    // POST: api/notes
    [HttpPost]
    public async Task<ActionResult<NoteDto>> Create([FromBody] NoteCreateDto dto, CancellationToken ct)
    {
        var uid = GetUserId();
        if (uid is null) return Unauthorized();

        var now = DateTime.UtcNow;

        var n = new Note
        {
            Title = dto.Title,
            Content = dto.Content,
            UserId = uid,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.Notes.Add(n);
        await _db.SaveChangesAsync(ct);

        var result = new NoteDto(n.Id, n.Title, n.Content, n.CreatedAtUtc, n.UpdatedAtUtc);
        return CreatedAtAction(nameof(Get), new { id = n.Id }, result);
    }

    // PUT: api/notes/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] NoteUpdateDto dto, CancellationToken ct)
    {
        var uid = GetUserId();
        if (uid is null) return Unauthorized();

        var n = await _db.Notes.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid, ct);
        if (n is null) return NotFound();

        if (dto.UpdatedAtUtc < n.UpdatedAtUtc)
            return Conflict(new { message = "Local versie ouder dan server." });

        n.Title = dto.Title;
        n.Content = dto.Content;
        n.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE: api/notes/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var uid = GetUserId();
        if (uid is null) return Unauthorized();

        var n = await _db.Notes.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid, ct);
        if (n is null) return NotFound();

        _db.Notes.Remove(n);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
