# AgriGuard Backend 🌿

ASP.NET Core Web API backend for the AgriGuard platform.

## Overview

AgriGuard is an intelligent system for plant disease diagnosis and smart crop management.  
This backend provides authentication, crop management, task scheduling, community features, and communication with the AI microservice.

## Features

- JWT Authentication & Authorization
- Smart Crop Task Management
- Community Posts & Comments
- AI Diagnosis Integration
- Gemini AI Treatment Recommendations
- Admin Dashboard APIs
- RESTful API Architecture

## Technologies Used

- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT Authentication
- BCrypt
- Swagger
- REST APIs

## AI Integration

The backend communicates with a Python FastAPI microservice for plant disease prediction using HTTP requests.

## Architecture

Frontend (React.js)
↓
ASP.NET Core Web API
↓
FastAPI AI Microservice
↓
TensorFlow / Keras Model

## Author

Mohamed Ibrahim
