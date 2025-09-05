using System;

namespace MultiDb.Extensions.DI.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterSingletonAttribute : Attribute
    {
        public RegisterSingletonAttribute(params Type[] serviceTypes)
        {
            ServiceTypes = serviceTypes ?? Array.Empty<Type>();
        }

        public Type[] ServiceTypes { get; }

        public bool AlsoRegisterAsSelf { get; init; } = true;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterScopedAttribute : Attribute
    {
        public RegisterScopedAttribute(params Type[] serviceTypes)
        {
            ServiceTypes = serviceTypes ?? Array.Empty<Type>();
        }

        public Type[] ServiceTypes { get; }

        public bool AlsoRegisterAsSelf { get; init; } = false;
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RegisterTransientAttribute : Attribute
    {
        public RegisterTransientAttribute(params Type[] serviceTypes)
        {
            ServiceTypes = serviceTypes ?? Array.Empty<Type>();
        }

        public Type[] ServiceTypes { get; }

        public bool AlsoRegisterAsSelf { get; init; } = false;
    }
}

