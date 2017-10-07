using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using IEP_Petar.Models;
using Microsoft.AspNet.Identity;
using PagedList;

namespace IEP_Petar.Controllers
{
    public class OrdersController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // GET: Orders
        [Authorize(Roles = "Admin")]
        public ActionResult Index()
        {
            var orders = db.Orders.Include(o => o.ApplicationUser);
            return View(orders.ToList());
        }

        // GET: Orders/Details/5
        [Authorize(Roles = "Admin")]
        public ActionResult Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }
            return View(order);
        }

        // GET: Orders/Create
        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            //ViewBag.ApplicationUserID = new SelectList(db.ApplicationUsers, "Id", "Name");
            return View();
        }

        // POST: Orders/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Create([Bind(Include = "OrderId,ApplicationUserID,Tokens,Price,Status,Time")] Order order)
        {
            if (ModelState.IsValid)
            {
                db.Orders.Add(order);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            //ViewBag.ApplicationUserID = new SelectList(db.ApplicationUsers, "Id", "Name", order.ApplicationUserID);
            return View(order);
        }

        // GET: Orders/Edit/5
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }
            //ViewBag.ApplicationUserID = new SelectList(db.ApplicationUsers, "Id", "Name", order.ApplicationUserID);
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit([Bind(Include = "OrderId,ApplicationUserID,Tokens,Price,Status,Time")] Order order)
        {
            if (ModelState.IsValid)
            {
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            //ViewBag.ApplicationUserID = new SelectList(db.ApplicationUsers, "Id", "Name", order.ApplicationUserID);
            return View(order);
        }

        // GET: Orders/Delete/5
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }
            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteConfirmed(long id)
        {
            Order order = db.Orders.Find(id);
            db.Orders.Remove(order);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize]
        public ActionResult Order()
        {
            Order order = new Models.Order();
            order.Price = 0;
            order.Tokens = 0;
            order.Status = 1;
            order.Time = DateTime.Now;
            order.ApplicationUserID = User.Identity.GetUserId();
            if(ModelState.IsValid)
            {
                db.Orders.Add(order);
                db.SaveChanges();
            }
            else
            {
                logger.Error("Invalid model state");
                return RedirectToAction("Index");
            }
            return Redirect("http://stage.centili.com/widget/WidgetModule?api=3afb1e38398d17bc95dc7fb35c99f6d2&&clientid=" + order.OrderId);
        }

        [HttpPost]
        public HttpStatusCode confirmOrder()
        {
            if(Request.Form["status"] == "success")
            {
                long orderId = long.Parse(Request.Form["clientid"]);
                Order order = db.Orders.Find(orderId);
                order.Tokens = int.Parse(Request.Form["amount"]);
                order.Price = (int)float.Parse(Request.Form["enduserprice"]);
                ApplicationUser user = db.Users.Find(order.ApplicationUserID);
                user.Tokens += order.Tokens;
                order.Status = 2;
                db.Entry(user).State = EntityState.Modified;
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();

            }
            return HttpStatusCode.Accepted;
        }

        [Authorize]
        public ActionResult List(int? page)
        {

            int pageNumber = (page ?? 1);

            ApplicationUser user = db.Users.Find(User.Identity.GetUserId());
            List<Order> orders = new List<Order>();
            foreach (Order o in user.Orders)
            {                 
                orders.Add(o);
            }

            ViewData["paginacija"] = orders.Count > 10;

            return View(orders.ToPagedList(pageNumber, 10));
        }
    

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
