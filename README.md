# Unpack-Dark-Souls-For-Modding-CSharp
 UDSFM but in C#  
 By Nordgaren
 
This tool uses modified code from [UXM](https://github.com/JKAnderson/UXM) and [Yabber](https://github.com/JKAnderson/Yabber).  
It will unpack the Dark Souls PTDE folder selected by the user and copy over the data files in the directory the EXE is run from.  

### Instructions
1) Put EXE in Dark Souls PTDE folder
2) Watch text scroll until it says completed

I made this so that it could be used in my [PTDE Mod Installer](https://github.com/Nordgaren/PTDE-Mod-Installer)

If you're using this to patch a non-steam EXE, please get in contact with me so I can add the checksum.  

### Thank You

**[HotPocketRemix](https://github.com/HotPocketRemix)** for making the original UDSFM and helping me understand how to patch the EXE properly.  
**[TKGP](https://github.com/JKAnderson)** for making SoulsFormats, UXM, and Yabber.  
**[thefifthmatt](https://github.com/thefifthmatt)** for suggesting how to make the unapacker faster.  

### Patch Notes  
## V 1.1  
* Fixed patching only the exe and added logger
## V 1  
* Initial release
* Functionally identical to UDSFM + can patch non-steam EXEs
