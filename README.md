# 🔐 SecureComm – End-to-End Encrypted Messaging

A secure, real-time messaging application built with **.NET 9** that combines a **C# Console Client** with an **ASP.NET Core Web API** backend to connected to a Supabase database.  
Messages and session keys are exchanged via **RSA public-private key cryptography**, ensuring confidentiality and integrity across communication channels.

<p align="center">
  <img src="https://img.shields.io/badge/.NET-9.0-purple" />
  <img src="https://img.shields.io/badge/API-ASP.NET_Core-blue" />
  <img src="https://img.shields.io/badge/Encryption-RSA_2048-orange" />
  <img src="https://img.shields.io/badge/License-MIT-yellow" />
</p>

---

## 🧠 Overview

SecureComm is designed for **secure, private chatrooms** where users exchange encrypted messages without exposing plaintext data over the network.  
It supports:
- **Encrypted session key exchange** between host and participants
- **End-to-end encrypted messages** using RSA
- **Console-based UI** with interactive chat room handling
- **API-based room creation, validation, and message persistence**

---

## 📦 Features

- 🔑 **RSA 2048 Encryption** – all messages and keys are encrypted before transmission
- 🗝️ **Secure Session Key Exchange** – clients request keys from the host in segmented encrypted chunks
- 🖥️ **Console Chat Interface** – minimal, responsive interface with real-time message updates
- 🏠 **Room Management** – create, validate, and join rooms with password authentication
- 📨 **Direct Messaging** – ability to send encrypted messages to a specific user
- 🌐 **API Backend** – handles room state, user connections, and message storage

---

## 📁 File Structure

```
SecureComm/
├── API/
│   └── SecureCommAPI/
│       ├── Controllers/
│       │   ├── MessageController.cs       # Handles message retrieval & sending
│       │   └── RoomController.cs          # Manages rooms and connected users
│       ├── Models/
│       │   ├── MessageModel.cs
│       │   ├── RoomModel.cs
│       │   └── SecureCommDbContext.cs     # EF Core DB context
│       ├── Program.cs
│       ├── appsettings.json
│       └── SecureCommAPI.csproj
│
└── Program/
    └── SecureCommProgram/
        ├── Models/
        │   ├── MessageModel.cs
        │   └── RoomModel.cs
        ├── Screens/
        │   ├── EnterGuidScreen.cs
        │   ├── EnterPasswordScreen.cs
        │   ├── EnterUserIdScreen.cs
        │   └── RoomScreen.cs              # Main chat room logic
        ├── ApiClient.cs                   # HTTP API calls
        ├── ScreenManager.cs               # Screen navigation
        ├── Program.cs
        └── SecureComm.csproj
```

---

## 🛠️ Installation & Usage

### 📦 Prerequisites

- **.NET 9 SDK** installed
- SQL database (configured in `appsettings.json` for API)
- IDE such as **Visual Studio** or **Rider** for development

### ▶️ Run the Application

#### 1. Clone the repository:
```bash
git clone https://github.com/ribhav1/SecureComm.git
cd SecureComm
```

#### 2. Start the API backend:
```bash
cd API/SecureCommAPI
dotnet run
```
The API will be available at `https://localhost:5001`.

#### 3. Start the console client:
```bash
cd Program/SecureCommProgram
dotnet run
```

---

## 💬 Chatroom Flow

1. **Host creates a room** via the client → API stores room & password
2. **Participants join** using Room GUID & password → API validates credentials
3. **Public keys exchanged** → Host sends session key in encrypted chunks
4. **Messages sent**:
   - Encrypted using session key (RSA)
   - Stored in database by API
   - Retrieved and decrypted in real-time by clients

---

## 🔐 Encryption Details

- **Key Generation:**  
  Each user generates their own RSA 2048 key pair on joining.
  
- **Session Key Exchange:**  
  Host encrypts session public/private keys in 80-char chunks and sends via direct messages.

- **Message Encryption:**  
  ```
  Original Message → Unicode Encoding → RSA Encryption → Base64 String
  ```

- **Decryption Flow:**  
  ```
  Base64 String → RSA Decryption → Unicode Decoding → Plaintext Message
  ```

---

## 📊 API Endpoints

### **MessageController**
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET`  | `/Message/getMessages/{roomGUID}/{lastCheckedTime}` | Get new messages since `lastCheckedTime` |
| `POST` | `/Message/send/{roomGUID}/{sentByUserId}/{username}/{messageContent}/{color}` | Send a message |

### **RoomController**
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET`  | `/Room/getRoom/{id}` | Get room by ID |
| `GET`  | `/Room/validateRoom/{id}` | Check if room exists |
| `GET`  | `/Room/validatePassword/{id}/{password}` | Validate room password |
| `POST` | `/Room/createRoom/{roomGUID}/{password}` | Create new room |
| `POST` | `/Room/addConnectedUser/{roomGUID}/{userId}/{publicKey}` | Add a user to the room |

---

## 🧩 Potential Extensions

- AES session keys for performance (RSA for exchange only)
- Web or mobile client

---

## 🧑‍💻 Contributing

Pull requests are welcome! If you’d like to contribute:
1. Fork this repository
2. Create a feature branch (`git checkout -b feature/SecureComm`)
3. Commit your changes (`git commit -m 'Added optimizations'`)
4. Push to the branch (`git push origin feature/SecureComm`)
5. Open a Pull Request

---

## 📜 License

This project is licensed under the MIT License.  
See the [LICENSE](LICENSE) file for details.

---

## 🙋‍♂️ Author

Created by [Ribhav Malhotra](https://github.com/ribhav1)
