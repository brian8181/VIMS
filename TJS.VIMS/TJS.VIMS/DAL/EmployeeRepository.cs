using System;
using System.Data.Entity;
using System.Linq;

namespace TJS.VIMS.DAL
{
    public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
    {
        public EmployeeRepository(VIMSDBContext context) : base(context)
        {
        }

        public Employee GetByNamePass(string userName, string password)
        {
            return SingleOrDefault(m => m.UserName.ToLower() == userName.ToLower() && m.Password == password);
        }
        
        public Employee GetByAspId(string asp_id)
        {
            return context.Employees.Where(e => e.AspNetUsers_Id == asp_id).SingleOrDefault();
        }

        public bool Create(Employee employee)
        {
            int count = Find(m => m.UserName == employee.UserName).Count();
            if (count == 0)
            {
                employee.Active = true;
                employee.CreatedDt = DateTime.Now;
                employee.UpdatedBy = null; // reset to null if not already
                employee.UpdatedDt = null; // reset to null if not already
                Add(employee);
                return true;
            }
            return false;
        }

        public bool Delete(long id)
        {
            Employee employee = Find((int)id); //BKP should be long
            if (employee != null && (bool)employee.Active)
            {
                employee.Active = false;
                employee.UpdatedDt = DateTime.Now;
                return true;
            }
            return false;
        }

        public bool Update(Employee employee)
        {
            Employee current_employee = Find((int)employee.Id);
            if (employee != null && (bool)employee.Active) // BKP fix should not be nullable
            {
                int count = Find(m => m.UserName == employee.UserName && m.Id != employee.Id).Count();
                if (count == 0)
                {
                    employee.UpdatedDt = System.DateTime.Now;
                    context.Entry(employee).State = EntityState.Modified;
                    //context.Entry(current_employee).CurrentValues.SetValues(employee);
                    return true;
                }
            }
            return false;
        }


    }
}