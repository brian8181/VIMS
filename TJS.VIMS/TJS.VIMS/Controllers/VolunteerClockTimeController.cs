using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Web.Mvc;
using TJS.VIMS.DAL;
using TJS.VIMS.Models;
using TJS.VIMS.ViewModel;
using System.Linq;


namespace TJS.VIMS.Controllers
{
    //[Authorize]
    public class VolunteerClockTimeController : Controller
    {
        private IEmployeeRepository employeeRepository;
        private ILookUpRepository lookUpRepository;
        private IVolunteerInfoRepository volunteerInfoRepository;

        public VolunteerClockTimeController(IEmployeeRepository employeeRepository, ILookUpRepository lookUpRepository, IVolunteerInfoRepository volunteerInfoRepository)
        {
            this.employeeRepository = employeeRepository;
            this.lookUpRepository = lookUpRepository;
            this.volunteerInfoRepository = volunteerInfoRepository;
        }

        public VolunteerClockTimeController()
        {
        }

        /// <summary>
        /// VolunteerLookUp: opens the volunteer lookup view
        /// </summary>
        /// <param name="id">a location id</param>
        /// <returns>an ActionResult</returns>
        [HttpGet]
        //[Authorize]
        public ActionResult VolunteerLookUp(int locationId)
        {
            Location location = lookUpRepository.GetLocationById(locationId);
            VolunteerLookUpViewModel vm = new VolunteerLookUpViewModel();
            vm.LocationId = (int)location.Id;
            vm.LocationName = location.Name;

            return View(vm);
        }
           
        /// <summary>
        /// VolunteerLookUpNext: volunteer pressed next 
        /// </summary>
        /// <param name="model">TimeClockInViewModel</param>
        /// <returns>ActionResult</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VolunteerLookUp(int locationId, string userName)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Login", "AccountController");
            }

            Volunteer volunteer = volunteerInfoRepository.GetVolunteer(userName);
                      
            if (volunteer != null && volunteer.Id > 0)
            {
                ViewBag.VolunteerId = volunteer.Id;
                ViewBag.LocationId = locationId;
                ViewBag.UserName = userName;
                ViewBag.VolunteerId = volunteer.Id;

                return View("VolunteerPhotoCapture");
            }

            return RedirectToAction("VolunteerLookUp", "VolunteerClockTime", new { locationId = locationId });
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="locationId"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        //[HttpGet]
        public ActionResult VolunteerClockIn(int locationId, string userName)
        {
            Volunteer volunteer = volunteerInfoRepository.GetVolunteer(userName);
            Location location = lookUpRepository.GetLocationById((int)locationId);

            if (volunteer != null && volunteer.Id > 0)
            {
                VolunteerProfile profile =
                   volunteerInfoRepository.GetLastProfileInfo(volunteer.Id);
                VolunteerTimeClock clockInfo = volunteerInfoRepository.GetClockedInInfo(volunteer);

                //todo move to a view model?
                ViewBag.isClockedIn = (clockInfo != null); // clocked in
                ViewBag.Case = profile != null ? profile.CaseNumber : "NA";

                TempData["VolunteerInfo"] = volunteer;
                TempData["Location"] = location;

                VolunteerClockInViewModel vm = new VolunteerClockInViewModel();
                vm.Volunteer = volunteer;
                vm.LocationId = (int)location.Id;
                vm.LocationName = location.Name;
                VolunteerPhoto photo = volunteerInfoRepository.GetLastPhotoInfo(volunteer);
                if(photo != null)
                    vm.DefaultPhotoPath = volunteerInfoRepository.GetLastPhotoInfo(volunteer).Path;

                //dispose now, new context will be created in next request 
                volunteerInfoRepository.Dispose();

                return View(vm);
            }

            return RedirectToAction("VolunteerLookUp", "VolunteerClockTime", new { locationId = location.Id });
        }

