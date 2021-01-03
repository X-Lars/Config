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
        /// Stores the name of the configuration section.
        /// </summary>
        private static readonly string _ConfigName = typeof(T).Name;

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
                Debug.Print($"CONFIG WARNING: <{_ConfigName}> class doesn't implement the INotifyPropertyChanged interface, " +
                            $"use {nameof(Config<T>)}<{_ConfigName}>().{nameof(Save)}() " +
                            $"to save configuration changes to the App.config.");
            }

            Initialize();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the configuration changes are automatically saved.
        /// </summary>
        /// <remarks><i>Changes are automatically saved if the configuration type implements the <see cref="INotifyPropertyChanged"/> interface.</i></remarks>
        public static bool IsAutoSaveEnabled { get; private set; }

        /// <summary>
        /// Gets wheter the configuration has unsaved changes.
        /// </summary>
        public static bool IsDirty { get; private set; }

        /// <summary>
        /// Gets the <see cref="Config{T}"/> associated class from the App.config to get or set a value.
        /// </summary>
        /// <returns>The class <see cref="T"/> from the App.config.</returns>
        public static T Get
        {
            get
            {
                if (!IsAutoSaveEnabled) IsDirty = true;

                return _Config;
            }
        }

        /// <summary>
        /// Gets the <see cref="Config{T}"/> associated class from the App.config to get or set a value.
        /// </summary>
        /// <returns>The class <see cref="T"/> from the App.config.</returns>
        public static T Set
        {
            get
            {
                if (!IsAutoSaveEnabled) IsDirty = true;

                return _Config;
            }
        }

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
            if (IsAutoSaveEnabled == false && IsDirty == true)
                Save();
        }

        /// <summary>
        /// Handles the PropertyChanged event if the configuration class type implements the <see cref="INotifyPropertyChanged"/> interface to auto save changes to the App.config.
        /// </summary>
        /// <param name="sender">The <see cref="object"/> that raised the event.</param>
        /// <param name="e">A <see cref="PropertyChangedEventArgs"/> containing event data.</param>
        /// <remarks><i></i></remarks>
        private static void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!_IsInitialized)
                return;

            PropertyInfo property = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.Name == e.PropertyName).FirstOrDefault();

            if (property.GetValue(_Config) == null)
                property.SetValue(_Config, string.Empty);

            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConfigSection configSection = (ConfigSection)configuration.GetSection(_ConfigName);

            configSection.Settings[property.Name] = new ConfigElement { Key = property.Name, Value = property.GetValue(_Config).ToString() };
            configuration.Save(ConfigurationSaveMode.Modified);

            ConfigurationManager.RefreshSection(_ConfigName);

            Debug.Print($"CONFIG INFO: Property <{property.Name}> is saved.");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes or creates a new <see cref="Config{T}"/> in the App.config.
        /// </summary>
        /// <exception cref="ConfigurationException">When App.config elements don't match <see cref="Config{T}"/>'s type properties.</exception>
        private static void Initialize()
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // Adds the section to the config sections if doesn't exist
            if (configuration.Sections[_ConfigName] == null)
            {
                configuration.Sections.Add(_ConfigName, new ConfigSection());
                configuration.Save(ConfigurationSaveMode.Minimal);
            }

            ConfigSection configSection = (ConfigSection)configuration.GetSection(_ConfigName);

            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            if (configSection.Settings.Count == 0)
            {
                Debug.Print($"CONFIG INFO: Configuration for <{typeof(T)}> not found in App.config, new configuration created with default property values.");

                Save();
            }
            else if (configSection.Settings.Count != properties.Length)
            {
                _IsInitialized = false;

                // Property count doesn't match, the configuration might contain a modified class
                throw new ConfigurationException($"Invalid <{_ConfigName}> configuration in App.config, number of elements don't match <{_ConfigName}> property count.");
            }
            else
            {
                // Initialize the internal configuration from the App.config
                foreach (ConfigElement element in configSection.Settings)
                {
                    PropertyInfo property = properties.Where(p => p.Name == element.Key).FirstOrDefault();

                    if (property == null)
                        throw new ConfigurationException($"Invalid <{_ConfigName}> configuration in App.config, elements don't match <{_ConfigName}> properties.");

                    try
                    {
                        // Match the configuration property types
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
                    catch(Exception)
                    {
                        throw new ConfigurationException($"Unable to parse <{element.Value.GetType().Name}> {element.Value} to <{property.PropertyType.Name}>.");
                    }
                }

                Debug.Print($"CONFIG INFO: Configuration for <{_ConfigName}> is initialized from the App.config.");
            }

            _IsInitialized = true;
        }

        /// <summary>
        /// Sets and saves the provided property.
        /// </summary>
        /// <param name="key">A <see cref="string"/> specifying the property to set.</param>
        /// <param name="value">An <see cref="object"/> specifying the property value.</param>
        public static void SetProperty(string key, object value)
        {
            if (key == null || key == string.Empty)
            {
                throw new ConfigurationException($"{nameof(Config<T>)}<{_ConfigName}>.{nameof(Set)}() no valid key provided.");
            }

            if (value == null)
                value = string.Empty;

            PropertyInfo property = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.Name == key).FirstOrDefault();

            if (property == null)
                throw new ConfigurationException($"{nameof(Config<T>)}<{_ConfigName}>.{nameof(Set)}() key not found.");

            if (property.PropertyType != value.GetType())
                throw new ConfigurationException($"Type mismatch, provided {nameof(value)} for {key} has to be of type {property.PropertyType.Name}.");
            
            // Set the internal configuration value
            property.SetValue(_Config, value);

            // Use the property changed method to save the value to the App.config
            PropertyChanged(null, new PropertyChangedEventArgs(key));
        }

        /// <summary>
        /// Saves the <see cref="Config{T}"/> associated class into the App.config, if <paramref name="config"/> is provided the current class configuration is overwritten.
        /// </summary>
        /// <param name="config">A class <see cref="T"/> to save..</param>
        /// <returns>A class <see cref="T"/> with updated properties.</returns>
        public static T Save(T config = null)
        {
            if(config != null)
                _Config = config;

            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConfigSection configSection = (ConfigSection)configuration.GetSection(_ConfigName);

            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (property.GetValue(_Config) == null)
                    property.SetValue(_Config, string.Empty);

                configSection.Settings[property.Name] = new ConfigElement { Key = property.Name, Value = property.GetValue(_Config).ToString() };
            }

            configuration.Save();
            ConfigurationManager.RefreshSection(_ConfigName);

            IsDirty = false;

            Debug.Print($"CONFIG INFO: Configuration for <{_ConfigName}> is saved.");

            return _Config;
        }
       
        /// <summary>
        /// Prints the current configuration to the <see cref="Console"/>.
        /// </summary>
        public static void Print()
        {
            PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            Console.WriteLine($"<{_ConfigName} AutoSaveEnabled=\"{IsAutoSaveEnabled}\" IsDirty=\"{IsDirty}\">");
            Console.WriteLine($"  <Settings>");

            foreach (var property in properties)
            {
                Console.WriteLine($"    <Add Key=\"{property.Name}\" Value=\"{property.GetValue(_Config)}\"/>");
            }

            Console.WriteLine($"  </Settings>");
            Console.WriteLine($"</{_ConfigName}>");
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
            set { base[nameof(Key)] = value; }
        }

        /// <summary>
        /// Gets or sets the value associated with the property value.
        /// </summary>
        [ConfigurationProperty(nameof(Value))]
        public string Value
        {
            get { return (string)base[nameof(Value)]; }
            set { base[nameof(Value)] = value; }
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
    /// <remarks><i>This will represent the &lt;ClassName&gt; tag in the app config set by the <see cref="Config{T}"/>.</i></remarks>
    internal class ConfigSection : ConfigurationSection
    {
        #region Properties

        /// <summary>
        /// Gets or sets the collection of properties in the app config.
        /// </summary>
        [ConfigurationProperty(nameof(Settings))]
        [ConfigurationCollection(typeof(ConfigElements))]
        public ConfigElements Settings
        {
            get
            {
                return (ConfigElements)base[nameof(Settings)];
            }
            set
            {
                base[nameof(Settings)] = value;

            }
        }

        #endregion
    }

    /// <summary>
    /// Defines a <see cref="Config{T}"/> specific exception.
    /// </summary>
    public class ConfigurationException : Exception 
    {
        public ConfigurationException(string message) : base(message) { }
    }
}
