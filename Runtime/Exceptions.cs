using System;

namespace RicKit.RFramework
{
    public class ServiceNotFoundException : Exception
    {
        public ServiceNotFoundException(Type t) : base($"Service {t.Name} not found")
        {
        }
    }

    public class ServiceAlreadyExistsException : Exception
    {
        public ServiceAlreadyExistsException(Type t) : base($"Service {t.Name} already exists")
        {
        }
    }
}