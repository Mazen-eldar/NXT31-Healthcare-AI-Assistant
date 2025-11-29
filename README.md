# 🏥 Healthcare AI Assistant

A comprehensive healthcare management system with an integrated local AI chatbot for medical assistance and appointment management.

## 📋 Project Overview

Healthcare AI Assistant is a full-stack medical management platform built with ASP.NET Core, featuring:
- Patient and doctor management
- Smart appointment scheduling
- Local AI-powered medical chatbot
- Clinic administration
- Medical specialty tracking
- Secure authentication and authorization

## 👥 Project Background

This project was developed as part of the **Digital Egypt Pioneers (رواد مصر الرقمية)** initiative by the **Ministry of Communications and Information Technology**.

**Project Type:** Graduation Mega Project  
**Role:** Team Lead  
**Team Size:** 5 members (1 Lead + 4 Developers)  
**Initiative:** Digital Egypt Pioneers - Ministry of Communications and Information Technology

## 🚀 Key Features

### Core Functionality
- **User Management**: Comprehensive role-based access control (Admin, Doctor, Receptionist, Patient)
- **Appointment System**: Automated slot generation and scheduling
- **Clinic Management**: Multi-clinic support with specialties
- **AI Chatbot**: Local healthcare assistant for patient queries
- **Authentication**: JWT-based secure authentication with 2FA support
- **Email Notifications**: Automated email system for appointments and OTP

### Technical Features
- Clean Architecture (API, DAL, Repository, Services layers)
- Entity Framework Core with Code-First migrations
- Hangfire for background job processing
- Swagger/OpenAPI documentation
- RESTful API design
- Comprehensive error handling middleware

## 🏗️ Architecture

```
Healthcare-AI-Assistant/
├── Base.API/              # Web API Layer
│   ├── Controllers/       # API Endpoints
│   ├── Middleware/        # Custom middleware
│   └── Authorization/     # Auth handlers
├── Base.DAL/             # Data Access Layer
│   ├── Models/           # Entity models
│   ├── Config/           # EF configurations
│   └── Migrations/       # Database migrations
├── Base.Repo/            # Repository Pattern
│   ├── Implementations/  # Generic repository
│   └── Specifications/   # Query specifications
├── Base.Services/        # Business Logic Layer
│   ├── Implementations/  # Service implementations
│   └── Interfaces/       # Service contracts
├── Base.Shared/          # Shared DTOs & Responses
└── Base.Tests/           # Unit Tests
```

## 🛠️ Technology Stack

- **Framework**: ASP.NET Core 8.0
- **Database**: SQL Server with Entity Framework Core
- **Authentication**: JWT + ASP.NET Core Identity
- **Background Jobs**: Hangfire
- **API Documentation**: Swagger/Swashbuckle
- **Testing**: xUnit
- **Architecture**: Clean Architecture with Repository Pattern

## 📦 Prerequisites

- .NET 8.0 SDK or higher
- SQL Server 2019 or higher
- Visual Studio 2022 / VS Code / Rider

## ⚙️ Installation & Setup

### 1. Clone the Repository
```bash
git clone https://github.com/Mazen-eldar/NXT31-Healthcare-AI-Assistant.git
cd NXT31-Healthcare-AI-Assistant
```

### 2. Configure Database Connection
Update the connection string in `Base.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=HealthcareDB;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### 3. Apply Migrations
```bash
cd Base.API
dotnet ef database update
```

### 4. Run the Application
```bash
dotnet run
```

The API will be available at: `https://localhost:7xxx` or `http://localhost:5xxx`

## 📚 API Documentation

Once the application is running, access the Swagger UI at:
```
https://localhost:7xxx/swagger
```

## 🔑 Default Credentials

After running migrations and seeding data, default admin credentials will be available.
Check `Base.DAL/Data/IdentitySeeder.cs` for details.

## 🤖 AI Chatbot

The integrated local AI chatbot provides:
- Medical symptom analysis
- General health information
- Appointment assistance
- Medication information

**Note**: The chatbot runs locally and doesn't require external API calls.

## 🔐 Security Features

- JWT token-based authentication
- Refresh token mechanism
- Token blacklisting on logout
- Two-Factor Authentication (2FA)
- Role-based authorization
- Password reset with OTP verification
- Active user validation

## 📊 Key Modules

### Authentication Module
- User registration with email verification
- Login with JWT tokens
- Password reset flow
- 2FA enable/disable
- Token refresh mechanism

### Appointment Module
- Automated slot generation
- Appointment booking
- Schedule management
- Conflict prevention

### Clinic Management
- Multi-clinic support
- Medical specialty assignment
- Doctor-clinic associations
- Working hours configuration

### User Management
- Role assignment
- Profile management
- User activation/deactivation
- Comprehensive user types (Admin, Doctor, Receptionist, Patient)

## 🧪 Running Tests

```bash
cd Base.Tests
dotnet test
```

## 📝 API Endpoints Overview

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh-token` - Refresh access token
- `POST /api/auth/logout` - User logout
- `POST /api/auth/forgot-password` - Request password reset

### Appointments
- `GET /api/appointments` - Get all appointments
- `POST /api/appointments` - Create appointment
- `PUT /api/appointments/{id}` - Update appointment
- `DELETE /api/appointments/{id}` - Cancel appointment

### Clinics
- `GET /api/clinic` - Get all clinics
- `POST /api/clinic` - Create clinic
- `PUT /api/clinic/{id}` - Update clinic
- `DELETE /api/clinic/{id}` - Delete clinic

### Users
- `GET /api/users` - Get all users
- `GET /api/users/{id}` - Get user by ID
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

## 🔄 Background Jobs

Automated tasks using Hangfire:
- Appointment slot generation
- Token cleanup
- Refresh token expiration
- Email queue processing

## 📧 Email Configuration

Configure SMTP settings in `appsettings.json` for email functionality:
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

## 🚧 Future Enhancements

- [ ] Enhanced AI chatbot capabilities
- [ ] Mobile application integration
- [ ] Telemedicine features
- [ ] Analytics dashboard
- [ ] Multi-language support
- [ ] Payment gateway integration

## 📄 License

This project is licensed under the MIT License.

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

**Note**: This is a comprehensive healthcare management system developed as part of the Digital Egypt Pioneers initiative. The system showcases enterprise-level architecture and real-world healthcare solutions.
