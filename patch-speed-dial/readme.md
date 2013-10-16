Patch Opera Developer to support more speed dial columns and smaller/larger thumbnail images, set custom thumbnails and injects custom CSS styles into internal pages.

#Usage
1. exit Opera
2. run **SpeedDialPatch.exe**
3. locate your Opera installation folder containing **launcher.exe**
4. enter speed dial configuration values
5. run Opera again

You have to delete and add again default speed dial entries (like Facebook, YouTube, etc.) if you disable built-in speed dial thumbnail images. 

If you want to use a custom thumbnail for site *www.something.com*, *something.com* or *anything.something.com*, create *something.com.png* image file with the desired resolution in the *sdimages* folder. Using a lot of custom thumbnail may slow down Speed Dial.

Because this tool modifies the **opera.pak** file (and in newer builds **opera.exe** also), you must run the tool after each Opera update.

###Opera 15, 16 and 17
Not supported.

###Opera 18
*Opera 18.0.1284.5* is supported. I will try to support all future *Opera Developer* builds until built-in Speed Dial customization will be supported. *Opera Next* and *Opera Stable* builds will **not** be supported (I am using developer stream builds only and binary patching takes a lot of time). Because this tool has to patch *opera.exe* also, you have to wait for me to update the tool after each Opera Developer update if heuristics fails.

#Requirements
1. *XP only:* install Microsoft .NET Framework 2.0 if it is not installed already 

###Changes on 2013-10-16
* Updated for *Opera 18.0.1284.5*
* Dropped support for Opera 15, 16 and 17
* Added custom speed dial thumbnails

###Changes on 2013-09-29
* Updated for *Opera 18.0.1274.0.8*
* Added heuristics to for patching unknown versions
* Added program icon
* Code refactoring, sources transformed into a VS2010 project

###Changes in 1.7.0 (2013-09-23)
* Updated for *Opera 18.0.1271.0*

###Changes in 1.6.0 (2013-09-19)
* Updated for *Opera 18.0.1267.0*

###Changes in 1.5.0 (2013-09-16)
* Updated for *Opera 18.0.1264.0*

###Changes in 1.4.0 (2013-09-15)
* Updated for *Opera 18.0.1258.1* (thanks to [Izer0](http://my.opera.com/nanit76/about/) for the executable patch)
* Patching *opera.exe* if necessary

###Changes in 1.3.0 (2013-08-22)
* Updated for *Opera 15.0.1147.153*, *Opera 16.0.1196.55* and *Opera 17.0.1232.0*
* Stricter Opera version checking
* Added CSS injection with some sample scripts

###Changes in version 1.2.0 (2013-08-13)
* Updated for Opera 16.0.1196.41 (third resource layout)

###Changes in version 1.1.0 (2013-08-08)
* Updated for Opera 17.0.1224.1 (second resource layout)

###Thanks
Thanks to [Izer0](http://my.opera.com/nanit76/about/) for the patches, to [dAKirby309](http://www.iconarchive.com/artist/dakirby309.html) and to [Klukeart](http://www.iconarchive.com/artist/klukeart.html) for the icons.