﻿using Bit.App.Abstractions;
using Bit.App.Controls;
using Bit.App.Resources;
using Bit.Core;
using Bit.Core.Abstractions;
using Bit.Core.Enums;
using Bit.Core.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Bit.App.Pages
{
    public partial class GroupingsPage : BaseContentPage
    {
        private readonly IBroadcasterService _broadcasterService;
        private readonly ISyncService _syncService;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IStorageService _storageService;
        private readonly ILockService _lockService;
        private readonly IDeviceActionService _deviceActionService;
        private readonly GroupingsPageViewModel _vm;
        private readonly string _pageName;

        public GroupingsPage(bool mainPage, CipherType? type = null, string folderId = null,
            string collectionId = null, string pageTitle = null)
        {
            _pageName = string.Concat(nameof(GroupingsPage), "_", DateTime.UtcNow.Ticks);
            InitializeComponent();
            ListView = _listView;
            SetActivityIndicator(_mainContent);
            _broadcasterService = ServiceContainer.Resolve<IBroadcasterService>("broadcasterService");
            _syncService = ServiceContainer.Resolve<ISyncService>("syncService");
            _pushNotificationService = ServiceContainer.Resolve<IPushNotificationService>("pushNotificationService");
            _storageService = ServiceContainer.Resolve<IStorageService>("storageService");
            _lockService = ServiceContainer.Resolve<ILockService>("lockService");
            _deviceActionService = ServiceContainer.Resolve<IDeviceActionService>("deviceActionService");
            _vm = BindingContext as GroupingsPageViewModel;
            _vm.Page = this;
            _vm.MainPage = mainPage;
            _vm.Type = type;
            _vm.FolderId = folderId;
            _vm.CollectionId = collectionId;
            if(pageTitle != null)
            {
                _vm.PageTitle = pageTitle;
            }

            if(Device.RuntimePlatform == Device.iOS)
            {
                _absLayout.Children.Remove(_fab);
            }
            else
            {
                _fab.Clicked = AddButton_Clicked;
                ToolbarItems.Add(_syncItem);
                ToolbarItems.Add(_lockItem);
                ToolbarItems.Add(_exitItem);
            }
        }

        public ExtendedListView ListView { get; set; }

        protected async override void OnAppearing()
        {
            base.OnAppearing();
            if(_syncService.SyncInProgress)
            {
                IsBusy = true;
            }

            _broadcasterService.Subscribe(_pageName, async (message) =>
            {
                if(message.Command == "syncStarted")
                {
                    Device.BeginInvokeOnMainThread(() => IsBusy = true);
                }
                else if(message.Command == "syncCompleted")
                {
                    await Task.Delay(500);
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        IsBusy = false;
                        var task = _vm.LoadAsync();
                    });
                }
            });

            var migratedFromV1 = await _storageService.GetAsync<bool?>(Constants.MigratedFromV1);
            await LoadOnAppearedAsync(_mainLayout, false, async () =>
            {
                if(!_syncService.SyncInProgress)
                {
                    await _vm.LoadAsync();
                }
                else
                {
                    await Task.Delay(5000);
                    if(!_vm.Loaded)
                    {
                        await _vm.LoadAsync();
                    }
                }
                // Forced sync if for some reason we have no data after a v1 migration
                if(_vm.MainPage && !_syncService.SyncInProgress && migratedFromV1.GetValueOrDefault() &&
                    !_vm.HasCiphers && !_vm.HasFolders &&
                    Xamarin.Essentials.Connectivity.NetworkAccess != Xamarin.Essentials.NetworkAccess.None)
                {
                    var triedV1ReSync = await _storageService.GetAsync<bool?>(Constants.TriedV1Resync);
                    if(!triedV1ReSync.GetValueOrDefault())
                    {
                        await _storageService.SaveAsync(Constants.TriedV1Resync, true);
                        await _syncService.FullSyncAsync(true);
                    }
                }
            }, _mainContent);

            if(!_vm.MainPage)
            {
                return;
            }

            // Push registration
            var lastPushRegistration = await _storageService.GetAsync<DateTime?>(Constants.PushLastRegistrationDateKey);
            lastPushRegistration = lastPushRegistration.GetValueOrDefault(DateTime.MinValue);
            if(Device.RuntimePlatform == Device.iOS)
            {
                var pushPromptShow = await _storageService.GetAsync<bool?>(Constants.PushInitialPromptShownKey);
                if(!pushPromptShow.GetValueOrDefault(false))
                {
                    await _storageService.SaveAsync(Constants.PushInitialPromptShownKey, true);
                    await DisplayAlert(AppResources.EnableAutomaticSyncing, AppResources.PushNotificationAlert,
                        AppResources.OkGotIt);
                }
                if(!pushPromptShow.GetValueOrDefault(false) ||
                    DateTime.UtcNow - lastPushRegistration > TimeSpan.FromDays(1))
                {
                    await _pushNotificationService.RegisterAsync();
                }
            }
            else if(Device.RuntimePlatform == Device.Android)
            {
                if(DateTime.UtcNow - lastPushRegistration > TimeSpan.FromDays(1))
                {
                    await _pushNotificationService.RegisterAsync();
                }
                if(!_deviceActionService.AutofillAccessibilityServiceRunning()
                    && !_deviceActionService.AutofillServiceEnabled())
                {
                    if(migratedFromV1.GetValueOrDefault())
                    {
                        var migratedFromV1AutofillPromptShown = await _storageService.GetAsync<bool?>(
                            Constants.MigratedFromV1AutofillPromptShown);
                        if(!migratedFromV1AutofillPromptShown.GetValueOrDefault())
                        {
                            await DisplayAlert(AppResources.Autofill,
                                AppResources.AutofillServiceNotEnabled, AppResources.Ok);
                        }
                    }
                }
                await _storageService.SaveAsync(Constants.MigratedFromV1AutofillPromptShown, true);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            IsBusy = false;
            _broadcasterService.Unsubscribe(_pageName);
        }

        private async void RowSelected(object sender, SelectedItemChangedEventArgs e)
        {
            ((ListView)sender).SelectedItem = null;
            if(!DoOnce())
            {
                return;
            }
            if(!(e.SelectedItem is GroupingsPageListItem item))
            {
                return;
            }

            if(item.Cipher != null)
            {
                await _vm.SelectCipherAsync(item.Cipher);
            }
            else if(item.Folder != null)
            {
                await _vm.SelectFolderAsync(item.Folder);
            }
            else if(item.Collection != null)
            {
                await _vm.SelectCollectionAsync(item.Collection);
            }
            else if(item.Type != null)
            {
                await _vm.SelectTypeAsync(item.Type.Value);
            }
        }

        private async void Search_Clicked(object sender, EventArgs e)
        {
            if(DoOnce())
            {
                var page = new CiphersPage(_vm.Filter, _vm.FolderId != null, _vm.CollectionId != null,
                    _vm.Type != null);
                await Navigation.PushModalAsync(new NavigationPage(page), false);
            }
        }

        private async void Sync_Clicked(object sender, EventArgs e)
        {
            await _vm.SyncAsync();
        }

        private async void Lock_Clicked(object sender, EventArgs e)
        {
            await _lockService.LockAsync(true, true);
        }

        private async void Exit_Clicked(object sender, EventArgs e)
        {
            await _vm.ExitAsync();
        }

        private async void AddButton_Clicked(object sender, EventArgs e)
        {
            if(DoOnce())
            {
                var page = new AddEditPage(null, _vm.Type, _vm.FolderId, _vm.CollectionId);
                await Navigation.PushModalAsync(new NavigationPage(page));
            }
        }
    }
}
