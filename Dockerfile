FROM mono:6.6.0

# Install .NET Core
RUN apt-get update && \
    apt-get -y install wget && \
    wget -q https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    apt-get -y install apt-transport-https && \
    apt-get update && \
    apt-get -y install dotnet-sdk-2.2