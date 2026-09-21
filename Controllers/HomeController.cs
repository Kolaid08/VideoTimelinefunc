using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using VideoTimelineApp.Data;
using VideoTimelineApp.Models;

namespace VideoTimelineApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db = new AppDbContext();

        // GET /
        public async Task<ActionResult> Index()
        {
            var videos = await _db.VideoSessions
                .Include(v => v.Segments)
                .OrderByDescending(v => v.UploadedAt)
                .ToListAsync();

            return View(videos);
        }

        // POST /Home/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Upload(HttpPostedFileBase file, string title)
        {
            if (file == null || file.ContentLength == 0)
            {
                TempData["Error"] = "Vui lòng chọn file video!";
                return RedirectToAction("Index");
            }

            int maxMB = int.Parse(WebConfigurationManager.AppSettings["MaxFileSizeMB"] ?? "500");
            if (file.ContentLength > maxMB * 1024 * 1024)
            {
                TempData["Error"] = string.Format("File quá lớn! Tối đa {0} MB.", maxMB);
                return RedirectToAction("Index");
            }

            // Determine upload folder
            string relPath    = WebConfigurationManager.AppSettings["VideoUploadPath"].TrimStart('~', '/');
            string uploadDir  = Server.MapPath("~/" + relPath);
            Directory.CreateDirectory(uploadDir);

            // Save with GUID name to avoid collisions
            string ext        = Path.GetExtension(file.FileName);
            string storedName = Guid.NewGuid().ToString("N") + ext;
            string fullPath   = Path.Combine(uploadDir, storedName);
            file.SaveAs(fullPath);

            // Persist to DB
            var session = new VideoSession
            {
                Title            = string.IsNullOrWhiteSpace(title)
                                        ? Path.GetFileNameWithoutExtension(file.FileName)
                                        : title.Trim(),
                OriginalFileName = file.FileName,
                StoredFileName   = storedName,
                FileSize         = file.ContentLength,
                UploadedAt       = DateTime.UtcNow
            };

            _db.VideoSessions.Add(session);
            await _db.SaveChangesAsync();

            TempData["Success"] = string.Format("Đã upload \"{0}\" thành công!", session.Title);
            return RedirectToAction("Player", "Video", new { id = session.Id });
        }

        // POST /Home/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var session = await _db.VideoSessions.FindAsync(id);
            if (session == null) return HttpNotFound();

            // Delete physical file
            string relPath  = WebConfigurationManager.AppSettings["VideoUploadPath"].TrimStart('~', '/');
            string filePath = Path.Combine(Server.MapPath("~/" + relPath), session.StoredFileName);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            _db.VideoSessions.Remove(session);
            await _db.SaveChangesAsync();

            TempData["Success"] = string.Format("Đã xoá video \"{0}\".", session.Title);
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
