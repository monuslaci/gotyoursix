using Microsoft.AspNetCore.Components;
using gotyoursix.Services;
using static gotyoursix.Data.DBContext;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Blazored.Toast.Services;
using gotyoursix.Helpers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.Authorization;
using static gotyoursix.Data.CommonClasses;
using static gotyoursix.Helpers.GeneralHelpers;


namespace gotyoursix.Pages
{
    public partial class Login
    {
        private LoginModel loginModel = new LoginModel();

        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] Blazored.Toast.Services.IToastService ToastService { get; set; }
        [Inject] MongoDbService MongoDbService { get; set; }
        [Inject] NavigationManager Navigation { get; set; }
        [Inject] EmailService EmailService { get; set; }

        private bool _isInitialized = false;

        protected override async Task OnInitializedAsync()
        {
            // Initialization logic if necessary
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && !_isInitialized)
            {
                var storedUser = Preferences.Get("userEmail", string.Empty);
                var storedExpiration = Preferences.Get("sessionExpiration", string.Empty);

                if (!string.IsNullOrEmpty(storedUser) && DateTime.TryParse(storedExpiration, out var expirationTime))
                {
                    if (DateTime.UtcNow < expirationTime) // Check if session is still valid
                    {
                        if (AuthStateProvider is CustomAuthenticationStateProvider customAuthStateProvider)
                        {
                            await customAuthStateProvider.MarkUserAsAuthenticated(storedUser);
                            StateHasChanged();
                            await Task.Delay(2000);
                            Navigation.NavigateTo("/");  // Redirect to home page
                        }
                    }
                    else
                    {
                        // Session expired, clear storage
                        Preferences.Remove("userEmail");
                        Preferences.Remove("sessionExpiration");
                    }
                }
            }
        }

        private async Task HandleLogin()
        {
            var loginResult = await MongoDbService.ValidateUserAsync(loginModel.Email, loginModel.Password);

            if (loginResult.Result)
            {
                // Mark the user as authenticated
                if (AuthStateProvider is CustomAuthenticationStateProvider customAuthStateProvider)
                {
                    await customAuthStateProvider.MarkUserAsAuthenticated(loginModel.Email);
                }

                var user = await MongoDbService.GetUser(loginModel.Email);
                await EmailService.SendEmailNotification(CreateLoginEmailTemplate(user.Email, $"{user.FirstName} {user.LastName}"));

                // Store session details
                Preferences.Set("userEmail", loginModel.Email);
                Preferences.Set("sessionExpiration", DateTime.UtcNow.AddMinutes(30).ToString()); // Set expiration time

                // Redirect to internal page after login
                await Task.Delay(2000);
                Navigation.NavigateTo("/");  // Redirect to home page 
            }
            else
            {
                ToastService.ShowError($"{loginResult.Description}");
            }
        }

        private EmailSendingClass CreateLoginEmailTemplate(string email, string name)
        {
            EmailSendingClass emailDetails = new EmailSendingClass();

            var url = Environment.GetEnvironmentVariable("Environment") == "Development"
                ? "https://localhost:7225"
                : "https://snipster.co";

            var loginEmailTemplate = @"
                <!DOCTYPE html>
                <html>
                <head><style> p { margin: 0;} </style></head>
                <body>
                    <div><p>Dear <Name>,</p>
                    <p>You received this email because you logged in from this URL: <url>.</p>
                    <p>Best regards,</p>
                    <p>Snipster Team</p>
                </body>
                </html>";

            loginEmailTemplate = Regex.Replace(loginEmailTemplate, "<url>", url);
            loginEmailTemplate = Regex.Replace(loginEmailTemplate, "<Name>", name);

            emailDetails.htmlContent = loginEmailTemplate;
            emailDetails.To = email;  // Send email to the user
            emailDetails.Subject = "Login Notification from Snipster";

            return emailDetails;
        }
    }
}
