using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Reflection;

namespace AccountingSystem.Data.Extensions
{
    /// <summary>
    /// امتدادات لتحسين معالجة النصوص العربية في Entity Framework
    /// ضمان استخدام NVARCHAR بدلاً من VARCHAR للنصوص العربية
    /// </summary>
    public static class ArabicTextExtensions
    {
        /// <summary>
        /// تكوين جميع خصائص النصوص لاستخدام NVARCHAR
        /// </summary>
        public static void ConfigureArabicTextSupport(this ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(string))
                    {
                        var builder = modelBuilder.Entity(entityType.ClrType)
                            .Property(property.Name);
                        
                        // تأكد من استخدام NVARCHAR
                        builder.IsUnicode(true);
                        
                        // تعيين طول افتراضي مناسب
                        if (property.GetMaxLength() == null)
                        {
                            var columnName = property.Name.ToLower();
                            if (columnName.Contains("name") || columnName.Contains("title"))
                            {
                                builder.HasMaxLength(200);
                            }
                            else if (columnName.Contains("description") || columnName.Contains("note"))
                            {
                                builder.HasMaxLength(1000);
                            }
                            else if (columnName.Contains("address"))
                            {
                                builder.HasMaxLength(500);
                            }
                            else
                            {
                                builder.HasMaxLength(100);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// تكوين خاصية نص لدعم العربية مع تخصيص الطول
        /// </summary>
        public static PropertyBuilder<string?> ConfigureArabicText<T>(
            this EntityTypeBuilder<T> builder, 
            string propertyName, 
            int maxLength = 200) where T : class
        {
            return (PropertyBuilder<string?>)builder.Property(propertyName)
                .HasMaxLength(maxLength)
                .IsUnicode(true)
                .HasColumnType($"nvarchar({maxLength})");
        }

        /// <summary>
        /// تكوين خاصية نص طويل لدعم العربية
        /// </summary>
        public static PropertyBuilder<string?> ConfigureArabicLongText<T>(
            this EntityTypeBuilder<T> builder, 
            string propertyName) where T : class
        {
            return (PropertyBuilder<string?>)builder.Property(propertyName)
                .IsUnicode(true)
                .HasColumnType("nvarchar(max)");
        }
    }
    
    // تم تعطيل UnicodeStringConvention مؤقتاً - سيتم استخدام ConfigureConventions بدلاً منها
}