        /// <summary>
        /// VolunteerClockedInOut: volunteer punched clock
        /// </summary>
        /// <returns>a clocked in or out view</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public ActionResult VolunteerClockedInOut(int locationId, string userName)
        public ActionResult VolunteerClockedInOut()
        {
            VIMSDBContext context = ((Repository<Volunteer>)volunteerInfoRepository).Context;
            Volunteer volunteer = (Volunteer)TempData["VolunteerInfo"];
            Location location = ((Location)TempData["Location"]);
            
            context.Entry(volunteer).State = EntityState.Modified; // reload after request

            VolunteerTimeClock clockInfo = volunteerInfoRepository.GetClockedInInfo(volunteer);
            VolunteerPhoto photo = volunteerInfoRepository.GetLastPhotoInfo(volunteer);
            VolunteerProfile profile = volunteerInfoRepository.GetDefaultProfileInfo(volunteer.Id);
            
            if (clockInfo != null)
            {
                // was clocked in so clock out
                clockInfo.ClockOut = DateTime.Now;
                clockInfo.ClockOutPhoto =
                    photo != null ? photo.Path : null;
                context.SaveChanges(); //BKP todo, merge with repo code
            }
            else
            {
                // clock in 
                VolunteerTimeClock clockIn = new VolunteerTimeClock();
                clockIn.VolunteerProfile = profile;
                clockIn.LocationId = location.Id;
                clockIn.ClockIn = DateTime.Now;
                clockIn.LocationId = ViewBag.LocationId;
                clockIn.ClockInPhoto =
                    photo != null ? photo.Path : null;
                clockIn.CreatedBy = null; 
                clockIn.CreatedDt = DateTime.Now;
                volunteer.VolunteerTimeClocks.Add(clockIn);
                context.SaveChanges(); //BKP todo, merge with repo code
            }
                
            // build model for view
            VolunteerClockedInOutViewModel model = new VolunteerClockedInOutViewModel();
            model.isClockedIn = (clockInfo == null); // now!, clocked in
            model.Volunteer = volunteer;
            model.LocationId = (int)location.Id;
            model.LocationName = location.Name; 
            model.TimeLogged = VolunteerClockedInOutViewModel.GetHoursLogged(volunteerInfoRepository.GetVolunteersCompletedInOutInfos(volunteer));
            model.RecentClockInformation = volunteerInfoRepository.GetVolunteersRecentClockInOutInfos(volunteer, Utility.Globals.RECENT_LIST_LEN);
            model.CaseNumber = profile != null ? profile.CaseNumber : "NA";
            model.TimeNeeded = profile != null ? new TimeSpan((short)profile.Volunteer_Hours_Needed, 0, 0) : new TimeSpan(0, 0, 0);

            return View(model);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="locationId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public ActionResult VolunteerAllReadyClockedIn(int locationId, int userId)
        {
            // build model for view
            VolunteerAllReadyClockedInViewModel model = new VolunteerAllReadyClockedInViewModel();
            Location location = lookUpRepository.GetLocationById(locationId);
            model.LocationId = locationId;
            model.LocationName = location.Name;
            model.UserId = userId;
            model.OrganizationName = lookUpRepository.GetOrganizationById((int)location.OrganizationId).Name;

            return View(model);
        }

        public ActionResult VolunteerPhotoCaptured()
        {
            //BKP todo 
            List<SelectListItem> items = new List<SelectListItem>();
            items.Add(new SelectListItem { Text = "MyId1", Value = "MyId1", Selected = true });
            items.Add(new SelectListItem { Text = "MyId2", Value = "MyId2" });
            ViewBag.IdList = items;
            return View();
        }

        [HttpGet]
        public ActionResult VolunteerEditProfile(Volunteer volunteer, int locationId)
        {
            ViewBag.LocationId = locationId;
            var locations = lookUpRepository.GetLocations();
            var organizations = lookUpRepository.GetOrganizations();
            VolunteerViewModel model = new VolunteerViewModel(locations, organizations);
            model.Volunteer = volunteer;
            return View("EditVolunteerProfile", model);
        }

        [HttpPost]
        public ActionResult VolunteerEditProfile(Volunteer volunteer, VolunteerProfile profile, int locationId)
        {
           return View();
        }

        /// <summary>
        /// Capture: capture action for webcam 
        /// </summary>
        /// <param name="userId">user id</param>
        [HttpPost]
        public ActionResult Capture(int userId)
        {
            var stream = Request.InputStream;
            string dump = string.Empty;

            using (var reader = new StreamReader(stream))
                dump = reader.ReadToEnd();

            //create a unique name save to ViewData
            string name = Guid.NewGuid().ToString("N") + ".jpg"; //BKP can I be sure is always a jpeg?

            var path = Server.MapPath("~/Capture/" + name);
            System.IO.File.WriteAllBytes(path, Utility.Utility.HexToBytes(dump));

            DbContext context = ((Repository<Volunteer>)volunteerInfoRepository).Context;
            Volunteer volunteer = volunteerInfoRepository.GetVolunteerById(userId);
            VolunteerPhoto photo = new VolunteerPhoto();
            photo.Path = name;
            photo.CreatedDt = DateTime.Now;

            volunteer.VolunteerPhotoes.Add(photo);
            context.SaveChanges(); //BKP todo, merge with repo code

            return RedirectToAction("VolunteerClockIn");
        }
    }
}