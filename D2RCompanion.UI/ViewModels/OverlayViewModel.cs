// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using D2RCompanion.UI.Messages;
using D2RCompanion.UI.Services;
using Microsoft.Extensions.Logging;

namespace D2RCompanion.UI.ViewModels
{
    public partial class OverlayViewModel :
        ObservableObject
    {
        private readonly HomeViewModel _homeViewModel;
        private readonly SettingsViewModel _settingsViewModel;
        private readonly PriceCheckViewModel _priceCheckViewModel;

        [ObservableProperty]
        public object currentViewModel;

        private readonly PipelineService _pipelineService;
        private readonly ILogger _logger;


        public OverlayViewModel(
            HomeViewModel home,
            SettingsViewModel settings,
            PriceCheckViewModel priceCheck,
            PipelineService pipelineService,
            ILogger<OverlayViewModel> logger
            )
        {
            _homeViewModel = home;
            _settingsViewModel = settings;
            _priceCheckViewModel = priceCheck;

            _pipelineService = pipelineService;
            _logger = logger;

            RegisterMessageHandlers();
            ResetView(); // Default = Home
        }

        private void RegisterMessageHandlers()
        {
            WeakReferenceMessenger.Default.Register<NavigationRequestMessage>(this, (r, m) =>
            {
                SetView(m.ToView);
            });
        }

        public void ResetView()
        {
            CurrentViewModel = _homeViewModel;
        }

        private void SetView(OverlayContentView view = OverlayContentView.Home)
        {
            CurrentViewModel = view switch
            {
                OverlayContentView.Home => _homeViewModel,
                OverlayContentView.Settings => _settingsViewModel,
                OverlayContentView.PriceCheck => _priceCheckViewModel,
                _ => _homeViewModel
            };

            _logger.LogDebug("Switched to view: {View}", view);
        }

        [RelayCommand]
        public async Task InitiatePriceCheck()
        {
            _logger.LogInformation("Initiate price check!");

            SetView(OverlayContentView.PriceCheck);

            try
            {
                var result = await Task.Run(() => _pipelineService.RunAsync());

                WeakReferenceMessenger.Default.Send(new PipelineCompletedMessage(result));
            }
            catch (Exception ex)
            {

            }
            finally
            {
                // TODO busy false.
            }
        }
    }

    public enum OverlayContentView
    {
        Home,
        Settings,
        PriceCheck
    }
}
