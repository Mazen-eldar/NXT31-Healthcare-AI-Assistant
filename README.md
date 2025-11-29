# 🏥 Healthcare AI Assistant

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Active-success?style=for-the-badge)

> A comprehensive healthcare management system with integrated local AI chatbot, built with enterprise-grade architecture.

---

## 👥 Project Background

This project was developed as part of the **Digital Egypt Pioneers (رواد مصر الرقمية)** initiative by the **Ministry of Communications and Information Technology**.

**Project Type:** Graduation Mega Project  
**Role:** Team Lead  
**Team Size:** 5 members (1 Lead + 4 Developers)  
**Initiative:** Digital Egypt Pioneers - Ministry of Communications and Information Technology

---

## 💡 Why This Project?

- ✨ **Real Healthcare Workflow** - Mirrors actual hospital operations
- 🏗️ **Enterprise Architecture** - Clean Architecture scaled for production systems
- 🔐 **Bank-Level Security** - JWT + Identity + 2FA implementation
- 🤖 **Local AI Assistant** - Offline medical chatbot for patient queries
- 📊 **Production Ready** - Suitable for deployment in real clinics
- ⚡ **Automated Workflows** - Smart scheduling and background job processing

---

## 🖼️ Screenshots

### API Documentation (Swagger)

<img width="769" height="395" alt="Swagger API" src="https://github.com/user-attachments/assets/c662d669-ce61-45ba-870e-122ce74e5326" />

*Comprehensive REST API with interactive documentation*

---

### Authentication System

<img width="778" height="174" alt="Auth API - Login" src="https://github.com/user-attachments/assets/97810305-3498-412f-a96c-809f421a5b76" />

<img width="775" height="420" alt="Auth API - Registration" src="https://github.com/user-attachments/assets/2e501643-e93b-4818-bac4-7aa596f11dfa" />

*Secure authentication with JWT, 2FA, and OTP verification*

---

### Appointment Management

<img width="778" height="415" alt="Appointments API" src="https://github.com/user-attachments/assets/2701f0c1-ef72-4b20-959d-9be1075ed4e0" />

*Smart scheduling system with automated slot generation*

---

### Clinic Management

<img width="773" height="358" alt="Clinic API" src="https://github.com/user-attachments/assets/a91aaaa9-df37-4af1-9947-2e6f6a7eedd4" />

<img width="774" height="401" alt="Clinic Management API" src="https://github.com/user-attachments/assets/871b776b-c789-46c7-a402-cd49cb86816b" />

*Multi-clinic support with specialized departments*

---

### Medical Specialty

<img width="777" height="427" alt="Medical Specialty API" src="https://github.com/user-attachments/assets/4b26ee4a-c4bb-45e5-9c6d-ff32c7ae414e" />

*Medical specialties and department management*

---

### Frontend Interface

<img width="1920" height="990" alt="Frontend Home" src="https://github.com/user-attachments/assets/eb47ba37-9f0b-45d6-bff9-8fce3cb2c137" />

*Clean, modern UI for healthcare management*

---

## 🚀 Key Features

### Core Functionality
- 👥 **Multi-Role System** - Admin, Doctor, Receptionist, Patient with granular permissions
- 📅 **Smart Scheduling** - Automated appointment slot generation with conflict prevention
- 🏥 **Multi-Clinic Support** - Manage multiple clinics with specialized departments
- 🤖 **AI Health Assistant** - Local chatbot for medical queries and appointment guidance
- 🔐 **Advanced Security** - JWT tokens, 2FA, token blacklisting, OTP verification
- 📧 **Email Automation** - Notifications for appointments, verification, and updates

### Technical Highlights
- 🏛️ **Clean Architecture** - Separation of concerns with 5 distinct layers
- 🗄️ **EF Core** - Code-First approach with comprehensive migrations
- ⏰ **Background Jobs** - Hangfire for scheduled tasks and cleanup operations
- 📝 **API Documentation** - Auto-generated Swagger/OpenAPI specs
- 🎯 **Repository Pattern** - Generic repository with specification pattern
- 🛡️ **Middleware Pipeline** - Custom error handling and response formatting

---

## 🏗️ Architecture

```
Healthcare-AI-Assistant/
├── 🎯 Base.API/              # Presentation Layer
│   ├── Controllers/          # REST endpoints
│   ├── Middleware/           # Error handling, auth
│   └── Authorization/        # Custom policies
├── 🗄️ Base.DAL/             # Data Access Layer
│   ├── Models/               # Domain entities
│   ├── Config/               # EF configurations
│   └── Migrations/           # Database versions
├── 📦 Base.Repo/            # Repository Layer
│   ├── Implementations/      # Generic repo
│   └── Specifications/       # Query patterns
├── ⚙️ Base.Services/        # Business Logic Layer
│   ├── Implementations/      # Service logic
│   ├── HangfireJobs/         # Background tasks
│   └── Interfaces/           # Contracts
├── 📋 Base.Shared/          # Shared Layer
│   ├── DTOs/                 # Data transfer objects
│   └── Responses/            # Response models
└── 🧪 Base.Tests/           # Testing Layer
```

---

## 🛠️ Technology Stack

| Category | Technologies |
|----------|-------------|
| **Framework** | ASP.NET Core 8.0 |
| **Language** | C# 12.0 |
| **Database** | SQL Server 2019+ with Entity Framework Core |
| **Authentication** | JWT + ASP.NET Core Identity + 2FA |
| **Background Jobs** | Hangfire |
| **API Docs** | Swagger/Swashbuckle |
| **Testing** | xUnit |
| **Pattern** | Clean Architecture + Repository + Specification |

