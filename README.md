# wim_hacker v1.0.0.0
*This little tool removes "packages" like Windows Media Center/Player, IE, IIS, Games, etc... so you can lower the size of your install.wim*

## What is it?

I use [NTLite](https://www.ntlite.com/) a lot.  However it doesn't support removals on Server editions, and
on client versions sometimes there's cruft I want to manually remove.

For those times, I've been using an awesome tool: [win6x_registry_tweak](https://github.com/shiitake/win6x_registry_tweak).

Sadly, development of it has stopped, and when removing a lot of packages inside
of a loop, it's really slow as it has to mount/unmount the hive after every set
of packages to be removed.

This is a crude fork to help solve that.  I ripped out all of the "online"
processing as I only want to edit .wim files.  For online purposes, you can use
PowerShell instead!  I also slimmed down the options to the bare minimum,
removing anything that I don't feel is beneficial for this use case.

Now instead of using:

/c ComponentMask

You use:

/f Packages.txt

Where packages.txt is a text file containing the component package masks, one
per line, like:

Microsoft-Hyper-V
HyperV

### Important Disclaimer: I did not write the original code, and I'm NOT a C# dev!

#### Please see the win6x_registry_tweak link above for history!

*wim_hacker.exe /?*  
This will show all available options..  

*wim_hacker.exe /p <MountPath> /l*  
This will list all the packages available in the selected image and write them to a text file in the same directory.

*install_wim_tweaks.exe /p <MountPath> /f Packages.txt*
This will remove the selected components from the selected image.

**Changes made from win6x_registry_tweak**

- No online (/o) mode
- No unhiding, JUST removing (/r is implied)
- Dropped /d option
- Dropped /h option
- /c option replaced with /f option
- No backups are performed, deprecating the /n option.
