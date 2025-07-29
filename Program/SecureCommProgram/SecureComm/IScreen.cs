using SecureComm.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureComm
{
    public interface IScreen
    {
        Task DrawScreen(ScreenManager screenManager);
    }

}
