RemoteUpdate uses the [Powershell Module PSWindowsUpdate](https://www.powershellgallery.com/packages/PSWindowsUpdate) to install Windows Updates on Remote Hosts without the need of scheduled jobs (like described [here](http://woshub.com/pswindowsupdate-module/))

<p align="center">
  <img alt="RemoteUpdate in action" src="https://raw.githubusercontent.com/aimaat/RemoteUpdate/master/RemoteUpdate.png">
</p>

It is meant for small environments where no SCCM or other solutions are existent or bearable.
As default it is not possible to install Updates via Remote Powershell, therefore the tool uses a little workaround with a Powershell VirtualAccount.

# Requirements:
* Windows Server 2012 or newer
* Powershell 5
* .net Framework 4.5
* ICMP Echo allowed on the remote hosts
* Default Firewall Rule "Windows Remote Management (HTTP-In)" enabled on the remote hosts
* Administrative credentials on the remote hosts
* Internet Access to download PSWindowsUpdate or PSWindowsUpdate already installed on the remote hosts

# How To:
* Add the DNS Name of the server you want to update
* Choose between the options:
* * Do you want to accept all available updates or choose by hand which one should be installed
* * Do You want driver updates installed/shown
* * Do you want an automatic reboot after the installation
* * Do you want to see the Powershell GUI or just let it work in the background (currently not working)
* * Do you want to get an email report
* Set your credentials. If you are in a domain and your user has admin rights you don't need this.
* Press Start

If you have a high amount of servers and want to start all at the same time, enable them with the last checkbox and press "Start All"
