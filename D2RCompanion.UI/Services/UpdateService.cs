// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Velopack;
using Velopack.Sources;

namespace D2RCompanion.UI.Services
{
    public class UpdateService
    {
        private readonly UpdateManager _manager;

        public UpdateService()
        {
            var source = new GithubSource(
                "https://github.com/ebrodlic/D2RCompanion",
                null,
                false);

            _manager = new UpdateManager(source);
        }

        public Task<UpdateInfo?> CheckAsync()
            => _manager.CheckForUpdatesAsync();

        public Task DownloadAsync(UpdateInfo update)
            => _manager.DownloadUpdatesAsync(update);

        public void ApplyAndRestart(UpdateInfo update)
            => _manager.ApplyUpdatesAndRestart(update);
    }
}
