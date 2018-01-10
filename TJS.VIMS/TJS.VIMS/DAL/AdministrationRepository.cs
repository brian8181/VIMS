using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TJS.VIMS.DAL
{
    public class AdministrationRepository : IDisposable
    {
        private VIMSDBContext context = new VIMSDBContext();
        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    context.Dispose();
                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Organization FindOrganization(long id)
        {
            return context.Organizations.Find(id);
        }

        public Employee FindEmployee(long id)
        {
            return context.Employees.Find(id);
        }

        public int Save()
        {
            return context.SaveChanges();
        }

        public Employee GetEmployeeByAspId(string asp_id)
        {
            return context.Employees.Where(e => e.AspNetUsers_Id == asp_id).SingleOrDefault();
        }

        public bool EmployeeUserNameExist(string userName)
        {
            return context.Employees.
                    Where(m => m.UserName == userName).Count() != 0;
        }

        public bool CreateEmployee(Employee employee)
        {
            employee.Active = true;
            employee.CreatedDt = DateTime.Now;
            employee.UpdatedBy = null; // reset to null if not already
            employee.UpdatedDt = null; // reset to null if not already
            context.Employees.Add(employee);
            context.SaveChanges();

            return true;
        }

        public bool CreateOrganization(Organization organization)
        {
            int count = context.Organizations.
                     Where(m => m.Name == organization.Name).Count();
            if (count == 0)
            {
                organization.Active = true;
                organization.CreatedDt = DateTime.Now;
                organization.UpdatedBy = null; // reset to null if not already
                organization.UpdatedDt = null; // reset to null if not already
                context.Organizations.Add(organization);
                context.SaveChanges();
                return true;
            }
            return false;
        }

        public bool DeleteEmployee(long id)
        {
            Employee employee = FindEmployee(id);
            if (employee != null && (bool)employee.Active)
            {
                employee.Active = false;
                employee.UpdatedDt = DateTime.Now;
                context.SaveChanges();
                return true;
            }
            return false;
        }

        public bool DeleteOrganization(long id)
        {
            Organization organization = FindOrganization(id);
            if (organization != null && (bool)organization.Active)
            {
                organization.Active = false;
                organization.UpdatedDt = DateTime.Now;
                context.SaveChanges();
                return true;
            }
            return false;
        }

        public bool EditEmployee(Employee employee)
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
                        employee.UpdatedDt = System.DateTime.Now;
                        context.Entry(current_employee).CurrentValues.SetValues(employee);
                        context.SaveChanges();
                        return true;
                    }
                }
            }
            return false;
        }

        public bool UpdateOrganization(Organization organization)
        {
            Organization organization_current = FindOrganization(organization.Id);
            if (organization != null && (bool)organization.Active) // BKP fix should not be nullable
            {
                int count = context.Organizations.
                    Where(m => m.Name == organization.Name && m.Id != organization.Id).
                    Count();

                if (count == 0)
                {
                    //organization.UpdatedBy = admin_id; // todo
                    organization.UpdatedDt = System.DateTime.Now;
                    context.Entry(organization_current).CurrentValues.SetValues(organization);
                    context.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        public bool UnEditOrganization(Organization organization)
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
                        return true;
                    }
                }
            }
            return false;
        }

        public bool UnDeleteEmployee(long admin_id, long employee_id)
        {
            using (VIMSDBContext context = new VIMSDBContext())
            {
                Employee employee = context.Employees.Find(employee_id);
                if (employee != null && employee.Active != true)
                {
                    employee.Active = true; // just set to active
                    employee.UpdatedBy = admin_id;
                    employee.UpdatedDt = System.DateTime.Now;
                    context.SaveChanges();
                    return true;
                }
            }
            return false;
        }

        public bool UnDeleteOrganization(long admin_id, long organization_id)
        {
            using (VIMSDBContext context = new VIMSDBContext())
            {
                Organization organization = context.Organizations.Find(organization_id);
                if (organization != null && organization.Active != true)
                {
                    organization.Active = true; // just set to active
                    organization.UpdatedBy = admin_id;
                    organization.UpdatedDt = System.DateTime.Now;
                    context.SaveChanges();
                    return true;
                }
            }
            return false;
        }
    }
}