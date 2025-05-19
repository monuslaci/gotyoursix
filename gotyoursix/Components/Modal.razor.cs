using Microsoft.AspNetCore.Components;
using gotyoursix.Services;
using System;
using System.Reflection;
using static gotyoursix.Data.DBContext;


namespace gotyoursix.Components
{
    public partial class Modal : ComponentBase
    {
        [Parameter] public string Title { get; set; } = "Modal Title";
        [Parameter] public RenderFragment ChildContent { get; set; }
        private bool IsVisible;
        public bool IsSpinner { get; set; } = false;

        public void ShowModal()
        {
            IsVisible = true;
            StateHasChanged();
        }

        public void CloseModal()
        {
            IsVisible = false;
            StateHasChanged();
        }
    }
}