﻿using Bit.Core.Models.View;
using System;
using Xamarin.Forms;

namespace Bit.App.Pages
{
    public partial class FoldersPage : BaseContentPage
    {
        private FoldersPageViewModel _vm;

        public FoldersPage()
        {
            InitializeComponent();
            SetActivityIndicator(_mainContent);
            _vm = BindingContext as FoldersPageViewModel;
            _vm.Page = this;

            if(Device.RuntimePlatform == Device.iOS)
            {
                _absLayout.Children.Remove(_fab);
            }
            else
            {
                ToolbarItems.RemoveAt(0);
                _fab.Clicked = AddButton_Clicked;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadOnAppearedAsync(_mainLayout, true, async () =>
            {
                await _vm.InitAsync();
            }, _mainContent);
        }

        private async void RowSelected(object sender, SelectedItemChangedEventArgs e)
        {
            ((ListView)sender).SelectedItem = null;
            if(!DoOnce())
            {
                return;
            }
            if(!(e.SelectedItem is FolderView folder))
            {
                return;
            }
            var page = new FolderAddEditPage(folder.Id);
            await Navigation.PushModalAsync(new NavigationPage(page));
        }

        private async void AddButton_Clicked(object sender, EventArgs e)
        {
            if(DoOnce())
            {
                var page = new FolderAddEditPage();
                await Navigation.PushModalAsync(new NavigationPage(page));
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
