using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TJS.VIMS.Models;
using TJS.VIMS.DAL;
using Microsoft.AspNet.Identity;

namespace TJS.VIMS.Controllers
{
    public class AdministrationController : Controller
    {
        //AdministrationRepository repo = new AdministrationRepository(new VIMSDBContext());
        // need to dispose !

        [HttpGet]
        public ActionResult Index()
        {

            string id = User.Identity.GetUserId();

            // BKP testing !
            //"select * from Employee join VIMSAuthDB.dbo.AspNetUsers on VIMSAuthDB.dbo.AspNetUsers.Id = Employee.AspNetUsers_Id"
            //VIMSDBContext db = new VIMSDBContext();
            //db.Employees.Where(e => e.AspNetUser == id);

            return View();
        }

        [HttpGet]
        public ActionResult CreateOrganization()
        {
            return View();
        }

        [HttpPost]  
        public ActionResult CreateOrganization(Organization organization)
        {
            if (ModelState.IsValid)
            {
                using (AdministrationRepository repo = new AdministrationRepository())
                {
                    string asp_id = User.Identity.GetUserId();
                    Employee e = repo.GetEmployeeByAspId(asp_id);

                    organization.CreatedBy = e.Id;
                    bool success = repo.CreateOrganization(organization);
                    repo.Save();
                    if (success) return View("CreateOrganizationConfirmation");
                }
            }
            return View("Error");
        }

        [HttpGet] 
        public ActionResult EditOrganization(long id)
        {
            using (VIMSDBContext context = new VIMSDBContext())
            {
                Organization organization = context.Organizations.Find(id);
                if (organization != null && (bool)organization.Active) // BKP fix should not be nullable
                {
                    //TempData["admin_id"] = admin_id; // todo validate
                    return View("EditOrganization", organization);
                }
            }
            return View("Error");
        }

        [HttpPost]
        public ActionResult EditOrganization(Organization organization)
        {
            if (ModelState.IsValid)
            {
                AdministrationRepository repo = new AdministrationRepository();
                Organization organization_saved = repo.FindOrganization(organization.Id).ShallowCopy();
                //organization.UpdatedBy = (long)TempData["admin_id"];
                bool success = repo.UpdateOrganization(organization);
                if(success)
                    return RedirectToAction("EditOrganizationConfirmation", organization_saved);
            }
            return View("Error");
        }

        [HttpGet]
        public ActionResult EditOrganizationConfirmation(Organization organization)
        {
            return View("EditOrganizationConfirmation", organization);
        }

        [HttpPost]
        public ActionResult UndoEditOrganization(Organization organization)
        {
            if (ModelState.IsValid)
            {
                using (VIMSDBContext context = new VIMSDBContext())
                {
                    Organization current_organization = context.Organizations.Find(organization.Id);
                    if (organization != null && (bool)organization.Active) // BKP fix should not be nullable
                    {
                        int count = context.Organizations.
                            Where(m => m.Name == organization.Name && m.Id != organization.Id).
                            Count();

                        if (count == 0)
                        {
                            context.Entry(current_organization).CurrentValues.SetValues(organization);
                            context.SaveChanges();
                            return View("UndoEditOrganizationConfirmation");
                        }
                    }
                }
            }
            return View("Error");
        }

        [HttpGet]
        public ActionResult DeleteOrganization(long id)
        {
            using (AdministrationRepository repo = new AdministrationRepository())
            {
                if (repo.DeleteOrganization(id))
                {
                    repo.Save();
                    return View("DeleteOrganizationConfirmation");
                }
            }
            return View("Error");
        }

        [HttpGet]
        public ActionResult UndoDeleteOrganization(long id)
        {
            using (VIMSDBContext context = new VIMSDBContext())
            {
                Organization organization = context.Organizations.Find(id);
                if (organization != null && !(bool)organization.Active)
                {
                    organization.Active = true;
                    //organization.UpdatedBy = admin_id;
                    organization.UpdatedDt = DateTime.Now;
                    context.SaveChanges();
                    return View("UndoDeleteOrganizationConfirmation");
                }
            }
            return View("Error");
        }
        
        [HttpGet] 
        public ActionResult CreateEmployee()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CreateEmployee(Employee employee)
        {
            if (ModelState.IsValid)
            {
                using (AdministrationRepository repo = new AdministrationRepository())
                { 
                    if(repo.EmployeeUserNameExist(employee.UserName))
                    {
                        // todo user name not unique view()
                    }
                    if(repo.CreateEmployee(employee))
                        View("CreateEmployeeConfirmation");
                }
            }
            return View("Error");
        }

