#Installation
1. *XP only:* install Microsoft .NET Framework 2.0 if it is not installed already 
2. save *SpeedDialPatch.cs* and ``CompileSpeedDialPatch.bat`` into the folder of *launcher.exe*
3. open *SpeedDialPatch.cs* in a text editor and set ``MAX_X_COUNT``, ``DIAL_WIDTH`` and ``DIAL_HEIGHT`` to the desired values. Set ``UsePreinstalledSpeedDials`` to ``false`` when you does not like predefines SD images or you set really small SD previews
4. run ``CompileSpeedDialPatch.bat`` (*warning CS0162* is not a problem)

Run ``PatchSpeedDial.exe`` every time after Opera has updated itself. It will patch always the latest build. If something went wrong,  just rename the latest *opera.pak.backup.timestamp* file to *opera.pak* and start over patching.

