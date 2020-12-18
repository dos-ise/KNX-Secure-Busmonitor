using System;
using Autofac;
using Busmonitor.Model;
using Busmonitor.ViewModels;
using Xamarin.Forms;

namespace Busmonitor.Bootstrap
{
  class AppContainer
  {
    private static IContainer container;

    public AppContainer()
    {
      var builder = new ContainerBuilder();
      // services
      builder.RegisterType<Settings>().SingleInstance();
      builder.RegisterType<TelegrammList>().SingleInstance();
      
      // view models
      builder.RegisterType<ExportViewModel>().SingleInstance();
      builder.RegisterType<GroupAddressImportViewModel>().SingleInstance();
      builder.RegisterType<HomeViewModel>().SingleInstance();
      builder.RegisterType<AppShellViewModel>().SingleInstance();
      builder.RegisterType<InterfacesViewModel>().SingleInstance();
      builder.RegisterType<SecurtiyViewModel>().SingleInstance();
      builder.RegisterInstance(GetPlattformImplementation<INotificationManager>()).SingleInstance();
      
      container = builder.Build();
    }

    private T GetPlattformImplementation<T>() where T : class
    {
      return DependencyService.Get<T>();
    }

    public T Resolve<T>() => container.Resolve<T>();

    public object Resolve(Type type) => container.Resolve(type);
  }
}
