using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TJS.VIMS.DAL;
using TJS.VIMS.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin;
using Owin;
using System.Security.Claims;

namespace TJS.VIMS.Controllers
{
    public class EmployeesController : Controller
    {
        private VIMSDBContext db = new VIMSDBContext();

     

        // GET: Employees
        public ActionResult Index()
        {
            return View(db.Employees.ToList());
        }

        // GET: Employees/Details/5
        public ActionResult Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Employee employee = db.Employees.Find(id);
            if (employee == null || employee.Active == false)
            {
                return HttpNotFound();
            }
            return View(employee);
        }

        // GET: Employees/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Employees/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public ActionResult Create([Bind(Include = "Id,FirstName,LastName,UserName,Password,Admin,Active,CreatedBy,CreatedDt,UpdatedBy,UpdatedDt")] Employee employee)
        {
            if (ModelState.IsValid)
            {
                employee.Active = true;
                EmployeeRepository r = new EmployeeRepository(db);
                string admin_id = User.Identity.GetUserId();
                employee.CreatedBy = r.GetByAspId(admin_id).Id; 
                employee.CreatedDt = DateTime.Now;

                ApplicationDbContext identity_context = new ApplicationDbContext();
                // add to Identity
                var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(identity_context));
                var UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(identity_context));
                var user = new ApplicationUser();
                user.UserName = employee.UserName;
                var result = UserManager.Create(user, employee.Password);
                //Add default User to Role Admin
                if (result.Succeeded)
                {
                    var result1 = UserManager.AddToRole(user.Id, "Employee");
                    var result2 = UserManager.AddToRole(user.Id, "Administrator");

                    employee.AspNetUsers_Id = user.Id;

                    db.Employees.Add(employee);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            //BKP todo error
            return View(employee);
        }

        // GET: Employees/Edit/5
        public ActionResult Edit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Employee employee = db.Employees.Find(id);
            if (employee == null)
            {
                return HttpNotFound();
            }
            return View(employee);
        }

        // POST: Employees/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,FirstName,LastName,UserName,Password,Admin,Active,CreatedBy,CreatedDt,UpdatedBy,UpdatedDt")] Employee employee)
        {
            if (ModelState.IsValid)
            {
                EmployeeRepository r = new EmployeeRepository(db);
                string asp_id = User.Identity.GetUserId();
                employee.UpdatedBy = r.GetByAspId(asp_id).Id;
                employee.CreatedDt = DateTime.Now;
                r.Update(employee);
                r.Save();
                return RedirectToAction("Index");
            }
            return View(employee);
        }

        // GET: Employees/Delete/5
        public ActionResult Delete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Employee employee = db.Employees.Find(id);
            if (employee == null)
            {
                return HttpNotFound();
            }
            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(long id)
        {
            Employee employee = db.Employees.Find(id);
            db.Employees.Remove(employee);
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
    }
}
