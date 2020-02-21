RemoteUpdate uses the Powershell Module <a href="https://www.powershellgallery.com/packages/PSWindowsUpdate" target="_blank">PSWindowsUpdate</a> to install Windows Updates on Remote Hosts without the need of scheduled jobs (like described <a href="http://woshub.com/pswindowsupdate-module/" target="_blank">here</a>)

<p align="center">
  <img alt="RemoteUpdate in action" src="https://raw.githubusercontent.com/aimaat/RemoteUpdate/master/RemoteUpdate.png">
</p>

It is meant for small environments where no SCCM or other solutions are existent or bearable.
As default it is not possible to install Updates via Remote Powershell, therefore the tool uses a little workaround with a Powershell VirtualAccount.

# Requirements:
* Windows Server 2012 or newer
* Powershell
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
* Save your settings for next month (2 xml files will be created in the same directory, one for the servers and one for the general settings)
* Press Start

If you have a high amount of servers and want to start all at the same time, enable them with the last checkbox and press "Start All"<br>
For each server you selected or clicked start a powershell windows will open and ask you which updates should be installed or show you the progress of the installation directly (if you checked AcceptAll)

# FAQ
* Are the credentials i saved safe? I tried my best but can not guarantee for anything. The credentials are encrypted and should only be readable on the host and from the user who created the save. So you should be safe except someone knows your host dns name and your Windows SID.
* Is it safe to use in a productive environment? Please decide for yourself after you tested it in in your lab
* But why? Because i can or tried at least
* Can you code? Not really but i did my best
* Do you want feedback or feature requests? I would highly appreciate it and i'm going to try my best to develop it further
* How can i contact you? via <a href="mailto:info@aima.at?subject=RemoteUpdate">Mail</a> or via <a href="https://www.linkedin.com/in/markus-aigner-388022104/" target="_blank">LinkedIn</a>
