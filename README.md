RemoteUpdate uses the [Powershell Module PSWindowsUpdate](https://www.powershellgallery.com/packages/PSWindowsUpdate/2.1.1.2) to deploy Windows Updates on Remote Hosts without the need of scheduled jobs (like described [here](http://woshub.com/pswindowsupdate-module/))

<p align="center">
  <img alt="RemoteUpdate in action" src="https://raw.githubusercontent.com/aimaat/RemoteUpdate/master/RemoteUpdate.png">
</p>

# Requirements:
* Windows Server 2012 or newer
* Powershell 5
* Default Firewall Rule "Windows Remote Management (HTTP-In)" enabled on the remote hosts
* Administrative credentials on the remote hosts
* Internet Access to download PSWindowsUpdate or PSWindowsUpdate already installed on the remote hosts

# How To:
