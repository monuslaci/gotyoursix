﻿using Microsoft.AspNetCore.Components;
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
using static gotyoursix.Helpers.GeneralHelpers;
using System.Text.RegularExpressions;


namespace gotyoursix.Pages
{
    public partial class EditUser
    {
        [Inject] Blazored.Toast.Services.IToastService ToastService { get; set; }
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; }
        [Inject] NavigationManager Navigation { get; set; }
        [Inject] MongoDbService _mongoDbService { get; set; }
        private string? userEmail { get; set; }
        private Modal spinnerModal = new Modal();
        private EditUserDTO editUser = new EditUserDTO();
        private Users user = new Users();

        protected override async Task OnInitializedAsync()
        {


        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                spinnerModal.IsSpinner = true;
                spinnerModal.ShowModal();
                var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                userEmail = authState.User.Identity?.Name;
                user = await _mongoDbService.GetUser(userEmail);
                if (user != null) 
                { 
                    editUser.Email = userEmail;
                    editUser.FirstName = user.FirstName; 
                    editUser.LastName = user.LastName;
                }
                StateHasChanged();
                spinnerModal.CloseModal();

            }

        }
        private async Task HandleSave()
        {
            spinnerModal.ShowModal();
            user.FirstName = editUser.FirstName;
            user.LastName = editUser.LastName;

            await _mongoDbService.UpdateUser(user);

            spinnerModal.CloseModal();

            ToastService.ShowSuccess("Successfully changed the user details.");

   
            //ToastService.ShowError("Changing the user details was unsuccessful.");
        }
        private void Cancel()
        {
            Navigation.NavigateTo("/");
        }

    }

}




