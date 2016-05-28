﻿using System;
using System.Globalization;
using GitHub.Extensions;
using GitHub.Models;
using GitHub.Services;
using System.Threading.Tasks;
using GitHub.Api;
using GitHub.Primitives;
using NullGuard;
using System.Diagnostics;

namespace GitHub.VisualStudio
{
    public abstract class MenuBase
    {
        readonly IServiceProvider serviceProvider;
        readonly ISimpleApiClientFactory apiFactory;

        protected IServiceProvider ServiceProvider { get { return serviceProvider; } }

        protected ISimpleRepositoryModel ActiveRepo { get; private set; }

        ISimpleApiClient simpleApiClient;
        [AllowNull]
        public ISimpleApiClient SimpleApiClient
        {
            [return: AllowNull]
            get { return simpleApiClient; }
            set
            {
                if (simpleApiClient != value && value == null)
                    apiFactory.ClearFromCache(simpleApiClient);
                simpleApiClient = value;
            }
        }

        protected ISimpleApiClientFactory ApiFactory => apiFactory;

        protected MenuBase()
        {
        }

        protected MenuBase(IServiceProvider serviceProvider, ISimpleApiClientFactory apiFactory)
        {
            this.serviceProvider = serviceProvider;
            this.apiFactory = apiFactory;
        }

        void RefreshRepo()
        {
            ActiveRepo = ServiceProvider.GetExportedValue<ITeamExplorerServiceHolder>().ActiveRepo;

            if (ActiveRepo == null)
            {
                var vsservices = ServiceProvider.GetExportedValue<IVSServices>();
                string path = vsservices?.GetActiveRepoPath() ?? String.Empty;
                try
                {
                    ActiveRepo = !String.IsNullOrEmpty(path) ? new SimpleRepositoryModel(path) : null;
                }
                catch (Exception ex)
                {
                    VsOutputLogger.WriteLine(string.Format(CultureInfo.CurrentCulture, "{0}: Error loading the repository from '{1}'. {2}", GetType(), path, ex));
                }
            }
        }

        protected async Task<bool> IsGitHubRepo()
        {
            RefreshRepo();

            var uri = ActiveRepo.CloneUrl;
            if (uri == null)
                return false;

            Debug.Assert(apiFactory != null, "apiFactory cannot be null. Did you call the right constructor?");
            SimpleApiClient = apiFactory.Create(uri);

            var isdotcom = HostAddress.IsGitHubDotComUri(uri.ToRepositoryUrl());
            if (!isdotcom)
            {
                var repo = await SimpleApiClient.GetRepository();
                return (repo.FullName == ActiveRepo.Name || repo.Id == 0) && SimpleApiClient.IsEnterprise();
            }
            return isdotcom;
        }
    }
}