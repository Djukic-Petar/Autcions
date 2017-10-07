using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using IEP_Petar.Models;
using PagedList;
using Microsoft.AspNet.Identity;

namespace IEP_Petar.Controllers
{
    public class AuctionsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private static bool BiddingFailed = false;
        readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // GET: Auctions
        public ActionResult Index(string currentSearch, string currentMin, string currentMax, string currentStatus, string search, string minPrice, string maxPrice, string status, int? page)
        {

            if (BiddingFailed)
            {
                logger.Error("Error");
                ViewBag.BiddingError = "You do not have enough tokens!";
                BiddingFailed = false;
            }
            else
                ViewBag.BiddingError = "";

            if (search != null || minPrice != null || maxPrice != null || status != null)
            {
                page = 1;
            }
            else
            {
                search = currentSearch;
                minPrice = currentMin;
                maxPrice = currentMax;
                status = currentStatus;
            }

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentMin = minPrice;
            ViewBag.CurrentMax = maxPrice;
            ViewBag.CurrentStatus = status;
            ViewBag.Pagination = true;

            var allAuctions = from a in db.Auctions select a;
            //Filtriramo "obrisane" aukcije
            allAuctions = allAuctions.Where(a => a.State != 6);

            if (!User.IsInRole("Admin"))
            {
                //Korisnik ne vidi aukcije koje su ready ili draft.
                allAuctions = allAuctions.Where(a => a.State != Auction.READY && a.State != Auction.DRAFT);
            }

            int pageSize = 12;
            int pageNumber = (page ?? 1);

            if (String.IsNullOrEmpty(search) && String.IsNullOrEmpty(minPrice) && String.IsNullOrEmpty(maxPrice) && String.IsNullOrEmpty(status))
            {
                ViewBag.Pagination = false;
                if (User.IsInRole("Admin"))
                {
                    allAuctions = allAuctions.Where(a => a.State == Auction.READY);
                    return View(allAuctions.OrderByDescending(p => p.CreationTime).Take(Math.Min(12, allAuctions.Count())).ToPagedList(pageNumber, pageSize));
                }
                allAuctions = allAuctions.Where(a => a.State == Auction.OPEN);
                return View(allAuctions.OrderByDescending(p => p.OpeningTime).Take(Math.Min(12, allAuctions.Count())).ToPagedList(pageNumber, pageSize));
            }

            if (!String.IsNullOrEmpty(search))
            {
                String[] searchStrings = search.Split(' ');
                foreach (string ss in searchStrings)
                {
                    allAuctions = allAuctions.Where(s => s.ProductName.Contains(ss));
                }
            }

            if (!String.IsNullOrEmpty(minPrice))
            {
                int min = Int32.Parse(minPrice);
                allAuctions = allAuctions.Where(s => s.StartPrice >= min);
            }

            if (!String.IsNullOrEmpty(maxPrice))
            {
                int max = Int32.Parse(maxPrice);
                allAuctions = allAuctions.Where(s => s.StartPrice <= max);
            }

            if (!String.IsNullOrEmpty(status))
            {
                int state = 0;
                if ("DRAFT".Equals(status)) state = Auction.DRAFT;
                if ("READY".Equals(status)) state = Auction.READY;
                if ("OPEN".Equals(status)) state = Auction.OPEN;
                if ("SOLD".Equals(status)) state = Auction.SOLD;
                if ("EXPIRED".Equals(status)) state = Auction.EXPIRED;
                if (state != 0) allAuctions = allAuctions.Where(s => s.State == state);
            }

            return View(allAuctions.OrderBy(s => s.CreationTime).ToPagedList(pageNumber, pageSize));
        }

