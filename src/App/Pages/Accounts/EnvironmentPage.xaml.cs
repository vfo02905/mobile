﻿using System;
using Xamarin.Forms;

namespace Bit.App.Pages
{
    public partial class EnvironmentPage : BaseContentPage
    {
        private EnvironmentPageViewModel _vm;

        public EnvironmentPage()
        {
            InitializeComponent();
            _vm = BindingContext as EnvironmentPageViewModel;
            _vm.Page = this;
            if(Device.RuntimePlatform == Device.Android)
            {
                ToolbarItems.RemoveAt(0);
            }

            _webVaultEntry.ReturnType = ReturnType.Next;
            _webVaultEntry.ReturnCommand = new Command(() => _apiEntry.Focus());
            _apiEntry.ReturnType = ReturnType.Next;
            _apiEntry.ReturnCommand = new Command(() => _identityEntry.Focus());
            _identityEntry.ReturnType = ReturnType.Next;
            _identityEntry.ReturnCommand = new Command(() => _iconsEntry.Focus());
        }

        private async void Submit_Clicked(object sender, EventArgs e)
        {
            if(DoOnce())
            {
                await _vm.SubmitAsync();
            }
        }

        private async void Close_Clicked(object sender, System.EventArgs e)
        {
            if(DoOnce())
            {
                await Navigation.PopModalAsync();
            }
        }
    }
}
