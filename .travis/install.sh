#!/bin/bash

if [[ $TRAVIS_OS_NAME == 'osx' ]]; then
    # Install .NET Core on OS X
    brew update
    brew install openssl
    mkdir -p /usr/local/lib
    ln -s /usr/local/opt/openssl/lib/libcrypto.1.0.0.dylib /usr/local/lib/
    ln -s /usr/local/opt/openssl/lib/libssl.1.0.0.dylib /usr/local/lib/
    wget -O - https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.sh | bash -s -- -i ./dotnetcli/
else
    # Install .NET Core on Linux
    sudo apt-get -qq update
    sudo add-apt-repository ppa:ubuntu-toolchain-r/test -y
    sudo apt-get install -y libunwind8
    wget -O - https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.sh | bash -s -- -i ./dotnetcli/
fi