        // GET: Auctions/Details/5
        public ActionResult Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Auction auction = db.Auctions.Find(id);
            if (auction == null)
            {
                return HttpNotFound();
            }
            return View(auction);
        }

        // GET: Auctions/Create
        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            //ViewBag.ApplicationUserID = new SelectList(db.ApplicationUsers, "Id", "Name");
            return View();
        }

        // POST: Auctions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Create([Bind(Include = "ProductName,Duration,StartPrice,ImageToUpload")] Auction auction)
        {
            if (ModelState.IsValid)
            {
                auction.OpeningTime = auction.ClosingTime = auction.CreationTime = DateTime.Now;
                auction.RowVersion = BitConverter.GetBytes(DateTime.Now.ToBinary());
                auction.State = Auction.DRAFT;
                auction.CurrentPrice = auction.StartPrice;
                if (auction.ImageToUpload == null)
                {
                    logger.Error("Error");
                    ViewBag.slika = "Please upload a photo!";
                    return View(auction);
                }
                auction.Photo = new byte[auction.ImageToUpload.ContentLength];
                auction.ImageToUpload.InputStream.Read(auction.Photo, 0, auction.Photo.Length);
                db.Auctions.Add(auction);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            //ViewBag.ApplicationUserID = new SelectList(db.ApplicationUsers, "Id", "Name", auction.ApplicationUserID);
            return View(auction);
        }

        // GET: Auctions/Edit/5
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Auction auction = db.Auctions.Find(id);
            if (auction == null)
            {
                return HttpNotFound();
            }
            //ViewBag.ApplicationUserID = new SelectList(db.ApplicationUsers, "Id", "Name", auction.ApplicationUserID);
            return View(auction);
        }

        // POST: Auctions/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit([Bind(Include = "AuctionId,ProductName,Duration,StartPrice,ImageToUpload,State")] Auction auction)
        {
            if (ModelState.IsValid)
            {
                Auction toEdit = db.Auctions.Find(auction.AuctionId);
                if(toEdit == null)
                {
                    logger.Error("Auction not found");
                    return RedirectToAction("Index");
                }
                toEdit.ProductName = auction.ProductName;
                toEdit.Duration = auction.Duration;
                toEdit.StartPrice = auction.StartPrice;
                toEdit.State = auction.State;
                if(auction.ImageToUpload != null)
                {
                    toEdit.Photo = new byte[auction.ImageToUpload.ContentLength];
                    auction.ImageToUpload.InputStream.Read(toEdit.Photo, 0, toEdit.Photo.Length);
                }
                db.Entry(toEdit).State = EntityState.Modified;
                db.SaveChanges();
            }
            //ViewBag.ApplicationUserID = new SelectList(db.ApplicationUsers, "Id", "Name", auction.ApplicationUserID);
            return RedirectToAction("Index");
        }

        //GET: Auctions/Open/5
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult Open(long ?id)
        {
            if (id == null)
            {
                logger.Equals("No ID in auction opening");
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Auction aukcija = db.Auctions.Find(id);
            if (aukcija == null)
            {
                logger.Error("Invalid ID in auction opening");
                return HttpNotFound();
            }
            if (aukcija.State != Auction.READY)
            {
                logger.Error("Tried opening an auction that's not ready");
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            aukcija.OpeningTime = DateTime.Now;
            aukcija.ClosingTime = aukcija.OpeningTime.AddSeconds(aukcija.Duration);
            aukcija.State = Auction.OPEN;
            db.Entry(aukcija).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Auctions/Delete/5
        public ActionResult Delete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Auction auction = db.Auctions.Find(id);
            if (auction == null)
            {
                return HttpNotFound();
            }
            return View(auction);
        }

        // POST: Auctions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(long id)
        {
            Auction auction = db.Auctions.Find(id);
            db.Auctions.Remove(auction);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        [Authorize]
        public ActionResult List(int? page)
        {

            int pageNumber = (page ?? 1);

            ApplicationUser user = db.Users.Find(User.Identity.GetUserId());
            List<Auction> aukcije = new List<Auction>();
            foreach (Auction a in user.Auctions)
            {
                if (a.State == 4)
                    aukcije.Add(a);
            }

            ViewData["paginacija"] = aukcije.Count > 10;

            return View(aukcije.ToPagedList(pageNumber, 10));
        }
    }
}
