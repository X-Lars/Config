using Config;
using System;

namespace Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create configuration with default values or returns an exising configuration
            var autoConfig = Config<ExampleConfigAuto>.Create();

            // Sets the ExampleConfigAuto.Example enum, the property is automatically saved
            autoConfig.ExampleEnum = ExampleConfigAuto.TestEnum.Test3;

            Console.ReadKey();
        }
    }
}
