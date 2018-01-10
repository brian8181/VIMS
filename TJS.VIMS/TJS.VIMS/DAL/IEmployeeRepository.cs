using System;

namespace TJS.VIMS.DAL
{
    public interface IEmployeeRepository : IDisposable
    {
        Employee GetByNamePass(String userName, String password);
    }
}