        [HttpGet]
        public ActionResult EditEmployee(long id)
        {
            using (VIMSDBContext context = new VIMSDBContext())
            {
                Employee employee = context.Employees.Find(id);
                if (employee != null)
                {
                    //ViewBag.AdminId = admin_id;
                    return View("EditEmployee", employee);
                }
            }
            return View("Error");
        }

        [HttpPost]
        public ActionResult EditEmployee(Employee employee)
        {
            if (ModelState.IsValid)
            {
                using (VIMSDBContext context = new VIMSDBContext())
                {
                    Employee current_employee = context.Employees.Find(employee.Id);
                    if (employee != null && (bool)employee.Active) // BKP fix should not be nullable
                    {
                        int count = context.Employees.
                            Where(m => m.UserName == employee.UserName && m.Id != employee.Id).
                            Count();

                        if (count == 0)
                        {
                            //employee.UpdatedBy = admin_id;
                            employee.UpdatedDt = System.DateTime.Now;
                            context.Entry(current_employee).CurrentValues.SetValues(employee);
                            context.SaveChanges();
                            return View("EditemployeeConfirmation", employee);
                        }
                    }
                }
            }
            return View("Error");
        }

        //[HttpPost]
        public ActionResult UndoEditEmployee(Employee employee)
        {
            if (ModelState.IsValid)
            {
                using (VIMSDBContext context = new VIMSDBContext())
                {
                    employee.Active = true;
                    employee.CreatedBy = 0;
                    employee.CreatedDt = System.DateTime.Now;
                    context.Employees.Add(employee);
                    context.SaveChanges();
                }
                return View("UndoEditEmployeeConfirmation", employee);
            }
            return View("Error");
        }

        [HttpGet]
        public ActionResult DeleteEmployee(long id)
        {
            using (VIMSDBContext context = new VIMSDBContext())
            {
                Employee employee = context.Employees.Find(id);
                if (employee != null && employee.Active != false)
                {
                    //ViewBag.AdminId = admin_id;
                    ViewBag.Id = id;
                    employee.Active = false; // just set to not active
                    //employee.UpdatedBy = admin_id;  
                    employee.UpdatedDt = System.DateTime.Now;
                    context.SaveChanges();
                    return View("DeleteEmployeeConfirmation", employee);
                }
                return View("Error");
            }
        }

        [HttpGet]
        public ActionResult UndoDeleteEmployee(long id)
        {
            using (VIMSDBContext context = new VIMSDBContext())
            {
                Employee employee = context.Employees.Find(id);
                if (employee != null && employee.Active != true)
                {
                    employee.Active = true; // just set to active
                    //employee.UpdatedBy = admin_id;  
                    employee.UpdatedDt = System.DateTime.Now;
                    context.SaveChanges();
                    return View("UndoDeleteEmployeeConfirmation");
                }
            }
            return View("Error");
        }

        //todo...

        [HttpGet]
        public ActionResult VolunteerInformation(long id)
        {
            using (VIMSDBContext context = new VIMSDBContext())
            {
                Volunteer volunteer = context.Volunteers.Find(id);
                if(volunteer != null)
                    return View("VolunteerInformation", volunteer);
            }
            return View("Error");
        }

        [HttpPost]
        public ActionResult VolunteerInformation(Volunteer volunteer)
        {
            if (ModelState.IsValid)
            {
                using (VIMSDBContext context = new VIMSDBContext())
                {
                    Volunteer info = context.Volunteers.Find(volunteer.Id);
                    if (info != null)
                    {
                        context.Entry(info).CurrentValues.SetValues(volunteer);
                        context.SaveChanges();
                        return View("VolunteerInformationConfirmation");
                    }
                }
            }
            return View("Error");
        }

        /// <summary>
        /// time clock activity
        /// </summary>
        /// <returns></returns>
        public ActionResult TimeClockActivity()
        {
            // todo
            return View();
        }

        /// <summary>
        /// activity of single volunteer
        /// </summary>
        /// <param name="volunteer"></param>
        /// <returns></returns>
        public ActionResult TimeClockActivity(Volunteer volunteer)
        {
            // todo
            return View();
        }

        public ActionResult NoShows(Volunteer volunteer)
        {
            // todo
            return View();
        }

        public ActionResult VolunteersReport()
        {
            // todo
            return View();
        }

        public ActionResult ForgotPassword()
        {
            //todo
            return View();
        }
        
        public ActionResult TestView()
        {
            // todo
            return View();
        }
    }
}