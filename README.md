# WindowsRecentFilesFilterer

Removes items from Recent Files based on filter criteria specified in the config file. 
* Stays in system tray. Has option to run at system startup. 
* Using the default settings, currently it only filters the Recent Files folder
  * Can filter by shortcut name or the shortcuts target. 
* Any folder (except symlinks) can be filtered by specifying it in the config. 
  * Folders are watched so newly added items are filtered immediately. 
  * There is still a periodic timer to run the entire filterset. You can also manually run the filters.
* The Filter matching syntax used is the VB.Net LikeOperator.
  * Allows wildcards, digits, charlist, and any character. 
  * Simplier than Regex, but not as powerful. 
  * More info: https://docs.microsoft.com/en-us/dotnet/visual-basic/language-reference/operators/like-operator
* In the future I plan to add support for filtering and modifying JumpLists. But there's a lot of work to do there yet. 
  
If there is not config file, the program will load an embedded config file that by default filters torrents and searches.
Right click the system tray icon to create a config file that you can edit and customize. 

![Running without a config file.](https://i.imgur.com/j863UVA.png)

You can reload the configuration after making chages.

![You can reload the configuration after making chages.](https://i.imgur.com/MvAAjaU.png)

The config file is an xml file. If you make a mistake and break the program for some reason, the program will offer to create a new config file.

![Generate new config](https://i.imgur.com/Uz3S1rr.png)
