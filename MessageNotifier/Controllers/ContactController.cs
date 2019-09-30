using MessageNotifierLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MessageNotifier.Controllers
{
    public class ContactController : Controller
    {

        private readonly DatabaseContext db;

        public ContactController()
        {
            db = new DatabaseContext();
        }

        public ActionResult Index()
        {
            ViewData["ContactInfo"] = db.ContactInfo.ToList();

            return View();
        }

        public ActionResult Remove(int? id)
        {
            var item = db.ContactInfo.First(c => c.Id == id);

            db.ContactInfo.Remove(item);
            db.SaveChanges();

            return RedirectToAction("Index", "Contact");
        }

        [HttpPost]
        public ActionResult Add(ContactInfo info)
        {

            db.ContactInfo.Add(info);
            db.SaveChanges();

            return RedirectToAction("Index", "Contact");
        }

    }
}