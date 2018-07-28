using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Client;
using Microsoft.Xrm.Client.Services;
using CrmPluginRegExt.VSPackage.Model;
using Microsoft.Xrm.Sdk.Discovery;
using System.Collections.ObjectModel;
using System.Runtime.Caching;
using System.ServiceModel.Description;
using LinkDev.Libraries.EnhancedOrgService.Builders;
using LinkDev.Libraries.EnhancedOrgService.Factories;
using LinkDev.Libraries.EnhancedOrgService.Params;
using LinkDev.Libraries.EnhancedOrgService.Pools;
using LinkDev.Libraries.EnhancedOrgService.Services;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Client.Caching;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;

namespace CrmPluginRegExt.VSPackage.Helpers
{
	public class ConnectionHelper
    {
		private static readonly IDictionary<string, IEnhancedServicePool<EnhancedOrgService>> poolCache
			= new Dictionary<string, IEnhancedServicePool<EnhancedOrgService>>();
		private static readonly IDictionary<string, EnhancedServiceFactory<EnhancedOrgService>> factoryCache
			= new Dictionary<string, EnhancedServiceFactory<EnhancedOrgService>>();

		internal static IEnhancedOrgService GetConnection(string connectionString, bool noCache = false)
		{
			Status.Update("Creating connection to CRM ... ", false);

			poolCache.TryGetValue(connectionString, out var pool);
			factoryCache.TryGetValue(connectionString, out var factory);

			if (pool == null || factory == null)
			{
				var template = EnhancedServiceBuilder.NewBuilder
					.Initialise(connectionString)
					.AddCaching()
					.Finalise()
					.GetBuild();
				factoryCache[connectionString] = factory = new EnhancedServiceFactory<EnhancedOrgService>(template);
				poolCache[connectionString] = pool = new EnhancedServicePool<EnhancedOrgService>(factory);
			}

			if (noCache)
			{
				factory.ClearCache();
			}

			var service = pool.GetService();

			Status.Update("done!");

			return service;
		}

		public static OrganizationDetail GetOrganizationDetails(Settings settings)
		{
			var orgs = GetOrganizations(settings);
			var details = orgs.FirstOrDefault(d => d.UrlName == settings.CrmOrg);
			return details;
		}

		public static ObservableCollection<string> GetOrgList(Settings settings)
		{
			var orgs = GetOrganizations(settings);
			var newOrgs = new ObservableCollection<string>(orgs.Select(d => d.UrlName).ToList());
			return newOrgs;
		}

		public static OrganizationDetailCollection GetOrganizations(Settings settings)
		{
			var credentials = new ClientCredentials();

			if (settings.UseIFD || settings.UseOffice365 || settings.UseOnline)
			{
				credentials.UserName.UserName = settings.Username;
				credentials.UserName.Password = settings.Password;
			}
			else
			{
				credentials.Windows.ClientCredential =
					new System.Net.NetworkCredential(settings.Username, settings.Password, settings.Domain);
			}

			using (var discoveryProxy = new DiscoveryServiceProxy(settings.GetDiscoveryUri(), null, credentials, null))
			{
				discoveryProxy.Authenticate();

				var retrieveOrganizationsRequest = new RetrieveOrganizationsRequest
				{
					AccessType = EndpointAccessType.Default,
					Release = OrganizationRelease.Current
				};

				var retrieveOrganizationsResponse = (RetrieveOrganizationsResponse)discoveryProxy
					.Execute(retrieveOrganizationsRequest);

				return retrieveOrganizationsResponse.Details;
			}
		}
	}
}
