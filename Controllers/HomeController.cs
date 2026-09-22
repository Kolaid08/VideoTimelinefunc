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

using VideoTimelineApp.Services;

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

            ViewBag.CloudinaryConfigured = CloudinaryService.IsConfigured;
            return View(videos);
        }

        // GET /Home/GetUploadSignature (Used for Client-Side Direct Upload)
        [HttpGet]
        public JsonResult GetUploadSignature()
        {
            if (!CloudinaryService.IsConfigured)
            {
                return Json(new { success = false, message = "Cloudinary chưa được cấu hình!" }, JsonRequestBehavior.AllowGet);
            }

            var sig = CloudinaryService.GetUploadSignature();
            return Json(new { success = true, data = sig }, JsonRequestBehavior.AllowGet);
        }

        // POST /Home/SaveUploadedVideo (Called by browser after direct upload to Cloudinary completes)
        [HttpPost]
        public async Task<ActionResult> SaveUploadedVideo(SaveUploadedVideoDto model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.VideoUrl))
            {
                return Json(new { success = false, message = "Dữ liệu video không hợp lệ!" });
            }

            string videoTitle = string.IsNullOrWhiteSpace(model.Title)
                ? (string.IsNullOrWhiteSpace(model.OriginalFileName) ? "Video mới" : Path.GetFileNameWithoutExtension(model.OriginalFileName))
                : model.Title.Trim();

            var session = new VideoSession
            {
                Title            = videoTitle,
                OriginalFileName = model.OriginalFileName ?? "video.mp4",
                VideoUrl         = model.VideoUrl,
                PublicId         = model.PublicId,
                Duration         = model.Duration,
                FileSize         = model.FileSize,
                UploadedAt       = DateTime.UtcNow
            };

            _db.VideoSessions.Add(session);
            await _db.SaveChangesAsync();

            TempData["Success"] = string.Format("Đã tải lên trực tiếp Cloudinary \"{0}\" thành công!", session.Title);

            return Json(new
            {
                success     = true,
                redirectUrl = Url.Action("Player", "Video", new { id = session.Id })
            });
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

            string videoTitle = string.IsNullOrWhiteSpace(title)
                ? Path.GetFileNameWithoutExtension(file.FileName)
                : title.Trim();

            // Check if Cloudinary is configured -> Use Cloudinary chunked upload
            if (CloudinaryService.IsConfigured)
            {
                string tempFile = Path.Combine(Path.GetTempPath(), "cloudinary_upload_" + Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName));
                file.SaveAs(tempFile);

                CloudinaryUploadResult uploadResult;
                try
                {
                    uploadResult = await CloudinaryService.UploadVideoFromFileAsync(tempFile, file.FileName);
                }
                finally
                {
                    try
                    {
                        if (System.IO.File.Exists(tempFile))
                        {
                            System.IO.File.Delete(tempFile);
                        }
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }

                if (!uploadResult.Success)
                {
                    TempData["Error"] = "Lỗi khi upload lên Cloudinary: " + uploadResult.ErrorMessage;
                    return RedirectToAction("Index");
                }

                var session = new VideoSession
                {
                    Title            = videoTitle,
                    OriginalFileName = file.FileName,
                    VideoUrl         = uploadResult.SecureUrl,
                    PublicId         = uploadResult.PublicId,
                    Duration         = uploadResult.Duration,
                    FileSize         = uploadResult.Bytes > 0 ? uploadResult.Bytes : file.ContentLength,
                    UploadedAt       = DateTime.UtcNow
                };

                _db.VideoSessions.Add(session);
                await _db.SaveChangesAsync();

                TempData["Success"] = string.Format("Đã upload lên Cloudinary \"{0}\" thành công!", session.Title);
                return RedirectToAction("Player", "Video", new { id = session.Id });
            }
            else
            {
                // Fallback: Local storage if Cloudinary credentials are not configured yet
                string relPath    = WebConfigurationManager.AppSettings["VideoUploadPath"].TrimStart('~', '/');
                string uploadDir  = Server.MapPath("~/" + relPath);
                Directory.CreateDirectory(uploadDir);

                string ext        = Path.GetExtension(file.FileName);
                string storedName = Guid.NewGuid().ToString("N") + ext;
                string fullPath   = Path.Combine(uploadDir, storedName);
                file.SaveAs(fullPath);

                var session = new VideoSession
                {
                    Title            = videoTitle,
                    OriginalFileName = file.FileName,
                    StoredFileName   = storedName,
                    FileSize         = file.ContentLength,
                    UploadedAt       = DateTime.UtcNow
                };

                _db.VideoSessions.Add(session);
                await _db.SaveChangesAsync();

                TempData["Success"] = string.Format("Đã lưu cục bộ \"{0}\" (Hãy cấu hình Cloudinary trong Web.config để lưu đám mây).", session.Title);
                return RedirectToAction("Player", "Video", new { id = session.Id });
            }
        }

        // POST /Home/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id)
        {
            var session = await _db.VideoSessions.FindAsync(id);
            if (session == null) return HttpNotFound();

            // If stored on Cloudinary, delete from Cloudinary
            if (!string.IsNullOrEmpty(session.PublicId))
            {
                await CloudinaryService.DeleteVideoAsync(session.PublicId);
            }

            // If stored locally, delete physical file
            if (!string.IsNullOrEmpty(session.StoredFileName))
            {
                string relPath  = WebConfigurationManager.AppSettings["VideoUploadPath"].TrimStart('~', '/');
                string filePath = Path.Combine(Server.MapPath("~/" + relPath), session.StoredFileName);
                if (System.IO.File.Exists(filePath))
                    System.IO.File.Delete(filePath);
            }

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

    public class SaveUploadedVideoDto
    {
        public string Title { get; set; }
        public string OriginalFileName { get; set; }
        public string VideoUrl { get; set; }
        public string PublicId { get; set; }
        public double Duration { get; set; }
        public long FileSize { get; set; }
    }
}
