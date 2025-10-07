using System;

namespace AccountingSystem.Business.Exceptions
{
    /// <summary>
    /// استثناء أساسي لجميع الأخطاء المحاسبية
    /// Base exception for all business logic errors
    /// </summary>
    public class BusinessException : Exception
    {
        public string ErrorCode { get; set; }
        public object? ErrorData { get; set; }

        public BusinessException(string message) 
            : base(message)
        {
            ErrorCode = "BUSINESS_ERROR";
        }

        public BusinessException(string message, Exception innerException) 
            : base(message, innerException)
        {
            ErrorCode = "BUSINESS_ERROR";
        }

        public BusinessException(string errorCode, string message) 
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public BusinessException(string errorCode, string message, Exception innerException) 
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// استثناء عند عدم العثور على كيان
    /// Exception when entity is not found
    /// </summary>
    public class EntityNotFoundException : BusinessException
    {
        public Type? EntityType { get; }
        public object EntityId { get; }

        public EntityNotFoundException(Type entityType, object entityId)
            : base("ENTITY_NOT_FOUND", $"لم يتم العثور على {entityType.Name} برقم {entityId}")
        {
            EntityType = entityType;
            EntityId = entityId;
        }

        public EntityNotFoundException(string entityName, object entityId)
            : base("ENTITY_NOT_FOUND", $"لم يتم العثور على {entityName} برقم {entityId}")
        {
            EntityId = entityId;
        }
    }

    /// <summary>
    /// استثناء عند فشل التحقق من صحة البيانات
    /// Exception when validation fails
    /// </summary>
    public class ValidationException : BusinessException
    {
        public string[] ValidationErrors { get; }

        public ValidationException(string message)
            : base("VALIDATION_ERROR", message)
        {
            ValidationErrors = new[] { message };
        }

        public ValidationException(string[] errors)
            : base("VALIDATION_ERROR", "فشل التحقق من صحة البيانات")
        {
            ValidationErrors = errors;
        }

        public ValidationException(string field, string message)
            : base("VALIDATION_ERROR", $"{field}: {message}")
        {
            ValidationErrors = new[] { message };
        }
    }

    /// <summary>
    /// استثناء عند انتهاك قواعد العمل
    /// Exception when business rules are violated
    /// </summary>
    public class BusinessRuleViolationException : BusinessException
    {
        public string RuleName { get; }

        public BusinessRuleViolationException(string ruleName, string message)
            : base("BUSINESS_RULE_VIOLATION", message)
        {
            RuleName = ruleName;
        }

        public BusinessRuleViolationException(string ruleName, string message, Exception innerException)
            : base("BUSINESS_RULE_VIOLATION", message, innerException)
        {
            RuleName = ruleName;
        }
    }

    /// <summary>
    /// استثناء عند فشل العمليات المحاسبية
    /// Exception when accounting operations fail
    /// </summary>
    public class AccountingException : BusinessException
    {
        public string OperationType { get; }

        public AccountingException(string operationType, string message)
            : base("ACCOUNTING_ERROR", message)
        {
            OperationType = operationType;
        }

        public AccountingException(string operationType, string message, Exception innerException)
            : base("ACCOUNTING_ERROR", message, innerException)
        {
            OperationType = operationType;
        }
    }

    /// <summary>
    /// استثناء عند عدم وجود صلاحيات كافية
    /// Exception when user lacks required permissions
    /// </summary>
    public class InsufficientPermissionsException : BusinessException
    {
        public string RequiredPermission { get; }
        public string Username { get; }

        public InsufficientPermissionsException(string username, string permission)
            : base("INSUFFICIENT_PERMISSIONS", $"المستخدم {username} لا يملك الصلاحية: {permission}")
        {
            Username = username;
            RequiredPermission = permission;
        }
    }

    /// <summary>
    /// استثناء عند فشل عمليات قاعدة البيانات
    /// Exception when database operations fail
    /// </summary>
    public class DataAccessException : BusinessException
    {
        public string Operation { get; }

        public DataAccessException(string operation, string message)
            : base("DATA_ACCESS_ERROR", message)
        {
            Operation = operation;
        }

        public DataAccessException(string operation, string message, Exception innerException)
            : base("DATA_ACCESS_ERROR", message, innerException)
        {
            Operation = operation;
        }
    }

    /// <summary>
    /// استثناء عند تجاوز حد المخزون
    /// Exception when stock is insufficient
    /// </summary>
    public class InsufficientStockException : BusinessException
    {
        public int ProductId { get; }
        public string ProductName { get; }
        public decimal RequestedQuantity { get; }
        public decimal AvailableQuantity { get; }

        public InsufficientStockException(int productId, string productName, decimal requested, decimal available)
            : base("INSUFFICIENT_STOCK", 
                  $"مخزون غير كافٍ للمنتج {productName}. المطلوب: {requested}، المتاح: {available}")
        {
            ProductId = productId;
            ProductName = productName;
            RequestedQuantity = requested;
            AvailableQuantity = available;
        }
    }

    /// <summary>
    /// استثناء عند محاولة حذف كيان مرتبط
    /// Exception when trying to delete an entity with dependencies
    /// </summary>
    public class EntityHasDependenciesException : BusinessException
    {
        public Type EntityType { get; }
        public object EntityId { get; }
        public string[] Dependencies { get; }

        public EntityHasDependenciesException(Type entityType, object entityId, string[] dependencies)
            : base("ENTITY_HAS_DEPENDENCIES", 
                  $"لا يمكن حذف {entityType.Name} لوجود بيانات مرتبطة: {string.Join(", ", dependencies)}")
        {
            EntityType = entityType;
            EntityId = entityId;
            Dependencies = dependencies;
        }
    }

    /// <summary>
    /// استثناء عند محاولة إنشاء كيان مكرر
    /// Exception when trying to create a duplicate entity
    /// </summary>
    public class DuplicateEntityException : BusinessException
    {
        public Type? EntityType { get; }
        public string DuplicateField { get; }
        public object DuplicateValue { get; }

        public DuplicateEntityException(Type entityType, string field, object value)
            : base("DUPLICATE_ENTITY", 
                  $"{entityType.Name} موجود بالفعل بنفس {field}: {value}")
        {
            EntityType = entityType;
            DuplicateField = field;
            DuplicateValue = value;
        }

        public DuplicateEntityException(string entityName, string field, object value)
            : base("DUPLICATE_ENTITY", 
                  $"{entityName} موجود بالفعل بنفس {field}: {value}")
        {
            DuplicateField = field;
            DuplicateValue = value;
        }
    }
}
