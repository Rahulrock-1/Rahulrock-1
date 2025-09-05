using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MultiDb.Extensions.DI.Attributes;

namespace MultiDb.Extensions.DI
{
    internal static class ServiceRegistrar
    {
        public static void RegisterAttributedServices(IServiceCollection services, IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies.Distinct())
            {
                foreach (var type in GetLoadableTypes(assembly))
                {
                    if (type.IsAbstract || type.IsInterface)
                    {
                        continue;
                    }

                    RegisterIfAnnotated(services, type);
                }
            }
        }

        private static void RegisterIfAnnotated(IServiceCollection services, Type implementationType)
        {
            var singletonAttributes = implementationType.GetCustomAttributes<RegisterSingletonAttribute>(false);
            foreach (var attr in singletonAttributes)
            {
                Register(services, implementationType, ServiceLifetime.Singleton, attr.ServiceTypes, attr.AlsoRegisterAsSelf);
            }

            var scopedAttributes = implementationType.GetCustomAttributes<RegisterScopedAttribute>(false);
            foreach (var attr in scopedAttributes)
            {
                Register(services, implementationType, ServiceLifetime.Scoped, attr.ServiceTypes, attr.AlsoRegisterAsSelf);
            }

            var transientAttributes = implementationType.GetCustomAttributes<RegisterTransientAttribute>(false);
            foreach (var attr in transientAttributes)
            {
                Register(services, implementationType, ServiceLifetime.Transient, attr.ServiceTypes, attr.AlsoRegisterAsSelf);
            }
        }

        private static void Register(IServiceCollection services, Type implementationType, ServiceLifetime lifetime, Type[] serviceTypes, bool alsoAsSelf)
        {
            var targets = serviceTypes?.Length > 0 ? serviceTypes : GuessServiceTypes(implementationType);

            foreach (var serviceType in targets)
            {
                services.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
            }

            if (alsoAsSelf)
            {
                services.Add(new ServiceDescriptor(implementationType, implementationType, lifetime));
            }
        }

        private static IEnumerable<Type> GuessServiceTypes(Type implementationType)
        {
            // Prefer the first non-generic interface implemented by the type; fallback to the type itself.
            var interfaces = implementationType.GetInterfaces()
                .Where(i => !i.IsGenericType || i.IsConstructedGenericType)
                .ToArray();

            if (interfaces.Length > 0)
            {
                return interfaces.Take(1);
            }

            return new[] { implementationType };
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t is not null)!;
            }
        }
    }
}

