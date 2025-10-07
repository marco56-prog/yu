using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.Business
{
    public interface ISmartNotificationService
    {
        Task<IEnumerable<Notification>> GetAllNotificationsAsync();
        Task<Notification?> GetNotificationByIdAsync(int id);
        Task<bool> CreateNotificationAsync(Notification notification);
        Task<bool> UpdateNotificationAsync(Notification notification);
        Task<bool> DeleteNotificationAsync(int id);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
        Task<bool> DismissNotificationAsync(int notificationId, int userId);
        Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
    }

    public class SmartNotificationService : ISmartNotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SmartNotificationService> _logger;

        public SmartNotificationService(IUnitOfWork unitOfWork, ILogger<SmartNotificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<IEnumerable<Notification>> GetAllNotificationsAsync()
        {
            var repo = _unitOfWork.Repository<Notification>();
            return await repo.GetAllAsync();
        }

        public async Task<Notification?> GetNotificationByIdAsync(int id)
        {
            var repo = _unitOfWork.Repository<Notification>();
            return await repo.GetByIdAsync(id);
        }

        public async Task<bool> CreateNotificationAsync(Notification notification)
        {
            var repo = _unitOfWork.Repository<Notification>();
            await repo.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateNotificationAsync(Notification notification)
        {
            var repo = _unitOfWork.Repository<Notification>();
            repo.Update(notification);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteNotificationAsync(int id)
        {
            var repo = _unitOfWork.Repository<Notification>();
            var notification = await repo.GetByIdAsync(id);
            if (notification != null)
            {
                repo.Remove(notification);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var repo = _unitOfWork.Repository<Notification>();
            var notification = await repo.GetByIdAsync(notificationId);
            if (notification != null)
            {
                notification.Status = NotificationStatus.Read;
                notification.ReadDate = DateTime.Now;
                repo.Update(notification);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("تم تمييز الإشعار {NotificationId} كمقروء للمستخدم {UserId}", notificationId, userId);
                return true;
            }
            return false;
        }

        public async Task<bool> DismissNotificationAsync(int notificationId, int userId)
        {
            var repo = _unitOfWork.Repository<Notification>();
            var notification = await repo.GetByIdAsync(notificationId);
            if (notification != null)
            {
                notification.Status = NotificationStatus.Dismissed;
                repo.Update(notification);
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation("تم إخفاء الإشعار {NotificationId} للمستخدم {UserId}", notificationId, userId);
                return true;
            }
            return false;
        }

        public async Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(int userId)
        {
            var repo = _unitOfWork.Repository<Notification>();
            return await repo.FindAsync(n => n.Status == NotificationStatus.Unread);
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            var notifications = await GetUnreadNotificationsAsync(userId);
            return notifications.Count();
        }
    }
}
