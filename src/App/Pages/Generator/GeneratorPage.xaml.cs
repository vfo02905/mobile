﻿using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Bit.App.Pages
{
    public partial class GeneratorPage : BaseContentPage
    {
        private GeneratorPageViewModel _vm;
        private readonly bool _fromTabPage;
        private readonly Action<string> _selectAction;
        private readonly TabsPage _tabsPage;

        public GeneratorPage(bool fromTabPage, Action<string> selectAction = null, TabsPage tabsPage = null)
        {
            _tabsPage = tabsPage;
            InitializeComponent();
            _vm = BindingContext as GeneratorPageViewModel;
            _vm.Page = this;
            _fromTabPage = fromTabPage;
            _selectAction = selectAction;
            if(selectAction == null)
            {
                ToolbarItems.Remove(_selectItem);
            }
        }

        public async Task InitAsync()
        {
            await _vm.InitAsync();
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();
            if(!_fromTabPage)
            {
                await InitAsync();
            }
        }

        protected override bool OnBackButtonPressed()
        {
            if(Device.RuntimePlatform == Device.Android && _tabsPage != null)
            {
                _tabsPage.ResetToVaultPage();
                return true;
            }
            return base.OnBackButtonPressed();
        }

        private async void Regenerate_Clicked(object sender, EventArgs e)
        {
            await _vm.RegenerateAsync();
        }

        private async void Copy_Clicked(object sender, EventArgs e)
        {
            await _vm.CopyAsync();
        }

        private void Select_Clicked(object sender, EventArgs e)
        {
            _selectAction?.Invoke(_vm.Password);
        }

        private async void History_Clicked(object sender, EventArgs e)
        {
            var page = new GeneratorHistoryPage();
            await Navigation.PushModalAsync(new NavigationPage(page));
        }

        private async void LengthSlider_DragCompleted(object sender, EventArgs e)
        {
            await _vm.SliderChangedAsync();
        }
    }
}
