using Ganss.Xss;
using IHeard.Data;
using IHeard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Linq;

namespace IHeard.Controllers
{
    [Authorize]
    public class NewssController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public NewssController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;
        }


        // Se afiseaza lista tuturor articolelor impreuna cu categoria 
        // din care fac parte dar
        // Pentru fiecare articol se afiseaza si userul care a postat articolul respectiv
        // HttpGet implicit
        
        
        
        [Authorize(Roles = "User,Admin")]
        public IActionResult Index()
        {
            var newss = db.Newss.Include("Category")
                                      .Include("User").OrderBy(a => a.NewsDate);

            var search = "";

            // MOTOR DE CAUTARE

            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim(); // eliminam spatiile libere 

                // Cautare in articol (Title si Content)

                List<int> newsIds = db.Newss.Where
                                        (
                                         at => at.NewsTitle.Contains(search)
                                         || at.NewsContent.Contains(search)
                                        ).Select(a => a.NewsId).ToList();

                // Cautare in comentarii (Content)
                List<int> newsIdsOfCommentsWithSearchString = db.Comments
                                        .Where
                                        (
                                         c => c.CommentContent.Contains(search)

                                        ).Select(c => (int)c.NewsId).ToList();

                // Se formeaza o singura lista formata din toate id-urile selectate anterior
                List<int> mergedIds = newsIds.Union(newsIdsOfCommentsWithSearchString).ToList();


                // Lista articolelor care contin cuvantul cautat
                // fie in articol -> Title si Content
                // fie in comentarii -> Content


                newss = db.Newss.Where(newss => mergedIds.Contains(newss.NewsId))
                                      .Include("Category")
                                      .Include("User")
                                      .OrderBy(a => a.NewsDate);

            }

            ViewBag.SearchString = search;

            // AFISARE PAGINATA

            // Alegem sa afisam 3 articole pe pagina
            int _perPage = 3;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.message = TempData["message"].ToString();
            }


            // Fiind un numar variabil de articole, verificam de fiecare data utilizand 
            // metoda Count()

            int totalItems = newss.Count();


            // Se preia pagina curenta din View-ul asociat
            // Numarul paginii este valoarea parametrului page din ruta
            // /Articles/Index?page=valoare

            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);

            // Pentru prima pagina offsetul o sa fie zero
            // Pentru pagina 2 o sa fie 3 
            // Asadar offsetul este egal cu numarul de articole care au fost deja afisate pe paginile anterioare
            var offset = 0;

            // Se calculeaza offsetul in functie de numarul paginii la care suntem
            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * _perPage;
            }

            // Se preiau articolele corespunzatoare pentru fiecare pagina la care ne aflam 
            // in functie de offset
            var paginatedNewss = newss.Skip(offset).Take(_perPage);


            // Preluam numarul ultimei pagini

            ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);

            // Trimitem articolele cu ajutorul unui ViewBag catre View-ul corespunzator
            ViewBag.Newss = paginatedNewss;

            if (search != "")
            {
                ViewBag.PaginationBaseUrl = "/Newss/Index/?search=" + search + "&page";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/Newss/Index/?page";
            }


            return View();
        }


        // Se afiseaza un singur articol in functie de id-ul sau 
        // impreuna cu categoria din care face parte
        // In plus sunt preluate si toate comentariile asociate unui articol
        // Se afiseaza si userul care a postat articolul respectiv
        // HttpGet implicit

        //[Authorize(Roles = "User,Admin")]
        public IActionResult Show(int id)
        {
            News news = db.Newss.Include("Category")
                                         .Include("User")
                                         .Include("Comments")
                                         .Include("Comments.User")
                                         .Where(art => art.NewsId == id)
                                         .First();

            // Adaugam bookmark-urile utilizatorului pentru dropdown
            
            ViewBag.UserBookmarks = db.Bookmarks
                                      .Where(b => b.UserId == _userManager.GetUserId(User))
                                      .ToList();


            SetAccessRights();

            return View(news);
        }


        // Adaugarea unui comentariu asociat unui articol in baza de date
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Show([FromForm] Comment comment)
        {
            comment.CommentDate = DateTime.Now;
            comment.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Comments.Add(comment);
                db.SaveChanges();
                return Redirect("/Newss/Show/" + comment.NewsId);
            }

            else
            {
                News art = db.Newss.Include("Category")
                                         .Include("User")
                                         .Include("Comments")
                                         .Include("Comments.User")
                                         .Where(art => art.NewsId == comment.NewsId)
                                         .First();


                // Adaugam bookmark-urile utilizatorului pentru dropdown
                ViewBag.UserBookmarks = db.Bookmarks
                                          .Where(b => b.UserId == _userManager.GetUserId(User))
                                          .ToList();

                SetAccessRights();

                return View(art);
            }
        }

        [HttpPost]
        public IActionResult AddBookmark([FromForm] NewsBookmark newsBookmark)
        {
            // Daca modelul este valid
            if (ModelState.IsValid)
            {
                // Verificam daca avem deja articolul in colectie
                if (db.NewsBookmarks
                    .Where(ab => ab.NewsId == newsBookmark.NewsId)
                    .Where(ab => ab.BookmarkId == newsBookmark.BookmarkId)
                    .Count() > 0)
                {
                    TempData["message"] = "Aceasta stire este deja adaugata in colectie";
                    TempData["messageType"] = "alert-danger";
                }
                else
                {
                    // Adaugam asocierea intre articol si bookmark 
                    db.NewsBookmarks.Add(newsBookmark);
                    // Salvam modificarile
                    db.SaveChanges();

                    // Adaugam un mesaj de success
                    TempData["message"] = "Stirea a fost adaugata in colectia selectata";
                    TempData["messageType"] = "alert-success";
                }

            }
            else
            {
                TempData["message"] = "Nu s-a putut adauga stirea in colectie";
                TempData["messageType"] = "alert-danger";
            }

            // Ne intoarcem la pagina articolului
            return Redirect("/Newss/Show/" + newsBookmark.NewsId);
        }


        // Se afiseaza formularul in care se vor completa datele unui articol
        // impreuna cu selectarea categoriei din care face parte
        // Doar utilizatorii cu rolul de Editor sau Admin pot adauga articole in platforma
        // HttpGet implicit

        [Authorize(Roles = "User,Admin")]
        public IActionResult New()
        {
            News news = new News();

            // Se preia lista de categorii din metoda GetAllCategories()
            news.Categ = GetAllCategories();


            return View(news);
        }

        // Se adauga articolul in baza de date
        // Doar utilizatorii cu rolul de Editor sau Admin pot adauga articole in platforma

        [Authorize(Roles = "User,Admin")]
        [HttpPost]

        public IActionResult New(News news)
        {
            var sanitizer = new HtmlSanitizer();

            news.NewsDate = DateTime.Now;
            news.UserId = _userManager.GetUserId(User);


            if (ModelState.IsValid)
            {
                news.NewsContent = sanitizer.Sanitize(news.NewsContent);

                db.Newss.Add(news);
                db.SaveChanges();
                TempData["message"] = "Stirea a fost adaugata";
                return RedirectToAction("Index");
            }
            else
            {
                news.Categ = GetAllCategories();
                return View(news);
            }
        }

        // Se editeaza un articol existent in baza de date impreuna cu categoria
        // din care face parte
        // Categoria se selecteaza dintr-un dropdown
        // HttpGet implicit
        // Se afiseaza formularul impreuna cu datele aferente articolului
        // din baza de date
        [Authorize(Roles = "User,Admin")]
        public IActionResult Edit(int id)
        {

            News news = db.Newss.Include("Category")
                                        .Where(art => art.NewsId == id)
                                        .First();

            news.Categ = GetAllCategories();

            if (news.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                return View(news);
            }

            else
            {
                TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unei stiri care nu va apartine";
                return RedirectToAction("Index");
            }

        }

        // Se adauga articolul modificat in baza de date
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult Edit(int id, News requestNews)
        {
            var sanitizer = new HtmlSanitizer();

            News news = db.Newss.Find(id);


            if (ModelState.IsValid)
            {
                if (news.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
                {
                    news.NewsTitle = requestNews.NewsTitle;

                    requestNews.NewsContent = sanitizer.Sanitize(requestNews.NewsContent);

                    news.NewsContent = requestNews.NewsContent;

                    news.CategoryId = requestNews.CategoryId;
                    TempData["message"] = "Atirea a fost modificata";
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unei stiri care nu va apartine";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                requestNews.Categ = GetAllCategories();
                return View(requestNews);
            }
        }


        // Se sterge un articol din baza de date 
        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public ActionResult Delete(int id)
        {
            News news = db.Newss.Include("Comments")
                                         .Where(art => art.NewsId == id)
                                         .First();

            if (news.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                db.Newss.Remove(news);
                db.SaveChanges();
                TempData["message"] = "Stirea a fost stearsa";
                return RedirectToAction("Index");
            }

            else
            {
                TempData["message"] = "Nu aveti dreptul sa stergeti o stire care nu va apartine";
                return RedirectToAction("Index");
            }
        }

        [NonAction]
        public IEnumerable<SelectListItem> GetAllCategories()
        {
            // generam o lista de tipul SelectListItem fara elemente
            var selectList = new List<SelectListItem>();

            // extragem toate categoriile din baza de date
            var categories = from cat in db.Categories
                             select cat;

            // iteram prin categorii
            foreach (var category in categories)
            {
                // adaugam in lista elementele necesare pentru dropdown
                // id-ul categoriei si denumirea acesteia
                selectList.Add(new SelectListItem
                {
                    Value = category.CategoryId.ToString(),
                    Text = category.CategoryName.ToString()
                });
            }
            /* Sau se poate implementa astfel: 
             * 
            foreach (var category in categories)
            {
                var listItem = new SelectListItem();
                listItem.Value = category.Id.ToString();
                listItem.Text = category.CategoryName.ToString();

                selectList.Add(listItem);
             }*/


            // returnam lista de categorii
            return selectList;
        }

        // Metoda utilizata pentru exemplificarea Layout-ului
        // Am adaugat un nou Layout in Views -> Shared -> numit _LayoutNou.cshtml
        // Aceasta metoda are un View asociat care utilizeaza noul layout creat
        // in locul celui default generat de framework numit _Layout.cshtml
        public IActionResult IndexNou()
        {
            return View();
        }

        // Conditiile de afisare a butoanelor de editare si stergere
        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            if (User.IsInRole("User"))
            {
                ViewBag.AfisareButoane = true;
            }

            ViewBag.EsteAdmin = User.IsInRole("Admin");

            ViewBag.UserCurent = _userManager.GetUserId(User);
        }
    }
}

