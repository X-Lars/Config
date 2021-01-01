using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Examples
{
    /// <summary>
    /// Example configuration file implementing <see cref="INotifyPropertyChanged"/> to enable auto saving of changed properties.
    /// </summary>
    public class ExampleConfigAuto : INotifyPropertyChanged
    {
        public enum TestEnum
        {
            Test1,
            Test2,
            Test3,
            Test4,
            Test5
        }

        private int _Int;
        private string _String;
        private TestEnum _Enum;
        private bool _Bool;

        public int ExampleInt 
        { 
            get { return _Int; } 
            set
            {
                _Int = value;
                NotifyPropertyChanged();
            }
        }


        public string ExampleString
        {
            get { return _String; }
            set 
            { 
                _String = value;
                NotifyPropertyChanged();
            }
        }


        public TestEnum ExampleEnum
        {
            get { return _Enum; }
            set
            {
                _Enum = value;
                NotifyPropertyChanged();
            }
        }


        public bool ExampleBool
        {
            get { return _Bool; }
            set
            {
                _Bool = value;
                NotifyPropertyChanged();
            }
        }
    
        #region INotifyPropertyChanged

        /// <summary>
        /// Event raised when a property value is changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the specified property.
        /// </summary>
        /// <param name="propertyName">A <see cref="string"/> containing the name of the property that is changed.</param>
        /// <remarks><i>If no property name is specified, the actual name of the property in code is used.</i></remarks>
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// Example configuration file that has to be manual saved.
    /// </summary>
    public class ExampleConfigManual
    {
        public int ID { get; set; }
        public string Name { get; set; }
    }
}
