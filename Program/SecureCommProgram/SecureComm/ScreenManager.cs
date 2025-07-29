using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureComm
{
    public class ScreenManager
    {
        private Stack<IScreen> screens = new();
        private bool isRunning = true;

        public void Navigate(IScreen screen, bool saveCurrentInStack = true)
        {
            if (!saveCurrentInStack)
            {
                screens.Pop();
            }
            Console.Clear();
            screens.Push(screen);
        }

        public void GoBack()
        {
            Console.Clear();
            if (screens.Count > 1)
            {
                screens.Pop();
            }
        }

        public void Exit()
        {
            isRunning = false;
        }

        public async Task ManageScreens()
        {
            while (isRunning && screens.Count > 0)
            {
                IScreen currentScreen = screens.Peek();
                await currentScreen.DrawScreen(this);
            }

            Console.WriteLine("Program Exited");
        }
    }
}
