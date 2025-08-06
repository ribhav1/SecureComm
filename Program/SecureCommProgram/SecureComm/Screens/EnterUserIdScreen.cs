using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureComm.Screens
{
    class EnterUserIdScreen : IScreen
    {
        private Guid roomId = new Guid();

        public EnterUserIdScreen(Guid _roomId)
        {
            roomId = _roomId;
        }

        public async Task DrawScreen(ScreenManager screenManager)
        {
            Console.WriteLine("Please enter a display name.");
            Console.WriteLine("It must be at least 3 characters but no more than 8");
            string inputUsername = Console.ReadLine();

            while (inputUsername.Length < 3 || inputUsername.Length > 8)
            {
                Console.WriteLine("Does not meet requirements:");
                inputUsername = Console.ReadLine();
            }

            screenManager.Navigate(new RoomScreen(roomId, inputUsername, new Guid()));
            return;
        }

    }
}
