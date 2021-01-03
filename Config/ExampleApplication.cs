using Config;
using System;

namespace Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            // Any call to the Config<T> class saves an instance of T into the App.config

            // Creates an auto save enabled, ExampleConfigAuto implements INotifyPropertyChanged, configuration with specified values
            // Get and set properties have equal functionality, the set property is only implemented for code clarity
            // Both can be used to get and set property values
            var enumValue = Config<ExampleConfigAuto>.Get.ExampleEnum;
            var intValue = Config<ExampleConfigAuto>.Set.ExampleInt = 3;

            Config<ExampleConfigAuto>.Set.ExampleString = "Name";
            Config<ExampleConfigAuto>.Get.ExampleBool = true;
            Config<ExampleConfigAuto>.Print();


            // Create the configuration file manually
            var manualConfig = new ExampleConfigManual();
            manualConfig.ID = 7;
            manualConfig.Name = "Just a name";

            // Save the manually created config
            Config<ExampleConfigManual>.Save(manualConfig);
            Config<ExampleConfigManual>.Print();

            // Modifies a property, but it is not saved since ExampleConfigManual doesn't implement INotifyPropertyChanged
            Config<ExampleConfigManual>.Set.Name = "Modified Name";
            Config<ExampleConfigManual>.Print();

            // Save the configuration
            Config<ExampleConfigManual>.Save();
            Config<ExampleConfigManual>.Print();

            // Modifies the property using SetProperty() will save the property even if the configuration class doesn't implement INotifyPropertyChanged
            Config<ExampleConfigManual>.SetProperty("Name", "Another modification");
            Config<ExampleConfigManual>.Print();


            // Finally, when the programs exits in a regular way, even the manually created configuration is saved, press any key to exit and save the program
            // IMPORTANT: When the program is exits abrupt the changes are lost, try it by closing the console window with the close button.
            Config<ExampleConfigManual>.Set.Name = "Name that has to be saved";
            Config<ExampleConfigManual>.Print();

            Console.ReadKey();
        }
    }
}
