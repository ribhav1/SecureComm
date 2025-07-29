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
            string inputUserId = Console.ReadLine();

            while (inputUserId.Length < 3 || inputUserId.Length > 8)
            {
                Console.WriteLine("Does not meet requirements:");
                inputUserId = Console.ReadLine();
            }

            screenManager.Navigate(new RoomScreen(roomId, inputUserId));
            return;
        }

    }
}
