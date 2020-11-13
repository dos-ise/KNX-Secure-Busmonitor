using System;
using System.Collections.Generic;
using System.Text;

using Autofac;

namespace Busmonitor.Bootstrap
{
  class AppContainer
  {
    private static IContainer container;

    public AppContainer()
    {
      var builder = new ContainerBuilder();
      // services

      // view models

      container = builder.Build();
    }

    public T Resolve<T>() => container.Resolve<T>();

    public object Resolve(Type type) => container.Resolve(type);
  }
}
