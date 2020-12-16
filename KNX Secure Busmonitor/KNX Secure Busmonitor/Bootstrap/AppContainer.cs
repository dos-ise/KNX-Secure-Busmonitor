using System;
using System.Collections.Generic;
using System.Text;

using Autofac;
using Busmonitor.ViewModels;

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
      builder.RegisterType<ExportViewModel>().SingleInstance();
      builder.RegisterType<GroupAddressImportViewModel>().SingleInstance();
      builder.RegisterType<HomeViewModel>().SingleInstance();
      builder.RegisterType<InterfacesViewModel>().SingleInstance();
      builder.RegisterType<SecurtiyViewModel>().SingleInstance();

      container = builder.Build();
    }

    public T Resolve<T>() => container.Resolve<T>();

    public object Resolve(Type type) => container.Resolve(type);
  }
}
