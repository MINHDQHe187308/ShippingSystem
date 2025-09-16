using System;
using System.Linq;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace ASP.Utilss
{
    public static class ResourceValidator
    {
        public static void ValidateResourceKeys(params Type[] modelTypes)
        {
            foreach (var modelType in modelTypes)
            {
                var props = modelType.GetProperties();
                foreach (var prop in props)
                {
                    var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();
                    if (displayAttr != null)
                    {
                        var resourceType = displayAttr.ResourceType;
                        var resourceKey = displayAttr.Name;

                        if (resourceType != null && !string.IsNullOrEmpty(resourceKey))
                        {
                            // Tìm property trong resource class
                            var propInfo = resourceType.GetProperty(
                                resourceKey,
                                BindingFlags.Static | BindingFlags.Public
                            );

                            if (propInfo == null)
                            {
                                Debug.WriteLine(
                                    $"❌ Missing resource key: '{resourceKey}' in {resourceType.FullName} (Model: {modelType.Name}, Property: {prop.Name})"
                                );
                            }
                            else
                            {
                                var value = propInfo.GetValue(null)?.ToString();

                                if (string.IsNullOrEmpty(value))
                                {
                                    Debug.WriteLine(
                                        $"⚠ Resource key '{resourceKey}' found in {resourceType.FullName} but value is NULL or empty (Model: {modelType.Name}, Property: {prop.Name})"
                                    );
                                }
                                else
                                {
                                    Debug.WriteLine(
                                        $"✅ Resource key '{resourceKey}' = '{value}' in {resourceType.FullName}"
                                    );
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
