using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Configuration;
using System.Web.Mvc;
using VideoTimelineApp.Data;
using VideoTimelineApp.ViewModels;

namespace VideoTimelineApp.Controllers
{
    public class VideoController : Controller
    {
        private readonly AppDbContext _db = new AppDbContext();

        // GET /Video/Player/5
        public async Task<ActionResult> Player(int id)
        {
            var session = await _db.VideoSessions
                .Include(v => v.Segments)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (session == null) return HttpNotFound();

            string relPath = WebConfigurationManager.AppSettings["VideoUploadPath"]
                                 .Replace("~", string.Empty);
            string videoUrl = relPath + "/" + session.StoredFileName;

            var vm = new PlayerViewModel
            {
                Session  = session,
                VideoUrl = videoUrl
            };

            return View(vm);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