---

## ⚡ Quick Start

### Prerequisites
```
✅ .NET 8.0 SDK or higher
✅ SQL Server 2019 or higher
✅ Visual Studio 2022 / VS Code / Rider
```

### Installation

1️⃣ **Clone the Repository**
```bash
git clone https://github.com/Mazen-eldar/NXT31-Healthcare-AI-Assistant.git
cd NXT31-Healthcare-AI-Assistant
```

2️⃣ **Configure Database**

Update `Base.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=HealthcareDB;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

3️⃣ **Apply Migrations**
```bash
cd Base.API
dotnet ef database update
```

4️⃣ **Run the Application**
```bash
dotnet run
```

5️⃣ **Access Swagger UI**
```
https://localhost:7xxx/swagger
```

---

## 🔑 API Overview

### 🔐 Authentication
- `POST /api/auth/register` - User registration with email verification
- `POST /api/auth/login` - Login with JWT token generation
- `POST /api/auth/refresh-token` - Refresh expired tokens
- `POST /api/auth/logout` - Secure logout with token blacklisting
- `POST /api/auth/2fa/initiate` - Enable two-factor authentication

### 📅 Appointments
- `GET /api/appointments/clinics` - List all clinics
- `GET /api/appointments/specialties` - Get medical specialties
- `GET /api/appointments/available-slots/{doctorId}` - Available time slots
- `POST /api/appointments/book` - Book an appointment

### 🏥 Clinic Management
- `POST /api/clinic/create-clinicrequest` - Register new clinic
- `PATCH /api/clinic/approve-clinic-request` - Approve clinic registration
- `GET /api/clinic/system-clinics` - List system clinics
- `POST /api/clinic/create-clinicadmin` - Create clinic administrator

### 👥 User Management
- `GET /api/users` - List users with pagination
- `GET /api/users/{id}` - Get user details
- `PUT /api/users/{id}` - Update user information
- `DELETE /api/users/{id}` - Deactivate user

---

## 🤖 AI Chatbot

The integrated local AI chatbot provides:
- 💊 Medical symptom analysis
- 📚 General health information
- 📅 Appointment booking assistance
- 💬 24/7 patient support

**Note:** Runs completely offline without external API dependencies.

---

## 🔐 Security Features

| Feature | Implementation |
|---------|---------------|
| **Authentication** | JWT with refresh tokens |
| **Authorization** | Role-based access control (RBAC) |
| **Two-Factor Auth** | TOTP-based 2FA |
| **Token Security** | Blacklist for logged-out tokens |
| **Password Reset** | OTP verification via email |
| **Data Protection** | ASP.NET Core Data Protection |

---

## 📊 Background Jobs (Hangfire)

- ⏰ **Appointment Slot Generation** - Automated daily schedule creation
- 🧹 **Token Cleanup** - Remove expired blacklisted tokens
- 🔄 **Refresh Token Expiration** - Clean up old refresh tokens
- 📧 **Email Queue Processing** - Batch email sending

---

## 📧 Email Configuration

Configure SMTP in `appsettings.json`:
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "SenderPassword": "your-app-password"
  }
}
```

---

## 🧪 Running Tests

```bash
cd Base.Tests
dotnet test
```

---

## 🤝 Contributing

Contributions are welcome! For major changes, please open an issue first to discuss what you would like to change.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 🚧 Roadmap

- [ ] Enhanced AI chatbot with NLP capabilities
- [ ] Mobile app integration (iOS/Android)
- [ ] Telemedicine video consultations
- [ ] Advanced analytics dashboard
- [ ] Multi-language support (Arabic/English)
- [ ] Payment gateway integration
- [ ] Electronic Medical Records (EMR)

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 👨‍💻 Connect With Me

**Mazen Eldar** - Team Lead & Full Stack Developer

[![LinkedIn](https://img.shields.io/badge/LinkedIn-0077B5?style=for-the-badge&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/mazen-eldar)
[![GitHub](https://img.shields.io/badge/GitHub-100000?style=for-the-badge&logo=github&logoColor=white)](https://github.com/Mazen-eldar)
[![Portfolio](https://img.shields.io/badge/Portfolio-FF5722?style=for-the-badge&logo=google-chrome&logoColor=white)](https://mazen-eldar-portfolio.netlify.app/)
[![Email](https://img.shields.io/badge/Email-D14836?style=for-the-badge&logo=gmail&logoColor=white)](mailto:mazen.eldar.dev@gmail.com)

- 🌐 **Portfolio**: [mazen-eldar-portfolio.netlify.app](https://mazen-eldar-portfolio.netlify.app/)
- 💼 **LinkedIn**: [linkedin.com/in/mazen-eldar](https://www.linkedin.com/in/mazen-eldar)
- 📧 **Email**: mazen.eldar.dev@gmail.com
- 💻 **GitHub**: [@Mazen-eldar](https://github.com/Mazen-eldar)

---

## 🏷️ Keywords

`ASP.NET Core 8` `Clean Architecture` `Healthcare System` `Medical Management` `JWT Authentication` `Entity Framework Core` `Hangfire` `AI Chatbot` `REST API` `Repository Pattern` `Digital Egypt Pioneers` `Graduation Project` `Enterprise Application` `C# Backend` `SQL Server`

---

<div align="center">

**⭐ If you find this project useful, please consider giving it a star! ⭐**

Made with ❤️ by Mazen Eldar as part of Digital Egypt Pioneers Initiative

</div>
