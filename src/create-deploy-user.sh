#!/usr/bin/env bash
#file to prepare remote host for docker image of LinksBot deployment
sudo mkdir -p /home/deploy/.ssh
sudo touch /home/deploy/.ssh/authorized_keys
sudo useradd -d /home/deploy deploy
sudo chown -R deploy:deploy /home/deploy/
sudo chown root:root /home/deploy
sudo chmod 700 /home/deploy/.ssh
sudo chmod 644 /home/deploy/.ssh/authorized_keys
echo 'deploy  ALL=(ALL:ALL) NOPASSWD:/usr/bin/docker pull papersaltserver/itlinksbot\:latest,/usr/bin/docker rm -f oci-linksbot, /usr/bin/docker run -d --restart unless-stopped -v /home/ubuntu/net5.0/db/\:/app/db --env-file /home/ubuntu/net5.0/env.list  --name oci-linksbot papersaltserver/itlinksbot\:latest' | sudo tee --append /etc/sudoers

#put public key in authorized_keys
#sudo vim /home/deploy/.ssh/authorized_keys

#put bot key into env file
#LINKSBOT_BotApiKey=<API_KEY>
#set log level to appropriate level in env file
#LINKSBOT_Serilog__MinimumLevel=Debug
#vim ~/net5.0/env.list

