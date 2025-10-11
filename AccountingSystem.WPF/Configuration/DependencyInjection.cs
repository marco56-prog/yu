using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AccountingSystem.WPF.Services;
using AccountingSystem.WPF.ViewModels;
using System;
using System.Windows;
using AccountingSystem.Business;

namespace AccountingSystem.WPF
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAccountingSystemServices(this IServiceCollection services)
        {
            // خدمات النظام الأساسية
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddTransient<ISecurityService, SecurityService>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<SidebarViewModel>();

            // Windows - يتم إنشاؤها كـ Transient لأنها قد تفتح عدة مرات
            services.AddTransient<MainWindow>();

            return services;
        }
    }

    public class AppServiceProvider
    {
        private static IServiceProvider? _serviceProvider;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public static T GetService<T>() where T : class
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("Service provider not initialized. Call Initialize first.");

            return _serviceProvider.GetRequiredService<T>();
        }

        public static object GetService(Type serviceType)
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("Service provider not initialized. Call Initialize first.");

            return _serviceProvider.GetRequiredService(serviceType);
        }
    }
}