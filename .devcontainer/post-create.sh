# --------------------------------------------------------------
# install dotnet-sdks
# --------------------------------------------------------------
# Note: Use channels because other approaches didnt seem to register
# https://learn.microsoft.com/en-us/dotnet/core/install/linux-scripted-manual#scripted-install
wget -O dotnet-install.sh https://dot.net/v1/dotnet-install.sh 
sudo chmod +x ./dotnet-install.sh
# ./dotnet-install.sh --channel 2.1
# ./dotnet-install.sh --channel 3.0
# ./dotnet-install.sh --channel 5.0
# ./dotnet-install.sh --channel 6.0
rm ./dotnet-install.sh