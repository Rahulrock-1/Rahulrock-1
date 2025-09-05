using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MultiDb.Extensions.DI;

namespace MultiDb.Extensions
{
    public static class DependencyInjectionExtensions
    {
        /// <summary>
        /// Scans the given assemblies for types annotated with registration attributes
        /// and registers them into the service collection.
        /// </summary>
        public static IServiceCollection AddAttributedServices(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (assemblies == null || assemblies.Length == 0)
            {
                assemblies = new[] { Assembly.GetCallingAssembly() };
            }

            ServiceRegistrar.RegisterAttributedServices(services, assemblies);
            return services;
        }

        /// <summary>
        /// Scans all currently loaded assemblies in the AppDomain for annotated services and registers them.
        /// </summary>
        public static IServiceCollection AddAttributedServicesFromAppDomain(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            ServiceRegistrar.RegisterAttributedServices(services, loadedAssemblies);
            return services;
        }
    }
}

