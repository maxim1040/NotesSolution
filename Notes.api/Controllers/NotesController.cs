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
    public NotesController(AppDbContext db) { _db = db; }


    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


    [HttpGet]
    public async Task<ActionResult<IEnumerable<NoteDto>>> GetAll()
    {
        var uid = UserId; if (uid == null) return Unauthorized();
        var notes = await _db.Notes.Where(n => n.UserId == uid)
        .OrderByDescending(n => n.UpdatedAtUtc)
        .Select(n => new NoteDto(n.Id, n.Title, n.Content, n.CreatedAtUtc, n.UpdatedAtUtc))
        .ToListAsync();
        return notes;
    }


    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NoteDto>> Get(Guid id)
    {
        var uid = UserId; if (uid == null) return Unauthorized();
        var n = await _db.Notes.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
        if (n == null) return NotFound();
        return new NoteDto(n.Id, n.Title, n.Content, n.CreatedAtUtc, n.UpdatedAtUtc);
    }


    [HttpPost]
    public async Task<ActionResult<NoteDto>> Create(NoteCreateDto dto)
    {
        var uid = UserId; if (uid == null) return Unauthorized();
        var n = new Note { Title = dto.Title, Content = dto.Content, UserId = uid };
        _db.Notes.Add(n);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = n.Id },
        new NoteDto(n.Id, n.Title, n.Content, n.CreatedAtUtc, n.UpdatedAtUtc));
    }


    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, NoteUpdateDto dto)
    {
        var uid = UserId; if (uid == null) return Unauthorized();
        var n = await _db.Notes.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
        if (n == null) return NotFound();
        // eenvoudige LWW (last-write-wins) conflict-resolutie
        if (dto.UpdatedAtUtc < n.UpdatedAtUtc) return Conflict(new { message = "Local versie ouder dan server." });
        n.Title = dto.Title; n.Content = dto.Content; n.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }


    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var uid = UserId; if (uid == null) return Unauthorized();
        var n = await _db.Notes.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
        if (n == null) return NotFound();
        _db.Remove(n);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}