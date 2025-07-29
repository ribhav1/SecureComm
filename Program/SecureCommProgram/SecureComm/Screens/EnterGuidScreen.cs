using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureComm.Screens
{
    public class EnterGuidScreen : IScreen
    {
        private Guid roomId;

        public async Task DrawScreen(ScreenManager screenManager)
        {
            Console.Clear();

            Console.WriteLine("Please enter the ID of the room you would like to connect to:");
            roomId = CheckForValidGUID(Console.ReadLine());

            bool isValidRoom = await ApiClient.ValidateRoomById(roomId);
            while (!isValidRoom)
            {
                Console.WriteLine("No room with given GUID exists. Try again:");
                roomId = CheckForValidGUID(Console.ReadLine());
                isValidRoom = await ApiClient.ValidateRoomById(roomId);
            }

            screenManager.Navigate(new EnterPasswordScreen(roomId));
            return;
        }

        Guid CheckForValidGUID(string userInput)
        {
            Guid roomId;
            string _input = userInput;

            while (!Guid.TryParse(_input, out roomId))
            {
                Console.WriteLine("Invalid GUID. Please enter a valid GUID:");
                _input = Console.ReadLine();
            }

            return roomId;
        }
    }
}
