trap [Management.Automation.CommandNotFoundException] {
  Write-Error 'Docker cannot be found. Make sure it is installed and added to the path.'
  Start-Process -FilePath 'https://docs.docker.com/docker-for-windows/install/'
  continue;
}

docker build -t nunit-console-build .
docker run --rm -it -v ${PSScriptRoot}:/nunit-console -w=/nunit-console nunit-console-build bash build.sh $args