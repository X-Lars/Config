using System;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Config
{
    /// <summary>
    /// Provides an intuitive way for storing and retreiving a configuration or any other POCO class into and from the app config.
    /// </summary>
    /// <typeparam name="T">A <see cref="T"/> specifying the type of class to store or retreive.</typeparam>
    public static class Config<T> where T : class, new()
    {
        #region Fields

        /// <summary>
        /// Stores an instance of to the provided configuration type.
        /// </summary>
        private static T _Config;

        /// <summary>
        /// Stores whether the configuration is modified.
        /// </summary>
        private static bool _IsDirty = false;

        /// <summary>
        /// Stores wheter the configuration is initialized.
        /// </summary>
        private static bool _IsInitialized = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Creates and initialize a new <see cref="Config{T}"/> instance.
        /// </summary>
        static Config()
        {
            _Config = new T();

            // Bind the process exit event handler to be able to save the configuration when the application closes
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainProcessExit;

            // Bind the PropertyChanged event if the configuration type implements the INotifyPropertyChanged interface
            if (_Config is INotifyPropertyChanged)
            {
                IsAutoSaveEnabled = true;
                ((INotifyPropertyChanged)_Config).PropertyChanged += PropertyChanged;
            }
            else
            {
                IsAutoSaveEnabled = false;
                Debug.Print($"CONFIG WARNING: <{typeof(T).Name}> class doesn't implement the INotifyPropertyChanged interface, " +
                            $"use {nameof(Config<T>)}<{typeof(T).Name}>().{nameof(Save)}() " +
                            $"to save configuration changes to the App.config.");
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the configuration changes are automatically saved.
        /// </summary>
        /// <remarks><i>Changes are automatically saved if the configuration type implements the <see cref="INotifyPropertyChanged"/> interface.</i></remarks>
        public static bool IsAutoSaveEnabled { get; private set; }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the <see cref="Process.Exited"/> event to safe the configuration if <see cref="_IsAutoSaveEnabled"/> is false.
        /// </summary>
        /// <param name="sender">The <see cref="object"/> that raised the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> containing event data.</param>
        /// <remarks><i>Only catched if the process exits in a normal way.<br/>If <see cref="IsAutoSaveEnabled"/> is false make sure to call <see cref="Config{T}.Save"/> after changes are made.</i></remarks>
        private static void CurrentDomainProcessExit(object sender, EventArgs e)
        {
            if (IsAutoSaveEnabled == false && _IsDirty == true)
                Save();
        }

        /// <summary>
        /// Handles the PropertyChanged event if the configuration class type implements the <see cref="INotifyPropertyChanged"/> interface to auto save changes to the App.config.
        /// </summary>
        /// <param name="sender">The <see cref="object"/> that raised the event.</param>
        /// <param name="e">A <see cref="PropertyChangedEventArgs"/> containing event data.</param>
        private static void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!_IsInitialized)
                return;

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var configSection = (ConfigSection)config.GetSection(typeof(T).Name);

            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var property in properties)
            {
                if (property.Name == e.PropertyName)
                {
                    if (property.GetValue(_Config) == null)
                        ((ConfigElement)configSection.Settings[e.PropertyName]).Value = string.Empty;
                    else
                        ((ConfigElement)configSection.Settings[property.Name]).Value = property.GetValue(_Config).ToString();

                    config.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(typeof(T).Name);

                    break;
                }
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a new configuration in the App.config or returns an existing configuration if present.
        /// </summary>
        /// <returns>The created or existing class of type <see cref="T"/> with default or existing property values.</returns>
        public static T Create()
        {
            return Get();
        }

        /// <summary>
        /// Stores the provided property value into the App.config.
        /// </summary>
        /// <param name="key">A <see cref="string"/> specifying the key of the property.</param>
        /// <param name="value">A <see cref="string"/> specifying the value to store.</param>
        public static void SetProperty(string key, string value)
        {
            if (_Config == null)
            {
                Debug.Print($"CONFIG ERROR: {nameof(Config<T>)}<{typeof(T).Name}>.{nameof(SetProperty)}() no configuration found.");
                return;
            }

            if (key == null || key == string.Empty)
            {
                Debug.Print($"CONFIG ERROR: {nameof(Config<T>)}<{typeof(T).Name}>.{nameof(SetProperty)}() no valid key provided.");
                return;
            }

            if (value == null)
                value = string.Empty;

            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            //Set the internal configuration property value
            foreach (var property in properties)
            {
                if(property.Name == key)
                {
                    property.SetValue(_Config, value);
                    PropertyChanged(null, new PropertyChangedEventArgs(key));
                    return;
                }
            }

            Debug.Print($"CONFIG ERROR: {nameof(Config<T>)}<{typeof(T).Name}>.{nameof(SetProperty)}() {nameof(key)} \"{key}\" not found.");
        }
        
        /// <summary>
        /// Stores the current configuration into the App.config, if no configuration exist a new configuration is created with default values.<br/>
        /// If the <paramref name="updateConfig"/> parameter is provided, the provided configuration class will be used to update the App.config.
        /// </summary>
        /// <param name="updateConfig">A configuration type <see cref="T"/> to overwrite the current or create a new configuration from.</param>
        /// <returns>The class of type <see cref="T"/> with the updated properties.</returns>
        public static T Save(T updateConfig = null)
        {
            // Create a new instance of the configuration class if it doesn't exist
            if(updateConfig == null)
            {
                if (_Config == null)
                    _Config = new T();

                // Prevents overwriting existing configuration
                if(!_IsInitialized)
                    _Config = Get();
            }
            else
            {
                _Config = updateConfig;
            }

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // Adds the section to the config sections if doesn't exist
            if (config.Sections[typeof(T).Name] == null)
            {
                config.Sections.Add(typeof(T).Name, new ConfigSection());
            }

            var configSection = (ConfigSection)config.GetSection(typeof(T).Name);

            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var property in properties)
            {
                if (property.GetValue(_Config) == null)
                    property.SetValue(_Config, string.Empty);

                if (configSection.Settings[property.Name] == null)
                {
                    configSection.Settings[property.Name] = new ConfigElement { Key = property.Name, Value = property.GetValue(_Config).ToString() };
                }
                else
                {
                    ((ConfigElement)configSection.Settings[property.Name]).Value = property.GetValue(_Config).ToString();
                }
            }

            config.Save();
            ConfigurationManager.RefreshSection(typeof(T).Name);

            _IsDirty = false;

            Debug.Print($"CONFIG INFO: The <{typeof(T).Name}> configuration is saved.");

            return _Config;
        }

        /// <summary>
        /// Gets the configuration from the App.config or creates a new configuration in the App.config with default values if no configuration is found.
        /// </summary>
        /// <param name="configuration">A <see cref="T"/> to store the configuration.</param>
        /// <returns>A class of type <see cref="T"/> initialized with the properties from the App.config.</returns>
        public static T Get()
        {
            if (_IsInitialized)
                return _Config;

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var configSection = (ConfigSection)config.GetSection(typeof(T).Name);

            if (!IsAutoSaveEnabled)
                _IsDirty = true;

            if (_Config == null)
            {
                _Config = new T();
                _IsInitialized = false;
            }

            if (configSection != null)
            {
                PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                if (configSection.Settings.Count == 0)
                {
                    Debug.Print($"CONFIG INFO: No configuration found for <{typeof(T)}> in App.config, configuration created with default property values.");

                    // The section is empty, save the configuration
                    Save(_Config);
                }
                else if (configSection.Settings.Count != properties.Length)
                {
                    // Property count doesn't match, the configuration might contain a modified class
                    throw new ArgumentOutOfRangeException($"Invalide <{typeof(T).Name}> in App.config, element count doesn't match the number of properties.");
                }
                else
                {
                    // Initialize the provide configuration class properties with the property values from the App.config
                    foreach (ConfigElement element in configSection.Settings)
                    {
                        PropertyInfo property = properties.Where(p => p.Name == element.Key).First();

                        if (property != null)
                        {
                            if (property.PropertyType == typeof(string))
                            {
                                property.SetValue(_Config, element.Value);
                            }
                            else if (property.PropertyType == typeof(int))
                            {
                                property.SetValue(_Config, int.Parse(element.Value));
                            }
                            else if (property.PropertyType == typeof(float))
                            {
                                property.SetValue(_Config, float.Parse(element.Value));
                            }
                            else if (property.PropertyType.IsEnum)
                            {
                                property.SetValue(_Config, Enum.Parse(property.PropertyType, element.Value));
                            }
                            else if (property.PropertyType == typeof(bool))
                            {
                                property.SetValue(_Config, Convert.ToBoolean(element.Value));
                            }
                            else if (property.PropertyType == typeof(double))
                            {
                                property.SetValue(_Config, double.Parse(element.Value));
                            }
                            else
                                // Unsupported property type
                                throw new ArgumentException($"{nameof(Config<T>)} doesn't support {property.PropertyType}s.", property.Name);
                        }
                    }
                }
            }
            else
            {
                Debug.Print($"CONFIG INFO: No configuration found for <{typeof(T)}> in App.config, configuration created with default property values.");
                
                config.Sections.Add(typeof(T).Name, new ConfigSection());
                config.Save();

                ConfigurationManager.RefreshSection(typeof(T).Name);

                Save(_Config);
            }

            _IsInitialized = true;
            
            return _Config;
        }

        /// <summary>
        /// Prints the current in memory configuration to the <see cref="Console"/>.
        /// </summary>
        public static void Print()
        {
            if (_Config == null)
            {
                Debug.Print($"CONFIG WARNING: No configuration to print.");
                return;
            }

            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Console.WriteLine($"<{_Config.GetType().Name}>");
            Console.WriteLine($"  <Properties>");

            foreach (var property in properties)
            {
                if (property.GetValue(_Config) != null)
                    Console.WriteLine($"    <Add Key=\"{property.Name}\" Value=\"{property.GetValue(_Config).ToString()}\"/>");
                else
                    Console.WriteLine($"    <Add Key=\"{property.Name}\" Value=\"\"/>");
            }

            Console.WriteLine($"  </Properties>");
            Console.WriteLine($"</{_Config.GetType().Name}>");

        }

        #endregion
    }

    /// <summary>
    /// Defines the configuration element structure to store a class property by key value pair.
    /// </summary>
    /// <remarks><i>This will represent the &lt;Add&gt; tags in the app config, set by the <see cref="ConfigElements"/>.</i></remarks>
    internal class ConfigElement : ConfigurationElement
    {
        #region Properties

        /// <summary>
        /// Gets or sets the key associated with name of the property.
        /// </summary>
        [ConfigurationProperty(nameof(Key), IsKey = true, IsRequired = true)]
        public string Key
        {
            get { return (string)base[nameof(Key)]; }
            set 
            { 
                base[nameof(Key)] = value;
            }
        }

        /// <summary>
        /// Gets or sets the value associated with the property value.
        /// </summary>
        [ConfigurationProperty(nameof(Value))]
        public string Value
        {
            get { return (string)base[nameof(Value)]; }
            set 
            { 
                base[nameof(Value)] = value;
                
            }
        }

        #endregion
    }

    /// <summary>
    /// Defines the configuration element collection to store a collection of <see cref="ConfigElement"/>s.
    /// </summary>
    /// <remarks><i>This will represent the &lt;Properties&gt; tag in the app config, set by the <see cref="ConfigSection"/>.</i></remarks>
    internal class ConfigElements : ConfigurationElementCollection
    {
        #region Properties

        /// <summary>
        /// Indexer to get or set an element from the collection with the specified key.
        /// </summary>
        /// <param name="key">A <see cref="string"/> specifying the element key.</param>
        /// <returns>A <see cref="ConfigurationElement"/> containing the property key and value.</returns>
        public new ConfigurationElement this[string key]
        {
            get
            {
                return BaseGet(key);
            }

            set
            {
                // Remove the existing configuration element if it exists
                if (BaseGet(key) != null) BaseRemoveAt(BaseIndexOf(BaseGet(key)));

                // Add the new configuration element
                BaseAdd(value);
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).Save();

            }
        }

        #endregion

        #region Methods

        #region Methods: Public

        /// <summary>
        /// Clears the collection of elements.
        /// </summary>
        public void Clear()
        {
            BaseClear();
        }

        #endregion

        #region Methods: Overrides

        /// <summary>
        /// Implements the abstract base method to create a new element to store in the collection.
        /// </summary>
        /// <returns></returns>
        protected override ConfigurationElement CreateNewElement()
        {
            return new ConfigElement();
        }

        /// <summary>
        /// Implements the abstract base method to get the key associated with the element.
        /// </summary>
        /// <param name="element">A <see cref="ConfigurationElement"/> to get the key from.</param>
        /// <returns>An <see cref="object"/> representing the key of the provided <see cref="ConfigurationElement"/>.</returns>
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ConfigElement)element).Key;
        }

        #endregion

        #endregion
    }

    /// <summary>
    /// Defines the configuration section structure to contain a collection of <see cref="ConfigElement"/>s.
    /// </summary>
    /// <remarks><i>This will represent the &lt;ClassName&gt; tag in the app config set by the <see cref="Config{T}.Set(T)>"/>.</i></remarks>
    internal class ConfigSection : ConfigurationSection
    {
        #region Cosntants

        /// <summary>
        /// Defines the name of the section containing the collection of properties.
        /// </summary>
        private const string SECTION_NAME = "Properties";

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the collection of properties in the app config.
        /// </summary>
        [ConfigurationProperty(SECTION_NAME)]
        [ConfigurationCollection(typeof(ConfigElements))]
        public ConfigElements Settings
        {
            get
            {
                return (ConfigElements)base[SECTION_NAME];
            }
            set
            {
                base[SECTION_NAME] = value;

            }
        }

        #endregion
    }
}
