using Microsoft.AspNetCore.Components;
using gotyoursix.Services;
using System;
using System.Net.NetworkInformation;
using System.Reflection;
using gotyoursix.Components;
using static gotyoursix.Data.DBContext;
using Microsoft.JSInterop;
using System.Linq;
using Microsoft.AspNetCore.Components.Forms;
using MongoDB.Driver;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using static gotyoursix.Data.CommonClasses;
using Blazored.Toast.Services;
using AspNetCore.Identity.MongoDbCore.Models;

namespace gotyoursix.Pages
{
    public partial class ResetPassword
    {

        private NewPwModel loginModel = new NewPwModel();
        [Parameter] public string Token { get; set; } // Extract token from URL
        [Inject] Blazored.Toast.Services.IToastService ToastService { get; set; }

        private bool isProcessing = false;
        private bool isSuccess = false;

        protected override void OnInitialized()
        {
            // Extract token from URL
            Token = Navigation.ToAbsoluteUri(Navigation.Uri).Query.Split("token=")[1];
        }

        private async Task HandlePasswordReset()
        {
            isProcessing = true;

            bool success = await _mongoDbService.ResetPasswordAsync(Token, loginModel.Password);

            if (success)
            {
                isSuccess = true;
                ToastService.ShowSuccess($"Password successfully changed.  Redirecting to login...");
                await Task.Delay(2000);
                Navigation.NavigateTo("/login"); // Redirect to login after reset
            }
            else
            {
                ToastService.ShowError($"Invalid or expired token");
            }
            isProcessing = false;
        }

    }




}

