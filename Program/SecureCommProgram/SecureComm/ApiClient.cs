using SecureCommAPI.Models;
using System.Net.Http.Json;

namespace SecureComm
{
    public class ApiClient
    {
        private static HttpClient client = new()
        {
            BaseAddress = new Uri("https://localhost:7117")
        };

        public static async Task<RoomModel> GetRoomById(Guid id)
        {
            return await client.GetFromJsonAsync<RoomModel>($"/Room/getRoom/{id}");
        }

        public static async Task<bool> ValidateRoomById(Guid id)
        {
            return await client.GetFromJsonAsync<bool>($"/Room/validateRoom/{id}");
        }

        public static async Task<bool> ValidateRoomPassword(Guid id, string password)
        {
            try
            {
                return await client.GetFromJsonAsync<bool>($"/Room/validatePassword/{id}/{password}");
            } 
            catch (Exception e)
            {
                return false;
            }
            
        }

        public static async Task<List<MessageModel>> GetMessages(Guid roomGUID, DateTime lastTime)
        {
            try
            {
                string encodedTime = Uri.EscapeDataString(lastTime.ToString("yyyy-MM-dd HH:mm:ss.ffffffK"));
                List<MessageModel> newMessages = await client.GetFromJsonAsync<List<MessageModel>>($"/Message/getMessages/{roomGUID}/{encodedTime}");
                return newMessages;
            }
            catch (Exception e)
            {
                return new List<MessageModel>();
            }
        }

        public static async Task<MessageModel> SendMessage(Guid room_id, string user_id, string content, string color)
        {
            HttpResponseMessage response = await client.PostAsync($"/Message/send/{room_id}/{user_id}/{content}/{color}", null);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<MessageModel>();
            }
            else
            {
                return null;
            }
        }

    }
}
