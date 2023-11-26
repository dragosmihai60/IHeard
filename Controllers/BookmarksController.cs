using IHeard.Data;
using IHeard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IHeard.Controllers
{
    [Authorize]
    public class BookmarksController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public BookmarksController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;
        }

        [Authorize(Roles = "User,Admin")]
        // toti utilizatorii pot vedea Bookmark-urile existente in platforma
        // fiecare utilizator vede bookmark-urile pe care le-a creat
        // HttpGet - implicit
        public IActionResult Index()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
            }

            SetAccessRights();

            if (User.IsInRole("User") )
            {
                var bookmarks = from bookmark in db.Bookmarks.Include("User")
                               .Where(b => b.UserId == _userManager.GetUserId(User))
                                select bookmark;

                ViewBag.Bookmarks = bookmarks;

                return View();
            }
            else 
            if(User.IsInRole("Admin"))
            {
                var bookmarks = from bookmark in db.Bookmarks.Include("User")
                                select bookmark;

                ViewBag.Bookmarks = bookmarks;

                return View();
            }

            else
            {
                TempData["message"] = "Nu aveti drepturi";
                return RedirectToAction("Index", "Newss");
            }
            
        }

        // Afisarea tuturor articolelor pe care utilizatorul le-a salvat in 
        // bookmark-ul sau 

        [Authorize(Roles = "User,Admin")]
        public IActionResult Show(int id)
        {
            SetAccessRights();

            if (User.IsInRole("User"))
            {
                var bookmarks = db.Bookmarks
                                  .Include("NewsBookmarks.News.Category")
                                  .Include("NewsBookmarks.News.User")
                                  .Include("User")
                                  .Where(b => b.BookmarkId == id)
                                  .Where(b => b.UserId == _userManager.GetUserId(User))
                                  .FirstOrDefault();
                
                if(bookmarks == null)
                {
                    TempData["message"] = "Nu aveti drepturi";
                    return RedirectToAction("Index", "Newss");
                }

                return View(bookmarks);
            }

            else 
            if (User.IsInRole("Admin"))
            {
                var bookmarks = db.Bookmarks
                                  .Include("NewsBookmarks.News.Category")
                                  .Include("NewsBookmarks.News.User")
                                  .Include("User")
                                  .Where(b => b.BookmarkId == id)
                                  .FirstOrDefault();


                if (bookmarks == null)
                {
                    TempData["message"] = "Resursa cautata nu poate fi gasita";
                    return RedirectToAction("Index", "Newss");
                }


                return View(bookmarks);
            }

            else
            {
                TempData["message"] = "Nu aveti drepturi";
                return RedirectToAction("Index", "Newss");
            }  
        }


        [Authorize(Roles = "User,Admin")]
        public IActionResult New()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public ActionResult New(Bookmark bm)
        {
            bm.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Bookmarks.Add(bm);
                db.SaveChanges();
                TempData["message"] = "Colectia a fost adaugata";
                return RedirectToAction("Index");
            }

            else
            {
                return View(bm);
            }
        }


        // Conditiile de afisare a butoanelor de editare si stergere
        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            if ( User.IsInRole("User") )
            {
                ViewBag.AfisareButoane = true;
            }

            ViewBag.EsteAdmin = User.IsInRole("Admin");

            ViewBag.UserCurent = _userManager.GetUserId(User);
        }
    }
}
