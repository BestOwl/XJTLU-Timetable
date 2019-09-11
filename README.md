# XJTLU Timetable
![license](https://img.shields.io/github/license/mashape/apistatus.svg?style=flat-square)

Automatically export your timetable from XJTLU portal to Exchange calendar.


<a href='//www.microsoft.com/store/apps/9N1KKT6GL9FH?cid=storebadge&ocid=badge'><img src='https://assets.windowsphone.com/85864462-9c82-451e-9355-a3d5f874397a/English_get-it-from-MS_InvariantCulture_Default.png' alt='English badge' width='142' height='52' /></a>

<img src='https://github.com/BestOwl/XJTLU-Timetable/blob/master/docs/screenshot-main.png?raw=true' width='444.5' height='355.5' />

# Features
As you can see, there are already a lot of apps have the similar function. Probably you might ask, why reinvent the wheel?

This project is aiming to provide a simple and fully automatical way to export your personal class timetable to calendar, which means once the app is setup, you don't need to open it again.

So the main features are:
- Export your timetable to Exchange calendar so that it can auto sync across devices.
- Set a reminder for you before class begin.
- Auto update your timetable and calendar in background.
- Preview your timetable of this week in a list.


# How does it work?
This app use XJTLU Portal app internal api to access personal timetable data. It's more accurate than accessing data from e-bridge. You can simply think this is another third-party version of Portal app. So if you login your account in this app, the Portal app in your mobile phone need to re-login before you use it since one account can only login in one device. 

# System requirement
- Windows 10 Fall Creator Update (version 1709) or later.

# Futrue Plans
- [ ] More user-friendly UI
- [ ] Android app

# If I want to contribute to this project
### Requirements
- Windows 10 Fall Creator Update (version 1709) or later.
- Visual Studio 2017 or later with UWP workload
### Build
 1. Clone repo <br/>
 `git clone --recursive https://github.com/BestOwl/XJTLU-Timetable.git`
 2. Open `XJTLU-Timetable-UWP.sln` with Visual Studio
 3. Build XJTLU-Timetable-UWP solution

Feel free to contribute some code :) 
