using MessageNotifierLibrary.Interface;
using MessageNotifierLibrary.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace MessageNotifier.Controllers
{
    public class HomeController : Controller
    {

        private readonly DatabaseContext db;
        private readonly Credentials senderCredentials;

        public HomeController()
        {
            db = new DatabaseContext();
            senderCredentials = new Credentials
            {
                Mail = new Mail
                {
                    Address = ConfigurationManager.AppSettings["email:address"],
                    Port = int.Parse(ConfigurationManager.AppSettings["email:port"]),
                    Username = ConfigurationManager.AppSettings["email:username"],
                    Password = ConfigurationManager.AppSettings["email:password"],
                    Server = ConfigurationManager.AppSettings["email:server"],
                },
                Telegram = new Telegram
                {
                    Token = ConfigurationManager.AppSettings["tg:token"],
                    Proxy = new WebProxy
                    {
                        Address = new Uri($"{ConfigurationManager.AppSettings["tg:proxyType"]}://{ConfigurationManager.AppSettings["tg:proxyAddress"]}"),
                        Credentials = new NetworkCredential
                        {
                            UserName = ConfigurationManager.AppSettings["tg:proxyUsername"],
                            Password = ConfigurationManager.AppSettings["tg:proxyPassword"]
                        }
                    }
                },
                Sms = new Sms
                {
                    Server = ConfigurationManager.AppSettings["sms:server"],
                    Token = ConfigurationManager.AppSettings["sms:token"]
                }
            };
        }

        public ActionResult Index()
        {
            ViewBag.Users = db.Users.ToList();
            ViewBag.Contacts = db.Contacts.ToList();
            ViewBag.ContactInfo = db.ContactInfo.ToList();

            return View();
        }

        [HttpPost]
        public ActionResult Send(FormCollection form)
        {
            var statusTable = new List<Dictionary<string, string>>();

            var message = new TextMessage
            {
                Title = form["Title"].Replace("\n", ""),
                Content = form["Content"]
            };

            var senders = new List<ISender> {
                new MessageNotifier.Models.EmailSender(),
                new SmsSender(),
                //new TelegramSender()
            };

            var users = db.Users.ToList();

            // Условие: должны дойти _все возможные_ уведомления. Не обрывать из-за отсутствия смс или эмейл-шлюзов, переключать на другие.
            // users.ForEach(u => senders.ForEach(s => s.Send(u, message, senderCredentials)));


            foreach (var sender in senders)
            {
                foreach (var user in users)
                {
                    var success = sender.Send(user, message, senderCredentials);

                    statusTable.Add(new Dictionary<string, string>
                    {
                        ["user"] = user.ToString(),
                        ["service"] = sender.ToString().Split('.').Last(),
                        ["status"] = success ? "sent" : "failed"
                    });

                }
            }

            TempData["Status"] = new Dictionary<string, string>
            {
                ["type"] = "success",
                ["message"] = "Successfully sent!"
            };

            TempData["StatusTable"] = statusTable;


            //db.Dispose();

            TempData.Keep();

            return RedirectToAction("Index");
        }

        public ActionResult Remove(int? id)
        {
            var item = db.Users.First(u => u.Id == id);

            if (item != null)
            {
                db.Users.Remove(item);
                db.SaveChanges();

                TempData["Status"] = new Dictionary<string, string>
                {
                    ["type"] = "info",
                    ["message"] = $"Removed <strong>{item.Name} {item.LastName}</strong> from users"
                };
            }

            TempData.Keep();
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int? id)
        {
            var item = db.Users.First(u => u.Id == id);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult New(FormCollection form)
        {
            var user = new User
            {
                Name = form["Name"],
                LastName = form["LastName"],
                Contacts = new List<Contact>()
            };

            try
            {
                foreach (var key in form.AllKeys.Where(k => k.Contains("contact")))
                {
                    var contactType = key.Split('-')[1].ToLower();
                    var contact = new Contact
                    {
                        ContactInfo = db.ContactInfo.First(c => c.Title.ToLower() == contactType),
                        Value = form[key]
                    };

                    user.Contacts.Add(contact);
                }

                db.Users.Add(user);
                db.SaveChanges();

                TempData["Status"] = new Dictionary<string, string>
                {
                    ["type"] = "info",
                    ["message"] = $"Added <strong>{user.Name} {user.LastName}</strong> to users!"
                };
            }
            catch (Exception ex)
            {
                TempData["Status"] = new Dictionary<string, string>
                {
                    ["type"] = "danger",
                    ["message"] = $"Failed to submit <strong>{user.Name} {user.LastName}</strong>, details:\n{ex.Message}"
                };
            }

            TempData.Keep();
            return RedirectToAction("Index");
        }
    }
}