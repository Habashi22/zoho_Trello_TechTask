# Zoho CRM + Trello Integration

## 💡 Overview
This project automates Trello board creation when a Zoho CRM Deal reaches:
- **Stage** = Project Kickoff
- **Type** = New Implementation Project
- **Custom field** `Project_Board_ID__c` is empty

## ⚙️ Setup
1. Create accounts:
   - Zoho One: https://www.zoho.com/one/signup.html
   - Trello: https://trello.com/
2. Register a Zoho App for OAuth2:
   - https://api-console.zoho.com/
3. Get Trello API Key and Token:
   - https://trello.com/app-key
4. Add custom field `Project_Board_ID__c` to Zoho Deals.

## 🔧 Configuration
Replace the following variables in `ZohoTrelloIntegration.cs`:
- `zohoClientId`, `zohoClientSecret`, `zohoRefreshToken`
- `trelloApiKey`, `trelloToken`

## ▶️ How to Run
```bash
dotnet run
