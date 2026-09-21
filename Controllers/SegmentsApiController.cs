using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using VideoTimelineApp.Data;
using VideoTimelineApp.Models;

namespace VideoTimelineApp.Controllers
{
    /// <summary>
    /// REST API for Segments.
    /// All routes are prefixed with /api/segments
    /// </summary>
    [RoutePrefix("api/segments")]
    public class SegmentsApiController : ApiController
    {
        private readonly AppDbContext _db = new AppDbContext();

        // ──────────────────────────────────────────────────────────────────
        // GET  api/segments/{videoId}
        // Returns all segments for a video, ordered by start time
        // ──────────────────────────────────────────────────────────────────
        [HttpGet]
        [Route("{videoId:int}")]
        public async Task<IHttpActionResult> GetByVideo(int videoId)
        {
            var segments = await _db.Segments
                .Where(s => s.VideoSessionId == videoId)
                .OrderBy(s => s.StartTime)
                .Select(s => new
                {
                    s.Id,
                    s.Label,
                    s.StartTime,
                    s.EndTime,
                    duration = s.EndTime - s.StartTime,
                    s.Type,
                    s.Color
                })
                .ToListAsync();

            return Ok(segments);
        }

        // ──────────────────────────────────────────────────────────────────
        // POST  api/segments
        // Creates a new segment (validates overlap on server side too)
        // ──────────────────────────────────────────────────────────────────
        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> Create([FromBody] CreateSegmentDto dto)
        {
            if (dto == null)
                return BadRequest("Payload không hợp lệ.");

            if (dto.EndTime <= dto.StartTime)
                return BadRequest("Điểm cuối phải lớn hơn điểm đầu.");

            // Server-side overlap check
            bool overlap = await _db.Segments.AnyAsync(s =>
                s.VideoSessionId == dto.VideoSessionId &&
                s.StartTime < dto.EndTime &&
                s.EndTime   > dto.StartTime);

            if (overlap)
                return Content(HttpStatusCode.Conflict,
                    new { message = "Đoạn này đè lên đoạn đã tồn tại!" });

            var seg = new Segment
            {
                VideoSessionId = dto.VideoSessionId,
                Label          = dto.Label    ?? string.Empty,
                StartTime      = dto.StartTime,
                EndTime        = dto.EndTime,
                Type           = dto.Type     ?? "normal",
                Color          = dto.Color    ?? "#38bdf8",
                CreatedAt      = DateTime.UtcNow
            };

            _db.Segments.Add(seg);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                seg.Id,
                seg.Label,
                seg.StartTime,
                seg.EndTime,
                duration = seg.EndTime - seg.StartTime,
                seg.Type,
                seg.Color
            });
        }

        // ──────────────────────────────────────────────────────────────────
        // PUT  api/segments/{id}
        // Updates the label of an existing segment
        // ──────────────────────────────────────────────────────────────────
        [HttpPut]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> UpdateLabel(int id, [FromBody] UpdateLabelDto dto)
        {
            var seg = await _db.Segments.FindAsync(id);
            if (seg == null) return NotFound();

            seg.Label = dto != null && dto.Label != null ? dto.Label : seg.Label;
            await _db.SaveChangesAsync();

            return Ok(new { seg.Id, seg.Label });
        }

        // ──────────────────────────────────────────────────────────────────
        // DELETE  api/segments/{id}
        // Removes a segment
        // ──────────────────────────────────────────────────────────────────
        [HttpDelete]
        [Route("{id:int}")]
        public async Task<IHttpActionResult> Delete(int id)
        {
            var seg = await _db.Segments.FindAsync(id);
            if (seg == null) return NotFound();

            _db.Segments.Remove(seg);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Đã xoá", id });
        }

        // ──────────────────────────────────────────────────────────────────
        // PATCH  api/segments/video/{videoId}/duration
        // Called by the client once the HTML5 video metadata is loaded
        // ──────────────────────────────────────────────────────────────────
        [HttpPatch]
        [Route("video/{videoId:int}/duration")]
        public async Task<IHttpActionResult> UpdateDuration(int videoId, [FromBody] UpdateDurationDto dto)
        {
            var session = await _db.VideoSessions.FindAsync(videoId);
            if (session == null) return NotFound();

            if (dto != null && dto.Duration > 0)
                session.Duration = dto.Duration;

            await _db.SaveChangesAsync();
            return Ok(new { session.Id, session.Duration });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }

    // ── DTOs ──────────────────────────────────────────────────────────────
    public class CreateSegmentDto
    {
        public int    VideoSessionId { get; set; }
        public string Label         { get; set; }
        public double StartTime     { get; set; }
        public double EndTime       { get; set; }
        public string Type          { get; set; }
        public string Color         { get; set; }
    }

    public class UpdateLabelDto
    {
        public string Label { get; set; }
    }

    public class UpdateDurationDto
    {
        public double Duration { get; set; }
    }
}
