namespace System.Reflection
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class AssemblyCompanyAttribute : Attribute
    {
        private String m_company;

        public String Company
        {
            get { return m_company; }
        }

        public AssemblyCompanyAttribute(String company)
        {
            m_company = company;
        }
    }

    public class AssemblyConfigurationAttribute : Attribute
    {
        private String m_configuration;

        public AssemblyConfigurationAttribute(String configuration)
        {
            m_configuration = configuration;
        }

        public String Configuration
        {
            get { return m_configuration; }
        }
    }

    public class AssemblyFileVersionAttribute : Attribute
    {
        private String _version;

        public AssemblyFileVersionAttribute(String version)
        {
            _version = version;
        }

        public String Version
        {
            get { return _version; }
        }
    }

    public class AssemblyInformationalVersionAttribute : Attribute
    {
        private String m_informationalVersion;

        public AssemblyInformationalVersionAttribute(String informationalVersion)
        {
            m_informationalVersion = informationalVersion;
        }

        public String InformationalVersion
        {
            get { return m_informationalVersion; }
        }
    }

    public class AssemblyProductAttribute : Attribute
    {
        private String m_product;

        public AssemblyProductAttribute(String product)
        {
            m_product = product;
        }

        public String Product
        {
            get { return m_product; }
        }
    }

    public class AssemblyTitleAttribute : Attribute
    {
        private String m_title;

        public AssemblyTitleAttribute(String title)
        {
            m_title = title;
        }

        public String Title
        {
            get { return m_title; }
        }
    }

    public class AssemblyVersionAttribute : Attribute
    {
        private String m_version;

        public AssemblyVersionAttribute(String version)
        {
            m_version = version;
        }

        public String Version
        {
            get { return m_version; }
        }
    }

    public class AssemblyCopyrightAttribute : Attribute
    {
        private String m_copyright;

        public AssemblyCopyrightAttribute(String copyright)
        {
            m_copyright = copyright;
        }

        public String Copyright
        {
            get { return m_copyright; }
        }
    }

    public class AssemblyDescriptionAttribute : Attribute
    {
        private String m_description;

        public AssemblyDescriptionAttribute(String description)
        {
            m_description = description;
        }

        public String Description
        {
            get { return m_description; }
        }
    }

    public class AssemblyMetadataAttribute : Attribute
    {
        private String m_metadata;
        public String m_value;

        public AssemblyMetadataAttribute(String metadata, string value = "")
        {
            m_metadata = metadata;
            m_value = value;
        }

        public String Metadata
        {
            get { return m_metadata; }
        }


        public String Value
        {
            get { return m_value; }
        }
    }

}
