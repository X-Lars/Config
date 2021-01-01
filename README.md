# Custom App.config manager

Provides a way to store custom classes into the the App.config file.
If your custom class implements the INotifyPropertyChanged interface changes made to properties are automatically saved to the App.config file, else the custom class is saved when the program exits. Supports most CLR types and enums, could be easily extended if you wish.

### Examples
###### Creates and gets a reference to MyConfig class in the App.config
<code>var myCFG = Config&lt;MyConfig&gt;.Create();</code>
###### If the MyConfig class implements the INotifyPropertyChanged interface, changed properties are automatically saved to the App.config
`myCFG.myProperty = "New Property Value";`
###### If the MyConfig class doesn't implement the INotifyPropertyChanged interface, following line will save the MyConfig to the App.config
<code>Config&lt;MyConfig&gt;.Save();</code>
###### Gets a reference to an existing MyConfig from the App.config or creates a new one if none exist.
<code>var myCFG = Config&lt;MyConfig&gt;.Get();</code>
###### Sets a single property value of an existing MyConfig or creates a new one with the property set, all other values will be initialized with the types associated default values.
<code>Config&lt;MyConfig&gt;.Get().MyProperty = 3;</code>
###### You can also create a class first and store it in the App.config afterwards.
<code>var myCFG = new MyConfig();</code></br>
<code>myCFG.MyProperty = MyEnum.EnumValue;</code></br>
<code>myCFG.Anotherone = 3;</code></br>
<code>Config&lt;MyConfig&gt;.Save(myCFG);</code>
###### Setting a single property by name will store the changes even if the INotifyPropertyChanged interface is not implemented.
<code>Config&lt;MyConfig&gt;.Set("PropertyName", "PropertyValue");</code>

If the program closes normally, the configuration is automatically saved and you don't have to call the Save() method. But just to be sure, use the Save() method after you made your changes, if the program closes in an abnormal way your changes are lost.

The Config.cs file contains all functional code, just copy and paste in your own project if you like.
